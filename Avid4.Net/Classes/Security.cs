using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using NLog;

public class Security
{
    static Logger logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Each device has a name, and IP address and an indication if it is a socket (not a bulb)
    /// </summary>
    internal class Device
    {
        internal string name;
        internal string ipAddress;
        internal bool isSocket;
    }

    /// <summary>
    /// A period for which a Zone should be turned on
    /// </summary>
    public class OnPeriod
    {
        public DateTime StartTime { get; private set; }
        public DateTime StopTime { get; private set; }
        public OnPeriod(
            DateTime startTime,
            DateTime stopTime)
        {
            StartTime = startTime;
            StopTime = stopTime;
        }
    }

    /// <summary>
    /// The schedule for a zone (or the radio) is a sequence of OnPeriods and possible on/off explicit settings
    /// </summary>
    internal class Schedule
    {
        internal bool initiallyOn;
        internal bool initiallyOff;
        internal OnPeriod[] onPeriods;
    }

    /// <summary>
    /// Schedules are a schedule for the radio and for each named lighting zone
    /// </summary>
    internal class Schedules
    {
        internal Schedule radioSchedule;
        internal Dictionary<String, Schedule> zoneSchedules;
    }

    /// <summary>
    /// A profile has an Id (internally allocated), a name and a description. 
    /// It also has a set of schedules which depend on the day of the week, loaded on demand from the XML document
    /// </summary>
    public class Profile
    {
        public int Id { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }

        public Profile(
            int id,
            string name,
            string description)
        {
            Id = id;
            Name = name;
            Description = description;
        }

    }

    static int CurrentSecurityProfileId;
    static Schedules CurrentProfileSchedules;
    static Dictionary<string, IEnumerable<Device>> Zones;
    static Dictionary<string, String> ZoneStates;
    static string RadioState = null;
    static DateTime DateLoaded = DateTime.MinValue;

    public static void Initialize()
    {
        logger.Info("Initialize");
        try
        {
            //  If there is a current profile in the registry, then apply that immediately on starting
            //  This makes the secuity cycling persist acrosss web app restarts and even power failures.
            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Avid");
            var securityProfileId = key.GetValue("SecurityProfile") as string;
            if (securityProfileId != null)
            {
                LoadProfile(Int32.Parse(securityProfileId), true);
            }

        }
        catch (Exception ex)
        {
            logger.Error(ex,"Failed to initialize");
        }
    }

    static Random rnd = new Random();
    static int SplitDuration(int dur) => rnd.Next(dur / 4, dur - dur / 4);
    static bool FiftyFifty() => rnd.Next(100) > 50;

    /// <summary>
    /// Parse a string as an array of OnPeriod values
    /// </summary>
    /// <remarks>
    /// The array will be just one element except when a percentage is specified to randomize the "on" time within the period
    /// </remarks>
    /// <param name="encoding"></param>
    /// <returns></returns>
    static OnPeriod[] ParseOnPeriod(string encoding)
    {
        string range;
        var percentage = 100;
        if (encoding.Last() == '%')
        {
            var enc1 = encoding.Trim('%').Split('@');
            range = enc1[0];
            percentage = Int32.Parse(enc1[1]);
        }
        else
        {
            range = encoding;
        }
        var startStop = range.Split('-');
        var start = DateTime.ParseExact(startStop[0], "HHmm", CultureInfo.InvariantCulture);
        //  The stop time can be missing to indicate remaining on indefinitely
        var stop = startStop[1].Length == 0 ? DateTime.MaxValue : DateTime.ParseExact(startStop[1], "HHmm", CultureInfo.InvariantCulture);

        //  If 100% or the duration is less than 30 mins or there is no stop time, then schedule the entire period
        if (stop == DateTime.MaxValue || percentage >= 100 || (stop - start).TotalMinutes < 30)
        {
            return new OnPeriod[] { new OnPeriod(start, stop) };
        }
        else
        {
            //  Otherwise split the duration into 4 random(ish) chunks and then schedule randomly either the first or last percentage of each chunk.
            var durationMins = (int)((stop - start).TotalMinutes);
            var duration12 = SplitDuration(durationMins);
            var duration34 = durationMins - duration12;
            var duration1 = SplitDuration(duration12);
            var duration2 = duration12 - duration1;
            var duration3 = SplitDuration(duration34);
            var duration4 = duration34 - duration3;
            var startChunk1 = start;
            var startChunk2 = startChunk1.AddMinutes(duration1);
            var startChunk3 = startChunk2.AddMinutes(duration2);
            var startChunk4 = startChunk3.AddMinutes(duration3);
            bool atStart1 = FiftyFifty();
            bool atStart2 = FiftyFifty();
            bool atStart3 = FiftyFifty();
            bool atStart4 = FiftyFifty();
            var onDuration1 = duration1 * percentage / 100;
            var onDuration2 = duration2 * percentage / 100;
            var onDuration3 = duration3 * percentage / 100;
            var onDuration4 = duration4 * percentage / 100;
            var start1 = atStart1 ? startChunk1 : startChunk1.AddMinutes(duration1 - onDuration1);
            var stop1 = atStart1 ? startChunk1.AddMinutes(onDuration1) : startChunk1.AddMinutes(duration1);
            var start2 = atStart2 ? startChunk2 : startChunk2.AddMinutes(duration2 - onDuration2);
            var stop2 = atStart2 ? startChunk2.AddMinutes(onDuration2) : startChunk2.AddMinutes(duration3);
            var start3 = atStart3 ? startChunk3 : startChunk3.AddMinutes(duration3 - onDuration3);
            var stop3 = atStart3 ? startChunk3.AddMinutes(onDuration3) : startChunk3.AddMinutes(duration1);
            var start4 = atStart4 ? startChunk4 : startChunk4.AddMinutes(duration4 - onDuration4);
            var stop4 = atStart4 ? startChunk4.AddMinutes(onDuration4) : startChunk4.AddMinutes(duration1);

            //  Add the four random chunks separately
            return new OnPeriod[] {
                new OnPeriod(start1, stop1),
                new OnPeriod(start2, stop2),
                new OnPeriod(start3, stop3),
                new OnPeriod(start4, stop4) };
    }
}

    /// <summary>
    /// Pare the scedule for a zone, which can be "on", "off", empty or a (comma separated) sequence of OnPeriod encodings
    /// </summary>
    /// <param name="encoding"></param>
    /// <returns></returns>
    static Schedule ParseSchedule(string encoding)
    {
        var initiallyOn = false;
        var initiallyOff = false;
        string[] schedulePeriods;

        if (encoding == "on")
        {
            initiallyOn = true;
            schedulePeriods = new String[0];
        }
        else if (encoding == "off")
        {
            initiallyOff = true;
            schedulePeriods = new String[0];
        }
        else if (encoding == "")
        {
            schedulePeriods = new string[0];
        }
        else
        {
            schedulePeriods = encoding.Split(',');
        }

        return new Schedule
        {
            initiallyOn = initiallyOn,
            initiallyOff = initiallyOff,
            onPeriods = schedulePeriods.SelectMany(sp => ParseOnPeriod(sp)).ToArray()
        };
    }

    /// <summary>
    /// Load the schedule specified by a specific XML element
    /// </summary>
    /// <param name="elSchedule"></param>
    /// <returns></returns>
    static Schedules LoadSchedules(XElement elSchedule)
    {
        var elRadio = elSchedule.Element("Radio");
        var elZones = elSchedule.Elements("Zone");

        var radioSchedule = ParseSchedule(elRadio == null ? "" : elRadio.Attribute("power").Value);
        var zoneSchedules = elZones.ToDictionary(z => z.Attribute("name").Value, z => ParseSchedule(z.Attribute("power").Value));

        return new Schedules {
            radioSchedule = radioSchedule,
            zoneSchedules = zoneSchedules
        };
    }

    /// <summary>
    /// From a set of XML Schedule elements, load the one whose "days" attributes designated it for the current weekday. Otherwise load the last schedule.
    /// </summary>
    /// <param name="elSchedules"></param>
    /// <returns></returns>
    static Schedules LoadSchedulesForToday(
        IEnumerable<XElement> elSchedules)
    {
        var weekday = DateTime.Now.ToString("ddd");
        foreach (var elSchedule in elSchedules)
        {
            var atDays = elSchedule.Attribute("days");
            if (atDays != null && atDays.Value.Contains(weekday))
            {
                return LoadSchedules(elSchedule);
            }
        }
        return LoadSchedules(elSchedules.Last());
    }

    /// <summary>
    /// Load the zones from the XML element into a Dictionary to a set of Device objects
    /// </summary>
    /// <param name="elZones"></param>
    /// <returns></returns>
    static Dictionary<String,IEnumerable<Device>> LoadZones(IEnumerable<XElement> elZones)
    {
        return elZones.ToDictionary(z => z.Attribute("name").Value, z => z.Elements("Device").Select(d => new Device {
            ipAddress = d.Attribute("ip").Value,
            name = d.Attribute("name").Value,
            isSocket = d.Attribute("type").Value == "socket"
        }));
    }

    /// <summary>
    /// Apply any initial on/off settings in the current schedule, recording the current state for each device
    /// </summary>
    static void InitSchedule()
    {
        if (CurrentProfileSchedules.radioSchedule != null)
        {
            if (CurrentProfileSchedules.radioSchedule.initiallyOn || CurrentProfileSchedules.radioSchedule.initiallyOff || CurrentProfileSchedules.radioSchedule.onPeriods.Any())
            {
                Running.ExitAllPrograms();
                DesktopClient.SendSpecialkey("ClearDesktop");
            }

            if (CurrentProfileSchedules.radioSchedule.initiallyOn)
            {
                Receiver.Security();
                RadioState = "on";
            }

            if (CurrentProfileSchedules.radioSchedule.initiallyOff)
            {
                RadioState = "off";
            }
        }

        foreach (var zoneKV in CurrentProfileSchedules.zoneSchedules)
        {
            if (zoneKV.Value.initiallyOn)
            {
                foreach (Device d in Zones[zoneKV.Key])
                {
                    TP_Link.TurnOn(d.ipAddress, d.name, d.isSocket);
                }
                ZoneStates[zoneKV.Key] = "on";
            }
            if (zoneKV.Value.initiallyOff)
            {
                foreach (Device d in Zones[zoneKV.Key])
                {
                    TP_Link.TurnOff(d.ipAddress, d.name, d.isSocket);
                }
                ZoneStates[zoneKV.Key] = "off";
            }
        }
    }

    /// <summary>
    /// Load the profile anew from the XML file, selecting the appropriate "current" schedule to be used
    /// </summary>
    /// <param name="id"></param>
    /// <param name="isReload"></param>
    /// <returns></returns>
    public static bool LoadProfile(
        int id,
        XElement profile,
        bool isReload = false)
    {
        CurrentProfileSchedules = LoadSchedulesForToday(profile.Elements("Schedule"));
        CurrentSecurityProfileId = id;
        DateLoaded = DateTime.Now.Date;

        if (!isReload)
        {
            InitSchedule();

            if (profile.Attribute("nonpersistent") == null)
            {
                DesktopClient.PersistStringInRegistry("SecurityProfile", profile.Attribute("id").Value);
            }
        }

        Tick(DateTime.Now);

        logger.Info("Loaded profile {0}", profile.Attribute("name").Value);
        return true;
    }

    /// <summary>
    /// Load the profile anew from the XML file, selecting the appropriate "current" schedule to be used
    /// </summary>
    /// <param name="id"></param>
    /// <param name="isReload"></param>
    /// <returns></returns>
    public static bool LoadProfile(
        int id,
        bool isReload = false)
    {
        logger.Info("Load profile id {0}", id);
        try
        {
            var doc = XDocument.Load(@"C:\Avid.Net\Security.xml");
            Zones = LoadZones(doc.Root.Elements("Zone"));
            ZoneStates = new Dictionary<string, string>();

            return LoadProfile(id, doc.Root.Elements("Profile").ElementAt(id), isReload);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Can't load profile");
            return false;
        }
    }

    /// <summary>
    /// Load the default profile anew from the XML file, selecting the appropriate "current" schedule to be used
    /// </summary>
    /// <param name="isReload"></param>
    /// <returns></returns>
    public static bool LoadDefaultProfile(
        bool isReload = false)
    {
        logger.Info("Load default profile");
        try
        {
            var doc = XDocument.Load(@"C:\Avid.Net\Security.xml");
            Zones = LoadZones(doc.Root.Elements("Zone"));
            ZoneStates = new Dictionary<string, string>();
            var allProfiles = doc.Root.Elements("Profile").ToArray();
            var id = Array.FindIndex(allProfiles, p => p.Attribute("default") != null);

            return LoadProfile(id, allProfiles[id], isReload);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Can't load default profile");
            return false;
        }
    }

    /// <summary>
    /// Get the set of available profiles from the XML file for display and selection
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<Profile> GetProfiles()
    {
        var doc = XDocument.Load(@"C:\Avid.Net\Security.xml");
        int id = 0;
        foreach (var profile in doc.Root.Elements("Profile"))
        {
            yield return new Profile(id++, profile.Attribute("name").Value, profile.Attribute("description").Value);
        }
    }

    /// <summary>
    /// Get the currently loaded schedule for display
    /// </summary>
    /// <returns></returns>
    public static Dictionary<String,IEnumerable<OnPeriod>> GetCurrentSchedule()
    {
        var dict = new Dictionary<String, IEnumerable<OnPeriod>>();
        if (CurrentProfileSchedules != null)
        {
            if (CurrentProfileSchedules.radioSchedule != null && CurrentProfileSchedules.radioSchedule.onPeriods.Length != 0)
            {
                dict.Add("Radio", CurrentProfileSchedules.radioSchedule.onPeriods);
            }
        }

        foreach (var zoneKV in CurrentProfileSchedules.zoneSchedules)
        {
            if (zoneKV.Value.onPeriods.Length != 0)
            {
                dict.Add(zoneKV.Key, zoneKV.Value.onPeriods);

            }
        }

        return dict;
    }

    /// <summary>
    /// Periodically (typically evey minute) check the current time against the schedule to determine if ant lights need switching
    /// </summary>
    /// <param name="when"></param>
    public static void Tick(DateTime when)
    {
        if (CurrentProfileSchedules != null)
        {
            //  If the date has changed, re-load the profile in order to get the new day's schedule
            if (when.Date != DateLoaded)
            {
                LoadProfile(CurrentSecurityProfileId, true);
            }

            if (CurrentProfileSchedules.radioSchedule.onPeriods.Length != 0)
            {
                try
                {
                    var radioOn = TestIfOn(when, CurrentProfileSchedules.radioSchedule.onPeriods);
                    if (radioOn && RadioState != "on")
                    {
                        Receiver.Security();
                        RadioState = "on";
                    }
                    else if (!radioOn && RadioState != "off")
                    {
                        Receiver.TurnOff();
                        RadioState = "off";
                    }

                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Can't schedule Radio");
                }
            }

            foreach (var zoneKV in CurrentProfileSchedules.zoneSchedules)
            {
                if (zoneKV.Value.onPeriods.Length != 0)
                {
                    try
                    {
                        var zoneOn = TestIfOn(when, zoneKV.Value.onPeriods);
                        if (zoneOn && (!ZoneStates.ContainsKey(zoneKV.Key) || ZoneStates[zoneKV.Key] != "on"))
                        {
                            foreach (Device d in Zones[zoneKV.Key])
                            {
                                TP_Link.TurnOn(d.ipAddress, d.name, d.isSocket);
                            }
                            ZoneStates[zoneKV.Key] = "on";
                        }
                        if (!zoneOn && (!ZoneStates.ContainsKey(zoneKV.Key) || ZoneStates[zoneKV.Key] != "off"))
                        {
                            foreach (Device d in Zones[zoneKV.Key])
                            {
                                TP_Link.TurnOff(d.ipAddress, d.name, d.isSocket);
                            }
                            ZoneStates[zoneKV.Key] = "off";
                        }

                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Can't schedule Zone {0}", zoneKV.Key);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Is the specified time within any of the OnPeriods?
    /// </summary>
    /// <param name="when"></param>
    /// <param name="onPeriods"></param>
    /// <returns></returns>
    static bool TestIfOn(DateTime when, OnPeriod[] onPeriods)
    {
        foreach (var onPeriod in onPeriods)
        {
            if (when.TimeOfDay >= onPeriod.StartTime.TimeOfDay && (when.Date < onPeriod.StopTime.Date || when.TimeOfDay < onPeriod.StopTime.TimeOfDay))
            {
                return true;
            }
        }
        return false;
    }
}
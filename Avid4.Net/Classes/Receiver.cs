using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Web;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text;
using NLog;

/// <summary>
/// Class to control and query a Yamaha dual zone AV Receiver
/// </summary>
public static class Receiver
{
    static Logger logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// The HTTP Url through which the receiver is accessed
    /// </summary>
    static string Url = null;

    /// <summary>
    /// Post an HTTP request to the Yamaha AV Receiver, expecting an XML response, which is returned
    /// </summary>
    /// <param name="body"></param>
    /// <returns></returns>
    static XDocument GetXml(
        string body)
    {
        if (Url == null)
        {
            //  If the ReceiverAddress is not configured, we do nothing.
            //  This allows testing on a development machine on the same LAN as the media PC and Receiver
            if (Config.ReceiverAddress != null)
            {
                Url = "http://" + Config.ReceiverAddress + "/YamahaRemoteControl/ctrl";
            }
            else
            {
                return null;
            }
        }

        Uri requestUri = new Uri(Url);

        HttpWebRequest request =
            (HttpWebRequest)HttpWebRequest.Create(requestUri);
        request.Method = WebRequestMethods.Http.Post;
        StreamWriter requestWriter = new StreamWriter(request.GetRequestStream(), Encoding.UTF8);
        requestWriter.Write(body);
        requestWriter.Close();

        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        XDocument xDoc =
            XDocument.Load(new StreamReader(response.GetResponseStream()));

        return xDoc;
    }

    /// <summary>
    /// The currently set Receiver volume for the current zone
    /// </summary>
    static int volumeLevel;

    /// <summary>
    /// True if the current zone's volume is muted
    /// </summary>
    static bool volumeMute = false;

    /// <summary>
    /// True if the currently selected xone is the "main" (digital) zone
    /// </summary>
    static bool mainZone;

    /// <summary>
    /// True if either Receiver zone is switched on
    /// </summary>
    static bool switchedOn;

    /// <summary>
    /// The (digital) input selector for the main zone
    /// </summary>
    /// <remarks>
    /// HDMI2 is the Media PC; HDMI3 is the Sky box
    /// </remarks>
    static string MainZoneInput { get; set; }

    /// <summary>
    /// What is the name for the current zone to be used communicating with the Yamaha Receiver
    /// </summary>
    static string ZoneName { get { return mainZone ? "Main_Zone" : "Zone_2"; } }

    /// <summary>
    /// The currently selected input: "Sky" or "Computer"
    /// </summary>
    public static string SelectedInput { get; private set; }

    /// <summary>
    /// The currently selected output: "TV" (main zone) or "Rooms" (zone 2)
    /// </summary>
    public static string SelectedOutput { get; private set; }

    static string StraightOutputMode = "Straight";

    public static readonly string[] AvailableOutputModes = new string[] {
        StraightOutputMode,
        "Surround Decoder",
        "Standard",
        "Drama",
        "Mono Movie",
//         "Adventure",
//         "Sci-Fi",
//         "Spectacle",
//         "Cellar Club",
//         "Chamber",
//         "The Bottom Line",
//         "The Roxy Theatre",
//         "Hall in Munich",
//         "Hall in Vienna",
//         "2ch Stereo",
        "7ch Stereo",
    };

    public static string DefaultOutputMode = "Surround Decoder";

    /// <summary>
    /// Selected mode from a set of preferred sound programs for the Yamaha output
    /// </summary>
    public static string SelectedOutputMode { get; private set; }

    /// <summary>
    /// A formatted string for displaying the current volume level
    /// </summary>
    public static string VolumeDisplay { get { return !switchedOn ? "Off" : String.Format(volumeMute ? "({0}%)" : "{0}%", volumeLevel); } }

    /// <summary>
    /// Initialize by querying the current receiver state
    /// </summary>
    /// <remarks>
    /// This allows the Avid software to restart and retain the current settings
    /// </remarks>
    public static void Initialize()
    {
        XDocument state = GetXml(String.Format(
            "<YAMAHA_AV cmd=\"GET\"><{0}><Basic_Status>GetParam</Basic_Status></{0}></YAMAHA_AV>",
            "Main_Zone"));

        if (state != null)
        {
            var basicStatus = state.Element("YAMAHA_AV").Element("Main_Zone").Element("Basic_Status");
            string powerString = basicStatus.Element("Power_Control").Element("Power").Value;

            switchedOn = powerString == "On";

            MainZoneInput = basicStatus.Element("Input").Element("Input_Sel").Value;
        }

        //  If the digital input is HDMI3, the input is from Sky.
        if (switchedOn && MainZoneInput == "HDMI3")
        {
            SelectSkyInput();
        }
        else
        {
            SelectComputerInput();
        }
    }

    /// <summary>
    /// Set the current state values from the Receiver
    /// </summary>
    public static void GetState()
    {
        if (switchedOn)
        {
            XDocument state = GetXml(String.Format(
                "<YAMAHA_AV cmd=\"GET\"><{0}><Basic_Status>GetParam</Basic_Status></{0}></YAMAHA_AV>",
                ZoneName));

            if (state == null)
            {
                return;
            }

            var basicStatus = state.Element("YAMAHA_AV").Element(ZoneName).Element("Basic_Status");
            string volumeString = basicStatus.Element("Volume").Element("Lvl").Element("Val").Value;

            volumeLevel = (Convert.ToInt32(volumeString) + 850) / 10;
            //volumeMute = basicStatus.Element("Volume").Element("Mute").Value == "On";

            //  Get the audio program program - note that "Straight" mode is encoded separately
            if (mainZone)
            {
                var audioProgram = basicStatus.Element("Surround").Element("Program_Sel").Element("Current");
                if (audioProgram.Element("Straight").Value == "On")
                {
                    SelectedOutputMode = StraightOutputMode;
                }
                else
                {
                    SelectedOutputMode = audioProgram.Element("Sound_Program").Value;
                }
            }

            //  Set the selected digital input and muting state to what we think it should be, 
            //  as it may have been changed under our feet by HDMI-CEC action from the screen through the receiver
            //  This mechanism may now be unnecessary if HDMI-CEC is not enabled in the receiver
            if (mainZone)
            {
                string currentlySelectedInput = basicStatus.Element("Input").Element("Input_Sel").Value;
                if (string.IsNullOrEmpty(MainZoneInput))
                {
                    MainZoneInput = currentlySelectedInput;
                }
                if (MainZoneInput != currentlySelectedInput)
                {

                    logger.Warn("GetState input is unexpectedly {0}", currentlySelectedInput);
                    if (SelectedInput == "Sky")
                    {
                        SelectSkyInput();
                    }
                    else
                    {
                        SelectComputerInput();
                    }
                    SetMute(volumeMute);
                }
            }
        }
    }

    /// <summary>
    /// Set the Receiver to take digital input from the Media PC (HDMI2) and analog input from the "matched" input (AV6)
    /// </summary>
    public static void SelectComputerInput()
    {
        logger.Info("SelectComputerInput");
        SelectedInput = "Computer";
        MainZoneInput = "HDMI2";
        GetXml("<YAMAHA_AV cmd=\"PUT\"><Main_Zone><Input><Input_Sel>HDMI2</Input_Sel></Input></Main_Zone></YAMAHA_AV>");
        GetXml("<YAMAHA_AV cmd=\"PUT\"><Zone_2><Input><Input_Sel>AV6</Input_Sel></Input></Zone_2></YAMAHA_AV>");
    }

    /// <summary>
    /// Set the Receiver to take digital input from the Sky box (HDMI3) and analog input from the "matched" input (AV5)
    /// </summary>
    public static void SelectSkyInput()
    {
        logger.Info("SelectSkyInput");
        SelectedInput = "Sky";
        MainZoneInput = "HDMI3";
        GetXml("<YAMAHA_AV cmd=\"PUT\"><Main_Zone><Input><Input_Sel>HDMI3</Input_Sel></Input></Main_Zone></YAMAHA_AV>");
        GetXml("<YAMAHA_AV cmd=\"PUT\"><Zone_2><Input><Input_Sel>AV5</Input_Sel></Input></Zone_2></YAMAHA_AV>");
    }

    /// <summary>
    /// Ensure the Receiver actually has the digital input we currently expect it to have
    /// </summary>
    public static void ReselectInput()
    {
        if (!string.IsNullOrEmpty(MainZoneInput))
        {
            GetXml("<YAMAHA_AV cmd=\"PUT\"><Main_Zone><Input><Input_Sel>" + MainZoneInput + "</Input_Sel></Input></Main_Zone></YAMAHA_AV>");
        }
    }

    /// <summary>
    /// Set the receiver to output the digital input on the main "TV" (5.1) speakers
    /// </summary>
    /// <param name="menuIndex"></param>
    /// <param name="unmute"></param>
    public static void SelectTVOutput(string selectedMode = null, bool unmute = true)
    {
        logger.Info("SelectTVOutput {0}", selectedMode ?? DefaultOutputMode);
       if (!switchedOn || SelectedOutput != "TV")
        {
            MainZoneOn();
            ReselectInput();
            Zone2Off();
        }

        SelectedOutput = "TV";
        SelectedOutputMode = selectedMode ?? DefaultOutputMode;
        mainZone = true;
        switchedOn = true;

        //  Set the Yamaha audio program - note that "Straight" mode is encoded separately
        if (SelectedOutputMode == StraightOutputMode)
        {
            GetXml("<YAMAHA_AV cmd=\"PUT\"><Main_Zone><Surround><Program_Sel><Current><Straight>On</Straight></Current></Program_Sel></Surround></Main_Zone></YAMAHA_AV>");
        }
        else
        {
            GetXml("<YAMAHA_AV cmd=\"PUT\"><Main_Zone><Surround><Program_Sel><Current><Straight>Off</Straight><Sound_Program>" + SelectedOutputMode + "</Sound_Program></Current></Program_Sel></Surround></Main_Zone></YAMAHA_AV>");
        }

        //  Unmute if requested
        if (volumeMute && unmute)
        {
            ToggleMute();
        }

        //  Update the current state
        GetState();
    }

    /// <summary>
    /// Set the receiver to output the analog input on the zone 2 "Rooms" (stereo) speakers
    /// </summary>
    public static void SelectRoomsOutput()
    {
        logger.Info("SelectRoomsOutput");
        if (!switchedOn || SelectedOutput != "Rooms")
        {
            Zone2On();
            MainZoneOff();
        }

        SelectedOutput = "Rooms";
        SelectedOutputMode = DefaultOutputMode;
        mainZone = false;
        switchedOn = true;

        //  Unmute
        if (volumeMute)
        {
            ToggleMute();
        }

        //  Update the current state
        GetState();
    }

    /// <summary>
    /// Turn off both zones
    /// </summary>
    public static void TurnOff()
    {
        MainZoneOff();
        Zone2Off();

        switchedOn = false;
    }

    /// <summary>
    /// Increase the volume on the current zone
    /// </summary>
    public static void IncreaseVolume()
    {
        volumeLevel++;
        SetVolume();
    }

    /// <summary>
    /// Decrease the volume on the current zone
    /// </summary>
    public static void DecreaseVolume()
    {
        volumeLevel--;
        SetVolume();
    }

    /// <summary>
    /// Set the Receiver volume on the current zone to that in volumeLevel
    /// </summary>
    static void SetVolume()
    {
        //  Scale the volumeLevel to the Yamaha encoding
        ReselectInput();
        GetXml(String.Format(
            "<YAMAHA_AV cmd=\"PUT\"><{0}><Volume><Lvl><Val>{1}</Val><Exp>1</Exp><Unit>dB</Unit></Lvl></Volume></{0}></YAMAHA_AV>",
            ZoneName,
            volumeLevel * 10 - 850));
    }

    /// <summary>
    /// Toggle muting on the current zone
    /// </summary>
    public static void ToggleMute()
    {
        SetMute(!volumeMute);
    }

    /// <summary>
    /// Set or unset muting on the current zone
    /// </summary>
    /// <param name="muted"></param>
    public static void SetMute(
        bool muted)
    {
        ReselectInput();
        volumeMute = muted;
        GetXml(String.Format(
            "<YAMAHA_AV cmd=\"PUT\"><{0}><Volume><Mute>{1}</Mute></Volume></{0}></YAMAHA_AV>",
            ZoneName,
            volumeMute ? "On" : "Off"));
    }

    /// <summary>
    /// Turn the main zone (digital) off
    /// </summary>
    static void MainZoneOff()
    {
        GetXml("<YAMAHA_AV cmd=\"PUT\"><Main_Zone><Power_Control><Power>Standby</Power></Power_Control></Main_Zone></YAMAHA_AV>");
    }

    /// <summary>
    /// Turn the zone 2 (analog) off
    /// </summary>
    static void Zone2Off()
    {
        GetXml("<YAMAHA_AV cmd=\"PUT\"><Zone_2><Power_Control><Power>Standby</Power></Power_Control></Zone_2></YAMAHA_AV>");
    }

    /// <summary>
    /// Turn the main zone (digital) on
    /// </summary>
    static void MainZoneOn()
    {
        GetXml("<YAMAHA_AV cmd=\"PUT\"><Main_Zone><Power_Control><Power>On</Power></Power_Control></Main_Zone></YAMAHA_AV>");
    }

    /// <summary>
    /// Turn the zone 2 (analog) on, setting the input to the appropriate "matched" analog input
    /// </summary>
    static void Zone2On()
    {
        GetXml("<YAMAHA_AV cmd=\"PUT\"><Zone_2><Power_Control><Power>On</Power></Power_Control></Zone_2></YAMAHA_AV>");
        GetXml("<YAMAHA_AV cmd=\"PUT\"><Zone_2><Input><Input_Sel>" + ((SelectedInput == "Computer") ? "AV6" : "AV5") + "</Input_Sel></Input></Zone_2></YAMAHA_AV>");
    }

    /// <summary>
    /// Is the receiver switched on?
    /// </summary>
    /// <returns></returns>
    public static bool IsOn()
    {
        return switchedOn;
    }
}
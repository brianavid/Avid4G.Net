using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using IniParser;
using System.Globalization;
using System.IO;

/// <summary>
/// A class to represent the set of TS recorded TV files
/// </summary>
public class TsRecordings
{
    public class Recording 
    {
        public String Id { get; private set; }
        public String Title { get; private set; }
        public String Description { get; private set; }
        public String Channel { get; private set; }
        public DateTime StartTime { get; private set; }
        public String EpisodeTitle { get; private set; }
        public String Filename { get; private set; }
        public TimeSpan Duration { get; private set; }
        public DateTime StopTime { get { return StartTime + Duration; } }

        public Recording(
            String contentFilename)
        {
            var iniFileName = Path.ChangeExtension(contentFilename, ".txt");
            var iniParser = new FileIniDataParser();
            var data = iniParser.ReadFile(iniFileName);

            Channel = data["Media"]["Channel"];
            Id = data["0"]["Id"];
            Title = data["0"]["Title"];
            Description = data["0"]["Info"];
            StartTime = DateTime.ParseExact(
                data["0"]["Date"] + " " + data["0"]["Time"], 
                new[] { "dd.MM.yyyy HH:mm:ss" }, 
                CultureInfo.InvariantCulture, DateTimeStyles.None);
            Duration = TimeSpan.Parse( data["0"]["Duration"]);
            EpisodeTitle = null;
            Filename = contentFilename;
        }
    }

    /// <summary>
    /// All recordings, keyed by Id
    /// </summary>
    static public Dictionary<String, Recording> AllRecordings { get; private set; }

    /// <summary>
    /// All recordings, most recent first
    /// </summary>
    static public IEnumerable<Recording> AllRecordingsInReverseTimeOrder
    {
        get { return AllRecordings.Values.OrderByDescending(r => r.StartTime); }
    }

    /// <summary>
    /// All recordings sharing the same title, most recent first
    /// </summary>
    /// <param name="title"></param>
    /// <returns></returns>
    static public IEnumerable<Recording> AllRecordingsForTitle(
        string title)
    {
        return AllRecordingsInReverseTimeOrder.Where(r => r.Title == title);
    }

    /// <summary>
    /// All recordings as a collection of Lists, grouped with those sharing a title in the same List
    /// </summary>
    static public IEnumerable<List<Recording>> AllRecordingsGroupedByTitle
    {
        get
        {
            Dictionary<string, List<Recording>> recordingsForTitle = new Dictionary<string, List<Recording>>();

            foreach (Recording r in AllRecordingsInReverseTimeOrder)
            {
                if (!recordingsForTitle.ContainsKey(r.Title))
                {
                    recordingsForTitle[r.Title] = new List<Recording>();
                }
                recordingsForTitle[r.Title].Add(r);
            }

            return recordingsForTitle.Values;
        }
    }

    /// <summary>
    /// Load the collection of recordings from DVBViewer
    /// </summary>
    public static void LoadAllRecordings()
    {
        AllRecordings = new Dictionary<String, Recording>();

        try
        {
            var tsFiles = System.IO.Directory.GetFiles(Config.VideoPath, "*.ts");
            if (tsFiles.Length != 0)
            {
                foreach (var file in tsFiles)
                {
                    Recording r = new Recording(file);
                    AllRecordings[r.Id] = r;
                }
            }
        }
        catch (Exception)
        {

        }
    }

    /// <summary>
    /// Delete the file containing an particular recording
    /// </summary>
    /// <param name="programmeId"></param>
    /// <returns></returns>
    public static void DeleteRecording(
        Recording recording)
    {
        string contentFilename = recording.Filename;
        if (System.IO.File.Exists(contentFilename))
        {
            foreach (var file in System.IO.Directory.GetFiles(Config.VideoPath, Path.GetFileNameWithoutExtension(contentFilename) + ".*"))
            {
                System.IO.File.Delete(file);
            }
            LoadAllRecordings();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Web;

/// <summary>
/// Summary description for ScheduledRecordings
/// </summary>
public class ScheduledRecordings
{
	public ScheduledRecordings(
        XDocument recordingsBlob)
    {
        requests = new Dictionary<string, XElement>();
        recordings = new Dictionary<string, XElement>();
        programmes = new Dictionary<string, XElement>();

        foreach (var request in recordingsBlob.Element("RPRecordingsBlob").Element("RPRequests").Elements("RPRequest"))
        {
            string id = request.Element("ID").Value;
            requests[id] = request;
        }

        foreach (var recording in recordingsBlob.Element("RPRecordingsBlob").Element("RPRecordings").Elements("RPRecording"))
        {
            string id = recording.Element("Id").Value;
            recordings[id] = recording;
        }

        foreach (var programme in recordingsBlob.Element("RPRecordingsBlob").Element("TVProgrammes").Elements("TVProgramme"))
        {
            string id = programme.Element("Id").Value;
            programmes[id] = programme;
        }
    }

    Dictionary<string, XElement> requests;
    Dictionary<string, XElement> recordings;
    Dictionary<string, XElement> programmes;

    public IEnumerable<XElement> Recordings { get { return recordings.Values; } }

    public XElement GetRequest(XElement recording)
    {
        return requests[recording.Element("RPRequestID").Value];
    }

    public XElement GetProgramme(XElement recording)
    {
        return programmes[recording.Element("TVProgrammeID").Value];
    }

    public bool IsScheduled(string programmeId)
    {
        return programmes.ContainsKey(programmeId);
    }
}
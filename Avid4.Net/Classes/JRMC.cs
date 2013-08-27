using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

[Serializable]
public class TrackData
{
    public TrackData(Dictionary<string, string> _info)
    {
        info = _info;
    }
    Dictionary<string, string> info;
    public Dictionary<string, string> Info { get { return info; } }

    public static string GetArtistName(AlbumData album)
    {
        var track0 = album.Track0.Info;
        var artist = track0.ContainsKey("Album Artist") ? track0["Album Artist"] : track0.ContainsKey("Artist") ? track0["Artist"] : string.Empty;

        foreach (var track in album.Tracks)
        {
            if (track.Info.ContainsKey("Album Artist"))
            {
                artist = track.Info["Album Artist"];
                break;
            }

            string trackArtist = track.Info.ContainsKey("Artist") ? track.Info["Artist"] : string.Empty;
            if (trackArtist != artist)
            {
                return "Various Artists";
            }
        }

        if (artist.StartsWith("The "))
        {
            artist = artist.Substring(4);
        }

        return artist;
    }

    public static string GetAlbumName(AlbumData album)
    {
        var track0 = album.Track0.Info;
        return track0.ContainsKey("Album") ? track0["Album"] : string.Empty;
    }
};

[Serializable]
public class AlbumData 
{
    public AlbumData(string _albumId, TrackData[] _tracks)
    {
        albumId = _albumId;
        tracks = _tracks;
    }

    string albumId;
    TrackData[] tracks;
    public TrackData[] Tracks { get { return tracks; } }
    public TrackData Track0 { get { return tracks[0]; } }
    public string AlbumId { get { return albumId; } }
};

[Serializable]
public class AlbumCollection
{
    Dictionary<string, AlbumData> albums = new Dictionary<string, AlbumData>();

    public IEnumerable<string> Keys { get { return albums.Keys.ToArray(); } }

    public int Count { get { return albums.Count; } }

    public IEnumerable<AlbumData> InArtistOrder
    {
        get
        {
            AlbumData[] sortedAlbums = albums.Values.ToArray();
            Array.Sort(sortedAlbums, (a1, a2) => string.Compare(TrackData.GetArtistName(a1), TrackData.GetArtistName(a2)));
            return sortedAlbums;
        }
    }

    public IEnumerable<AlbumData> InAlbumOrder
    {
        get
        {
            AlbumData[] sortedAlbums = albums.Values.ToArray();
            Array.Sort(sortedAlbums, (a1, a2) => string.Compare(TrackData.GetAlbumName(a1), TrackData.GetAlbumName(a2)));
            return sortedAlbums;
        }
    }

    public AlbumData GetById(
        string albumId)
    {
        return albums[albumId];
    }

    public AlbumData GetByIndex(
        int index)
    {
        return albums.Values.ToArray()[index];
    }

    public void Add(
        string albumId,
        AlbumData album)
    {
        albums[albumId] = album;
    }
}

/// <summary>
/// Summary description for JRMC
/// </summary>
[Serializable]
public class JRMC
{
    private const string RequiredTrackData = "Name,Track,Album,Artist,Genre,Composer,Duration,Album Artist,Filename";

    static string host = null;
    public static string Host 
    {
        get {
            if (host == null)
            {
                string ipAddr = Config.IpAddress;
                if (ipAddr != null)
                {
                    host = "http://" + ipAddr + ":52199/";
                }
            }
            if (host == null)
            {
                IPAddress[] addresses = Dns.GetHostAddresses(Dns.GetHostName());
                foreach (var addr in addresses)
                {
                    if (addr.AddressFamily == AddressFamily.InterNetwork)
                    {
                        host = "http://" + addr.ToString() + ":52199/";
                    }
                }
            }
            return host;
        }
    }

    public static string Url
    {
        get { return Host + "MCWS/v1/"; }
    }

    public static void OutputHost(System.Web.HttpResponse Response)
    {
        Response.Write(Host);
    }

    public static void OutputUrl(System.Web.HttpResponse Response)
    {
        Response.Write(Url);
    }

    static public XDocument GetXml(
        string url)
    {
        Uri requestUri = new Uri(url);

        for (int i = 0; i < 5; i++)
        {
            HttpWebRequest request =
                (HttpWebRequest)HttpWebRequest.Create(requestUri);
            request.Method = WebRequestMethods.Http.Get;
            request.ContentType = "text/xml";
            XDocument xDoc = null;

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            try
            {
                xDoc = XDocument.Load(new StreamReader(response.GetResponseStream()));

                return xDoc;
            }
            catch (System.Exception ex)
            {
                System.Threading.Thread.Sleep(2000);
            }
        }

        return null;
    }

    static Dictionary<string, string> GetNameValues(
        XElement parent,
        string childElementName)
    {
        return parent.Elements(childElementName).ToDictionary(elem => elem.Attribute("Name").Value, elem => elem.Value);
    }

    public static Dictionary<string, string> GetChildren(
        string itemId)
    {
        var x = GetXml(Url + "Browse/Children?ID=" + itemId);

        var dict = GetNameValues(x.Root, "Item");
        if (dict != null && dict.Count != 0)
        {
            var firstKey = dict.Keys.First();
            if (firstKey.StartsWith("All ") && firstKey.EndsWith(")"))
            {
                dict.Remove(firstKey);
            }
        }
        return dict;
    }

    static Dictionary<string, string> GetFields(
        XElement file)
    {
        return GetNameValues(file, "Field");
    }

    public static TrackData[] GetTracks(
        string itemId)
    {
        var x = GetXml(Url + "Browse/Files?Fields=" + RequiredTrackData + "&ID=" + itemId);

        return (from item in x.Root.Elements("Item") select (new TrackData(GetFields(item)))).ToArray();
    }

    public static Dictionary<string, string>[] GetQueue()
    {
        var x = GetXml(Url + "Playback/Playlist");

        return (from item in x.Root.Elements("Item") select GetFields(item)).ToArray();
    }

    public static Dictionary<string, string>[] GetPlayLists()
    {
        var x = GetXml(Url + "Playlists/List");

        return (from item in x.Root.Elements("Item") select GetFields(item)).ToArray();
    }

    public static bool IsLetters(Dictionary<string, string> children)
    {
        if (children.Count < 8)
        {
            return false;
        }

        string[] keys = children.Keys.ToArray();
        for (int i = 1; i < children.Count - 1; i++)
        {
            if (keys[i].Length != 1)
            {
                return false;
            }
        }

        return true;
    }

    public static XElement GetPlaybackInfo()
    {
        var x = GetXml(Url + "Playback/Info");
        if (x != null)
        {
            var trackId = x.Root.DescendantsAndSelf("Item").Where(el => el.Attribute("Name").Value == "FileKey").First().Value;
            var track = JRMC.GetTrackByTrackId(trackId);
            if (track != null)
            {
                if (track.Info.ContainsKey("Composer") && track.Info.ContainsKey("Genre"))
                {
                    var genre = track.Info["Genre"];
                    var composer = track.Info["Composer"];
                    var name = track.Info["Name"];
                    if (genre == "Classical" && composer != "" && !name.StartsWith(composer, StringComparison.InvariantCultureIgnoreCase))
                    {
                        x.Root.Add(new XElement("Item", new XAttribute("Name", "ClassicalComposer"), composer + ": "));
                    }
                }
            }
            return x.Root;
        }

        return null;
    }

    public static void SendCommand(
        string command)
    {
        GetXml(Url + command);
    }

    public static int GetDisplayMode()
    {
        var x = GetXml(Url + "UserInterface/Info");
        return Convert.ToInt32(x.Root.Elements("Item").Where(item => item.Attribute("Name").Value == "Mode").First().Value)+1;
    }

    static JRMC theJRMC = null;
    AlbumCollection albumList = null;
    AlbumCollection photoAlbumList = null;

    static AlbumCollection AlbumList
    {
        get { return theJRMC == null ? null : theJRMC.albumList; }
    }

    public static AlbumCollection PhotoAlbumList
    {
        get { return theJRMC == null ? null : theJRMC.photoAlbumList; }
    }

    static SortedDictionary<string, AlbumCollection> albumsByComposerId = null;
    static SortedDictionary<string, AlbumCollection> albumsByArtistId = null;
    static SortedDictionary<string, AlbumCollection> albumsByInitialLetter = null;
    static SortedDictionary<string, SortedDictionary<string, string>> artistsByInitialLetter = null;
    static SortedDictionary<string, string> artistIdByName = null;
    static SortedDictionary<string, string> composerIdByName = null;
    static Dictionary<string, AlbumData> albumByTrackId = null;
    static Dictionary<string, TrackData> trackByTrackId = null;

    public static AlbumCollection LoadAndIndexAllAlbums(string[] itemIds, bool refresh)
    {
        const string CachePath = @"C:\Avid.Net\Avid4.Net\JRMC Cache.bin";

        if (refresh && File.Exists(CachePath))
        {
            File.Delete(CachePath);
        }

        if (File.Exists(CachePath))
        {
            IFormatter formatter = new BinaryFormatter();
            using (FileStream s = File.OpenRead(CachePath))
            {
                theJRMC = (JRMC)formatter.Deserialize(s);
            }
        }
        else
        {
            if (theJRMC == null)
            {
                theJRMC = new JRMC();
            }

            try
            {
                theJRMC.albumList = new AlbumCollection();
                theJRMC.photoAlbumList = new AlbumCollection();

                foreach (var itemId in itemIds)
                {
                    FetchAllAlbums(itemId, AlbumList, PhotoAlbumList);
                }
            }
            catch
            {
                theJRMC.albumList = null;
            }

            if (AlbumList != null)
            {
                IFormatter formatter = new BinaryFormatter();
                using (FileStream s = File.Create(CachePath))
                {
                    formatter.Serialize(s, theJRMC);
                }
            }
        }

        if (AlbumList != null)
        {
            BuildIndexesByTrackId();
            BuildIndexByComposer();
            BuildIndexByArtist();
        }

        return AlbumList;
    }

    public static IEnumerable<string> GetAllComposers()
    {
        return composerIdByName.Keys;
    }

    public static string GetIdForComposer(
        string composerName)
    {
        if (!composerIdByName.ContainsKey(composerName)) return null;
        return composerIdByName[composerName];
    }

    public static AlbumCollection GetAlbumsForComposerId(
        string composerId)
    {
        if (!albumsByComposerId.ContainsKey(composerId)) return null;
        return albumsByComposerId[composerId];
    }

    public static string GetIdForArtist(
        string artistName)
    {
        if (!artistIdByName.ContainsKey(artistName)) return null;
        return artistIdByName[artistName];
    }

    public static AlbumCollection GetAlbumsForArtistId(
        string artistId)
    {
        if (!albumsByArtistId.ContainsKey(artistId)) return null;
        return albumsByArtistId[artistId];
    }

    public static IEnumerable<AlbumData> GetAlbumsByTrackId(
        string trackId)
    {
        AlbumData album = albumByTrackId.ContainsKey(trackId) ? albumByTrackId[trackId] : null;
        return album == null ? null : new List<AlbumData> { album };
    }

    public static IEnumerable<string> GetAlbumInitialLetters()
    {
        return albumsByInitialLetter.Keys;
    }

    public static IEnumerable<string> GetArtistInitialLetters()
    {
        return artistsByInitialLetter.Keys;
    }

    public static IEnumerable<string> GetArtistsByInitialLetter(
        string initial)
    {
        return artistsByInitialLetter.ContainsKey(initial) ? artistsByInitialLetter[initial].Keys : null;
    }

    public static AlbumCollection GetAlbumsByInitialLetter(
        string initial)
    {
        return albumsByInitialLetter.ContainsKey(initial) ? albumsByInitialLetter[initial] : null;
    }


    public static IEnumerable<AlbumData> GetLuckyDipAlbums()
    {
        List<AlbumData> luckyDip = new List<AlbumData>();

        Random rand = new Random();
        int count = AlbumList.Count;

        for (int i = 0; i < 20; i++)
        {
            int index = rand.Next(count);
            var album = AlbumList.GetByIndex(index);
            luckyDip.Add(album);
        }

        return luckyDip;
    }


    public static TrackData[] GetTracksByAlbumId(
        string albumId)
    {
        AlbumData album = AlbumList.GetById(albumId);
        return album == null ? null : album.Tracks;
    }



    public static string GetAlbumIdByTrackId(
        string trackId)
    {
        AlbumData album = albumByTrackId.ContainsKey(trackId) ? albumByTrackId[trackId] : null;
        return album == null ? null : album.AlbumId;
    }


    public static TrackData GetTrackByTrackId(
        string trackId)
    {
        return trackByTrackId.ContainsKey(trackId) ? trackByTrackId[trackId] : null;
    }



    public static TrackData[] SearchTracks(
        string searchText)
    {
        List<TrackData> tracks = new List<TrackData>();
        foreach (var album in AlbumList.InAlbumOrder)
        {
            foreach (TrackData track in album.Tracks)
            {
                if (track.Info["Name"].IndexOf(searchText, StringComparison.CurrentCultureIgnoreCase) >= 0)
                {
                    tracks.Add(track);
                    if (tracks.Count >= 200)
                    {
                        break;
                    }
                }
            }
        }

        TrackData[] result = tracks.ToArray();
        Array.Sort(result, (t1, t2) => string.Compare(t1.Info["Name"], t2.Info["Name"]));
        return result;
    }


    public static int SearchCount(
        string searchText)
    {
        int count = 0;
        foreach (TrackData track in trackByTrackId.Values)
        {
            if (track.Info["Name"].IndexOf(searchText, StringComparison.CurrentCultureIgnoreCase) >= 0)
            {
                count++;
            }
        }

        return count;
    }



    static void FetchAllAlbums(string itemiD, AlbumCollection albumList, AlbumCollection photoAlbumList)
    {
        var childIds = GetChildren(itemiD);

        if (childIds != null && childIds.Count != 0)
        {
            if (Convert.ToInt32(itemiD) >= 1000)
            {
                foreach (var childName in childIds.Keys)
                {
                    var childId = childIds[childName];
                    FetchAllAlbums(childId, albumList, photoAlbumList);
                }
            }
            else if (childIds.ContainsKey("Artist"))
            {
                FetchAllAlbums(childIds["Artist"], albumList, photoAlbumList);
            }
            else if (childIds.ContainsKey("Album"))
            {
                FetchAllAlbums(childIds["Album"], albumList, photoAlbumList);
            }
        }
        else
        {
            var tracks = GetTracks(itemiD);

            if (tracks != null && tracks.Length != 0)
            {
                string albumId = tracks[0].Info["Key"];

                if (tracks[0].Info["Filename"].Contains(@"\Photos\"))
                {
                    if (!photoAlbumList.Keys.Contains(albumId))
                    {
                        var album = new AlbumData(albumId, tracks);
                        photoAlbumList.Add(albumId, album);
                    }
                }
                else
                {
                    if (!albumList.Keys.Contains(albumId))
                    {
                        var album = new AlbumData(albumId, tracks);
                        albumList.Add(albumId, album);
                    }
                }
            }
        }
    }

    private static void BuildIndexesByTrackId()
    {
        albumByTrackId = new Dictionary<string, AlbumData>();
        trackByTrackId = new Dictionary<string, TrackData>();
        foreach (var album in AlbumList.InAlbumOrder)
        {
            foreach (var track in album.Tracks)
            {
                string trackId = track.Info["Key"];
                albumByTrackId[trackId] = album;
                trackByTrackId[trackId] = track;
            }
        }
    }

    private static void BuildIndexByComposer()
    {
        albumsByComposerId = new SortedDictionary<string, AlbumCollection>();
        composerIdByName = new SortedDictionary<string, string>();

        foreach (var albumId in AlbumList.Keys)
        {
            var album = AlbumList.GetById(albumId);
            if (IsClassicalAlbum(album))
            {
                foreach (var track in album.Tracks)
                {
                    var trackInfo = track.Info;
                    if (trackInfo.ContainsKey("Composer"))
                    {
                        var composer = trackInfo["Composer"];
                        string composerId;
                        if (!composerIdByName.ContainsKey(composer))
                        {
                            composerId = trackInfo["Key"];
                            albumsByComposerId[composerId] = new AlbumCollection();
                            composerIdByName[composer] = composerId;
                        }
                        else
                        {
                            composerId = composerIdByName[composer];
                        }

                        albumsByComposerId[composerId].Add(albumId, album);
                    }
                }
            }
        }
    }

    public static bool IsClassicalAlbum(
        AlbumData album)
    {
        var trackInfo = album.Track0.Info;
        return trackInfo["Filename"].ToLower().Contains(@"\classical\");
    }

    public static string GetAlbumComposers(
        AlbumData album)
    {
        var albumComposers = new List<string>();
        foreach (var track in album.Tracks)
        {
            var trackInfo = track.Info;
            if (trackInfo.ContainsKey("Composer"))
            {
                var composer = trackInfo["Composer"];
                if (!albumComposers.Contains(composer))
                {
                    albumComposers.Add(composer);
                }
            }
        }

        string result = "";
        if (albumComposers.Count <= 3)
        {
            foreach (var composer in albumComposers)
            {
                if (!String.IsNullOrEmpty(result))
                {
                    result += ", ";
                }
                result += composer;
            }
        }
        else
        {
            result = albumComposers[0] + ", " + albumComposers[1] + ", ...";
        }

        return result;
    }

    private static void BuildIndexByArtist()
    {
        albumsByArtistId = new SortedDictionary<string, AlbumCollection>();
        albumsByInitialLetter = new SortedDictionary<string, AlbumCollection>();
        artistsByInitialLetter = new SortedDictionary<string, SortedDictionary<string, string>>();
        artistIdByName = new SortedDictionary<string, string>();

        foreach (var albumId in AlbumList.Keys)
        {
            var album = AlbumList.GetById(albumId);
            if (!IsClassicalAlbum(album))
            {
                var track0 = album.Track0.Info;
                var artistName = TrackData.GetArtistName(album);
                var albumName = track0.ContainsKey("Album") ? track0["Album"] : "?";

                if (!string.IsNullOrEmpty(artistName) && !string.IsNullOrEmpty(albumName))
                {
                    var initialArtistLetter = artistName.Substring(0, 1);
                    if (!artistsByInitialLetter.ContainsKey(initialArtistLetter))
                    {
                        artistsByInitialLetter[initialArtistLetter] = new SortedDictionary<string,string>();
                    }
                    artistsByInitialLetter[initialArtistLetter][artistName] = artistName;

                    var initialAlbumLetter = albumName.Substring(0, 1);
                    if (!albumsByInitialLetter.ContainsKey(initialAlbumLetter))
                    {
                        albumsByInitialLetter[initialAlbumLetter] = new AlbumCollection();
                    }
                    albumsByInitialLetter[initialAlbumLetter].Add(albumId, album);

                    string artistId;
                    if (!artistIdByName.ContainsKey(artistName))
                    {
                        artistId = track0["Key"];
                        albumsByArtistId[artistId] = new AlbumCollection();
                        artistIdByName[artistName] = artistId;
                    }
                    else
                    {
                        artistId = artistIdByName[artistName];
                    }

                    albumsByArtistId[artistId].Add(albumId, album);
                }
            }
        }
    }

    public static string FormatDuration(string rawDuration)
    {
        int seconds = (int)float.Parse(rawDuration);
        return seconds < 0 ? "<0:00" : string.Format("{0}:{1:00}", seconds / 60, seconds % 60);
    }

}
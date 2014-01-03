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

/// <summary>
/// The JRMC class encapsulates all access to the J River Media Center player which is used for 
/// cataloging and playing all stored music.
/// </summary>
[Serializable]
public class JRMC
{
    /// <summary>
    /// Represents a particular track as a Dictionary of name=value pairs for the supported subset of available track properties
    /// </summary>
    [Serializable]
    public class TrackData
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="_info">The Dictionary of name=value pairs for the track - all the track info</param>
        public TrackData(
            Dictionary<string, string> _info)
        {
            Info = _info;
        }

        /// <summary>
        /// The properties of the track as a string-keyed Dictionary
        /// </summary>
        public Dictionary<string, string> Info { get; private set; } 
    };

    /// <summary>
    /// Represents an album as an array of tracks in index order
    /// </summary>
    [Serializable]
    public class AlbumData
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="_albumId">The ID of the album as a whole</param>
        /// <param name="_tracks">The array of tracks</param>
        public AlbumData(
            string _albumId, 
            TrackData[] _tracks)
        {
            AlbumId = _albumId;
            Tracks = _tracks;
        }

        /// <summary>
        /// The ID of the album as a whole
        /// </summary>
        public string AlbumId { get; private set; }

        /// <summary>
        /// The array of tracks
        /// </summary>
        /// <remarks>This will never be empty</remarks>
        public TrackData[] Tracks { get; private set; }

        /// <summary>
        /// The first track (which will always exist)
        /// </summary>
        public TrackData Track0 { get { return Tracks[0]; } }

        /// <summary>
        /// Get the Artist name for the album - not the artist for any particular track. 
        /// This can be the "Album Artist" (if provided) 
        /// or "Various Artists" if the tracks have different "Artist" values
        /// or the single "Artist" value common across all tracks
        /// </summary>
        /// <remarks>Any definitive article "The" is stripped off</remarks>
        /// <returns></returns>
        public string GetArtistName()
        {
            var track0 = Track0.Info;
            var artist = track0.ContainsKey("Album Artist") ? track0["Album Artist"] : track0.ContainsKey("Artist") ? track0["Artist"] : string.Empty;

            foreach (var track in Tracks)
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

        /// <summary>
        /// Get the name of the Album, which can be determined from the first track alone.
        /// </summary>
        /// <returns></returns>
        public string GetAlbumName()
        {
            var track0 = Track0.Info;
            return track0.ContainsKey("Album") ? track0["Album"] : string.Empty;
        }

        /// <summary>
        /// Get the date at which the album was imported
        /// </summary>
        public DateTime GetAlbumDateImported()
        {
            var track0 = Track0.Info;
            DateTime dateImported = track0.ContainsKey("Date Imported") ? new DateTime(Convert.ToInt64(track0["Date Imported"])) : DateTime.MinValue;
            return dateImported;
        }
    };

    /// <summary>
    /// Represents a collection of album, selected in some manner
    /// </summary>
    [Serializable]
    public class AlbumCollection
    {
        //  The collection of albums keyed by AlbumId
        Dictionary<string, AlbumData> albums = new Dictionary<string, AlbumData>();

        /// <summary>
        /// The array of AlbumId keys for all the albums in the collection
        /// </summary>
        public IEnumerable<string> Keys { get { return albums.Keys.ToArray(); } }

        /// <summary>
        /// The count of all albums in the collection
        /// </summary>
        public int Count { get { return albums.Count; } }

        /// <summary>
        /// Get all the albums in the collection sorted by album artist name
        /// </summary>
        public IEnumerable<AlbumData> InArtistOrder
        {
            get
            {
                AlbumData[] sortedAlbums = albums.Values.ToArray();
                Array.Sort(sortedAlbums, (a1, a2) => string.Compare(a1.GetArtistName(), a2.GetArtistName()));
                return sortedAlbums;
            }
        }

        /// <summary>
        /// Get all the albums in the collection sorted by album name
        /// </summary>
        public IEnumerable<AlbumData> InAlbumOrder
        {
            get
            {
                AlbumData[] sortedAlbums = albums.Values.ToArray();
                Array.Sort(sortedAlbums, (a1, a2) => string.Compare(a1.GetAlbumName(), a2.GetAlbumName()));
                return sortedAlbums;
            }
        }

        /// <summary>
        /// Get all the albums in the collection sorted by the date in which the album was added to JRMC (most recent first)
        /// </summary>
        public IEnumerable<AlbumData> MostRecentFirst
        {
            get
            {
                AlbumData[] sortedAlbums = albums.Values.ToArray();
                Array.Sort(sortedAlbums, (a1, a2) => DateTime.Compare(a2.GetAlbumDateImported(), a1.GetAlbumDateImported()));
                return sortedAlbums;
            }
        }

        /// <summary>
        /// Get an album in the collection by its AlbumId
        /// </summary>
        /// <param name="albumId"></param>
        /// <returns></returns>
        public AlbumData GetById(
            string albumId)
        {
            return albums[albumId];
        }

        /// <summary>
        /// Get an album in the collection by its numeric index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public AlbumData GetByIndex(
            int index)
        {
            return albums.Values.ToArray()[index];
        }

        /// <summary>
        /// Add an album to the collection
        /// </summary>
        /// <param name="albumId"></param>
        /// <param name="album"></param>
        public void Add(
            string albumId,
            AlbumData album)
        {
            albums[albumId] = album;
        }
    }

    /// <summary>
    /// The subset of available JRMC fields included in the TrackData info name=value Dictionary and used within Avid
    /// </summary>
    private const string RequiredTrackData = "Name,Track,Album,Artist,Genre,Composer,Duration,Album Artist,Filename,Date Imported";

    /// <summary>
    /// The real Host address of the JRMC web service on the local computer (not 127.0.0.1 which is unreliable)
    /// </summary>
    /// <remarks>This is determined only once</remarks>
    public static string Host 
    {
        get {
            if (host == null)
            {
                //  Try the IP address from the config (if provided)
                string ipAddr = Config.IpAddress;
                if (ipAddr != null)
                {
                    host = "http://" + ipAddr + ":52199/";
                }
            }
            if (host == null)
            {
                //  Try a local address
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
    static string host = null;

    /// <summary>
    /// The URL of the JRMC web service on the local computer
    /// </summary>
    public static string Url
    {
        get { return Host + "MCWS/v1/"; }
    }

    /// <summary>
    /// Execute a JRMC web service call, returning any result as XML
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Send a JRMC web service command, not expecting and XML returned
    /// </summary>
    /// <param name="command"></param>
    public static void SendCommand(
        string command)
    {
        GetXml(Url + command);
    }

    //  Stored data to be returned as the current track when playing a stream
    static string currentStreamingName = null;
    static string currentStreamingStation = null;
    static DateTime currentStreamingStartTime = DateTime.MinValue;

    /// <summary>
    /// Set stored values to be return as the currently queued playing track info for a streamed URL
    /// </summary>
    /// <param name="name"></param>
    /// <param name="station"></param>
    /// <param name="startTime"></param>
    public static void SetStreaming(
        string name,
        string station,
        DateTime startTime)
    {
        if (currentStreamingName != null)
        {
            GetXml(Url + "Playback/ClearPlaylist");
        }
        currentStreamingName = name;
        currentStreamingStation = station;
        currentStreamingStartTime = startTime;
    }

    /// <summary>
    /// Are we currently playing a URL stream and not a stored track?
    /// </summary>
    /// <returns></returns>
    public static bool IsStreaming()
    {
        return currentStreamingName != null;
    }

    /// <summary>
    /// Note that we are no longer playing a URL stream
    /// </summary>
    public static void ClearStreaming()
    {
        if (currentStreamingName != null)
        {
            currentStreamingName = null;
            currentStreamingStation = null;
            currentStreamingStartTime = DateTime.MinValue;
            GetXml(Url + "Playback/ClearPlaylist");
        }
    }

    /// <summary>
    /// Get the array of track info for the currently queued playing tracks
    /// </summary>
    /// <returns></returns>
    public static Dictionary<string, string>[] GetQueue()
    {
        //  In the special case of streaming from a URL, we will have stored the values to return
        if (IsStreaming())
        {
            return new Dictionary<string,string>[] {
                new Dictionary<string,string> {
                    {"Name", currentStreamingName },
                    {"Artist", currentStreamingStation },
                    {"Album", currentStreamingStartTime.ToString("ddd HH:mm") }
                }
            };
        }

        var x = GetXml(Url + "Playback/Playlist");

        return (from item in x.Root.Elements("Item") select GetFields(item)).ToArray();
    }

    /// <summary>
    /// Remove a particular identified track from the queue of those playing or to play
    /// </summary>
    /// <param name="id"></param>
    public static void RemoveQueuedTrack(
        string id)
    {
        Dictionary<string, string>[] queue = GetQueue();
        int index = 0;

        foreach (Dictionary<string, string> queuedTrack in queue)
        {
            if (queuedTrack["Key"] == id)
            {
                break;
            }
            index++;
        }

        //  Remove by numerical index within the queue
        GetXml(Url + "Playback/EditPlaylist?Action=Remove&Source=" + index.ToString());
    }

    /// <summary>
    /// Get an array of playlists known to JRMC, each playlist represented by a dictionary of its name=value properties
    /// </summary>
    /// <returns></returns>
    public static Dictionary<string, string>[] GetPlayLists()
    {
        var x = GetXml(Url + "Playlists/List");

        return (from item in x.Root.Elements("Item") select GetFields(item)).ToArray();
    }

    /// <summary>
    /// Return as XML, information about the curently playing track
    /// </summary>
    /// <returns></returns>
    public static XElement GetPlaybackInfo()
    {
        var x = GetXml(Url + "Playback/Info");
        if (x != null)
        {
            var trackId = x.Root.DescendantsAndSelf("Item").Where(el => el.Attribute("Name").Value == "FileKey").First().Value;
            var track = JRMC.GetTrackByTrackId(trackId);
            if (track != null)
            {
                //  Add an additional "ClassicalComposer" if that does not duplicate information in the track name
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

            //  Fake up some data when plating a streamed URL
            if (currentStreamingName != null)
            {
                SetItemValueInInfo( x.Root, "Name", currentStreamingName);
                SetItemValueInInfo( x.Root, "Artist", currentStreamingStation);
                SetItemValueInInfo(x.Root, "Album", currentStreamingStartTime.ToString("ddd HH:mm"));
                SetItemValueInInfo(x.Root, "ImageURL", "/Music/GetListenAgainIcon");
            }
            return x.Root;
        }

        return null;
    }

    /// <summary>
    /// Add or replace a track property value in the XML to be returned
    /// </summary>
    /// <param name="root"></param>
    /// <param name="name"></param>
    /// <param name="value"></param>
    static void SetItemValueInInfo(
        XElement root,
        string name,
        string value)
    {
        List<XElement> items = root.Elements("Item").Where(e => e.Attribute("Name").Value == name).ToList();
        if (items.Count > 0)
        {
            items.First().Value = value;
        }
        else
        {
            root.Add(new XElement("Item", new XAttribute("Name", name), value));
        }
    }

    /// <summary>
    /// What display mode is JRMC in currently?
    /// </summary>
    /// <returns>The numeric code of the display mode : 2 is displaying visualization</returns>
    public static int GetDisplayMode()
    {
        var x = GetXml(Url + "UserInterface/Info");
        return Convert.ToInt32(x.Root.Elements("Item").Where(item => item.Attribute("Name").Value == "Mode").First().Value)+1;
    }

    /// <summary>
    /// The single instance collection of loaded album and track information optimized for fast binary serialization
    /// </summary>
    static JRMC theJRMC = null;

    /// <summary>
    /// The internal collection of music albums
    /// </summary>
    AlbumCollection albumList = null;

    /// <summary>
    /// The internal collection of photo albums
    /// </summary>
    AlbumCollection photoAlbumList = null;

    /// <summary>
    /// The collection of music albums
    /// </summary>
    static AlbumCollection AlbumList
    {
        get { return theJRMC == null ? null : theJRMC.albumList; }
    }

    /// <summary>
    /// The collection of photo albums
    /// </summary>
    public static AlbumCollection PhotoAlbumList
    {
        get { return theJRMC == null ? null : theJRMC.photoAlbumList; }
    }

    //  Collections of albums, artists, composers and tracks all sorted and keyed in different ways for efficient access
    static SortedDictionary<string, AlbumCollection> albumsByComposerId = null;
    static SortedDictionary<string, AlbumCollection> albumsByArtistId = null;
    static SortedDictionary<string, AlbumCollection> albumsByInitialLetter = null;
    static SortedDictionary<string, SortedDictionary<string, string>> artistsByInitialLetter = null;
    static SortedDictionary<string, string> artistIdByName = null;
    static SortedDictionary<string, string> composerIdByName = null;
    static Dictionary<string, AlbumData> albumByTrackId = null;
    static Dictionary<string, TrackData> trackByTrackId = null;

    /// <summary>
    /// Main initialization and re-initialization method to load and index all albums from the JRMC web API into
    /// the optimized and indexed storate
    /// </summary>
    /// <param name="itemIds"></param>
    /// <param name="refresh"></param>
    /// <returns></returns>
    public static AlbumCollection LoadAndIndexAllAlbums(
        string[] itemIds, 
        bool refresh)
    {
        //  THe information is normally loaded from a cache in a fixed location
        const string CachePath = @"C:\Avid.Net\Avid4.Net\JRMC Cache.bin";

        //  IF we are explicitly refreshing the cache, delete the old one
        if (refresh && File.Exists(CachePath))
        {
            File.Delete(CachePath);
        }

        //  If we have a cache (which we normally will), use it by simple binary deserialization
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
            //  We are loading all data from JRMC
            if (theJRMC == null)
            {
                theJRMC = new JRMC();
            }

            try
            {
                //  Create and fetch all music and photo albums
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

            //  If we have created an album list, save it to the cache for next time
            if (AlbumList != null)
            {
                IFormatter formatter = new BinaryFormatter();
                using (FileStream s = File.Create(CachePath))
                {
                    formatter.Serialize(s, theJRMC);
                }
            }
        }

        //  Irrespective f how we loaded the album and track information, build all internal index 
        if (AlbumList != null)
        {
            BuildIndexesByTrackId();
            BuildIndexByComposer();
            BuildIndexByArtist();
        }

        return AlbumList;
    }

    /// <summary>
    /// Recursively walk the JRMC tree of items and files to find all music tracks, music albums and photo albums
    /// </summary>
    /// <param name="itemiD"></param>
    /// <param name="albumList"></param>
    /// <param name="photoAlbumList"></param>
    static void FetchAllAlbums(string itemiD, AlbumCollection albumList, AlbumCollection photoAlbumList)
    {
        var childIds = GetChildren(itemiD);

        //  If there are child items, recursively walk the tree
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
            else
            {
                if (childIds.ContainsKey("Artist"))
                {
                    FetchAllAlbums(childIds["Artist"], albumList, photoAlbumList);
                }
                if (childIds.ContainsKey("Album"))
                {
                    FetchAllAlbums(childIds["Album"], albumList, photoAlbumList);
                }
                if (childIds.ContainsKey("Composer"))
                {
                    FetchAllAlbums(childIds["Composer"], albumList, photoAlbumList);
                }
            }
        }
        else
        {
            //  If there are NO child items, the files are music tracks or photos
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

    /// <summary>
    /// Get the "Item" children of a JRMC browseable item ID as Dictionary of name=value pairs
    /// </summary>
    /// <param name="itemId"></param>
    /// <returns></returns>
    static Dictionary<string, string> GetChildren(
        string itemId)
    {
        var x = GetXml(Url + "Browse/Children?ID=" + itemId);

        var dict = GetNameValues(x.Root, "Item");

        //  If the first child is named with "All" and ending in ")", i is implicitly added by JRMC, but unwanted for Avid.
        //  So it must be removed
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

    /// <summary>
    /// Get the tracks of an album as an array of the Dictionary of name=value pairs, each Dictionary representing one track
    /// </summary>
    /// <param name="itemId"></param>
    /// <returns></returns>
    static TrackData[] GetTracks(
        string itemId)
    {
        var x = GetXml(Url + "Browse/Files?Fields=" + RequiredTrackData + "&ID=" + itemId);

        return (from item in x.Root.Elements("Item") select (new TrackData(GetFields(item)))).ToArray();
    }

    /// <summary>
    /// Get the "Field" children of a JRMC browseable item ID (which will be a track) as Dictionary of name=value pairs
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    static Dictionary<string, string> GetFields(
        XElement file)
    {
        return GetNameValues(file, "Field");
    }

    /// <summary>
    /// For any XML element with named child elements with a "Name" attributes, return the children as a Dictionary of name=value pairs
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="childElementName"></param>
    /// <returns></returns>
    static Dictionary<string, string> GetNameValues(
        XElement parent,
        string childElementName)
    {
        return parent.Elements(childElementName).ToDictionary(elem => elem.Attribute("Name").Value, elem => elem.Value);
    }
    
    /// <summary>
    /// For all tracks in all albums, build the albumByTrackId and trackByTrackId indexes
    /// </summary>
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

    /// <summary>
    /// For all tracks in all Classical albums, build the albumsByComposerId and composerIdByName indexes
    /// </summary>
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

    /// <summary>
    /// For all tracks in all albums, build the albumsByArtistId, albumsByInitialLetter, artistsByInitialLetter and artistIdByName indexes
    /// </summary>
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
                var artistName = album.GetArtistName();
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

    /// <summary>
    /// Return the collection of all composer names
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<string> GetAllComposers()
    {
        return composerIdByName.Keys;
    }

    /// <summary>
    /// Get the composer Id for a given name
    /// </summary>
    /// <param name="composerName"></param>
    /// <returns></returns>
    public static string GetIdForComposer(
        string composerName)
    {
        if (!composerIdByName.ContainsKey(composerName)) return null;
        return composerIdByName[composerName];
    }

    /// <summary>
    /// Get the collection of Classical albums on which the composer is represented
    /// </summary>
    /// <param name="composerId"></param>
    /// <returns></returns>
    public static AlbumCollection GetAlbumsForComposerId(
        string composerId)
    {
        if (!albumsByComposerId.ContainsKey(composerId)) return null;
        return albumsByComposerId[composerId];
    }

    /// <summary>
    /// Get the collection of initial letters for all artists
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<string> GetAlbumInitialLetters()
    {
        return albumsByInitialLetter.Keys;
    }

    /// <summary>
    /// Get the collection of initial letters for all albums
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<string> GetArtistInitialLetters()
    {
        return artistsByInitialLetter.Keys;
    }

    /// <summary>
    /// Get the collection of artist names starting with a given initial letter
    /// </summary>
    /// <param name="initial"></param>
    /// <returns></returns>
    public static IEnumerable<string> GetArtistsByInitialLetter(
        string initial)
    {
        return artistsByInitialLetter.ContainsKey(initial) ? artistsByInitialLetter[initial].Keys : null;
    }

    /// <summary>
    /// Get the collection of albums whose names start with a given initial letter
    /// </summary>
    /// <param name="initial"></param>
    /// <returns></returns>
    public static AlbumCollection GetAlbumsByInitialLetter(
        string initial)
    {
        return albumsByInitialLetter.ContainsKey(initial) ? albumsByInitialLetter[initial] : null;
    }

    /// <summary>
    /// Get the ArtistID for a given artist name
    /// </summary>
    /// <param name="artistName"></param>
    /// <returns></returns>
    public static string GetIdForArtist(
        string artistName)
    {
        if (!artistIdByName.ContainsKey(artistName)) return null;
        return artistIdByName[artistName];
    }

    /// <summary>
    /// Get the collection of albums for an artist with thespecified ArtistID
    /// </summary>
    /// <param name="artistId"></param>
    /// <returns></returns>
    public static AlbumCollection GetAlbumsForArtistId(
        string artistId)
    {
        if (!albumsByArtistId.ContainsKey(artistId)) return null;
        return albumsByArtistId[artistId];
    }

    /// <summary>
    /// Get the collection of albums (containing at most one album) containing the identified track
    /// </summary>
    /// <param name="trackId"></param>
    /// <returns></returns>
    public static IEnumerable<AlbumData> GetAlbumsByTrackId(
        string trackId)
    {
        AlbumData album = albumByTrackId.ContainsKey(trackId) ? albumByTrackId[trackId] : null;
        return album == null ? null : new List<AlbumData> { album };
    }

    /// <summary>
    /// Get the AlbumId for the album containing the identified track
    /// </summary>
    /// <param name="trackId"></param>
    /// <returns></returns>
    public static string GetAlbumIdByTrackId(
        string trackId)
    {
        AlbumData album = albumByTrackId.ContainsKey(trackId) ? albumByTrackId[trackId] : null;
        return album == null ? null : album.AlbumId;
    }

    /// <summary>
    /// Get a randon collection of 20 albums
    /// </summary>
    /// <returns></returns>
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

    /// <summary>
    /// Get the 20 most recently added albums
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<AlbumData> GetRecentAlbums()
    {
        return AlbumList.MostRecentFirst.Take(20);
    }

    /// <summary>
    /// Get the collection of tracks which comprise an album
    /// </summary>
    /// <param name="albumId"></param>
    /// <returns></returns>
    public static TrackData[] GetTracksByAlbumId(
        string albumId)
    {
        AlbumData album = AlbumList.GetById(albumId);
        return album == null ? null : album.Tracks;
    }

    /// <summary>
    /// Get the track data for the identified track
    /// </summary>
    /// <param name="trackId"></param>
    /// <returns></returns>
    public static TrackData GetTrackByTrackId(
        string trackId)
    {
        return trackByTrackId.ContainsKey(trackId) ? trackByTrackId[trackId] : null;
    }

    /// <summary>
    /// Search for all tracks (up to 200) whose name contains the specified text
    /// </summary>
    /// <param name="searchText"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Count the number of tracks whose name contains the specified text
    /// </summary>
    /// <param name="searchText"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Is the album a Classical album
    /// </summary>
    /// <param name="album"></param>
    /// <returns></returns>
    public static bool IsClassicalAlbum(
        AlbumData album)
    {
        var trackInfo = album.Track0.Info;
        return trackInfo["Filename"].ToLower().Contains(@"\classical\");
    }

    /// <summary>
    /// Get a formated string of the one or more composers represented on the album
    /// </summary>
    /// <param name="album"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Format a track's duration for display
    /// </summary>
    /// <param name="rawDuration"></param>
    /// <returns></returns>
    public static string FormatDuration(string rawDuration)
    {
        int seconds = (int)float.Parse(rawDuration);
        return seconds < 0 ? "<0:00" : string.Format("{0}:{1:00}", seconds / 60, seconds % 60);
    }

}

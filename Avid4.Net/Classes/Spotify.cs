using Avid.Spotify;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net.Http;
using SpotifyAPI.SpotifyWebAPI;
using SpotifyAPI.SpotifyWebAPI.Models;
using NLog;
using Microsoft.Win32;
using System.Net;
using System.Net.Cache;
using System.IO;
using Newtonsoft.Json;
using System.Xml.Linq;

/// <summary>
/// Class of static methods to access the Spotify player through its WebAPI interface
/// </summary>
public static class Spotify
{
    static Logger logger = LogManager.GetCurrentClassLogger();

    static HttpClient trayAppClient = new HttpClient();
    static SpotifyWebAPIClass webAppService = null;
    static DateTime webApiExpiry = DateTime.Now;
    static string webApiCurrentUserId = null;

    static Dictionary<String, FullArtist> artistCache = new Dictionary<String, FullArtist>();
    static Dictionary<String, FullAlbum> albumCache = new Dictionary<String, FullAlbum>();
    static Dictionary<String, FullTrack> trackCache = new Dictionary<String, FullTrack>();

    static IEnumerable<SpotifyData.Track> AllSavedTracks = null;
    static SpotifyData.Album[] AllSavedAlbums;
    static SpotifyData.Artist[] AllSavedArtists;

    static string PreferredMarket = Config.SpotifyMarket ?? "GB";

    /// <summary>
    /// Initialize and memoize the we API service using the authentication token stored in the registry
    /// </summary>
    static SpotifyWebAPIClass WebAppService
    {
        get
        {
            lock (logger)
            {
	            if (webAppService == null || webApiExpiry <= DateTime.Now)
	            {
                    logger.Info("Connecting and authenticating to Spotify Web API");
	                try
	                {
		                RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Avid");

	                    string refreshUrl = key.GetValue("SpotifyRefreshUrl") as string;

	                    if (!string.IsNullOrEmpty(refreshUrl))
		                {
		                    HttpWebRequest request =
		                        (HttpWebRequest)HttpWebRequest.Create(refreshUrl);
		                    request.Method = WebRequestMethods.Http.Get;
		                    request.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);

		                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
		                    var tokenJsonString = new StreamReader(response.GetResponseStream()).ReadToEnd();
		                    if (!string.IsNullOrEmpty(tokenJsonString))
		                    {
		                        Token token = JsonConvert.DeserializeObject<Token>(tokenJsonString);
		                        if (!string.IsNullOrEmpty(token.AccessToken) && !string.IsNullOrEmpty(token.TokenType))
		                        {
                                    if (!string.IsNullOrEmpty(token.RefreshToken))
                                    {
                                        var equalsPos = refreshUrl.LastIndexOf('=');
                                        if (equalsPos > 0)
                                        {
                                            var newRefreshUrl = refreshUrl.Substring(0, equalsPos + 1) + token.RefreshToken;
                                            if (newRefreshUrl != refreshUrl)
                                            {
                                                try
                                                {
	                                                RegistryKey updateKey = Registry.LocalMachine.OpenSubKey(@"Software\Avid", true);
	                                                updateKey.SetValue("SpotifyRefreshUrl", newRefreshUrl);
	                                                logger.Info("Updated saved authentication data for Spotify Web API");
                                                }
                                                catch (System.Exception ex)
                                                {
                                                    logger.Info(ex, "Unable to update saved authentication data for Spotify Web API", ex.Message);
                                                }
                                            }
                                        }
                                    }
		                            webApiExpiry = DateTime.Now.AddSeconds(token.ExpiresIn * 4 / 5);    // Only use the token for 80% of its promised life
		                            webAppService = new SpotifyWebAPIClass()
		                            {
		                                AccessToken = token.AccessToken,
		                                TokenType = token.TokenType,
		                                UseAuth = true
		                            };
		                            webApiCurrentUserId = webAppService.GetPrivateProfile().Id;
                                    logger.Info("Connected and authenticated to Spotify Web API (expires at {0})",
                                        webApiExpiry.ToShortTimeString());
                                }
	                            else
	                            {
	                                logger.Error("Invalid response from authentication for Spotify Web API");
	                            }
	                        }
	                        else
	                        {
	                            logger.Error("No response from authentication for Spotify Web API");
	                        }
		                }
	                    else
	                    {
	                        logger.Error("No saved authentication data for Spotify Web API");
	                    }
	                }
	                catch (System.Exception ex)
	                {
                        logger.Error(ex, "Failed to connect to Spotify Web API: {0}", ex.Message);
                    }
	            }

                if (webAppService == null || webApiExpiry <= DateTime.Now)
                {
                    logger.Error("Failed to connect to Spotify Web API");
                }

	            if (AllSavedTracks == null && webAppService != null)
	            {
	                LoadAndIndexAllSavedTracks();
                }
            }

            return webAppService;
        }
    }

    public static bool Probe()
    {
        lock (logger)
        {
            if (webAppService == null || webApiExpiry <= DateTime.Now)
            {
                logger.Info("Probing Authentication API");
                try
                {
                    HttpWebRequest request =
                        (HttpWebRequest)HttpWebRequest.Create("http://brianavid.dnsalias.com/SpotifyAuth/Auth/Probe");
                    request.Method = WebRequestMethods.Http.Get;
                    request.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
                    request.Timeout = 10000;

                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                    return response.StatusCode == HttpStatusCode.OK;
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed to probe Authentication API: {0}", ex.Message);
                    return false;
                }
            }
        }

        return true;
    }

    static FullTrack GetFullTrack(
        string id)
    {
        if (!trackCache.ContainsKey(id))
        {
            trackCache[id] = WebAppService.GetTrack(id);
        }
        return trackCache[id];
    }

    static FullAlbum GetFullAlbum(
        string id)
    {
        if (!albumCache.ContainsKey(id))
        {
            albumCache[id] = WebAppService.GetAlbum(id);
        }
        return albumCache[id];
    }

    static FullArtist GetFullArtist(
        string id)
    {
        if (!artistCache.ContainsKey(id))
        {
            artistCache[id] = WebAppService.GetArtist(id);
        }
        return artistCache[id];
    }

    /// <summary>
    /// Helper function to turn an unbounded IEnumerable collection into a collection of collections
    /// where each inner collection is no larger than batchSize
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="collection"></param>
    /// <param name="batchSize"></param>
    /// <returns></returns>
    static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> collection, int batchSize)
    {
        List<T> nextbatch = new List<T>(batchSize);
        foreach (T item in collection)
        {
            nextbatch.Add(item);
            if (nextbatch.Count == batchSize)
            {
                yield return nextbatch;
                nextbatch = new List<T>();
            }
        }
        if (nextbatch.Count > 0)
            yield return nextbatch;
    }


    /// <summary>
    /// Helper comparator function to compare albums, first by artist name and then
    /// (for the same artist) by the album name
    /// </summary>
    /// <param name="a1"></param>
    /// <param name="a2"></param>
    /// <returns></returns>
    private static int CompareAlbumByArtist(
        SpotifyData.Album a1,
        SpotifyData.Album a2)
    {
        var result = a1.ArtistName.CompareTo(a2.ArtistName);
        return result != 0 ? result : a1.Name.CompareTo(a2.Name);
    }


    /// <summary>
    /// Load and index all saved track, to build arrays of saved albums and saved artists
    /// </summary>
    public static void LoadAndIndexAllSavedTracks()
    {
        AllSavedTracks = new List<SpotifyData.Track>(); // prevents reentrancy

        if (WebAppService != null)
        {
            for (var retries = 0; retries < 20; retries++)
            {
                lock (WebAppService)
                {
                    try
                    {
                        logger.Info("LoadAndIndexAllSavedTracks start");
                        var pagedTracks = WebAppService.GetSavedTracks();
                        if (pagedTracks.HasError() && pagedTracks.ErrorResponse.Status == 429)
                        {
                            logger.Info("LoadAndIndexAllSavedTracks rate limited");
                            System.Threading.Thread.Sleep(1000);
                            continue;
                        }
                        var foundSavedTracks = MakeTracks(
                            pagedTracks,
                            next => WebAppService.DownloadData<Paging<PlaylistTrack>>(next));

                        if (foundSavedTracks == null)
                        {
                            logger.Info("LoadAndIndexAllSavedTracks tracks incomplete");
                            System.Threading.Thread.Sleep(1000);
                            continue;
                        }

                        AllSavedTracks = foundSavedTracks;
                        logger.Info("LoadAndIndexAllSavedTracks {0} tracks", AllSavedTracks.Count());


                        HashSet<String> albumIds = new HashSet<String>();
                        foreach (var track in AllSavedTracks)
                        {
                            if (!albumIds.Contains(track.AlbumId))
                            {
                                albumIds.Add(track.AlbumId);
                            }
                        }

                        List<SpotifyData.Album> savedAlbumList = new List<SpotifyData.Album>();
                        foreach (var batch in albumIds.Batch(20))
                        {
                            var batchOfIds = batch.Select(id => SimplifyId(id));
                            var batchOfAlbums = WebAppService.GetSeveralAlbums(batchOfIds.ToList(), PreferredMarket);
                            if (batchOfAlbums.Albums == null)
                            {
                                System.Threading.Thread.Sleep(2000);
                                batchOfAlbums = WebAppService.GetSeveralAlbums(batchOfIds.ToList(), PreferredMarket);
                            }
                            if (batchOfAlbums.Albums != null)
                                {
                                foreach (var album in batchOfAlbums.Albums)
                                {
                                    if (album != null && album.Artists != null && album.Artists.Count != 0)
                                    {
                                        savedAlbumList.Add(MakeAlbum(album));
                                    }
                                }
                            }
                        }
                        AllSavedAlbums = savedAlbumList.ToArray();
                        logger.Info("LoadAndIndexAllSavedTracks {0}/{1} albums", AllSavedAlbums.Count(), albumIds.Count);

                        HashSet<String> artistIds = new HashSet<String>();
                        foreach (var album in AllSavedAlbums)
                        {
                            if (!artistIds.Contains(album.ArtistId))
                            {
                                artistIds.Add(album.ArtistId);
                            }
                        }

                        List<SpotifyData.Artist> savedArtistList = new List<SpotifyData.Artist>();
                        foreach (var batch in artistIds.Batch(20))
                        {
                            var batchOfIds = batch.Select(id => SimplifyId(id));
                            var batchOfArtists = WebAppService.GetSeveralArtists(batchOfIds.ToList());
                            if (batchOfArtists.Artists == null)
                            {
                                System.Threading.Thread.Sleep(2000);
                                batchOfArtists = WebAppService.GetSeveralArtists(batchOfIds.ToList());
                            }
                            if (batchOfArtists.Artists != null)
                            {
                                foreach (var artist in batchOfArtists.Artists)
                                {
                                    savedArtistList.Add(MakeArtist(artist));
                                }
                            }
                        }

                        AllSavedArtists = savedArtistList.ToArray();
                        logger.Info("LoadAndIndexAllSavedTracks {0}/{1} artists", AllSavedArtists.Count(), artistIds.Count);

                        Array.Sort(AllSavedAlbums, CompareAlbumByArtist);
                        Array.Sort(AllSavedArtists, (a1, a2) => a1.Name.CompareTo(a2.Name));

                        break;
                    }
                    catch (System.Exception ex)
                    {
                        logger.Error(ex);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Format a track's duration for display
    /// </summary>
    /// <param name="rawDuration"></param>
    /// <returns></returns>
    public static string FormatDuration(int seconds)
    {
        return seconds < 0 ? "<0:00" : string.Format("{0}:{1:00}", seconds / 60, seconds % 60);
    }

    /// <summary>
    /// Initialize the WebAPI HTTP client, setting cache control to prevent caching
    /// </summary>
    public static void Initialize()
    {
        trayAppClient.BaseAddress = new Uri("http://localhost:8383");
        trayAppClient.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue();
        trayAppClient.DefaultRequestHeaders.CacheControl.NoCache = true;
        trayAppClient.DefaultRequestHeaders.CacheControl.MaxAge = new TimeSpan(0);

        if (Spotify.Probe())
            LoadAndIndexAllSavedTracks();
    }

    #region Browsing and Searching
    /// <summary>
    /// Search Spotify for tracks matching the specified track name
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static IEnumerable<SpotifyData.Track> SearchTracks(
        string name)
    {
        if (WebAppService != null)
        {
            lock (WebAppService)
            {
                logger.Info("SearchTracks {0}", name);

                try
                {
                    return MakeTracks(
                        WebAppService.SearchItems(HttpUtility.UrlEncode(name), SearchType.TRACK, limit: 50).Tracks,
                        next => WebAppService.DownloadData<SearchItem>(next).Tracks);
                }
                catch (System.Exception ex)
                {
                    logger.Error(ex);
                }
            }
        }

        return new List<SpotifyData.Track>();
    }

    /// <summary>
    /// Search Spotify for albums matching the specified album name
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static IEnumerable<SpotifyData.Album> SearchAlbums(
        string name)
    {
        if (WebAppService != null)
        {
            lock (WebAppService)
            {
                logger.Info("SearchAlbums {0}", name);

                try
                {
                    return MakeAlbums(
                        WebAppService.SearchItems(HttpUtility.UrlEncode(name), SearchType.ALBUM).Albums,
                        next => WebAppService.DownloadData<SearchItem>(next).Albums);
                }
                catch (System.Exception ex)
                {
                    logger.Error(ex);
                }
            }
        }

        return new List<SpotifyData.Album>();
    }

    /// <summary>
    /// Search Spotify for artists matching the specified artist name
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static IEnumerable<SpotifyData.Artist> SearchArtists(
        string name)
    {
        if (WebAppService != null)
        {
            lock (WebAppService)
            {
                logger.Info("SearchArtists {0}", name);

                try
                {
                    return MakeArtists(
                        WebAppService.SearchItems(HttpUtility.UrlEncode(name), SearchType.ARTIST, limit: 50).Artists,
                        next => WebAppService.DownloadData<SearchItem>(next).Artists);

                }
                catch (System.Exception ex)
                {
                    logger.Error(ex);
                }
            }
        }

        return new List<SpotifyData.Artist>();
    }

    /// <summary>
    /// Return track data for a track
    /// </summary>
    /// <param name="id">The Spotify Track URI</param>
    /// <returns></returns>
    public static SpotifyData.Track GetTrackById(
        string id)
    {
        if (WebAppService != null)
        {
            lock (WebAppService)
            {
                try
                {
                    return MakeTrack(GetFullTrack(SimplifyId(id)));
                }
                catch (System.Exception ex)
                {
                    logger.Error(ex);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Return album data for an identified album
    /// </summary>
    /// <param name="id">The Spotify Album URI</param>
    /// <returns></returns>
    public static SpotifyData.Album GetAlbumById(
        string id)
    {
        if (WebAppService != null)
        {
            lock (WebAppService)
            {
                try
                {
                    return MakeAlbum(GetFullAlbum(SimplifyId(id)));
                }
                catch (System.Exception ex)
                {
                    logger.Error(ex);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Return artist data for an identified artist
    /// </summary>
    /// <param name="id">The Spotify Artist URI</param>
    /// <returns></returns>
    public static SpotifyData.Artist GetArtistById(
        string id)
    {
        if (WebAppService != null)
        {
            lock (WebAppService)
            {
                try
                {
                    return MakeArtist(WebAppService.GetArtist(SimplifyId(id)));
                }
                catch (System.Exception ex)
                {
                    logger.Error(ex);
                }
            }
        }

        return null;
    }

    class ArtistHistory
    {
        const string XmlFilename = @"C:\Avid.Net\SpotifyArtists.xml";
        const int MaxHistory = 50;
        public string Name;
        public string Id;

        static List<ArtistHistory> artistHistory = null;

        ArtistHistory(
            string name,
            string id)
        {
            Id = id;
            Name = name;
        }

        ArtistHistory(
            XElement xSeries)
        {
            try
            {
                Id = xSeries.Attribute("Id").Value;
                Name = xSeries.Attribute("Name").Value;
            }
            catch (System.Exception ex)
            {
                logger.Error(ex, "Error parsing ArtistHistory XML: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Load the ...
        /// </summary>
        static void Load()
        {
            if (artistHistory == null)
            {
                if (File.Exists(XmlFilename))
                {
                    XElement seriesDoc = XDocument.Load(XmlFilename, LoadOptions.None).Root;
                    artistHistory = seriesDoc.Elements("Artist")
                        .Select(s => new ArtistHistory(s))
                        .ToList();
                }
                else
                {
                    artistHistory = new List<ArtistHistory>();
                }
            }
        }

        static void Save()
        {
            XElement root = new XElement("Artists",
                artistHistory.Select(s => s.ToXml));
            root.Save(XmlFilename);
        }

        XElement ToXml
        {
            get
            {
                return new XElement("Artist",
                    new XAttribute("Id", Id),
                    new XAttribute("Name", Name));
            }
        }

        public static List<ArtistHistory> All
        {
            get {
                if (artistHistory == null)
                {
                    Load();
                }
                return artistHistory;
            }
        }

        public static void Add(
            string name,
            string id)
        {
            var newHistory = All.Where(h => h.Id != id).Take(MaxHistory - 1).ToList();
            newHistory.Insert(0, new ArtistHistory(name, id));
            artistHistory = newHistory;
            Save();
        }
    }

    /// <summary>
    /// Get the collection of albums for an identified artist
    /// </summary>
    /// <param name="id">The Spotify Artist URI</param>
    /// <returns></returns>
    public static IEnumerable<SpotifyData.Album> GetAlbumsForArtist(
        string id)
    {
        if (WebAppService != null)
        {
            lock (WebAppService)
            {
                try
                {
                    var artist = GetArtistById(id);
                    ArtistHistory.Add(artist.Name, artist.Id);

                    return MakeAlbums(
                        WebAppService.GetArtistsAlbums(SimplifyId(id), AlbumType.ALBUM, PreferredMarket),
                        next => WebAppService.DownloadData<Paging<SimpleAlbum>>(next));

                }
                catch (System.Exception ex)
                {
                    logger.Error(ex);
                }
            }
        }

        return new List<SpotifyData.Album>();
    }

    /// <summary>
    /// Get the collection of similar artists for an identified artist
    /// </summary>
    /// <param name="id">The Spotify Artist URI</param>
    /// <returns></returns>
    public static IEnumerable<SpotifyData.Artist> GetSimilarArtistsForArtist(
        string id)
    {
        if (WebAppService != null)
        {
            lock (WebAppService)
            {
                try
                {
                    return WebAppService.GetRelatedArtists(SimplifyId(id)).Artists.Select(a => MakeArtist(a));
                }
                catch (System.Exception ex)
                {
                    logger.Error(ex);
                }
            }
        }

        return new List<SpotifyData.Artist>();
    }

    /// <summary>
    /// Get the collection of recently searched artists
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<SpotifyData.Artist> GetHistoryArtists()
    {
        return ArtistHistory.All.Select(h => MakeArtist(h));
    }

    /// <summary>
    /// Get the collection of tracks for an identified album
    /// </summary>
    /// <param name="id">The Spotify Album URI</param>
    /// <returns></returns>
    public static IEnumerable<SpotifyData.Track> GetTracksForAlbum(
        string id)
    {
        if (WebAppService != null)
        {
            lock (WebAppService)
            {
                try
                {
                    return MakeTracks(
                        WebAppService.GetAlbumTracks(SimplifyId(id), PreferredMarket, limit: 50),
                        GetFullAlbum(SimplifyId(id)),
                        next => WebAppService.DownloadData<Paging<SimpleTrack>>(next));
                }
                catch (System.Exception ex)
                {
                    logger.Error(ex);
                }
            }
        }

        return new List<SpotifyData.Track>();
    }

    /// <summary>
    /// Get the cover image Url for an identified album
    /// </summary>
    /// <param name="id">The Spotify Album URI</param>
    /// <returns></returns>
    public static String GetAlbumImageUrl(
        string id)
    {
        if (WebAppService != null)
        {
            lock (WebAppService)
            {
                try
                {
                    var a = GetFullAlbum(SimplifyId(id));
                    if (a != null && a.Images.Count != 0)
                    {
                        var url = a.Images[0].Url;
                        //  It's also useful (though not essential) for the player to have the album cover URL to display
                        lock (trayAppClient)
                        {
                            try
                            {
                                HttpResponseMessage resp = trayAppClient.GetAsync(string.Format("api/playqueue/NoteAlbumUrlForId?id={0}&url={1}", 
                                    HttpUtility.UrlEncode(id), HttpUtility.UrlEncode(url))).Result;
                                resp.EnsureSuccessStatusCode();

                                var dontCare = resp.Content.ReadAsAsync<Boolean>().Result;
                            }
                            catch (System.Exception ex)
                            {
                                logger.Error(ex.Message);
                            }
                        }
                        return url;
                    }
                }
                catch (System.Exception ex)
                {
                    logger.Error(ex);
                }
            }
        }

        return null;
    }

    #endregion

    #region Playlists and My Music
    public static Dictionary<String, SpotifyData.Playlist> CurrentPlaylists { get; private set; }

    /// <summary>
    /// Get the collection of named playlists, rebuilding from data on Spotify
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<SpotifyData.Playlist> GetPlayLists()
    {
        if (WebAppService != null)
        {
            lock (WebAppService)
            {
                try
                {
                    var playlists = MakePlaylists(
                        WebAppService.GetUserPlaylists(webApiCurrentUserId),
                        next => WebAppService.DownloadData<Paging<SimplePlaylist>>(next));
                    CurrentPlaylists = playlists.ToDictionary(p => p.Name);
                    return playlists;
                }
                catch (System.Exception ex)
                {
                    logger.Error(ex);
                }
            }
        }

        return new List<SpotifyData.Playlist>();
    }

    /// <summary>
    /// Get the collection of tracks for an identified playlist
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static IEnumerable<SpotifyData.Track> GetPlayListTracks(
        string id)
    {
        if (WebAppService != null)
        {
            lock (WebAppService)
            {
                try
                {
                    return MakeTracks(
                        WebAppService.GetPlaylistTracks(webApiCurrentUserId, SimplifyId(id), PreferredMarket),
                        next => WebAppService.DownloadData<Paging<PlaylistTrack>>(next));
                }
                catch (System.Exception ex)
                {
                    logger.Error(ex);
                }
            }
        }

        return new List<SpotifyData.Track>();
    }

    /// <summary>
    /// Get the collection of albums for an identified playlist
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static IEnumerable<SpotifyData.Album> GetPlayListAlbums(
        string id)
    {
        if (WebAppService != null)
        {
            lock (WebAppService)
            {
                try
                {
                    var tracks = MakeTracks(
                        WebAppService.GetPlaylistTracks(webApiCurrentUserId, SimplifyId(id), PreferredMarket),
                        next => WebAppService.DownloadData<Paging<PlaylistTrack>>(next));

                    HashSet<String> albumIds = new HashSet<String>();
                    foreach (var track in tracks)
                    {
                        if (!albumIds.Contains(track.AlbumId))
                        {
                            albumIds.Add(track.AlbumId);
                        }
                    }

                    return albumIds.Select(a => MakeAlbum(GetFullAlbum(SimplifyId(a))));
                }
                catch (System.Exception ex)
                {
                    logger.Error(ex);
                }
            }
        }

        return new List<SpotifyData.Album>();
    }

    /// <summary>
    /// Add a new (empty) named playlist
    /// </summary>
    /// <param name="name"></param>
    public static string AddPlayList(
        string name)
    {
        if (WebAppService != null)
        {
            lock (WebAppService)
            {
                try
                {
                    return WebAppService.CreatePlaylist(webApiCurrentUserId, name).Uri;
                }
                catch (System.Exception ex)
                {
                    logger.Error(ex);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Delete an identified playlist (just unfollows - does not actually delete)
    /// </summary>
    /// <param name="id"></param>
    public static void DeletePlayList(
        string id)
    {
        if (WebAppService != null)
        {
            lock (WebAppService)
            {
                try
                {
                    WebAppService.UnfollowPlaylist(webApiCurrentUserId, SimplifyId(id));
                }
                catch (System.Exception ex)
                {
                    logger.Error(ex);
                }
            }
        }
    }

    /// <summary>
    /// Rename an identified playlist
    /// </summary>
    /// <param name="id"></param>
    /// <param name="newName"></param>
    public static void RenamePlayList(
        string id,
        string newName)
    {
        if (WebAppService != null)
        {
            lock (WebAppService)
            {
                try
                {
                    WebAppService.UpdatePlaylist(webApiCurrentUserId, SimplifyId(id), newName);
                }
                catch (System.Exception ex)
                {
                    logger.Error(ex);
                }
            }
        }
    }

    /// <summary>
    /// Add an identified track to an identified playlist
    /// </summary>
    /// <param name="playlistId"></param>
    /// <param name="trackId"></param>
    public static void AddTrackToPlayList(
        string playlistId,
        string trackId)
    {
        if (WebAppService != null)
        {
            lock (WebAppService)
            {
                try
                {
                    WebAppService.AddTracks(webApiCurrentUserId, SimplifyId(playlistId), new List<String> { trackId });
                }
                catch (System.Exception ex)
                {
                    logger.Error(ex);
                }
            }
        }
    }

    /// <summary>
    /// Add all the tracks of an identified album to an identified playlist
    /// </summary>
    /// <param name="playlistId"></param>
    /// <param name="albumId"></param>
    public static void AddAlbumToPlayList(
        string playlistId,
        string albumId)
    {
        if (WebAppService != null)
        {
            lock (WebAppService)
            {
                try
                {
                    var tracks = WebAppService.GetAlbumTracks(SimplifyId(albumId), PreferredMarket);
                    for (; ; )
                    {
                        WebAppService.AddTracks(webApiCurrentUserId, SimplifyId(playlistId), tracks.Items.Select(t => t.Uri).ToList());
                        if (tracks.Next == null) break;
                        tracks = WebAppService.DownloadData<Paging<SimpleTrack>>(tracks.Next);
                    }
                }
                catch (System.Exception ex)
                {
                    logger.Error(ex);
                }
            }
        }
    }

    /// <summary>
    /// Remove an identified track from an identified playlist
    /// </summary>
    /// <param name="playlistId"></param>
    /// <param name="trackId"></param>
    public static void RemoveTrackFromPlayList(
        string playlistId,
        string trackId)
    {
        if (WebAppService != null)
        {
            lock (WebAppService)
            {
                try
                {
                    WebAppService.DeletePlaylistTracks(webApiCurrentUserId, SimplifyId(playlistId), new List<DeleteTrackArg> { new DeleteTrackArg(trackId) });
                }
                catch (System.Exception ex)
                {
                    logger.Error(ex);
                }
            }
        }
    }

    /// <summary>
    /// Remove all the tracks of an identified album from an identified playlist
    /// </summary>
    /// <param name="playlistId"></param>
    /// <param name="albumId"></param>
    public static void RemoveAlbumFromPlayList(
        string playlistId,
        string albumId)
    {
        if (WebAppService != null)
        {
            lock (WebAppService)
            {
                try
                {
                    var tracks = WebAppService.GetAlbumTracks(SimplifyId(albumId), PreferredMarket);
                    for (; ; )
                    {
                        WebAppService.DeletePlaylistTracks(webApiCurrentUserId, SimplifyId(playlistId), tracks.Items.Select(t => new DeleteTrackArg(t.Uri)).ToList());
                        if (tracks.Next == null) break;
                        tracks = WebAppService.DownloadData<Paging<SimpleTrack>>(tracks.Next);
                    }
                }
                catch (System.Exception ex)
                {
                    logger.Error(ex);
                }
            }
        }
    }

    /// <summary>
    /// Get the collection of albums saved by the current user
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<SpotifyData.Album> GetSavedAlbums()
    {
        logger.Info("Get Saved Albums");

        return WebAppService != null ? AllSavedAlbums : null;
    }

    /// <summary>
    /// Get the collection of artists saved by the current user
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<SpotifyData.Artist> GetSavedArtists()
    {
        logger.Info("Get Saved Artists");

        return WebAppService != null ? AllSavedArtists : null;
    }

    /// <summary>
    /// Save all the tracks of an identified album as a saved album
    /// </summary>
    /// <param name="playlistId"></param>
    /// <param name="albumId"></param>
    public static void AddSavedAlbum(
        string albumId)
    {
        if (WebAppService != null)
        {
            lock (WebAppService)
            {
                try
                {
                    var tracks = WebAppService.GetAlbumTracks(SimplifyId(albumId), PreferredMarket);
                    for (; ; )
                    {
                        WebAppService.SaveTracks(tracks.Items.Select(t => t.Id).ToList());
                        if (tracks.Next == null) break;
                        tracks = WebAppService.DownloadData<Paging<SimpleTrack>>(tracks.Next);
                    }
                    AllSavedTracks = null;
                }
                catch (System.Exception ex)
                {
                    logger.Error(ex);
                }
            }
        }
    }

    /// <summary>
    /// Remove all the tracks of an identified album as a saved album
    /// </summary>
    /// <param name="playlistId"></param>
    /// <param name="albumId"></param>
    public static void RemoveSavedAlbum(
        string albumId)
    {
        if (WebAppService != null)
        {
            lock (WebAppService)
            {
                try
                {
                    var tracks = WebAppService.GetAlbumTracks(SimplifyId(albumId), PreferredMarket);
                    for (; ; )
                    {
                        WebAppService.RemoveSavedTracks(tracks.Items.Select(t => t.Id).ToList());
                        if (tracks.Next == null) break;
                        tracks = WebAppService.DownloadData<Paging<SimpleTrack>>(tracks.Next);
                    }
                    AllSavedTracks = null;
                }
                catch (System.Exception ex)
                {
                    logger.Error(ex);
                }
            }
        }
    }

    /// <summary>
    /// Determine if all the tracks of an identified album are saved
    /// </summary>
    /// <remarks>
    /// The code only checks the first set of Paging results (20 tracks)
    /// </remarks>
    /// <param name="playlistId"></param>
    /// <param name="albumId"></param>
    public static Boolean IsSavedAlbum(
        string albumId)
    {
        if (WebAppService != null)
        {
            lock (WebAppService)
            {
                try
                {
                    var tracks = WebAppService.GetAlbumTracks(SimplifyId(albumId), PreferredMarket);
                    List<Boolean> saveIndications = WebAppService.CheckSavedTracks(tracks.Items.Select(t => t.Id).ToList()).List;
                    return saveIndications.All(v => v);
                }
                catch (System.Exception ex)
                {
                    logger.Error(ex);
                }
            }
        }

        return false;
    }

    #endregion

    #region Player Queue Management
    /// <summary>
    /// Play the identified track, either immediately or after the currently queued tracks
    /// </summary>
    /// <param name="id"></param>
    /// <param name="append"></param>
    /// <returns></returns>
    public static Boolean PlayTrack(
        string id,
        bool append = false)
    {
        lock (trayAppClient)
        {
            try
            {
                HttpResponseMessage resp = trayAppClient.GetAsync(string.Format("api/playqueue/PlayTrack?id={0}&append={1}", HttpUtility.UrlEncode(id), append)).Result;
                resp.EnsureSuccessStatusCode();

                return resp.Content.ReadAsAsync<Boolean>().Result;
            }
            catch (System.Exception ex)
            {
                logger.Error(ex);
                return false;
            }
        }
    }

    /// <summary>
    /// Play all tracks of the identified album, either immediately or after the currently queued tracks
    /// </summary>
    /// <param name="id"></param>
    /// <param name="append"></param>
    /// <returns></returns>
    public static Boolean PlayAlbum(
        string id,
        bool append = false)
    {
        lock (trayAppClient)
        {
            try
            {
                HttpResponseMessage resp = trayAppClient.GetAsync(string.Format("api/playqueue/PlayAlbum?id={0}&append={1}", HttpUtility.UrlEncode(id), append)).Result;
                resp.EnsureSuccessStatusCode();

                return resp.Content.ReadAsAsync<Boolean>().Result;
            }
            catch (System.Exception ex)
            {
                logger.Error(ex);
                return false;
            }
        }
    }

    /// <summary>
    /// Get the currently playing track
    /// </summary>
    /// <returns></returns>
    public static SpotifyData.Track GetCurrentTrack()
    {
        lock (trayAppClient)
        {
            try
            {
                HttpResponseMessage resp = trayAppClient.GetAsync(string.Format("api/playqueue/GetCurrentTrack")).Result;
                resp.EnsureSuccessStatusCode();

                var currentTrackId = resp.Content.ReadAsAsync<String>().Result;

                return currentTrackId == null ? null : MakeTrack(GetFullTrack(SimplifyId(currentTrackId)));
            }
            catch (System.Exception ex)
            {
                logger.Error(ex);
                return null;
            }
        }
    }

    /// <summary>
    /// Get the collection of all queued tracks
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<SpotifyData.Track> GetQueuedTracks()
    {
        List<SpotifyData.Track> result = new List<SpotifyData.Track>();

        lock (trayAppClient)
        {
            try
            {
                HttpResponseMessage resp = trayAppClient.GetAsync(string.Format("api/playqueue/GetQueuedTracks")).Result;
                resp.EnsureSuccessStatusCode();

                var queuedTracks = resp.Content.ReadAsAsync<IEnumerable<String>>().Result;

                foreach (var batch in queuedTracks.Batch(50))
                {
                    if (WebAppService != null)
                    {
                        lock (WebAppService)
                        {
                            foreach (var track in WebAppService.GetSeveralTracks(batch.Select(id => SimplifyId(id)).ToList(), PreferredMarket).Tracks)
                            {
                                result.Add(MakeTrack(track));
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                logger.Error(ex);
            }

        }
        return result;
    }

    /// <summary>
    /// Skip to a specified queued track
    /// </summary>
    public static SpotifyData.Track SkipToQueuedTrack(
        string id)
    {
        lock (trayAppClient)
        {
            try
            {
                HttpResponseMessage resp = trayAppClient.GetAsync(string.Format("api/playqueue/SkipToQueuedTrack?id={0}", HttpUtility.UrlEncode(id))).Result;
                resp.EnsureSuccessStatusCode();

                var resultId = resp.Content.ReadAsAsync<String>().Result;

                return MakeTrack(GetFullTrack(SimplifyId(resultId)));
            }
            catch (System.Exception ex)
            {
                logger.Error(ex);
                return null;
            }
        }
    }

    /// <summary>
    /// Remove the specified queued track from the queue
    /// </summary>
    public static SpotifyData.Track RemoveQueuedTrack(
        string id)
    {
        lock (trayAppClient)
        {
            try
            {
                HttpResponseMessage resp = trayAppClient.GetAsync(string.Format("api/playqueue/RemoveQueuedTrack?id={0}", HttpUtility.UrlEncode(id))).Result;
                resp.EnsureSuccessStatusCode();

                var resultId = resp.Content.ReadAsAsync<String>().Result;

                return MakeTrack(GetFullTrack(SimplifyId(resultId)));
            }
            catch (System.Exception ex)
            {
                logger.Error(ex);
                return null;
            }
        }
    }
    #endregion

    #region Player currently playing track operations
    /// <summary>
    /// Skip playing forwards to the next queued track
    /// </summary>
    /// <returns></returns>
    public static int Skip()
    {
        lock (trayAppClient)
        {
            try
            {
                HttpResponseMessage resp = trayAppClient.GetAsync(string.Format("api/playqueue/Skip")).Result;
                resp.EnsureSuccessStatusCode();

                return resp.Content.ReadAsAsync<int>().Result;
            }
            catch (System.Exception ex)
            {
                logger.Error(ex);
                return 0;
            }
        }
    }

    /// <summary>
    /// Skip playing backwards to the previous queued track
    /// </summary>
    /// <returns></returns>
    public static int Back()
    {
        lock (trayAppClient)
        {
            try
            {
                HttpResponseMessage resp = trayAppClient.GetAsync(string.Format("api/playqueue/Back")).Result;
                resp.EnsureSuccessStatusCode();

                return resp.Content.ReadAsAsync<int>().Result;
            }
            catch (System.Exception ex)
            {
                logger.Error(ex);
                return 0;
            }
        }
    }

    /// <summary>
    /// Start or continue playing the current track
    /// </summary>
    /// <returns></returns>
    public static int Play()
    {
        lock (trayAppClient)
        {
            try
            {
                HttpResponseMessage resp = trayAppClient.GetAsync(string.Format("api/player/Play")).Result;
                resp.EnsureSuccessStatusCode();

                return resp.Content.ReadAsAsync<int>().Result;
            }
            catch (System.Exception ex)
            {
                logger.Error(ex);
                return 0;
            }
        }
    }

    /// <summary>
    /// Pause playing the current track
    /// </summary>
    /// <returns></returns>
    public static int Pause()
    {
        lock (trayAppClient)
        {
            try
            {
                HttpResponseMessage resp = trayAppClient.GetAsync(string.Format("api/player/Pause")).Result;
                resp.EnsureSuccessStatusCode();

                return resp.Content.ReadAsAsync<int>().Result;
            }
            catch (System.Exception ex)
            {
                logger.Error(ex);
                return 0;
            }
        }
    }

    /// <summary>
    /// Is the player playing a track?
    /// </summary>
    /// <returns>+ve: Playing; 0: Paused; -ve: Stolen by another session</returns>
    public static int GetPlaying()
    {
        lock (trayAppClient)
        {
            try
            {
                HttpResponseMessage resp = trayAppClient.GetAsync(string.Format("api/player/GetPlaying")).Result;
                resp.EnsureSuccessStatusCode();

                return resp.Content.ReadAsAsync<int>().Result;
            }
            catch (System.Exception ex)
            {
                logger.Error(ex);
                return -1;  //  We don't know
            }
        }
    }

    /// <summary>
    /// Stop playing the current track
    /// </summary>
    /// <returns></returns>
    public static int Stop()
    {
        lock (trayAppClient)
        {
            try
            {
                HttpResponseMessage resp = trayAppClient.GetAsync(string.Format("api/player/Stop")).Result;
                resp.EnsureSuccessStatusCode();

                return resp.Content.ReadAsAsync<int>().Result;
            }
            catch (System.Exception ex)
            {
                logger.Error(ex);
                return 0;
            }
        }
    }

    /// <summary>
    /// Get the position at which the current track is playing
    /// </summary>
    /// <returns>Position in seconds</returns>
    public static int GetPosition()
    {
        lock (trayAppClient)
        {
            try
            {
                HttpResponseMessage resp = trayAppClient.GetAsync(string.Format("api/player/GetPosition")).Result;
                resp.EnsureSuccessStatusCode();

                return resp.Content.ReadAsAsync<int>().Result;
            }
            catch (System.Exception ex)
            {
                logger.Error(ex);
                return 0;
            }
        }
    }

    /// <summary>
    /// Seek to a particular position within the currently playing track
    /// </summary>
    /// <param name="pos">Position in seconds</param>
    /// <returns></returns>
    public static int SetPosition(
        int pos)
    {
        lock (trayAppClient)
        {
            try
            {
                HttpResponseMessage resp = trayAppClient.GetAsync(string.Format("api/player/SetPosition?pos={0}", pos)).Result;
                resp.EnsureSuccessStatusCode();

                return resp.Content.ReadAsAsync<int>().Result;
            }
            catch (System.Exception ex)
            {
                logger.Error(ex);
                return 0;
            }
        }
    }

    public static void ExitPlayer()
    {
        lock (trayAppClient)
        {
            HttpResponseMessage resp = trayAppClient.GetAsync(string.Format("api/player/ExitPlayer")).Result;
            resp.EnsureSuccessStatusCode();
        }

    }
    #endregion

    #region Constructors of SpotifyData from Web API model

    /// <summary>
    /// GIven a Spotify URI (an external ID), return the Spotify Id, which is its textually last component
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public static String SimplifyId(
        string id)
    {
        var pos = id.LastIndexOf(':');
        return (pos > 0) ? id.Substring(pos + 1) : id;

    }

    /// <summary>
    /// Make an external Artist structure from that returned by the Web API
    /// </summary>
    /// <param name="artist"></param>
    /// <returns></returns>
    static SpotifyData.Artist MakeArtist(SimpleArtist artist)
    {
        return artist == null ? null : new SpotifyData.Artist
        {
            Id = artist.Uri,
            Name = artist.Name
        };
    }

    /// <summary>
    /// Make an external Artist structure from that returned by the Web API
    /// </summary>
    /// <param name="artist"></param>
    /// <returns></returns>
    static SpotifyData.Artist MakeArtist(FullArtist artist)
    {
        return artist == null ? null : new SpotifyData.Artist
        {
            Id = artist.Uri,
            Name = artist.Name
        };
    }

    /// <summary>
    /// Make an external Artist structure from that int the Artists History
    /// </summary>
    /// <param name="artist"></param>
    /// <returns></returns>
    static SpotifyData.Artist MakeArtist(ArtistHistory artist)
    {
        return artist == null ? null : new SpotifyData.Artist
        {
            Id = artist.Id,
            Name = artist.Name
        };
    }

    /// <summary>
    /// Make a collection of external Artist structures from Paging data returned by the Web API
    /// </summary>
    /// <param name="col"></param>
    /// <param name="ReadNext"></param>
    /// <returns></returns>
    static IEnumerable<SpotifyData.Artist> MakeArtists(
        Paging<SimpleArtist> col,
        Func<String, Paging<SimpleArtist>> ReadNext)
    {
        List<SpotifyData.Artist> result = new List<SpotifyData.Artist>();
        int noFound = 0;

        while (noFound < 200)
        {
            if (col.Items == null) return null;
            foreach (var a in col.Items)
            {
                result.Add(MakeArtist(a));
                noFound++;
            }

            if (col.Next == null) break;

            for (var retries = 5; retries >= 0; retries--)
            {
                var newCol = ReadNext(col.Next);
                if (retries > 0 && newCol.HasError())
                {
                    logger.Info("Paged artists error - {0} - {1} [{2} found]",
                        newCol.ErrorResponse.Status, newCol.ErrorResponse.Message, noFound);
                    System.Threading.Thread.Sleep(1000);
                }
                else
                {
                    col = newCol;
                    break;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Get the year of release for an album
    /// </summary>
    /// <param name="album"></param>
    /// <returns></returns>
    static string ReleaseYear(FullAlbum album)
    {
        DateTime released;
        if (DateTime.TryParse(album.ReleaseDate, out released))
        {
            return released.Year.ToString();
        }

        return album.ReleaseDate ?? "";
    }

    /// <summary>
    /// Make an external Album structure from that returned by the Web API
    /// </summary>
    /// <param name="album"></param>
    /// <returns></returns>
    static SpotifyData.Album MakeAlbum(FullAlbum album)
    {
        return album == null ? null : new SpotifyData.Album
        {
            Id = album.Uri,
            Name = album.Name,
            ArtistId = album.Artists[0].Uri,
            ArtistName = album.Artists[0].Name,
            Year = ReleaseYear(album)
        };
    }

    /// <summary>
    /// Make a collection of external Album structures from Paging data returned by the Web API
    /// </summary>
    /// <param name="col"></param>
    /// <param name="ReadNext"></param>
    /// <returns></returns>
    static IEnumerable<SpotifyData.Album> MakeAlbums(
        Paging<SimpleAlbum> col,
        Func<String, Paging<SimpleAlbum>> ReadNext)
    {
        List<SpotifyData.Album> result = new List<SpotifyData.Album>();
        int noFound = 0;

        while (noFound < 200)
        {
            if (col.Items == null) return null;
            var albumIds = col.Items.Select(a => a.Id).ToList();
            foreach (var a in WebAppService.GetSeveralAlbums(albumIds, PreferredMarket).Albums)
            {
                if (a != null)
                {
                    result.Add(MakeAlbum(a));
                }
                noFound++;
            }

            if (col.Next == null) break;

            for (var retries = 5; retries >= 0; retries--)
            {
                var newCol = ReadNext(col.Next);
                if (retries > 0 && newCol.HasError())
                {
                    logger.Info("Paged albums error - {0} - {1} [{2} found]",
                        newCol.ErrorResponse.Status, newCol.ErrorResponse.Message, noFound);
                    System.Threading.Thread.Sleep(1000);
                }
                else
                {
                    col = newCol;
                    break;
                }
            }
        }

        return result;
    }

    /// Make an external Track structure from that returned by the Web API
    /// </summary>
    /// <param name="track"></param>
    /// <param name="album"></param>
    /// <returns></returns>
    /// <summary>
    static SpotifyData.Track MakeTrack(FullTrack track, FullAlbum album = null)
    {
        if (album == null && track != null)
        {
            album = GetFullAlbum(track.Album.Id);
        }
        try
        {
            var noFullAlbum = album.Artists == null || album.Artists.Count == 0;
	        return track == null ? null : new SpotifyData.Track
	        {
	            Id = track.Uri,
	            Name = track.Name,
                AlbumId = noFullAlbum ? track.Album.Uri : album.Uri,
	            AlbumName = noFullAlbum ? track.Album.Name : album.Name,
	            ArtistId = noFullAlbum ? track.Artists[0].Uri : album.Artists[0].Uri,
	            AlbumArtistName = noFullAlbum ? track.Artists[0].Uri : album.Artists[0].Name,
	            TrackArtistNames = track.Artists.Aggregate("", ConstructTrackArtistNames),
                TrackFirstArtistId = track.Artists[0].Uri,
	            Index = track.TrackNumber,
	            Duration = track.DurationMs / 1000
	        };
        }
        catch (System.Exception ex)
        {
            logger.Error(ex, "Can't make track {0}: {1}", track.Uri, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Make a collection of external Track structures from Paging data returned by the Web API
    /// </summary>
    /// <param name="col"></param>
    /// <param name="ReadNext"></param>
    /// <returns></returns>
    static IEnumerable<SpotifyData.Track> MakeTracks(
        Paging<FullTrack> col,
        Func<String, Paging<FullTrack>> ReadNext)
    {
        List<SpotifyData.Track> result = new List<SpotifyData.Track>();
        int noFound = 0;

        while (noFound < 200)
        {
            if (col.Items == null) return null;
            foreach (var t in col.Items)
            {
                var track = MakeTrack(t);
                if (track != null)
                {
                    result.Add(track);
                    noFound++;
                }
            }

            if (col.Next == null) break;

            for (var retries = 5; retries >= 0; retries--)
            {
                var newCol = ReadNext(col.Next);
                if (retries > 0 && newCol.HasError())
                {
                    logger.Info("Paged tracks error - {0} - {1} [{2} found]",
                        newCol.ErrorResponse.Status, newCol.ErrorResponse.Message, noFound);
                    System.Threading.Thread.Sleep(1000);
                }
                else
                {
                    col = newCol;
                    break;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Make a collection of external Track structures from Paging data returned by the Web API
    /// </summary>
    /// <param name="col"></param>
    /// <param name="album"></param>
    /// <param name="ReadNext"></param>
    /// <returns></returns>
    static IEnumerable<SpotifyData.Track> MakeTracks(
        Paging<SimpleTrack> col,
        FullAlbum album,
        Func<String, Paging<SimpleTrack>> ReadNext)
    {
        List<SpotifyData.Track> result = new List<SpotifyData.Track>();
        int noFound = 0;

        while (noFound < 200)
        {
            if (col.Items == null) return null;
            foreach (var t in col.Items)
            {
                var track = MakeTrack(GetFullTrack(t.Id), album);
                if (track != null)
                {
                    result.Add(track);
                    noFound++;
                }

            }

            if (col.Next == null) break;

            for (var retries = 5; retries >= 0; retries--)
            {
                var newCol = ReadNext(col.Next);
                if (retries > 0 && newCol.HasError())
                {
                    logger.Info("Paged tracks error - {0} - {1} [{2} found]",
                        newCol.ErrorResponse.Status, newCol.ErrorResponse.Message, noFound);
                    System.Threading.Thread.Sleep(1000);
                }
                else
                {
                    col = newCol;
                    break;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Make a collection of external Track structures from Paging data returned by the Web API as a Playlist
    /// </summary>
    /// <param name="col"></param>
    /// <param name="ReadNext"></param>
    /// <returns></returns>
    static IEnumerable<SpotifyData.Track> MakeTracks(
        Paging<PlaylistTrack> col,
        Func<String, Paging<PlaylistTrack>> ReadNext)
    {
        List<SpotifyData.Track> result = new List<SpotifyData.Track>();
        int noFound = 0;

        for (; ; )  //  Unbounded within a playlist or saved tracks
        {
            if (col.Items == null) return null;
            foreach (var t in col.Items)
            {
                var track = MakeTrack(t.Track);
                if (track != null)
                {
                    result.Add(track);
                    noFound++;
                }
            }

            if (col.Next == null) break;

            for (var retries = 5; retries >= 0; retries--)
            {
                var newCol = ReadNext(col.Next);
                if (retries > 0 && newCol.HasError())
                {
                    logger.Info("Paged tracks error - {0} - {1} [{2} found]",
                        newCol.ErrorResponse.Status, newCol.ErrorResponse.Message, noFound);
                    System.Threading.Thread.Sleep(1000);
                }
                else
                {
                    col = newCol;
                    break;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Make an external Playlist structure from that returned by the Web API
    /// </summary>
    /// <param name="playlist"></param>
    /// <returns></returns>
    static SpotifyData.Playlist MakePlaylist(SimplePlaylist playlist)
    {
        return playlist == null ? null : new SpotifyData.Playlist
        {
            Id = playlist.Uri,
            Name = playlist.Name,
        };
    }

    /// <summary>
    /// Make a collection of external Playlist structures from Paging data returned by the Web API
    /// </summary>
    /// <param name="col"></param>
    /// <param name="ReadNext"></param>
    /// <returns></returns>
    static IEnumerable<SpotifyData.Playlist> MakePlaylists(
        Paging<SimplePlaylist> col,
        Func<String, Paging<SimplePlaylist>> ReadNext)
    {
        List<SpotifyData.Playlist> result = new List<SpotifyData.Playlist>();
        int noFound = 0;

        while (noFound < 200)
        {
            if (col.Items == null) break;
            foreach (var p in col.Items)
            {
                result.Add(MakePlaylist(p));
                noFound++;
            }

            if (col.Next == null) break;

            for (var retries = 5; retries >= 0; retries--)
            {
                var newCol = ReadNext(col.Next);
                if (retries > 0 && newCol.HasError())
                {
                    logger.Info("Paged playlists error - {0} - {1} [{2} found]",
                        newCol.ErrorResponse.Status, newCol.ErrorResponse.Message, noFound);
                    System.Threading.Thread.Sleep(1000);
                }
                else
                {
                    col = newCol;
                    break;
                }
            }
        }

        return result;
    }


    /// <summary>
    /// Construct a formatted string for a (possibly multiple) artist names for a track
    /// </summary>
    /// <param name="names"></param>
    /// <param name="artist"></param>
    /// <returns></returns>
    static string ConstructTrackArtistNames(
        string names,
        SimpleArtist artist)
    {
        const string ellipsis = ", ...";
        if (string.IsNullOrEmpty(names))
        {
            return artist.Name;
        }
        if (names.EndsWith(ellipsis))
        {
            return names;
        }
        if (names.Count(c => c == ',') >= 2)
        {
            return names + ellipsis;
        }

        return names + ", " + artist.Name;
    }
    #endregion


}

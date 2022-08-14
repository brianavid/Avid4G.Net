﻿using Avid.Spotify;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net.Http;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Models;
using SpotifyAPI.Web.Enums;
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

    static SpotifyWebAPI webAppService = null;
    static DateTime webApiExpiry = DateTime.Now;
    static string webApiCurrentUserId = null;
    static object webAppServiceLock = new object();

    static string playbackDevice = null;

    static Dictionary<String, FullArtist> artistCache = new Dictionary<String, FullArtist>();
    static Dictionary<String, FullAlbum> albumCache = new Dictionary<String, FullAlbum>();
    static Dictionary<String, FullTrack> trackCache = new Dictionary<String, FullTrack>();

    static IEnumerable<SpotifyData.Album> AllSavedAlbumList = null;
    static SpotifyData.Album[] AllSavedAlbums;
    static SpotifyData.Artist[] AllSavedArtists;

    static string PreferredMarket = Config.SpotifyMarket ?? "GB";

    /// <summary>
    /// Initialize and memoize the we API service using the authentication token stored in the registry
    /// </summary>
    static SpotifyWebAPI WebAppService
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
		                            webAppService = new SpotifyWebAPI()
		                            {
		                                AccessToken = token.AccessToken,
		                                TokenType = token.TokenType,
		                                UseAuth = true
		                            };

                                    var profile = webAppService.GetPrivateProfile();
                                    webApiCurrentUserId = profile.Id;
                                    logger.Info("Connected and authenticated {0} to Spotify Web API (expires at {1})",
                                        webApiCurrentUserId, webApiExpiry.ToShortTimeString());

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

                if (AllSavedAlbumList == null && webAppService != null)
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
        if (id == null) return null;

        if (!trackCache.ContainsKey(id))
        {
            lock (webAppServiceLock)
            {
                trackCache[id] = WebAppService.GetTrack(id);
            }
        }
        return trackCache[id];
    }

    static FullAlbum GetFullAlbum(
        string id)
    {
        if (id == null) return null;

        if (!albumCache.ContainsKey(id))
        {
            lock (webAppServiceLock)
            {
                albumCache[id] = WebAppService.GetAlbum(id);
            }
        }
        return albumCache[id];
    }

    static FullArtist GetFullArtist(
        string id)
    {
        if (id == null) return null;

        if (!artistCache.ContainsKey(id))
        {
            lock (webAppServiceLock)
            {
                artistCache[id] = WebAppService.GetArtist(id);
            }
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
        if (webAppService != null)
        {
            logger.Info("LoadAndIndexAllSavedTracks start");

            AllSavedAlbumList = new List<SpotifyData.Album>(); // prevents reentrancy

            for (var retries = 0; retries < 20; retries++)
            {
                try
                {
                    Paging<SavedAlbum> pagedAlbums;

                    lock (webAppServiceLock)
                    {
                        pagedAlbums = WebAppService.GetSavedAlbums();
                    }
                    if (pagedAlbums.HasError() && pagedAlbums.Error.Status == 429)
                    {
                        logger.Info("LoadAndIndexAllSavedTracks rate limited");
                        System.Threading.Thread.Sleep(1000);
                        continue;
                    }
                    AllSavedAlbumList = MakeAlbums(
                        pagedAlbums,
                        next => WebAppService.DownloadData<Paging<SavedAlbum>>(next));

                    AllSavedAlbums = AllSavedAlbumList.ToArray();

                    logger.Info("LoadAndIndexAllSavedTracks {0} albums", AllSavedAlbums.Count());

                    break;
                }
                catch (System.Exception ex)
                {
                    logger.Error(ex);
                }
            }

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
                try
                {
                    var batchOfIds = batch.Select(id => SimplifyId(id));
                    SeveralArtists batchOfArtists;
                    lock (webAppServiceLock)
                    {
                        batchOfArtists = WebAppService.GetSeveralArtists(batchOfIds.ToList());
                    }
                    if (batchOfArtists.Artists == null)
                    {
                        System.Threading.Thread.Sleep(2000);
                        lock (webAppServiceLock)
                        {
                            batchOfArtists = WebAppService.GetSeveralArtists(batchOfIds.ToList());
                        }
                    }
                    if (batchOfArtists.Artists != null)
                    {
                        foreach (var artist in batchOfArtists.Artists)
                        {
                            savedArtistList.Add(MakeArtist(artist));
                        }
                    }
                    logger.Info("LoadAndIndexAllSavedTracks {0}/{1} artists", savedArtistList.Count, artistIds.Count);

                    if (savedArtistList.Count == artistIds.Count)
                        break;
                }
                catch (System.Exception ex)
                {
                    logger.Error(ex);
                }
            }

            AllSavedArtists = savedArtistList.ToArray();

            Array.Sort(AllSavedAlbums, CompareAlbumByArtist);
            Array.Sort(AllSavedArtists, (a1, a2) => a1.Name.CompareTo(a2.Name));
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

    public static void Initialize()
    {
        Probe();
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
            logger.Info("SearchTracks {0}", name);

            try
            {
                Paging<FullTrack> tracks;
                lock (webAppServiceLock)
                {
                    tracks = WebAppService.SearchItems(HttpUtility.UrlEncode(name), SearchType.Track, limit: 50).Tracks;
                }
                    return MakeTracks( tracks,
                                       next => WebAppService.DownloadData<SearchItem>(next).Tracks);
            }
            catch (System.Exception ex)
            {
                logger.Error(ex);
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
            logger.Info("SearchAlbums {0}", name);

            try
            {
                Paging<SimpleAlbum> albums;
                lock (webAppServiceLock)
                {
                    albums = WebAppService.SearchItems(HttpUtility.UrlEncode(name), SearchType.Album).Albums;
                }
                return MakeAlbums(
                    albums,
                    next => WebAppService.DownloadData<SearchItem>(next).Albums);
            }
            catch (System.Exception ex)
            {
                logger.Error(ex);
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
            logger.Info("SearchArtists {0}", name);

            try
            {
                Paging<FullArtist> artists;
                lock (webAppServiceLock)
                {
                    artists = WebAppService.SearchItems(HttpUtility.UrlEncode(name), SearchType.Artist, limit: 50).Artists;
                }
                return MakeArtists(
                    artists,
                    next => WebAppService.DownloadData<SearchItem>(next).Artists);

            }
            catch (System.Exception ex)
            {
                logger.Error(ex);
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
            lock (webAppServiceLock)
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
            lock (webAppServiceLock)
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
            lock (webAppServiceLock)
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
            try
            {
                var artist = GetArtistById(id);
                ArtistHistory.Add(artist.Name, artist.Id);

                Paging<SimpleAlbum> albums;
                lock (webAppServiceLock)
                {
                    albums = WebAppService.GetArtistsAlbums(SimplifyId(id), AlbumType.All, market: PreferredMarket, limit: 50);
                }
                return MakeAlbums(
                    albums,
                    next => WebAppService.DownloadData<Paging<SimpleAlbum>>(next));

            }
            catch (System.Exception ex)
            {
                logger.Error(ex);
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
            lock (webAppServiceLock)
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
            try
            {
                Paging<SimpleTrack> tracks;
                lock (webAppServiceLock)
                {
                    tracks = WebAppService.GetAlbumTracks(SimplifyId(id), market: PreferredMarket, limit: 50);
                }
                return MakeTracks(
                        tracks,
                        GetFullAlbum(SimplifyId(id)),
                        next => WebAppService.DownloadData<Paging<SimpleTrack>>(next));
            }
            catch (System.Exception ex)
            {
                logger.Error(ex);
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
            lock (webAppServiceLock)
            {
                try
                {
                    var a = GetFullAlbum(SimplifyId(id));
                    if (a != null && a.Images.Count != 0)
                    {
                        return a.Images[0].Url;
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
            try
            {
                Paging<SimplePlaylist> pagingPlaylist;

                lock (webAppServiceLock)
                {
                    pagingPlaylist = WebAppService.GetUserPlaylists(webApiCurrentUserId);
                }
                var playlists = MakePlaylists(
                    pagingPlaylist,
                    next => WebAppService.DownloadData<Paging<SimplePlaylist>>(next));
                CurrentPlaylists = playlists.ToDictionary(p => p.Name);
                return playlists;
            }
            catch (System.Exception ex)
            {
                logger.Error(ex);
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
            try
            {
                Paging<PlaylistTrack> tracks;
                lock (webAppServiceLock)
                {
                    tracks = WebAppService.GetPlaylistTracks(SimplifyId(id), market: PreferredMarket);
                }
                return MakeTracks(
                    tracks,
                    next => WebAppService.DownloadData<Paging<PlaylistTrack>>(next));
            }
            catch (System.Exception ex)
            {
                logger.Error(ex);
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
            try
            {
                Paging<PlaylistTrack> pagingTracks;
                lock (webAppServiceLock)
                {
                    pagingTracks = WebAppService.GetPlaylistTracks(SimplifyId(id), market: PreferredMarket);
                }
                var tracks = MakeTracks(
                    pagingTracks,
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
            lock (webAppServiceLock)
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
            lock (webAppServiceLock)
            {
                try
                {
                    WebAppService.UnfollowPlaylist(SimplifyId(id));
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
            lock (webAppServiceLock)
            {
                try
                {
                    WebAppService.UpdatePlaylist(SimplifyId(id), newName:newName);
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
            lock (webAppServiceLock)
            {
                try
                {
                    WebAppService.AddPlaylistTrack( SimplifyId(playlistId), trackId );
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
            lock (webAppServiceLock)
            {
                try
                {
                    var tracks = WebAppService.GetAlbumTracks(SimplifyId(albumId), market:PreferredMarket);
                    for (; ; )
                    {
                        WebAppService.AddPlaylistTracks(SimplifyId(playlistId), tracks.Items.Select(t => t.Uri).ToList());
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
            lock (webAppServiceLock)
            {
                try
                {
                    WebAppService.RemovePlaylistTracks( SimplifyId(playlistId), new List<DeleteTrackUri> { new DeleteTrackUri(trackId) });
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
            lock (webAppServiceLock)
            {
                try
                {
                    var tracks = WebAppService.GetAlbumTracks(SimplifyId(albumId), market:PreferredMarket);
                    for (; ; )
                    {
                        WebAppService.RemovePlaylistTracks(SimplifyId(playlistId), tracks.Items.Select(t => new DeleteTrackUri(t.Uri)).ToList());
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
    /// Save the  identified album as a saved album
    /// </summary>
    /// <param name="albumId"></param>
    public static void AddSavedAlbum(
        string albumId)
    {
        if (WebAppService != null)
        {
            lock (webAppServiceLock)
            {
                try
                {
                    WebAppService.SaveAlbum(SimplifyId(albumId));
                    AllSavedAlbumList = null;
                }
                catch (System.Exception ex)
                {
                    logger.Error(ex);
                }
            }
        }
    }

    /// <summary>
    /// Remove the identified album as a saved album
    /// </summary>
    /// <param name="albumId"></param>
    public static void RemoveSavedAlbum(
        string albumId)
    {
        if (WebAppService != null)
        {
            lock (webAppServiceLock)
            {
                try
                {
                    WebAppService.RemoveSavedAlbums(new List<string> { SimplifyId(albumId) });
                    AllSavedAlbumList = null;
                }
                catch (System.Exception ex)
                {
                    logger.Error(ex);
                }
            }
        }
    }

    /// <summary>
    /// Determine if all the identified album is saved
    /// </summary>
    /// <param name="albumId"></param>
    public static Boolean IsSavedAlbum(
        string albumId)
    {
        if (WebAppService != null)
        {
            lock (webAppServiceLock)
            {
                try
                {
                    var saveIndications = WebAppService.CheckSavedAlbums(new List<string> { SimplifyId(albumId) });
                    return saveIndications.List[0];
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

    static string GetPlaybackDevice()
    {
        if (playbackDevice == null)
        {
            var devices = WebAppService.GetDevices();
            if (devices != null && devices.Devices != null)
            {
                foreach (var dev in devices.Devices)
                {
                    logger.Info($"Spotify play device found: ${dev.Name}");
                    if (dev.Type == "Computer" && dev.Name == HttpContext.Current.Server.MachineName)
                    {
                        playbackDevice = dev.Id;
                    }
                }
            }
        }

        return playbackDevice;
    }

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
        if (WebAppService != null)
        {
            lock (webAppServiceLock)
            {
                try
                {
                    if (append)
                    {
                        WebAppService.AddToQueue(id);
                    }
                    else
                    {
                        WebAppService.ResumePlayback(deviceId: GetPlaybackDevice(), uris: new List<string> { id }, offset: 0);
                    }

                    return true;
                }
                catch (System.Exception ex)
                {
                    logger.Error(ex);
                }
            }
        }
        return false;
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
        if (WebAppService != null)
        {
            try
            {
                if (append)
                {
                    Paging<SimpleTrack> pagingTracks;
                    lock (webAppServiceLock)
                    {
                        pagingTracks = WebAppService.GetAlbumTracks(SimplifyId(id), market: PreferredMarket, limit: 50);
                    }
                    var tracks = MakeTracks(pagingTracks,
                                GetFullAlbum(SimplifyId(id)),
                                next => WebAppService.DownloadData<Paging<SimpleTrack>>(next));
                    lock (webAppServiceLock)
                    {
                        foreach (var t in tracks)
                        {
                            WebAppService.AddToQueue(t.Id);
                        }
                    }
                }
                else
                {
                    lock (webAppServiceLock)
                    {
                        WebAppService.ResumePlayback(deviceId: GetPlaybackDevice(), contextUri: id, offset: 0);
                    }
                }

                return true;
            }
            catch (System.Exception ex)
            {
                logger.Error(ex);
            }
        }
        return false;
    }

    /// <summary>
    /// Play all tracks of the identified playlist, either immediately or after the currently queued tracks
    /// </summary>
    /// <param name="id"></param>
    /// <param name="append"></param>
    /// <returns></returns>
    public static Boolean PlayPlaylist(
        string id,
        bool append = false)
    {
        if (WebAppService != null)
        {
            try
            {
                if (append)
                {
                    Paging<PlaylistTrack> pagingTracks;
                    lock (webAppServiceLock)
                    {
                        pagingTracks = WebAppService.GetPlaylistTracks(SimplifyId(id), market: PreferredMarket, limit: 50);
                    }
                    var tracks = MakeTracks(pagingTracks,
                                next => WebAppService.DownloadData<Paging<PlaylistTrack>>(next));
                    lock (webAppServiceLock)
                    {
                        foreach (var t in tracks)
                        {
                            WebAppService.AddToQueue(t.Id);
                        }
                    }
                }
                else
                {
                    lock (webAppServiceLock)
                    {
                        WebAppService.ResumePlayback(deviceId: GetPlaybackDevice(), contextUri: id, offset: 0);
                    }
                }

                return true;
            }
            catch (System.Exception ex)
            {
                logger.Error(ex);
            }
        }
        return false;
    }

    /// <summary>
    /// Get the currently playing track
    /// </summary>
    /// <returns></returns>
    public static SpotifyData.Track GetCurrentTrack()
    {
        if (WebAppService != null)
        {
            lock (webAppServiceLock)
            {
                try
                {
                    PlaybackContext playingTrack = WebAppService.GetPlayingTrack();
                    return MakeTrack(playingTrack.Item);
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
    /// Get the collection of all queued tracks
    /// </summary>
    /// <remarks>
    /// This queued list only contains the tracks from the original context (album or playlist) and takes no account of any 
    /// tracks subsequently added to the queue
    /// </remarks>
    /// <returns></returns>
    public static IEnumerable<SpotifyData.Track> GetQueuedTracks()
    {
        if (WebAppService != null)
        {
            try
            {
                var playback = WebAppService.GetPlayback();
                if (playback.Context != null)
                {
                    if (playback.Context.Type == "album")
                    {
                        Paging<SimpleTrack> pagingTracks;
                        lock (webAppServiceLock)
                        {
                            pagingTracks = WebAppService.GetAlbumTracks(SimplifyId(playback.Context.Uri), market: PreferredMarket, limit: 50);
                        }
                        return MakeTracks(
                            pagingTracks,
                            GetFullAlbum(SimplifyId(playback.Context.Uri)),
                            next => WebAppService.DownloadData<Paging<SimpleTrack>>(next));
                    }
                    if (playback.Context.Type == "playlist")
                    {
                        Paging<PlaylistTrack> pagingTracks;
                        lock (webAppServiceLock)
                        {
                            pagingTracks = WebAppService.GetPlaylistTracks(SimplifyId(playback.Context.Uri), market: PreferredMarket, limit: 50);
                        }
                        return MakeTracks(
                            pagingTracks,
                            next => WebAppService.DownloadData<Paging<PlaylistTrack>>(next));
                    }
                }

                if (playback.Item != null)
                {
                    return new List<SpotifyData.Track> { MakeTrack(GetFullTrack(SimplifyId(playback.Item.Uri))) };
                }
            }
            catch (System.Exception ex)
            {
                logger.Error(ex);
            }
        }
        return new List<SpotifyData.Track>();
    }

    /// <summary>
    /// Skip to a specified queued track
    /// </summary>
    public static SpotifyData.Track SkipToQueuedTrack(
        string id)
    {
        var playback = WebAppService.GetPlayback();
        WebAppService.ResumePlayback(deviceId: playback.Device.Id, contextUri: playback.Context.Uri, null, id, 0);
        return GetCurrentTrack();
    }

    /// <summary>
    /// Remove the specified queued track from the queue
    /// </summary>
    public static SpotifyData.Track RemoveQueuedTrack(
        string id)
    {
        return GetCurrentTrack();
    }
    #endregion

    #region Player currently playing track operations
    /// <summary>
    /// Skip playing forwards to the next queued track
    /// </summary>
    /// <returns></returns>
    public static int Skip()
    {
        if (WebAppService != null)
        {
            lock (webAppServiceLock)
            {
                try
                {
                    WebAppService.SkipPlaybackToNext();
                    return 0;
                }
                catch (System.Exception ex)
                {
                    logger.Error(ex);
                }
            }
        }
        return -1;
    }

    /// <summary>
    /// Skip playing backwards to the previous queued track
    /// </summary>
    /// <returns></returns>
    public static int Back()
    {
        if (WebAppService != null)
        {
            lock (webAppServiceLock)
            {
                try
                {
                    WebAppService.SkipPlaybackToPrevious();
                    return 0;
                }
                catch (System.Exception ex)
                {
                    logger.Error(ex);
                }
            }
        }
        return -1;
    }

    /// <summary>
    /// Start or continue playing the current track
    /// </summary>
    /// <returns></returns>
    public static int Play()
    {
        if (WebAppService != null)
        {
            lock (webAppServiceLock)
            {
                try
                {
                    var playback = WebAppService.GetPlayback();
                    if (!playback.IsPlaying && playback.Item != null)
                    {
                        if (playback.Context != null)
                            WebAppService.ResumePlayback(deviceId: playback.Device.Id, contextUri: playback.Context.Uri, null, playback.Item.Uri, playback.ProgressMs);
                        else
                            WebAppService.ResumePlayback(deviceId: playback.Device.Id, contextUri: null, new List<String> { playback.Item.Uri }, 0, playback.ProgressMs);
                    }
                    return 0;
                }
                catch (System.Exception ex)
                {
                    logger.Error(ex);
                }
            }
        }
        return -1;
    }

    /// <summary>
    /// Pause playing the current track
    /// </summary>
    /// <returns></returns>
    public static int Pause()
    {
        if (WebAppService != null)
        {
            lock (webAppServiceLock)
            {
                try
                {
                    WebAppService.PausePlayback();
                    return 0;
                }
                catch (System.Exception ex)
                {
                    logger.Error(ex);
                }
            }
        }
        return -1;
    }

    /// <summary>
    /// Is the player playing a track?
    /// </summary>
    /// <returns>+ve: Playing; 0: Paused; -ve: Stolen by another session</returns>
    public static int GetPlaying()
    {
        if (WebAppService != null)
        {
            lock (webAppServiceLock)
            {
                try
                {
                    var playback = WebAppService.GetPlayback();
                    return playback == null ? -1 : playback.IsPlaying ? 1 : 0;
                }
                catch (System.Exception ex)
                {
                    logger.Error(ex);
                }
            }
        }
        return -1;
    }

    /// <summary>
    /// Stop playing the current track
    /// </summary>
    /// <returns></returns>
    public static int Stop()
    {
        if (WebAppService != null)
        {
            lock (webAppServiceLock)
            {
                try
                {
                    WebAppService.PausePlayback();
                    return 0;
                }
                catch (System.Exception ex)
                {
                    logger.Error(ex);
                }
            }
        }
        return -1;
    }

    /// <summary>
    /// Get the position at which the current track is playing
    /// </summary>
    /// <returns>Position in seconds</returns>
    public static int GetPosition()
    {
        if (WebAppService != null)
        {
            lock (webAppServiceLock)
            {
                try
                {
                    var playback = WebAppService.GetPlayback();
                    return playback == null ? -1 : playback.ProgressMs/1000;
                }
                catch (System.Exception ex)
                {
                    logger.Error(ex);
                }
            }
        }
        return -1;
    }

    /// <summary>
    /// Seek to a particular position within the currently playing track
    /// </summary>
    /// <param name="pos">Position in seconds</param>
    /// <returns></returns>
    public static int SetPosition(
        int pos)
    {
        if (WebAppService != null)
        {
            lock (webAppServiceLock)
            {
                try
                {
                    WebAppService.SeekPlayback( pos * 1000);
                    return pos;
                }
                catch (System.Exception ex)
                {
                    logger.Error(ex);
                }
            }
        }
        return -1;
    }

    public static void ExitPlayer()
    {
        Stop();
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
        Paging<FullArtist> col,
        Func<String, Paging<FullArtist>> ReadNext)
    {
        List<SpotifyData.Artist> result = new List<SpotifyData.Artist>();
        int noFound = 0;

        while (col != null && noFound < 200)
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
                Paging<FullArtist> newCol;
                lock (webAppServiceLock)
                {
                    newCol = ReadNext(col.Next);
                }
                if (newCol != null && retries > 0 && newCol.HasError())
                {
                    logger.Info("Paged artists error - {0} - {1} [{2} found]",
                        newCol.Error.Status, newCol.Error.Message, noFound);
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
        if (DateTime.TryParse(album.ReleaseDate, out DateTime released))
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
            Year = ReleaseYear(album),
            TrackCount = album.TotalTracks
        };
    }

    /// <summary>
    /// Make a collection of external Album structures from Paging<SimpleAlbum> data returned by the Web API
    /// </summary>
    /// <param name="col"></param>
    /// <param name="ReadNext"></param>
    /// <returns></returns>
    static IEnumerable<SpotifyData.Album> MakeAlbums(
        Paging<SimpleAlbum> col,
        Func<String, Paging<SimpleAlbum>> ReadNext) => MakeAlbums(col, ReadNext, a => a.Id);

    /// <summary>
    /// Make a collection of external Album structures from Paging<SavedAlbum> data returned by the Web API
    /// </summary>
    /// <param name="col"></param>
    /// <param name="ReadNext"></param>
    /// <returns></returns>
    static IEnumerable<SpotifyData.Album> MakeAlbums(
        Paging<SavedAlbum> col,
        Func<String, Paging<SavedAlbum>> ReadNext) => MakeAlbums(col, ReadNext, a => a.Album.Id);

    /// <summary>
    /// Make a collection of external Album structures from Paging data returned by the Web API
    /// </summary>
    /// <param name="col"></param>
    /// <param name="ReadNext"></param>
    /// <returns></returns>
    static IEnumerable<SpotifyData.Album> MakeAlbums<T>(
        Paging<T> col,
        Func<String, Paging<T>> ReadNext,
        Func<T, string> GetAlbumId)
    {
        List<SpotifyData.Album> result = new List<SpotifyData.Album>();
        int noFound = 0;

        while (col != null && noFound < 200)
        {
            if (col.Items == null) return null;
            var albumIds = col.Items.Select(a => GetAlbumId(a)).ToList();
            List<FullAlbum> severalAlbums;
            lock (webAppServiceLock)
            {
                severalAlbums = WebAppService.GetSeveralAlbums(albumIds, market: PreferredMarket).Albums;
            }

            if (severalAlbums != null)
            {
                foreach (var a in severalAlbums)
                {
                    if (a != null)
                    {
                        result.Add(MakeAlbum(a));
                    }
                    noFound++;
                }
            }

            if (col.Next == null) break;

            for (var retries = 5; retries >= 0; retries--)
            {
                Paging<T> newCol;
                lock (webAppServiceLock)
                {
                    newCol = ReadNext(col.Next);
                }
                if (newCol != null && retries > 0 && newCol.HasError())
                {
                    logger.Info("Paged albums error - {0} - {1} [{2} found]",
                        newCol.Error.Status, newCol.Error.Message, noFound);
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
        if (album == null && track != null && track.Album != null)
        {
            album = GetFullAlbum(track.Album.Id);
        }
        try
        {
            var noFullAlbum = album == null || album.Artists == null || album.Artists.Count == 0;
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
                Count = noFullAlbum ? 0 : album.TotalTracks,
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

        while (col != null && noFound < 200)
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
                Paging<FullTrack> newCol;
                lock (webAppServiceLock)
                {
                    newCol = ReadNext(col.Next);
                }
                if (newCol != null && retries > 0 && newCol.HasError())
                {
                    logger.Info("Paged tracks error - {0} - {1} [{2} found]",
                        newCol.Error.Status, newCol.Error.Message, noFound);
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

        while (col != null && noFound < 200)
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
                Paging<SimpleTrack> newCol;
                lock (webAppServiceLock)
                {
                    newCol = ReadNext(col.Next);
                }
                if (newCol != null && retries > 0 && newCol.HasError())
                {
                    logger.Info("Paged tracks error - {0} - {1} [{2} found]",
                        newCol.Error.Status, newCol.Error.Message, noFound);
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

        while (col != null)  //  Unbounded within a playlist or saved tracks
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
                Paging<PlaylistTrack> newCol;
                lock (webAppServiceLock)
                {
                    newCol = ReadNext(col.Next);
                }
                if (newCol != null && retries > 0 && newCol.HasError())
                {
                    logger.Info("Paged tracks error - {0} - {1} [{2} found]",
                        newCol.Error.Status, newCol.Error.Message, noFound);
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

        while (col != null && noFound < 200)
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
                Paging<SimplePlaylist> newCol;
                lock (webAppServiceLock)
                {
                    newCol = ReadNext(col.Next);
                }
                if (newCol != null && retries > 0 && newCol.HasError())
                {
                    logger.Info("Paged playlists error - {0} - {1} [{2} found]",
                        newCol.Error.Status, newCol.Error.Message, noFound);
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

using Avid.Spotify;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net.Http; 

/// <summary>
/// Class of static methods to access the Spotify player through its WebAPI interface
/// </summary>
public static class Spotify
{
    static HttpClient client = new HttpClient();

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
        client.BaseAddress = new Uri("http://localhost:8383");
        client.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue();
        client.DefaultRequestHeaders.CacheControl.NoCache = true;
        client.DefaultRequestHeaders.CacheControl.MaxAge = new TimeSpan(0);

    }

    /// <summary>
    /// Search Spotify for up to 50 tracks matching the specified track name
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static IEnumerable<SpotifyData.Track> SearchTracks(
        string name)
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/browse/SearchTracks?name={0}", name)).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<IEnumerable<SpotifyData.Track>>().Result;
    }

    /// <summary>
    /// Search Spotify for up to 50 albums matching the specified album name
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static IEnumerable<SpotifyData.Album> SearchAlbums(
        string name)
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/browse/SearchAlbums?name={0}", name)).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<IEnumerable<SpotifyData.Album>>().Result;
    }

    /// <summary>
    /// Search Spotify for up to 50 artists matching the specified artist name
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static IEnumerable<SpotifyData.Artist> SearchArtists(
        string name)
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/browse/SearchArtists?name={0}", name)).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<IEnumerable<SpotifyData.Artist>>().Result;
    }

    /// <summary>
    /// Return cached track data for a tracks identified by a non-persistent cache Id
    /// </summary>
    /// <param name="id">The non-persistent cache Id</param>
    /// <returns></returns>
    public static SpotifyData.Track GetTrackById(
        int id)
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/browse/GetTrackById/{0}", id)).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<SpotifyData.Track>().Result;
    }

    /// <summary>
    /// Return cached album data for a tracks identified by a non-persistent cache Id
    /// </summary>
    /// <param name="id">The non-persistent cache Id</param>
    /// <returns></returns>
    public static SpotifyData.Album GetAlbumById(
        int id)
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/browse/GetAlbumById/{0}", id)).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<SpotifyData.Album>().Result;
    }

    /// <summary>
    /// Return cached artist data for a tracks identified by a non-persistent cache Id
    /// </summary>
    /// <param name="id">The non-persistent cache Id</param>
    /// <returns></returns>
    public static SpotifyData.Artist GetArtistById(
        int id)
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/browse/GetArtistById/{0}", id)).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<SpotifyData.Artist>().Result;
    }

    /// <summary>
    /// Get the collection of albums for an identified artist
    /// </summary>
    /// <param name="id">The non-persistent cache Id</param>
    /// <returns></returns>
    public static IEnumerable<SpotifyData.Album> GetAlbumsForArtist(
        int id)
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/browse/GetAlbumsForArtist/{0}", id)).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<IEnumerable<SpotifyData.Album>>().Result;
    }

    /// <summary>
    /// Get the collection of similar artists for an identified artist
    /// </summary>
    /// <param name="id">The non-persistent cache Id</param>
    /// <returns></returns>
    public static IEnumerable<SpotifyData.Artist> GetSimilarArtistsForArtist(
        int id)
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/browse/GetSimilarArtistsForArtist/{0}", id)).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<IEnumerable<SpotifyData.Artist>>().Result;
    }

    /// <summary>
    /// Get the collection of tracks for an identified album
    /// </summary>
    /// <param name="id">The non-persistent cache Id</param>
    /// <returns></returns>
    public static IEnumerable<SpotifyData.Track> GetTracksForAlbum(
        int id)
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/browse/GetTracksForAlbum/{0}", id)).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<IEnumerable<SpotifyData.Track>>().Result;
    }

    /// <summary>
    /// Get a PNG image as streamed data for the image file for an identified album
    /// </summary>
    /// <param name="id">The non-persistent cache Id</param>
    /// <returns>An HTTP response representing the content of the requested image file</returns>
    public static byte[] GetAlbumImage(
       int id)
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/browse/GetAlbumImage/{0}", id)).Result;

        resp.EnsureSuccessStatusCode();
        return resp.Content.ReadAsByteArrayAsync().Result;
    }

    /// <summary>
    /// Get the collection of named playlists, rebuilding from data on Spotify
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<string> GetPlayLists()
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/playlist/GetPlayLists")).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<IEnumerable<string>>().Result;
    }

    /// <summary>
    /// Get the collection of tracks for a named playlist
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static IEnumerable<SpotifyData.Track> GetPlayListTracks(
        string name)
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/playlist/GetPlayListTracks/{0}", name)).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<IEnumerable<SpotifyData.Track>>().Result;
    }

    /// <summary>
    /// Get the collection of albums for a named playlist
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static IEnumerable<SpotifyData.Album> GetPlayListAlbums(
        string name)
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/playlist/GetPlayListAlbums?name={0}", HttpUtility.UrlEncode(name))).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<IEnumerable<SpotifyData.Album>>().Result;
    }

    /// <summary>
    /// Add a new (empty) named playlist
    /// </summary>
    /// <param name="name"></param>
    public static void AddPlayList(
        string name)
    {
        //  THIS WILL FAIL AS IT USES UNIMPLEMENTED SPOTIFIRE METHODS
        HttpResponseMessage resp = client.GetAsync(string.Format("api/playlist/AddPlayList?name={0}", name)).Result;
        resp.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Delete a named playlist
    /// </summary>
    /// <param name="name"></param>
    public static void DeletePlayList(
        string name)
    {
        //  THIS WILL FAIL AS IT USES UNIMPLEMENTED SPOTIFIRE METHODS
        HttpResponseMessage resp = client.GetAsync(string.Format("api/playlist/DeletePlayList?name={0}", name)).Result;
        resp.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Rename a playlist
    /// </summary>
    /// <param name="oldName"></param>
    /// <param name="newName"></param>
    public static void RenamePlayList(
        string oldName,
        string newName)
    {
        //  THIS WILL FAIL AS IT USES UNIMPLEMENTED SPOTIFIRE METHODS
        HttpResponseMessage resp = client.GetAsync(string.Format("api/playlist/RenamePlayList?oldName={0}&newName={1}", oldName, newName)).Result;
        resp.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Add an identified track to a named playlist
    /// </summary>
    /// <param name="name"></param>
    /// <param name="id"></param>
    public static void AddTrackToPlayList(
        string name,
        int id)
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/playlist/AddTrackToPlayList?name={0}&id={1}", name, id)).Result;
        resp.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Add all the tracks of an identified album to a named playlist
    /// </summary>
    /// <param name="name"></param>
    /// <param name="id"></param>
    public static void AddAlbumToPlayList(
        string name,
        int id)
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/playlist/AddAlbumToPlayList?name={0}&id={1}", name, id)).Result;
        resp.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Remove an identified track from a named playlist
    /// </summary>
    /// <param name="name"></param>
    /// <param name="id"></param>
    public static void RemoveTrackFromPlayList(
        string name,
        int id)
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/playlist/RemoveTrackFromPlayList?name={0}&id={1}", name, id)).Result;
        resp.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Remove all the tracks of an identified album from a named playlist
    /// </summary>
    /// <param name="name"></param>
    /// <param name="id"></param>
    public static void RemoveAlbumFromPlayList(
        string name,
        int id)
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/playlist/RemoveAlbumFromPlayList?name={0}&id={1}", name, id)).Result;
        resp.EnsureSuccessStatusCode();
    }
    /// <summary>
    /// Play the identified track, either immediately or after the currently queued tracks
    /// </summary>
    /// <param name="id"></param>
    /// <param name="append"></param>
    /// <returns></returns>

    public static SpotifyData.Track PlayTrack(
        int id,
        bool append = false)
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/playqueue/PlayTrack/{0}?append={1}", id, append)).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<SpotifyData.Track>().Result;
    }

    /// <summary>
    /// Play all tracks of the identified album, either immediately or after the currently queued tracks
    /// </summary>
    /// <param name="id"></param>
    /// <param name="append"></param>
    /// <returns></returns>
    public static SpotifyData.Album PlayAlbum(
        int id,
        bool append = false)
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/playqueue/PlayAlbum/{0}?append={1}", id, append)).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<SpotifyData.Album>().Result;
    }

    /// <summary>
    /// Get the currently playing track
    /// </summary>
    /// <returns></returns>
    public static SpotifyData.Track GetCurrentTrack()
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/playqueue/GetCurrentTrack")).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<SpotifyData.Track>().Result;
    }

    /// <summary>
    /// Get the collection of all queued tracks
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<SpotifyData.Track> GetQueuedTracks()
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/playqueue/GetQueuedTracks")).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<IEnumerable<SpotifyData.Track>>().Result;
    }

    /// <summary>
    /// Skip to a specified queued track
    /// </summary>
    public static SpotifyData.Track SkipToQueuedTrack(
        int id)
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/playqueue/SkipToQueuedTrack/{0}", id)).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<SpotifyData.Track>().Result;
    }

    /// <summary>
    /// Remove the specified queued track from the queue
    /// </summary>
    public static SpotifyData.Track RemoveQueuedTrack(
        int id)
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/playqueue/RemoveQueuedTrack/{0}", id)).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<SpotifyData.Track>().Result;
    }

    /// <summary>
    /// Skip playing forwards to the next queued track
    /// </summary>
    /// <returns></returns>
    public static int Skip()
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/playqueue/Skip")).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<int>().Result;
    }

    /// <summary>
    /// Skip playing backwards to the previous queued track
    /// </summary>
    /// <returns></returns>
    public static int Back()
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/playqueue/Back")).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<int>().Result;
    }

    /// <summary>
    /// Start or continue playing the current track
    /// </summary>
    /// <returns></returns>
    public static int Play()
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/player/Play")).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<int>().Result;
    }

    /// <summary>
    /// Pause playing the current track
    /// </summary>
    /// <returns></returns>
    public static int Pause()
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/player/Pause")).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<int>().Result;
    }

    /// <summary>
    /// Is the player playing a track?
    /// </summary>
    /// <returns>+ve: Playing; 0: Paused; -ve: Stolen by another session</returns>
    public static int GetPlaying()
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/player/GetPlaying")).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<int>().Result;
    }

    /// <summary>
    /// Stop playing the current track
    /// </summary>
    /// <returns></returns>
    public static int Stop()
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/player/Stop")).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<int>().Result;
    }

    /// <summary>
    /// Get the position at which the current track is playing
    /// </summary>
    /// <returns>Position in seconds</returns>
    public static int GetPosition()
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/player/GetPosition")).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<int>().Result;
    }

    /// <summary>
    /// Seek to a particular position within the currently playing track
    /// </summary>
    /// <param name="pos">Position in seconds</param>
    /// <returns></returns>
    public static int SetPosition(
        int pos)
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/player/SetPosition?pos={0}", pos)).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<int>().Result;
    }

}

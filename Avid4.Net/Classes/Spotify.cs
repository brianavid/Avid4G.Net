using Avid.Spotify;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net.Http; 

public static class Spotify
{
    static HttpClient client = new HttpClient();

    public static string FormatDuration(int seconds)
    {
        return seconds < 0 ? "<0:00" : string.Format("{0}:{1:00}", seconds / 60, seconds % 60);
    }

    public static void Initialize()
    {
        client.BaseAddress = new Uri("http://localhost:8383");
        client.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue();
        client.DefaultRequestHeaders.CacheControl.NoCache = true;
        client.DefaultRequestHeaders.CacheControl.MaxAge = new TimeSpan(0);

    }

    public static IEnumerable<SpotifyData.Track> SearchTracks(
        string name)
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/browse/SearchTracks?name={0}", name)).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<IEnumerable<SpotifyData.Track>>().Result;
    }

    public static IEnumerable<SpotifyData.Album> SearchAlbums(
        string name)
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/browse/SearchAlbums?name={0}", name)).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<IEnumerable<SpotifyData.Album>>().Result;
    }

    public static IEnumerable<SpotifyData.Artist> SearchArtists(
        string name)
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/browse/SearchArtists?name={0}", name)).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<IEnumerable<SpotifyData.Artist>>().Result;
    }

    public static SpotifyData.Track GetTrackById(
        int id)
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/browse/GetTrackById/{0}", id)).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<SpotifyData.Track>().Result;
    }

    public static SpotifyData.Album GetAlbumById(
        int id)
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/browse/GetAlbumById/{0}", id)).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<SpotifyData.Album>().Result;
    }

    public static SpotifyData.Artist GetArtistById(
        int id)
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/browse/GetArtistById/{0}", id)).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<SpotifyData.Artist>().Result;
    }

    public static IEnumerable<SpotifyData.Album> GetAlbumsForArtist(
        int id)
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/browse/GetAlbumsForArtist/{0}", id)).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<IEnumerable<SpotifyData.Album>>().Result;
    }

    public static IEnumerable<SpotifyData.Artist> GetSimilarArtistsForArtist(
        int id)
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/browse/GetSimilarArtistsForArtist/{0}", id)).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<IEnumerable<SpotifyData.Artist>>().Result;
    }

    public static IEnumerable<SpotifyData.Track> GetTracksForAlbum(
        int id)
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/browse/GetTracksForAlbum/{0}", id)).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<IEnumerable<SpotifyData.Track>>().Result;
    }

    public static IEnumerable<string> GetPlayLists()
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/playlist/GetPlayLists")).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<IEnumerable<string>>().Result;
    }

    public static IEnumerable<SpotifyData.Track> GetPlayListTracks(
        string name)
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/playlist/GetPlayListTracks/{0}", name)).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<IEnumerable<SpotifyData.Track>>().Result;
    }

    public static IEnumerable<SpotifyData.Album> GetPlayListAlbums(
        string name)
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/playlist/GetPlayListAlbums?name={0}", HttpUtility.UrlEncode(name))).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<IEnumerable<SpotifyData.Album>>().Result;
    }

    public static SpotifyData.Track PlayTrack(
        int id,
        bool append = false)
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/playqueue/PlayTrack/{0}?append={1}", id, append)).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<SpotifyData.Track>().Result;
    }

    public static SpotifyData.Album PlayAlbum(
        int id,
        bool append = false)
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/playqueue/PlayAlbum/{0}?append={1}", id, append)).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<SpotifyData.Album>().Result;
    }

    public static SpotifyData.Track GetCurrentTrack()
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/playqueue/GetCurrentTrack")).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<SpotifyData.Track>().Result;
    }

    public static IEnumerable<SpotifyData.Track> GetQueuedTracks()
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/playqueue/GetQueuedTracks")).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<IEnumerable<SpotifyData.Track>>().Result;
    }

    public static SpotifyData.Track SkipToQueuedTrack(
        int id)
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/playqueue/SkipToQueuedTrack/{0}", id)).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<SpotifyData.Track>().Result;
    }

    public static SpotifyData.Track RemoveQueuedTrack(
        int id)
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/playqueue/RemoveQueuedTrack/{0}", id)).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<SpotifyData.Track>().Result;
    }

    public static byte[] GetAlbumImage(
        int id)
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/browse/GetAlbumImage/{0}", id)).Result;

        resp.EnsureSuccessStatusCode();
        return resp.Content.ReadAsByteArrayAsync().Result;
    }

    public static int Play()
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/player/Play")).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<int>().Result;
    }

    public static int Pause()
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/player/Pause")).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<int>().Result;
    }

    public static int GetPlaying()
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/player/GetPlaying")).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<int>().Result;
    }

    public static int Stop()
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/player/Stop")).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<int>().Result;
    }

    public static int Skip()
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/player/Skip")).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<int>().Result;
    }

    public static int Back()
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/player/Back")).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<int>().Result;
    }

    public static int GetPosition()
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/player/GetPosition")).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<int>().Result;
    }

    public static int SetPosition(
        int pos)
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/player/SetPosition?pos={0}", pos)).Result;
        resp.EnsureSuccessStatusCode();

        return resp.Content.ReadAsAsync<int>().Result;
    }

    public static void AddPlayList(
        string name)
    {
        //  THIS WILL FAIL AS IT USES UNIMPLEMENTED SPOTIFIRE METHODS
        HttpResponseMessage resp = client.GetAsync(string.Format("api/playlist/AddPlayList?name={0}", name)).Result;
        resp.EnsureSuccessStatusCode();
    }

    public static void DeletePlayList(
        string name)
    {
        //  THIS WILL FAIL AS IT USES UNIMPLEMENTED SPOTIFIRE METHODS
        HttpResponseMessage resp = client.GetAsync(string.Format("api/playlist/DeletePlayList?name={0}", name)).Result;
        resp.EnsureSuccessStatusCode();
    }

    public static void RenamePlayList(
        string oldName,
        string newName)
    {
        //  THIS WILL FAIL AS IT USES UNIMPLEMENTED SPOTIFIRE METHODS
        HttpResponseMessage resp = client.GetAsync(string.Format("api/playlist/RenamePlayList?oldName={0}&newName={1}", oldName, newName)).Result;
        resp.EnsureSuccessStatusCode();
    }

    public static void AddTrackToPlayList(
        string name,
        int id)
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/playlist/AddTrackToPlayList?name={0}&id={1}", name, id)).Result;
        resp.EnsureSuccessStatusCode();
    }

    public static void AddAlbumToPlayList(
        string name,
        int id)
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/playlist/AddAlbumToPlayList?name={0}&id={1}", name, id)).Result;
        resp.EnsureSuccessStatusCode();
    }

    public static void RemoveTrackFromPlayList(
        string name,
        int id)
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/playlist/RemoveTrackFromPlayList?name={0}&id={1}", name, id)).Result;
        resp.EnsureSuccessStatusCode();
    }

    public static void RemoveAlbumFromPlayList(
        string name,
        int id)
    {
        HttpResponseMessage resp = client.GetAsync(string.Format("api/playlist/RemoveAlbumFromPlayList?name={0}&id={1}", name, id)).Result;
        resp.EnsureSuccessStatusCode();
    }

}

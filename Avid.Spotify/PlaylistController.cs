using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Web.Http;
using SpotiFire;
using System.IO;
using NLog;

namespace Avid.Spotify
{
    /// <summary>
    /// Web API Controller, with public HttpGet web methods for managing Playlists stored in Spotify for the authenicated user
    /// </summary>
    public class PlaylistController : ApiController
    {
        static Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The collection of named playlists
        /// </summary>
        static Dictionary<string, Playlist> playlists = null;

        /// <summary>
        /// Build the collection of named playlists stored in Spotify
        /// </summary>
        /// <returns></returns>
        static async Task BuildPlayLists()
        {
            playlists = new Dictionary<string, Playlist>();
            try
            {
                foreach (Playlist playlist in (await SpotifySession.Session.PlaylistContainer).Playlists)
                {
                    playlists[playlist.Name] = playlist;
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex);
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// The collection of named playlists, built if necessary
        /// </summary>
        static Dictionary<string, Playlist> Playlists
        {
            get
            {
                if (playlists == null)
                {
                    BuildPlayLists().Wait();
                }
                return playlists;
            }
        }

        /// <summary>
        /// Get the collection of named playlists, rebuilding from data on Spotify
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IEnumerable<string> GetPlayLists()
        {
            playlists = null;
            return Playlists.Keys;
        }

        /// <summary>
        /// Get the collection of tracks for a named playlist
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [HttpGet]
        public IEnumerable<SpotifyData.Track> GetPlayListTracks(
            string name)
        {
            Cache.Clear();
            if (!Playlists.ContainsKey(name))
            {
                return new SpotifyData.Track[0];
            }
            try
            {
                return GetPlayListTracksAsync(name).Result;
            }
            catch (Exception ex)
            {
                logger.Warn(ex);
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
            }
        }

        async Task<IEnumerable<SpotifyData.Track>> GetPlayListTracksAsync(
            string name)
        {
            return (await playlists[name]).Tracks.Where(t => t.IsAvailable).Select(t => MakeData.Track(t));
        }


        /// <summary>
        /// Get the collection of albums for a named playlist
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [HttpGet]
        public IEnumerable<SpotifyData.Album> GetPlayListAlbums(
            string name)
        {
            Cache.Clear();
            if (!Playlists.ContainsKey(name))
            {
                return new SpotifyData.Album[0];
            }
            try
            {
                return GetPlayListAlbumsAsync(name).Result;
            }
            catch (Exception ex)
            {
                logger.Warn(ex);
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
            }
        }

        async Task<IEnumerable<SpotifyData.Album>> GetPlayListAlbumsAsync(
            string name)
        {
            HashSet<Album> albums = new HashSet<Album>();
            foreach (Track track in (await playlists[name]).Tracks.Where(t => t.IsAvailable))
            {
                Album album = track.Album;
                if (!albums.Contains(album))
                {
                    albums.Add(album);
                }
            }

            return albums.Select(a => MakeData.Album(a));
        }

        /// <summary>
        /// Add a new (empty) named playlist
        /// </summary>
        /// <param name="name"></param>
        [HttpGet]
        public void AddPlayList(
            string name)
        {
            if (Playlists.ContainsKey(name))
            {
                return;
            }
            AddPlayListAsync(name).Wait();
        }

        async Task AddPlayListAsync(
            string name)
        {
            await((await SpotifySession.Session.PlaylistContainer).Playlists.Create(name));
            await BuildPlayLists();
        }

        /// <summary>
        /// Delete a named playlist
        /// </summary>
        /// <param name="name"></param>
        [HttpGet]
        public void DeletePlayList(
            string name)
        {
            //  THIS WILL FAIL AS IT USES UNIMPLEMENTED SPOTIFIRE METHODS
            if (!Playlists.ContainsKey(name))
            {
                return;
            }
            DeletePlayListAsync(name).Wait();
        }

        async Task DeletePlayListAsync(
            string name)
        {
            Playlist playlist = await playlists[name];
            (await SpotifySession.Session.PlaylistContainer).Playlists.Remove(playlist);
            await BuildPlayLists();
        }

        /// <summary>
        /// Rename a playlist
        /// </summary>
        /// <param name="oldName"></param>
        /// <param name="newName"></param>
        [HttpGet]
        public void RenamePlayList(
            string oldName,
            string newName)
        {
            //  THIS WILL FAIL AS IT USES UNIMPLEMENTED SPOTIFIRE METHODS
            if (!Playlists.ContainsKey(oldName))
            {
                return;
            }
            RenamePlayListAsync(oldName, newName).Wait();
        }

        async Task RenamePlayListAsync(
            string oldName,
            string newName)
        {
            Playlist playlist = await playlists[oldName];
            playlist.Name = newName;
            await BuildPlayLists();
        }


        /// <summary>
        /// Add an identified track to a named playlist
        /// </summary>
        /// <param name="name"></param>
        /// <param name="id"></param>
        [HttpGet]
        public void AddTrackToPlayList(
            string name,
            int id)
        {
            Track track = Cache.Get(id) as Track;
            if (track == null)
            {
                return;
            }

            AddTrackToPlayListAsync(name, track).Wait();
        }

        async Task AddTrackToPlayListAsync(
            string name,
            Track track)
        {
            Playlist playlist;
            if (!Playlists.ContainsKey(name))
            {
                playlist = await((await SpotifySession.Session.PlaylistContainer).Playlists.Create(name));
                await BuildPlayLists();
            }
            else
            {
                playlist = await playlists[name];
            }

            if (!playlist.Tracks.Contains(track))
            {
                playlist.Tracks.Add(track);
            }

            await BuildPlayLists();
        }

        /// <summary>
        /// Add all the tracks of an identified album to a named playlist
        /// </summary>
        /// <param name="name"></param>
        /// <param name="id"></param>
        [HttpGet]
        public void AddAlbumToPlayList(
            string name,
            int id)
        {
            Album album = Cache.Get(id) as Album;
            if (album == null)
            {
                return;
            }

            AddAlbumToPlayListAsync(name, album).Wait();
        }

        async Task AddAlbumToPlayListAsync(
            string name,
            Album album)
        {
            Playlist playlist;
            if (!Playlists.ContainsKey(name))
            {
                playlist = await ((await SpotifySession.Session.PlaylistContainer).Playlists.Create(name));
                await BuildPlayLists();
            }
            else
            {
                playlist = await playlists[name];
            }

            foreach (Track track in (await album.Browse()).Tracks)
            {
                if (!playlist.Tracks.Contains(track))
                {
                    playlist.Tracks.Add(track);
                }
            }

            await BuildPlayLists();
        }

        /// <summary>
        /// Remove an identified track from a named playlist
        /// </summary>
        /// <param name="name"></param>
        /// <param name="id"></param>
        [HttpGet]
        public void RemoveTrackFromPlayList(
            string name,
            int id)
        {
            if (!Playlists.ContainsKey(name))
            {
                return;
            }

            Track track = Cache.Get(id) as Track;
            if (track == null)
            {
                return;
            }

            RemoveTrackFromPlayListAsync(name, track).Wait();
        }

        async Task RemoveTrackFromPlayListAsync(
            string name,
            Track track)
        {
            Playlist playlist = await playlists[name];
            if (playlist.Tracks.Contains(track))
            {
                playlist.Tracks.Remove(track);
            }

            await BuildPlayLists();
        }

        /// <summary>
        /// Remove all the tracks of an identified album from a named playlist
        /// </summary>
        /// <param name="name"></param>
        /// <param name="id"></param>
        [HttpGet]
        public void RemoveAlbumFromPlayList(
            string name,
            int id)
        {
            if (!Playlists.ContainsKey(name))
            {
                return;
            }

            Album album = Cache.Get(id) as Album;
            if (album == null)
            {
                return;
            }

            RemoveAlbumFromPlayListAsync(name, album).Wait();
        }

        async Task RemoveAlbumFromPlayListAsync(
            string name,
            Album album)
        {
            Playlist playlist = await playlists[name];
            var tracks = (await album.Browse()).Tracks;
            Track[] tracksCopy = new Track[tracks.Count];
            tracks.CopyTo(tracksCopy, 0);
            foreach (Track track in tracksCopy)
            {
                if (playlist.Tracks.Contains(track))
                {
                    playlist.Tracks.Remove(track);
                }
            }

            await BuildPlayLists();
        }


    }
}

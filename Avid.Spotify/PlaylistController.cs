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
    public class PlaylistController : ApiController
    {
        static Logger logger = LogManager.GetCurrentClassLogger();

        static Dictionary<string, Playlist> playlists = null;

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

        [HttpGet]
        public IEnumerable<string> GetPlayLists()
        {
            playlists = null;
            return Playlists.Keys;
        }

        async Task<IEnumerable<SpotifyData.Track>> GetPlayListTracksAsync(
            string name)
        {
            return (await playlists[name]).Tracks.Select(t => MakeData.Track(t));
        }

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


        async Task<IEnumerable<SpotifyData.Album>> GetPlayListAlbumsAsync(
            string name)
        {
            HashSet<Album> albums = new HashSet<Album>();
            foreach (Track track in (await playlists[name]).Tracks)
            {
                Album album = track.Album;
                if (!albums.Contains(album))
                {
                    albums.Add(album);
                }
            }

            return albums.Select(a => MakeData.Album(a));
        }

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

        [HttpGet]
        public void AddPlayList(
            string name)
        {
            //  THIS WILL FAIL AS IT USES UNIMPLEMENTED SPOTIFIRE METHODS
            if (Playlists.ContainsKey(name))
            {
                return;
            }
            AddPlayListAsync(name).Wait();
        }

        async Task AddPlayListAsync(
            string name)
        {
            Playlist playlist = null;   // await new Playlist();    --  NO constructor yet implemented
            playlist.Name = name;
            (await SpotifySession.Session.PlaylistContainer).Playlists.Add(playlist);
            await BuildPlayLists();
        }

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

        [HttpGet]
        public void AddTrackToPlayList(
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
            AddTrackToPlayListAsync(name, track).Wait();
        }

        async Task AddTrackToPlayListAsync(
            string name,
            Track track)
        {
            Playlist playlist = await playlists[name];
            if (!playlist.Tracks.Contains(track))
            {
                playlist.Tracks.Add(track);
            }
        }

        [HttpGet]
        public void AddAlbumToPlayList(
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
            AddAlbumToPlayListAsync(name, album).Wait();
        }

        async Task AddAlbumToPlayListAsync(
            string name,
            Album album)
        {
            Playlist playlist = await playlists[name];
            foreach (Track track in (await album.Browse()).Tracks)
            {
                if (!playlist.Tracks.Contains(track))
                {
                    playlist.Tracks.Add(track);
                }
            }
        }

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
        }

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
            foreach (Track track in (await album.Browse()).Tracks)
            {
                if (playlist.Tracks.Contains(track))
                {
                    playlist.Tracks.Remove(track);
                }
            }
        }


    }
}

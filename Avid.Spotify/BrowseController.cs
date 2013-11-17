using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Web.Http;
using SpotiFire;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using NLog;

namespace Avid.Spotify
{
    public class BrowseController : ApiController
    {
        static Logger logger = LogManager.GetCurrentClassLogger();

        [HttpGet]
        public IEnumerable<SpotifyData.Track> SearchTracks(
            string name)
        {
            Cache.Clear();
            return SearchTracksAsync(name).Result;
        }

        private async Task<IEnumerable<SpotifyData.Track>> SearchTracksAsync(
            string name)
        {
            Search search = null;
            try
            {
                search = await SpotifySession.Session.SearchTracks(name, 0, 50);
            }
            catch (System.Exception ex)
            {
                ;
            }
            if (search != null && search.Tracks.Count > 0)
            {
                return search.Tracks.Where(t => t.IsAvailable).Select(t => MakeData.Track(t));
            }

            return new SpotifyData.Track[0];
        }



        [HttpGet]
        public IEnumerable<SpotifyData.Album> SearchAlbums(
            string name)
        {
            Cache.Clear();
            return SearchAlbumsAsync(name).Result;
        }

        private async Task<IEnumerable<SpotifyData.Album>> SearchAlbumsAsync(
            string name)
        {
            Search search = null;
            try
            {
                search = await SpotifySession.Session.SearchAlbums(name, 0, 50);
            }
            catch (System.Exception ex)
            {
                ;
            }
            if (search != null && search.Albums.Count > 0)
            {
                return search.Albums.Where(a => a.IsAvailable).Select(a => MakeData.Album(a));
            }

            return new SpotifyData.Album[0];
        }



        [HttpGet]
        public IEnumerable<SpotifyData.Artist> SearchArtists(
            string name)
        {
            Cache.Clear();
            return SearchArtistsAsync(name).Result;
        }

        private async Task<IEnumerable<SpotifyData.Artist>> SearchArtistsAsync(
            string name)
        {
            Search search = null;
            try
            {
                search = await SpotifySession.Session.SearchArtists(name, 0, 50);
            }
            catch (System.Exception ex)
            {
                ;
            }
            if (search != null && search.Artists.Count > 0)
            {
                return search.Artists.Select(a => MakeData.Artist(a));
            }

            return new SpotifyData.Artist[0];
        }


        [HttpGet]
        public SpotifyData.Track GetTrackById(
            int id)
        {
            Track track = Cache.Get(id) as Track;
            return MakeData.Track(track);
        }

        [HttpGet]
        public SpotifyData.Album GetAlbumById(
            int id)
        {
            Album album = Cache.Get(id) as Album;
            return MakeData.Album(album);
        }

        [HttpGet]
        public SpotifyData.Artist GetArtistById(
            int id)
        {
            Artist artist = Cache.Get(id) as Artist;
            return MakeData.Artist(artist, GetArtistBiography(artist).Result);
        }

        async Task<string> GetArtistBiography(
            Artist artist)
        {
            if (artist == null)
            {
                return null;
            }
            ArtistBrowse artistBrowse = await artist.Browse(ArtistBrowseType.NoAlbums);
            return artistBrowse.Biography;
        }



        async Task<IEnumerable<SpotifyData.Track>> GetTracksForAlbumAsync(
            Album album)
        {
            return (await album.Browse()).Tracks.Select(t => MakeData.Track(t));
        }

        [HttpGet]
        public IEnumerable<SpotifyData.Track> GetTracksForAlbum(
            int id)
        {
            Album album = Cache.Get(id) as Album;
            try
            {
                return album == null ? new SpotifyData.Track[0] : GetTracksForAlbumAsync(album).Result;
            }
            catch (Exception ex)
            {
                logger.Warn(ex);
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
            }
        }

        async Task<IEnumerable<SpotifyData.Album>> GetAlbumsForArtistAsync(
            Artist artist)
        {
            return (await artist.Browse(ArtistBrowseType.NoTracks)).Albums.Where(a => a.IsAvailable).Select(a => MakeData.Album(a));
        }

        [HttpGet]
        public IEnumerable<SpotifyData.Album> GetAlbumsForArtist(
            int id)
        {
            Artist artist = Cache.Get(id) as Artist;
            try
            {
                return artist == null ? new SpotifyData.Album[0] : GetAlbumsForArtistAsync(artist).Result;
            }
            catch (Exception ex)
            {
                logger.Warn(ex);
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
            }
        }

        async Task<IEnumerable<SpotifyData.Artist>> GetSimilarArtistsForArtistAsync(
            Artist artist)
        {
            return (await artist.Browse(ArtistBrowseType.NoTracks)).SimilarArtists.Select(a => MakeData.Artist(a));
        }

        [HttpGet]
        public IEnumerable<SpotifyData.Artist> GetSimilarArtistsForArtist(
            int id)
        {
            Artist artist = Cache.Get(id) as Artist;
            try
            {
                return artist == null ? new SpotifyData.Artist[0] : GetSimilarArtistsForArtistAsync(artist).Result;
            }
            catch (Exception ex)
            {
                logger.Warn(ex);
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
            }
        }


        async Task<System.Drawing.Image> GetAlbumImageAsync(
            Album album)
        {
            var coverId = album.CoverId;
            var image = await Image.FromId(SpotifySession.Session, coverId);
            var imageData = image.GetImage();
            return imageData;
        }

        [HttpGet]
        public HttpResponseMessage GetAlbumImage(
            int id)
        {
            try
            {
                Album album = Cache.Get(id) as Album;
                System.Drawing.Image imageData = GetAlbumImageAsync(album).Result;

                Stream stream = new System.IO.MemoryStream();
                imageData.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Position = 0;

                HttpResponseMessage response = new HttpResponseMessage();
                response.Content = new StreamContent(stream);
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");

                return response;
            }
            catch (Exception)
            {
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
            }
        }
    }
}

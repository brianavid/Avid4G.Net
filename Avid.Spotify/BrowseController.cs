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
    /// <summary>
    /// Web API Controller, with public HttpGet web methods for browsing the Spotify catalog
    /// </summary>
    public class BrowseController : ApiController
    {
        static Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Search Spotify for up to 50 tracks matching the specified track name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
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
                //  Search for the tracks asynchronously
                search = await SpotifySession.Session.SearchTracks(name, 0, 50);
            }
            catch (System.Exception ex)
            {
                ;
            }

            //  If we have any tracks, return the collection of track data
            if (search != null && search.Tracks.Count > 0)
            {
                return search.Tracks.Where(t => t.IsAvailable).Select(t => MakeData.Track(t));
            }

            //  None found. Return an empty array
            return new SpotifyData.Track[0];
        }


        /// <summary>
        /// Search Spotify for up to 50 albums matching the specified album name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
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
                //  Search for the albums asynchronously
                search = await SpotifySession.Session.SearchAlbums(name, 0, 50);
            }
            catch (System.Exception ex)
            {
                ;
            }

            //  If we have any albums, return the collection of album data
            if (search != null && search.Albums.Count > 0)
            {
                return search.Albums.Where(a => a.IsAvailable).Select(a => MakeData.Album(a));
            }

            //  None found. Return an empty array
            return new SpotifyData.Album[0];
        }


        /// <summary>
        /// Search Spotify for up to 50 artists matching the specified artist name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
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
                //  Search for the artists asynchronously
                search = await SpotifySession.Session.SearchArtists(name, 0, 50);
            }
            catch (System.Exception ex)
            {
                ;
            }

            //  If we have any artists, return the collection of artist data
            if (search != null && search.Artists.Count > 0)
            {
                return search.Artists.Select(a => MakeData.Artist(a));
            }

            //  None found. Return an empty array
            return new SpotifyData.Artist[0];
        }

        /// <summary>
        /// Return cached track data for a tracks identified by a non-persistent cache Id
        /// </summary>
        /// <param name="id">The non-persistent cache Id</param>
        /// <returns></returns>
        [HttpGet]
        public SpotifyData.Track GetTrackById(
            int id)
        {
            Track track = Cache.Get(id) as Track;
            return MakeData.Track(track);
        }

        /// <summary>
        /// Return cached album data for a tracks identified by a non-persistent cache Id
        /// </summary>
        /// <param name="id">The non-persistent cache Id</param>
        /// <returns></returns>
        [HttpGet]
        public SpotifyData.Album GetAlbumById(
            int id)
        {
            Album album = Cache.Get(id) as Album;
            return MakeData.Album(album);
        }

        /// <summary>
        /// Return cached artist data for a tracks identified by a non-persistent cache Id
        /// </summary>
        /// <param name="id">The non-persistent cache Id</param>
        /// <returns></returns>
        [HttpGet]
        public SpotifyData.Artist GetArtistById(
            int id)
        {
            Artist artist = Cache.Get(id) as Artist;
            Task<string> artistBiography = GetArtistBiography(artist);
            return MakeData.Artist(artist, artistBiography == null ? null : artistBiography.Result);
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


        /// <summary>
        /// Get the collection of tracks for an identified album
        /// </summary>
        /// <param name="id">The non-persistent cache Id</param>
        /// <returns></returns>
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

        async Task<IEnumerable<SpotifyData.Track>> GetTracksForAlbumAsync(
            Album album)
        {
            return (await album.Browse()).Tracks.Select(t => MakeData.Track(t));
        }

        /// <summary>
        /// Get the collection of albums for an identified artist
        /// </summary>
        /// <param name="id">The non-persistent cache Id</param>
        /// <returns></returns>
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

        async Task<IEnumerable<SpotifyData.Album>> GetAlbumsForArtistAsync(
            Artist artist)
        {
            return (await artist.Browse(ArtistBrowseType.NoTracks)).Albums.Where(a => a.IsAvailable).Select(a => MakeData.Album(a));
        }


        /// <summary>
        /// Get the collection of similar artists for an identified artist
        /// </summary>
        /// <param name="id">The non-persistent cache Id</param>
        /// <returns></returns>
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

        async Task<IEnumerable<SpotifyData.Artist>> GetSimilarArtistsForArtistAsync(
            Artist artist)
        {
            return (await artist.Browse(ArtistBrowseType.NoTracks)).SimilarArtists.Select(a => MakeData.Artist(a));
        }


        /// <summary>
        /// Get a PNG image as streamed data for the image file for an identified album
        /// </summary>
        /// <param name="id">The non-persistent cache Id</param>
        /// <returns>An HTTP response representing the content of the requested image file</returns>
        [HttpGet]
        public HttpResponseMessage GetAlbumImage(
            int id)
        {
            try
            {
                //  Get the album and its image data
                Album album = Cache.Get(id) as Album;
                System.Drawing.Image imageData = GetAlbumImageAsync(album).Result;

                //  Write the image data to a MemoryStream buffer as a PNG file
                Stream stream = new System.IO.MemoryStream();
                imageData.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Position = 0;

                //  Write the MemoryStream buffer to an HTTP response with the correct ContentType
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

        async Task<System.Drawing.Image> GetAlbumImageAsync(
            Album album)
        {
            var coverId = album.CoverId;
            var image = await Image.FromId(SpotifySession.Session, coverId);
            var imageData = image.GetImage();
            return imageData;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Web.Http;
using SpotiFire;
using System.IO;

namespace Avid.Spotify
{
    /// <summary>
    /// For Spotify objects (tracks, albums, artists) construct serializable objects 
    /// replacing object references with cached non-persistent identifiers
    /// </summary>
    static class MakeData
    {
        /// <summary>
        /// Construct a serializable representation of a Spotify Track
        /// </summary>
        /// <param name="track"></param>
        /// <returns></returns>
        internal static SpotifyData.Track Track(
            Track track)
        {
            return track == null ? null : new SpotifyData.Track
            {
                Id = Cache.Key(track),
                Name = track.Name,
                AlbumId = Cache.Key(track.Album),
                AlbumName = track.Album.Name,
                ArtistId = Cache.Key(track.Album.Artist),
                AlbumArtistName = track.Album.Artist.Name,
                TrackArtistNames = track.Artists.Aggregate("", ConstructTrackArtistNames),
                TrackFirstArtistId = Cache.Key(track.Artists.First()),
                Index = track.Index,
                Duration = (int)Math.Round(track.Duration.TotalSeconds)
            };
        }

        /// <summary>
        /// Construct a formatted string for a (possibly multiple) artist names for a track
        /// </summary>
        /// <param name="names"></param>
        /// <param name="artist"></param>
        /// <returns></returns>
        static string ConstructTrackArtistNames(
            string names,
            Artist artist)
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

        /// <summary>
        /// Construct a serializable representation of a Spotify Album
        /// </summary>
        /// <param name="album"></param>
        /// <returns></returns>
        internal static SpotifyData.Album Album(
            Album album)
        {
            return album == null ? null : new SpotifyData.Album
            {
                Id = Cache.Key(album),
                Name = album.Name,
                ArtistId = Cache.Key(album.Artist),
                ArtistName = album.Artist.Name,
            };
        }

        /// <summary>
        /// Construct a serializable representation of a Spotify Artist
        /// </summary>
        /// <param name="artist"></param>
        /// <param name="biography"></param>
        /// <returns></returns>
        internal static SpotifyData.Artist Artist(
            Artist artist,
            string biography = null)
        {
            return artist == null ? null : new SpotifyData.Artist
            {
                Id = Cache.Key(artist),
                Name = artist.Name,
                Biography = biography,
            };
        }
    }
}

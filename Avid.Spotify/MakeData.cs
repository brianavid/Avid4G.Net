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
    static class MakeData
    {
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
                TrackArtistNames = track.Artists.Aggregate("", ConstructTrackActistNames),
                TrackFirstArtistId = Cache.Key(track.Artists.First()),
                Index = track.Index,
                Duration = (int)Math.Round(track.Duration.TotalSeconds)
            };
        }

        static string ConstructTrackActistNames(
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

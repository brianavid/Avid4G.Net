using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Avid.Spotify
{
    /// <summary>
    /// Representations of Spotify Artist, Album or Track in a form that can be communicated over WebAPI
    /// </summary>
    /// <remarks>
    /// Note that all references to other objects are via integer IDs, which are persistent until a "clearing" action
    /// </remarks>
    public class SpotifyData
    {
        /// <summary>
        /// Representation of Spotify Artist in a form that can be communicated over WebAPI
        /// </summary>
        public class Artist
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }

        /// <summary>
        /// Representation of Spotify Album in a form that can be communicated over WebAPI
        /// </summary>
        public class Album
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string ArtistId { get; set; }
            public string ArtistName { get; set; }
            public string Year { get; set; }
            public int TrackCount { get; set; }
        }

        /// <summary>
        /// Representation of Spotify Track in a form that can be communicated over WebAPI
        /// </summary>
        public class Track
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string AlbumId { get; set; }
            public string AlbumName { get; set; }
            public string ArtistId { get; set; }
            public string AlbumArtistName { get; set; }
            public string TrackArtistNames { get; set; }
            public string TrackFirstArtistId { get; set; }
            public int Index { get; set; }
            public int Count { get; set; }
            public int Duration { get; set; }
        }

        /// <summary>
        /// Representation of Spotify Playlist in a form that can be communicated over WebAPI
        /// </summary>
        public class Playlist
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }

    }
}

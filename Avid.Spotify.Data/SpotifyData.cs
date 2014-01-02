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
        /// 
        /// </summary>
        public class Artist
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Biography { get; set; }
        }

        /// <summary>
        /// Representation of Spotify Album in a form that can be communicated over WebAPI
        /// 
        /// </summary>
        public class Album
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int ArtistId { get; set; }
            public string ArtistName { get; set; }
        }

        /// <summary>
        /// Representation of Spotify Track in a form that can be communicated over WebAPI
        /// 
        /// </summary>
        public class Track
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int AlbumId { get; set; }
            public string AlbumName { get; set; }
            public int ArtistId { get; set; }
            public string AlbumArtistName { get; set; }
            public string TrackArtistNames { get; set; }
            public int TrackFirstArtistId { get; set; }
            public int Index { get; set; }
            public int Duration { get; set; }
        }
    }
}

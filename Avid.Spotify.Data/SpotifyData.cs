using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avid.Spotify
{
    public class SpotifyData
    {
        public class Artist
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Biography { get; set; }
        }

        public class Album
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int ArtistId { get; set; }
            public string ArtistName { get; set; }
        }

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

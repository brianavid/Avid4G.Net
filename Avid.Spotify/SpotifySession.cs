using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpotiFire;
using System.Threading;
using System.Configuration;
using NLog;

namespace Avid.Spotify
{
    internal static class SpotifySession
    {
        static Logger logger = LogManager.GetCurrentClassLogger();

        static Object SessionLock = new Object();

        static string cache = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "spotifire", "cache");
        static string settings = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "spotifire", "settings");
        static string userAgent = "Avid Spotify Player";
        static IPlayer player = new NAudioPlayer();

        static Session session = null;
        static bool playTokenStolen = false;

        static LinkedList<Track> trackQueue = null;
        static LinkedListNode<Track> currentPlayingTrackNode;

        public static string SpotifyUser { get; set; }
        public static string SpotifyPass { get; set; }

        static void InitializeSession()
        {
            try
            {
	            System.Diagnostics.Trace.WriteLine("InitializeSession");
	            if (session != null)
	            {
	                CloseSession();
	            }

                byte[] loadedKey = File.ReadAllBytes("spotify_appkey.key");
	            System.Diagnostics.Trace.WriteLine("CreateSession");
                session = SpotiFire.Spotify.CreateSession(loadedKey, cache, settings, userAgent).Result;
	            System.Diagnostics.Trace.WriteLine("InitializeSession");
	            session.MusicDelivered += session_MusicDeliver;
	
	            trackQueue = new LinkedList<Track>();
	            currentPlayingTrackNode = null;

                session.Login(SpotifyUser, SpotifyPass, false).Wait();
                session.PreferredBitrate = BitRate.Bitrate320k;
	            session.EndOfTrack += (s, e) => SkipTrack();
                session.PlayTokenLost += (s, e) => {playTokenStolen = true;};
	            System.Diagnostics.Trace.WriteLine("InitializeSession OK");
            }
            catch (System.Exception ex)
            {
                logger.Warn(ex);
            }
       }

        public static void CloseSession(
            bool dispose = true)
        {
            player.Pause();
            player.Stop();

            if (session != null)
            {
	            session.Logout();
                if (dispose)
                {
                    session.Dispose();
	                session = null;
                }
            }
        }

        static Int64 numSamplesPlayed = 0;
        static int currentTrackPos = 0;
        static int seekPosition = 0;

        static void session_MusicDeliver(Session sender, MusicDeliveryEventArgs e)
        {
            if (e.Samples.Length > 0)
            {
                e.ConsumedFrames = player.EnqueueSamples(e.Channels, e.Rate, e.Samples, Math.Min(e.Frames, e.Rate));
            }
            else
            {
                e.ConsumedFrames = 0;
            }
            numSamplesPlayed += e.ConsumedFrames;
            currentTrackPos = seekPosition + (int)(numSamplesPlayed / e.Rate) - (int)player.GetBufferedDuration().TotalSeconds;
        }

        public static Session Session
        {
            get
            {
                if (session == null)
                {
                    lock (SessionLock)
                    {
                        if (session == null)
                        {
                            InitializeSession();
                        }
                    }
                }

                return session;
            }
        }

        static void SkipTrack()
        {
            lock (SessionLock)
            {
                session.PlayerUnload();
                numSamplesPlayed = 0;
                currentTrackPos = 0;
                seekPosition = 0;
                var bufferedDuration = player.GetBufferedDuration();
                if (trackQueue.Count == 0 || trackQueue.Last == currentPlayingTrackNode)
                {
                    System.Threading.Thread.Sleep(bufferedDuration);
                    player.Pause();
                    currentPlayingTrackNode = null;
                }
                else
                {
                    currentPlayingTrackNode = currentPlayingTrackNode != null ? currentPlayingTrackNode.Next : trackQueue.First;
                    session.PlayerLoad(currentPlayingTrackNode.Value);
                    session.PlayerPlay();
                    if (!player.Playing())
                    {
                        playTokenStolen = false;
                        player.Play();
                    }
                }
            }
        }

        static void BackTrack()
        {
            lock (SessionLock)
            {
                player.Reset();
                session.PlayerUnload();
                currentTrackPos = 0;
                numSamplesPlayed = 0;
                seekPosition = 0;
                if (trackQueue.Count == 0 || trackQueue.First == currentPlayingTrackNode)
                {
                    player.Pause();
                    currentPlayingTrackNode = null;
                }
                else if (currentPlayingTrackNode != null)
                {
                    currentPlayingTrackNode = currentPlayingTrackNode.Previous;
                    session.PlayerLoad(currentPlayingTrackNode.Value);
                    session.PlayerPlay();
                    if (!player.Playing())
                    {
                        playTokenStolen = false;
                        player.Play();
                    }
                }
            }
        }

        internal static void Play()
        {
            lock (SessionLock)
            {
                if (currentPlayingTrackNode == null)
                {
                    SkipTrack();
                }
                playTokenStolen = false;
                player.Play();
            }
        }

        internal static void Pause()
        {
            lock (SessionLock)
            {
                player.Pause();
            }
        }

        internal static int GetPlaying()
        {
            lock (SessionLock)
            {
                return player.Playing() ? 1 : playTokenStolen ? -1 : 0;
            }
        }

        internal static void Stop()
        {
            lock (SessionLock)
            {
                player.Pause();
                player.Reset();
                currentPlayingTrackNode = null;
            }
        }

        internal static void Skip()
        {
            SkipTrack();
        }

        internal static void Back()
        {
            BackTrack();
        }

        internal static void SkipToQueuedTrack(
            Track track)
        {
            lock (SessionLock)
            {
                LinkedListNode<Track> foundTrackNode = trackQueue.Find(track);
                if (foundTrackNode != null)
                {
                    player.Reset();
                    session.PlayerUnload();
                    currentTrackPos = 0;
                    numSamplesPlayed = 0;
                    seekPosition = 0;
                    currentPlayingTrackNode = foundTrackNode;
                    session.PlayerLoad(currentPlayingTrackNode.Value);
                    session.PlayerPlay();
                    if (!player.Playing())
                    {
                        playTokenStolen = false;
                        player.Play();
                    }
                }
            }
        }

        internal static void RemoveQueuedTrack(
            Track track)
        {
            lock (SessionLock)
            {
                LinkedListNode<Track> foundTrackNode = trackQueue.Find(track);
                if (foundTrackNode != null)
                {
                    if (foundTrackNode == currentPlayingTrackNode)
                    {
                        player.Reset();
                        SkipTrack();
                    }
                    trackQueue.Remove(foundTrackNode);
                }
            }
        }

        internal static int GetPosition()
        {
            lock (SessionLock)
            {
                return currentTrackPos;
            }
        }

        internal static int SetPosition(
            int pos)
        {
            lock (SessionLock)
            {
                player.Reset();
                seekPosition = pos;
                numSamplesPlayed = 0;
                currentTrackPos = 0;
                session.PlayerSeek(seekPosition * 1000);
                return seekPosition;
            }
        }

        internal static Track GetCurrentTrack()
        {
            return currentPlayingTrackNode == null ? null : currentPlayingTrackNode.Value;
        }

        internal static IEnumerable<Track> GetQueuedTracks()
        {
            var session = Session;  //  Ensure initialization

            lock (SessionLock)
            {
                return trackQueue.ToArray();
            }
        }

        internal static void EnqueueTrack(
            Track track,
            bool append)
        {
            lock (SessionLock)
            {
                if (!append)
                {
                    player.Reset();
                    trackQueue.Clear();
                    currentPlayingTrackNode = null;
                }
                trackQueue.AddLast(track);
            }

            if (!append || !player.Playing())
            {
                SkipTrack();
            }
        }

        internal static void EnqueueTracks(
            IEnumerable<Track> tracks,
            bool append)
        {
            lock (SessionLock)
            {
                if (!append)
                {
                    player.Reset();
                    trackQueue.Clear();
                    currentPlayingTrackNode = null;
                }
                foreach (var track in tracks)
                {
                    trackQueue.AddLast(track);
                }
            }

            if (!append || !player.Playing())
            {
                SkipTrack();
            }
        }
    }
}

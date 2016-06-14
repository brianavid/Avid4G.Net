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
    /// <summary>
    /// Class to wrap an authenticated Spotify session accessed through the Spotifire library
    /// </summary>
    internal static class SpotifySession
    {
        static Logger logger = LogManager.GetCurrentClassLogger();

        static Object SessionLock = new Object();

        public const string SpotifyAppKeyFileName = "spotify_appkey.key";
        static readonly string Cache = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "spotifire", "cache");
        static readonly string Settings = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "spotifire", "settings");
        static readonly string UserAgent = "Avid Spotify Player";

        /// <summary>
        /// The audio player implementing the IPlayer interface
        /// </summary>
        static readonly IPlayer Player = new NAudioPlayer();

        /// <summary>
        /// The SpotiFire session
        /// </summary>
        static Session session = null;

        /// <summary>
        /// An indication of whether the logged on user's play token has been taken (stolen?) by another player on another computer
        /// </summary>
        static bool playTokenStolen = false;

        /// <summary>
        /// The queue tracks played and to be played
        /// </summary>
        static LinkedList<Track> trackQueue = null;

        /// <summary>
        /// The track currently playing - expected to be within the trackQueue
        /// </summary>
        static LinkedListNode<Track> currentPlayingTrackNode;

        /// <summary>
        /// Spotify credentials
        /// </summary>
        public static string SpotifyUser { get; set; }

        /// <summary>
        /// Spotify credentials
        /// </summary>
        public static string SpotifyPass { get; set; }

        /// <summary>
        /// Initialize the Spotify session, if necessary closing any existing session
        /// </summary>
        static void InitializeSession()
        {
            try
            {
	            System.Diagnostics.Trace.WriteLine("InitializeSession");
	            if (session != null)
	            {
	                CloseSession();
	            }

                //  A Spotify key file is required to exist at the fixed name
                //  Use tis to create a new SpotiFire session
                byte[] loadedKey = File.ReadAllBytes(SpotifyAppKeyFileName);
	            System.Diagnostics.Trace.WriteLine("CreateSession");
                session = SpotiFire.Spotify.CreateSession(loadedKey, Cache, Settings, UserAgent).Result;
	            System.Diagnostics.Trace.WriteLine("InitializeSession");
                session.MusicDelivered += MusicDeliver;

                //  Nothing currently playing
	            trackQueue = new LinkedList<Track>();
	            currentPlayingTrackNode = null;

                //  Login and configure the session
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

        /// <summary>
        /// Close the session, cleanly stopping the player
        /// </summary>
        /// <param name="dispose"></param>
        public static void CloseSession(
            bool dispose = true)
        {
            Player.Pause();
            Player.Stop();

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

        //  Where are we playing currently - used for position display
        static Int64 numSamplesPlayed = 0;
        static int currentTrackPos = 0;
        static int seekPosition = 0;

        /// <summary>
        /// Deliver the Spotify track's music samples to the player, allowing it to consume as many as it can buffer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void MusicDeliver(Session sender, MusicDeliveryEventArgs e)
        {
            try
            {
	            if (e.Samples.Length > 0)
	            {
	                //  Let the player consume as many as it can
	                e.ConsumedFrames = Player.EnqueueSamples(e.Channels, e.Rate, e.Samples, Math.Min(e.Frames, e.Rate));
	            }
	            else
	            {
	                e.ConsumedFrames = 0;
	            }

	            //  Where are we playing currently - used for position display
	            numSamplesPlayed += e.ConsumedFrames;
	            currentTrackPos = seekPosition + (int)(numSamplesPlayed / e.Rate) - (int)Player.GetBufferedDuration().TotalSeconds;
            }
            catch (System.Exception ex)
            {
                logger.Error(ex, "Failure in MusicDeliver: {0}");
            }
        }

        /// <summary>
        /// The current (automatically initialized) SpotiFire session
        /// </summary>
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

        /// <summary>
        /// Skip to the next queued track
        /// </summary>
        static bool SkipTrack(
            bool StopCurrentImmediately = false)
        {
            lock (SessionLock)
            {
                try
                {
	                if (StopCurrentImmediately)
	                {
		                //  Stop the current track playing immediately, discarding buffered music
		                Player.Reset();
	                }

	                session.PlayerUnload();
	                numSamplesPlayed = 0;
	                currentTrackPos = 0;
	                seekPosition = 0;

	                //  If this is the last queued track, sleep till it has played to the end and then pause the player
	                //  If the player is not paused it loops playing the last buffer-full of samples repeatedly
	                var bufferedDuration = Player.GetBufferedDuration();
	                if (!StopCurrentImmediately && (trackQueue.Count == 0 || trackQueue.Last == currentPlayingTrackNode))
	                {
	                    System.Threading.Thread.Sleep(bufferedDuration);
	                    Player.Pause();
	                    currentPlayingTrackNode = null;
                        logger.Info("Nothing to play");
	                }
	                else
	                {
	                    //  Get the next track to play, which will be the first if nothing is currently playing
	                    currentPlayingTrackNode = currentPlayingTrackNode != null ? currentPlayingTrackNode.Next : trackQueue.First;

	                    //  Load up the player
	                    session.PlayerLoad(currentPlayingTrackNode.Value);
	                    session.PlayerPlay();
                        logger.Info("Playing '{0}'", currentPlayingTrackNode.Value.Name);

	                    //  If the player is not already playing, start playing
	                    if (!Player.Playing())
	                    {
	                        playTokenStolen = false;
	                        Player.Play();
	                    }

                        return true;
	                }
                }
                catch (System.Exception ex)
                {
                    logger.Error(ex, "Failure in SkipTrack: {0}");
                }
            }

            return false;
        }

        //  Skip to the previous queued track
        static void BackTrack()
        {
            lock (SessionLock)
            {
                //  Stop the current track playing immediately, discarding buffered music
                Player.Reset();

                session.PlayerUnload();
                currentTrackPos = 0;
                numSamplesPlayed = 0;
                seekPosition = 0;

                //  If this is the first queued track, pause the player
                if (trackQueue.Count == 0 || trackQueue.First == currentPlayingTrackNode)
                {
                    Player.Pause();
                    currentPlayingTrackNode = null;
                }
                else if (currentPlayingTrackNode != null)
                {
                    //  Get the next (i.e. previous) track to play
                    currentPlayingTrackNode = currentPlayingTrackNode.Previous;

                    //  Load up the player
                    session.PlayerLoad(currentPlayingTrackNode.Value);
                    session.PlayerPlay();
                    logger.Info("Playing [SkipBack] '{0}'", currentPlayingTrackNode.Value.Name);

                    //  If the player is not already playing, start playing
                    if (!Player.Playing())
                    {
                        playTokenStolen = false;
                        Player.Play();
                    }
                }
            }
        }

        /// <summary>
        /// Continue playing the current track, or start at the first queued track
        /// </summary>
        internal static void Play()
        {
            lock (SessionLock)
            {
                if (currentPlayingTrackNode == null)
                {
                    SkipTrack();
                }
                playTokenStolen = false;
                Player.Play();
            }
        }

        /// <summary>
        /// Pause playing the current track
        /// </summary>
        internal static void Pause()
        {
            lock (SessionLock)
            {
                Player.Pause();
            }
        }

        /// <summary>
        /// Is the player playing a track?
        /// </summary>
        /// <returns>+ve: Playing; 0: Paused; -ve: Stolen by another session</returns>
        internal static int GetPlaying()
        {
            lock (SessionLock)
            {
                return Player.Playing() ? 1 : playTokenStolen ? -1 : 0;
            }
        }

        /// <summary>
        /// Stop playing the current track immediately
        /// </summary>
        internal static void Stop()
        {
            lock (SessionLock)
            {
                if (currentPlayingTrackNode != null)
                {
                    Player.Pause();
                    Player.Reset();
                    trackQueue.Clear();
                    currentPlayingTrackNode = null;
                }
            }
        }

        /// <summary>
        /// Skip forward a track
        /// </summary>
        internal static void Skip()
        {
            SkipTrack(StopCurrentImmediately: true);
        }

        /// <summary>
        /// Skip backward a track
        /// </summary>
        internal static void Back()
        {
            BackTrack();
        }

        /// <summary>
        /// Skip to a specified queued track
        /// </summary>
        internal static void SkipToQueuedTrack(
            Track track)
        {
            lock (SessionLock)
            {
                //  Find the track in the queue
                LinkedListNode<Track> foundTrackNode = trackQueue.Find(track);
                if (foundTrackNode != null)
                {
                    //  Stop the current track playing immediately, discarding buffered music
                    Player.Reset();

                    session.PlayerUnload();
                    currentTrackPos = 0;
                    numSamplesPlayed = 0;
                    seekPosition = 0;

                    //  Get the next (i.e. specified) track to play
                    currentPlayingTrackNode = foundTrackNode;

                    //  Load up the player
                    session.PlayerLoad(currentPlayingTrackNode.Value);
                    session.PlayerPlay();
                    logger.Info("Playing [SkipTo] '{0}'", currentPlayingTrackNode.Value.Name);

                    //  If the player is not already playing, start playing
                    if (!Player.Playing())
                    {
                        playTokenStolen = false;
                        Player.Play();
                    }
                }
            }
        }

        /// <summary>
        /// Remove the specified queued track from the queue
        /// </summary>
        internal static void RemoveQueuedTrack(
            Track track)
        {
            lock (SessionLock)
            {
                //  Find the track in the queue
                LinkedListNode<Track> foundTrackNode = trackQueue.Find(track);
                if (foundTrackNode != null)
                {
                    //  If the specified track is the current track skip forward immediately, discarding buffered music
                    if (foundTrackNode == currentPlayingTrackNode)
                    {
                        Player.Reset();
                        SkipTrack();
                    }

                    //  Remove the track from the queue
                    trackQueue.Remove(foundTrackNode);
                }
            }
        }

        /// <summary>
        /// Get the position at which the current track is playing
        /// </summary>
        /// <returns>Position in seconds</returns>
        internal static int GetPosition()
        {
            lock (SessionLock)
            {
                return currentTrackPos;
            }
        }

        /// <summary>
        /// Seek to a particular position within the currently playing track
        /// </summary>
        /// <param name="pos">Position in seconds</param>
        /// <returns></returns>
        internal static int SetPosition(
            int pos)
        {
            lock (SessionLock)
            {
                //  Stop the current track playing immediately, discarding buffered music
                Player.Reset();

                //  We are not starting playing at the start of the track
                seekPosition = pos;

                //  Get Spotify to start delivering music samples from the specified position
                numSamplesPlayed = 0;
                currentTrackPos = 0;
                session.PlayerSeek(seekPosition * 1000);

                return seekPosition;
            }
        }

        /// <summary>
        /// The currently playing track in the queu
        /// </summary>
        /// <returns></returns>
        internal static Track GetCurrentTrack()
        {
            return currentPlayingTrackNode == null ? null : currentPlayingTrackNode.Value;
        }

        /// <summary>
        /// Get the queued tracks as an array
        /// </summary>
        /// <returns></returns>
        internal static IEnumerable<Track> GetQueuedTracks()
        {
            var session = Session;  //  Ensure initialization

            lock (SessionLock)
            {
                return trackQueue.ToArray();
            }
        }

        /// <summary>
        /// Either set the queue of tracks to the specified track only, or else append that track to the existing queue
        /// </summary>
        /// <param name="track"></param>
        /// <param name="append"></param>
        internal static void EnqueueTrack(
            Track track,
            bool append)
        {
            lock (SessionLock)
            {
                //  If we are not appending, stop playing and clear the queue
                if (!append)
                {
                    Player.Reset();
                    trackQueue.Clear();
                    currentPlayingTrackNode = null;
                }

                //  Append the track to the (possibly cleared) queue
                trackQueue.AddLast(track);
            }

            //  Start playing if necessary
            if (!append || !Player.Playing())
            {
                SkipTrack();
            }
        }

        /// <summary>
        /// Either set the queue of tracks to the specified set of tracks, or else append those tracks to the existing queue
        /// </summary>
        /// <param name="tracks"></param>
        /// <param name="append"></param>
        internal static void EnqueueTracks(
            IEnumerable<Track> tracks,
            bool append)
        {
            lock (SessionLock)
            {
                //  If we are not appending, stop playing and clear the queue
                if (!append)
                {
                    Player.Reset();
                    trackQueue.Clear();
                    currentPlayingTrackNode = null;
                }

                //  Append all tracks to the (possibly cleared) queue
                foreach (var track in tracks)
                {
                    trackQueue.AddLast(track);
                }
            }

            //  Start playing if necessary
            if (!append || !Player.Playing())
            {
                SkipTrack();
            }
        }

        internal static Artist GetArtist(
            string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            Link link = Session.GetLink(id);

            return link == null ? null : link.AsArtist();
        }

        internal static Album GetAlbum(
            string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            Link link = Session.GetLink(id);

            return link == null ? null : link.AsAlbum();
        }

        internal static Track GetTrack(
            string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            Link link = Session.GetLink(id);

            return link == null ? null : link.AsTrack();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Avid.Spotify
{
    /// <summary>
    /// Objects (tracks, albums, artists) returned from SpotiFire are stored in a cache which prevents them from being garbage
    /// collected until the cache is cleared. The UI contains integer IDs referencing these objects that must remain valid until
    /// an explicit UI operation which is agreed to invalidate all such cached values.
    /// Adding an item to the cache returns an int by which the object can be found again.
    /// </summary>
    /// <remarks>
    /// The cache presents itself as a Dictionary of objects. Adding an object to the cache returns the Dictionary key, which
    /// would be object's unique hash code if that were guaranteed unique. But instead an ObjectIDGenerator is used to generate unique 
    /// object ids. As these are allocated monotonically from 1, casting the Int64 value to an Int32 is safe.
    ///
    /// The operations which will invalidate these cached values are the Browsing actions to search and access playlists.
    /// </remarks>
    class Cache
    {
        /// <summary>
        /// The Dictionary of cached objects
        /// </summary>
        Dictionary<int, object> cacheDictionary;

        /// <summary>
        /// A generator of (monotonically increasing) unique IDs for objects in the cache to be used as keys into the cacheDictionary
        /// </summary>
        ObjectIDGenerator idGenerator;

        /// <summary>
        /// Constructor
        /// </summary>
        Cache()
        {
            cacheDictionary = new Dictionary<int, object>();
            idGenerator = new ObjectIDGenerator();
        }

        /// <summary>
        /// Clear the cache and ID generator, so that IDs will be re-generated starting again at 1
        /// </summary>
        void ClearCache()
        {
            cacheDictionary.Clear();
            idGenerator = new ObjectIDGenerator();
        }

        /// <summary>
        /// Does the cache contain an object for the specified key?
        /// </summary>
        /// <param name="k"></param>
        /// <returns></returns>
        bool Contains(
            int k)
        {
            return cacheDictionary.ContainsKey(k);
        }

        /// <summary>
        /// Get the cached object for the key index value
        /// </summary>
        /// <param name="k"></param>
        /// <returns></returns>
        object this[int k] { get { return Contains(k) ? cacheDictionary[k] : null; } }

        /// <summary>
        /// Add an object to the cache, generating a unique key value which is returned
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        int Add(
            object o)
        {
            bool firstTime;
            int k = (int)idGenerator.GetId(o, out firstTime);
            cacheDictionary[k] = o;
            return k;
        }

        /// <summary>
        /// A singleton instance of the cache
        /// </summary>
        static Cache theCache = new Cache();

        /// <summary>
        /// Return a key for a newly cached object
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        internal static int Key(
            object o)
        {
            return theCache.Add(o);
        }

        /// <summary>
        /// Get the cached object for the specified key
        /// </summary>
        /// <param name="k"></param>
        /// <returns></returns>
        internal static object Get(
            int k)
        {
            return theCache[k];
        }

        /// <summary>
        /// Clear and re-populate the initial contents of the cache
        /// </summary>
        internal static void Clear()
        {
            theCache.ClearCache();

            //  Add all currently queued tracks so that they can be found by key
            var tracks = SpotifySession.GetQueuedTracks();
            foreach (var track in tracks)
            {
                theCache.Add(track);
            }
        }

    }
}

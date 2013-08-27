using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avid.Spotify
{
    class Cache
    {
        Dictionary<int, object> cacheDictionary;

        Cache()
        {
            cacheDictionary = new Dictionary<int, object>();
        }

        void ClearCache()
        {
            cacheDictionary.Clear();
        }

        bool Contains(
            int k)
        {
            return cacheDictionary.ContainsKey(k);
        }

        object this[int k] { get { return Contains(k) ? cacheDictionary[k] : null; } }

        int Add(
            object o)
        {
            int k = o.GetHashCode();
            cacheDictionary[k] = o;
            return k;
        }

        static Cache theCache = new Cache();

        internal static int Key(
            object o)
        {
            return theCache.Add(o);
        }

        internal static object Get(
            int k)
        {
            return theCache[k];
        }

        internal static void Clear()
        {
            theCache.ClearCache();
        }

    }
}

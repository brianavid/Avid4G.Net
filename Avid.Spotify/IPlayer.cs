using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avid.Spotify
{
    interface IPlayer
    {
        int EnqueueSamples(int channels, int rate, byte[] samples, int frames);
        void Reset();
        TimeSpan GetBufferedDuration();
        void Play();
        void Pause();
        void Stop();
        bool Playing();
    }
}

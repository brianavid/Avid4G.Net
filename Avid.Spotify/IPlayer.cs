using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avid.Spotify
{
    /// <summary>
    /// The music player interface expected to be implemented by NPlayer
    /// </summary>
    interface IPlayer
    {
        /// <summary>
        /// Add some music sample to the buffer. Not all samples are required to be accepted into the player buffer
        /// </summary>
        /// <param name="channels"></param>
        /// <param name="rate"></param>
        /// <param name="samples"></param>
        /// <param name="frames"></param>
        /// <returns></returns>
        int EnqueueSamples(int channels, int rate, byte[] samples, int frames);

        /// <summary>
        /// Discard the entire buffer to stop playing
        /// </summary>
        void Reset();

        /// <summary>
        /// What period of time is covered by the amount of samples buffered?
        /// </summary>
        /// <returns></returns>
        TimeSpan GetBufferedDuration();

        /// <summary>
        /// Start or continue playing buffered samples
        /// </summary>
        void Play();

        /// <summary>
        /// Pause playing buffered samples
        /// </summary>
        void Pause();

        /// <summary>
        /// Stop playing
        /// </summary>
        void Stop();

        /// <summary>
        /// Is the player currently playing?
        /// </summary>
        /// <returns></returns>
        bool Playing();
    }
}

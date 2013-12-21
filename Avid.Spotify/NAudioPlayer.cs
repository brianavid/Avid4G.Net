using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avid.Spotify
{
    /// <summary>
    /// An implementation of the IPlayer interface based on the NAudio library
    /// </summary>
    class NAudioPlayer : IPlayer
    {
        /// <summary>
        /// The buffer of samples ready to play
        /// </summary>
        NAudio.Wave.BufferedWaveProvider buffer;

        /// <summary>
        /// The output "device"
        /// </summary>
        NAudio.Wave.DirectSoundOut dso;

        /// <summary>
        /// Add some music sample to the buffer. Not all samples are required to be accepted into the player buffer
        /// </summary>
        /// <param name="channels"></param>
        /// <param name="rate"></param>
        /// <param name="samples"></param>
        /// <param name="frames"></param>
        /// <returns></returns>
        public int EnqueueSamples(int channels, int rate, byte[] samples, int frames)
        {
            //  If we don't yet have a buffer, allocate one and start playing from it as a barground activity
            if (buffer == null)
            {
                buffer = new NAudio.Wave.BufferedWaveProvider(new NAudio.Wave.WaveFormat(rate, channels));
                dso = new NAudio.Wave.DirectSoundOut(70);
                dso.Init(buffer);
                dso.Play();
            }

            //  Do we have room in the buffer to add all the new samples
            int space = buffer.BufferLength - buffer.BufferedBytes;
            if (space > samples.Length)
            {
                //  Add them all
                buffer.AddSamples(samples, 0, samples.Length);
                return frames;
            }

            //  None added as there was insufficient room for them all
            return 0;
        }

        /// <summary>
        /// Discard the entire buffer to stop playing
        /// </summary>
        public void Reset()
        {
            if (buffer != null)
            {
                buffer.ClearBuffer();
            }
        }

        /// <summary>
        /// What period of time is covered by the amount of samples buffered?
        /// </summary>
        /// <returns></returns>
        public TimeSpan GetBufferedDuration()
        {
            return buffer == null ? new TimeSpan() : buffer.BufferedDuration;
        }

        /// <summary>
        /// Start or continue playing buffered samples
        /// </summary>
        public void Play()
        {
            if (dso != null)
            {
                dso.Play();
            }
        }

        /// <summary>
        /// Pause playing buffered samples
        /// </summary>
        public void Pause()
        {
            if (dso != null)
            {
                dso.Pause();
            }
        }

        /// <summary>
        /// Stop playing
        /// </summary>
        public void Stop()
        {
            if (dso != null)
            {
                dso.Stop();
            }
            if (buffer != null)
            {
                buffer.ClearBuffer();
            }
        }

        /// <summary>
        /// Is the player currently playing?
        /// </summary>
        /// <returns></returns>
        public bool Playing()
        {
            if (dso != null)
            {
                return dso.PlaybackState == NAudio.Wave.PlaybackState.Playing;
            }
            return false;
        }
    }
}

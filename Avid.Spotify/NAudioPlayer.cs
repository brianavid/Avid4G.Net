using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avid.Spotify
{
    class NAudioPlayer : IPlayer
    {
        NAudio.Wave.BufferedWaveProvider buffer;
        NAudio.Wave.DirectSoundOut dso;

        public int EnqueueSamples(int channels, int rate, byte[] samples, int frames)
        {
            if (buffer == null)
            {
                buffer = new NAudio.Wave.BufferedWaveProvider(new NAudio.Wave.WaveFormat(rate, channels));
                dso = new NAudio.Wave.DirectSoundOut(70);
                dso.Init(buffer);
                dso.Play();
            }
            int space = buffer.BufferLength - buffer.BufferedBytes;
            if (space > samples.Length)
            {
                buffer.AddSamples(samples, 0, samples.Length);
                return frames;
            }
            return 0;
        }

        public void Reset()
        {
            if (buffer != null)
            {
                buffer.ClearBuffer();
            }
        }

        public TimeSpan GetBufferedDuration()
        {
            return buffer == null ? new TimeSpan() : buffer.BufferedDuration;
        }

        public void Play()
        {
            if (dso != null)
            {
                dso.Play();
            }
        }

        public void Pause()
        {
            if (dso != null)
            {
                dso.Pause();
            }
        }

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

using System;
using System.Numerics;

namespace AsioRouter.Spectrum
{
    public class SpectrumAnalyzer
    {
        private readonly object sync = new object();
        private readonly float[] circularBuffer;
        private int pos;
        private readonly int fftSize;
        private float[]? latestMags;

        public SpectrumAnalyzer(int fftSize = 1024)
        {
            if (fftSize <= 0 || (fftSize & (fftSize - 1)) != 0) throw new ArgumentException("fftSize must be power of two and > 0", nameof(fftSize));
            this.fftSize = fftSize;
            circularBuffer = new float[fftSize];
            pos = 0;
        }

        // Feed interleaved samples and extract a single channel starting at startChannel
        public void FeedInterleaved(float[] interleaved, int channels, int startChannel)
        {
            if (interleaved == null) return;
            if (channels <= 0) channels = 1;
            int frames = interleaved.Length / channels;
            lock (sync)
            {
                for (int i = 0; i < frames; i++)
                {
                    int idx = i * channels + Math.Min(startChannel, channels - 1);
                    float v = interleaved.Length > idx ? interleaved[idx] : 0f;
                    circularBuffer[pos++] = v;
                    if (pos >= circularBuffer.Length) pos = 0;
                }
                // compute mags from ordered buffer
                ComputeMagnitudes();
            }
        }

        public float[]? GetLatestMagnitudes()
        {
            lock (sync)
            {
                if (latestMags == null) return null;
                var copy = new float[latestMags.Length];
                Array.Copy(latestMags, copy, copy.Length);
                return copy;
            }
        }

        private void ComputeMagnitudes()
        {
            // build ordered buffer starting from pos
            float[] data = new float[fftSize];
            int tail = circularBuffer.Length - pos;
            Array.Copy(circularBuffer, pos, data, 0, tail);
            if (pos > 0) Array.Copy(circularBuffer, 0, data, tail, pos);

            // apply Hamming window and prepare complex buffer
            Complex[] buf = new Complex[fftSize];
            for (int i = 0; i < fftSize; i++)
            {
                double w = 0.54 - 0.46 * Math.Cos(2.0 * Math.PI * i / (fftSize - 1));
                buf[i] = new Complex(data[i] * (float)w, 0);
            }

            // in-place FFT (Cooley-Tukey)
            int m = (int)Math.Log(fftSize, 2);
            // bit reversal
            for (int i = 0; i < fftSize; i++)
            {
                int j = 0;
                for (int bit = 0; bit < m; bit++) j = (j << 1) | ((i >> bit) & 1);
                if (j > i) { var tmp = buf[i]; buf[i] = buf[j]; buf[j] = tmp; }
            }

            for (int s = 1; s <= m; s++)
            {
                int len = 1 << s;
                int half = len >> 1;
                double theta = -2.0 * Math.PI / len;
                Complex wlen = new Complex(Math.Cos(theta), Math.Sin(theta));
                for (int i = 0; i < fftSize; i += len)
                {
                    Complex w = Complex.One;
                    for (int j = 0; j < half; j++)
                    {
                        Complex u = buf[i + j];
                        Complex v = buf[i + j + half] * w;
                        buf[i + j] = u + v;
                        buf[i + j + half] = u - v;
                        w *= wlen;
                    }
                }
            }

            int bins = fftSize / 2;
            float[] mags = new float[bins];
            for (int i = 0; i < bins; i++) mags[i] = (float)buf[i].Magnitude;

            latestMags = mags;
        }
    }
}

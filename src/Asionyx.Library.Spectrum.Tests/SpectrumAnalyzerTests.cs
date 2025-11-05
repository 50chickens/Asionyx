using NUnit.Framework;
using System;
using System.Linq;
namespace AsioRouter.Spectrum.Tests
{
    [TestFixture]
    public class SpectrumAnalyzerTests
    {
        // Helper to create a single-channel interleaved buffer containing a sine wave
        private static float[] GenerateSineWave(int fftSize, int sampleRate, double freq, double amplitude = 0.5)
        {
            // generate exactly fftSize samples (one frame)
            var buf = new float[fftSize];
            for (int n = 0; n < fftSize; n++)
            {
                double t = n / (double)sampleRate;
                buf[n] = (float)(amplitude * Math.Sin(2.0 * Math.PI * freq * t));
            }
            return buf;
        }

        [Test]
        public void SingleTone_PeakAtExpectedBin()
        {
            int fftSize = 1024;
            int sampleRate = 48000;
            double freq = 1000.0; // 1 kHz test tone
            double amplitude = 0.6;

            var analyzer = new SpectrumAnalyzer(fftSize);

            // feed a single frame of interleaved samples (1 channel)
            var frame = GenerateSineWave(fftSize, sampleRate, freq, amplitude);
            analyzer.FeedInterleaved(frame, channels: 1, startChannel: 0);

            var mags = analyzer.GetLatestMagnitudes();
            Assert.IsNotNull(mags, "Magnitudes should not be null");
            Assert.AreEqual(fftSize / 2, mags.Length, "Expected bins = fftSize/2");

            // expected bin index for frequency: bin = freq * fftSize / sampleRate
            int expectedBin = (int)Math.Round(freq * fftSize / (double)sampleRate);
            expectedBin = Math.Clamp(expectedBin, 0, mags.Length - 1);

            int maxBin = Array.IndexOf(mags, mags.Max());

            // Peak should be very near expected bin (allow small tolerance due to windowing)
            Assert.LessOrEqual(Math.Abs(maxBin - expectedBin), 2, $"Peak bin {maxBin} not within 2 bins of expected {expectedBin}");

            // Peak magnitude sanity checks
            float peak = mags[maxBin];
            Assert.Greater(peak, 1e-4f, "Peak magnitude too small (likely no signal)");
            // neighboring bins should be noticeably lower; allow for spectral leakage when tone doesn't fall exactly on a bin
            float neighbor = Math.Max(mags[Math.Max(0, maxBin - 1)], mags[Math.Min(mags.Length - 1, maxBin + 1)]);
            double dbPeak = 20.0 * Math.Log10(Math.Max(1e-9, peak));
            double dbNeighbor = 20.0 * Math.Log10(Math.Max(1e-9, neighbor));
            // relax threshold to 2 dB to account for bin misalignment and windowing leakage in single-frame tests
            Assert.Greater(dbPeak - dbNeighbor, 2.0, "Peak is not sufficiently higher than neighbors (expected a clear tone)");
        }

        [Test]
        public void TwoTone_ShowsTwoPeaks()
        {
            int fftSize = 1024;
            int sampleRate = 48000;
            double f1 = 800.0;
            double f2 = 5000.0;
            double amplitude = 0.4;

            var analyzer = new SpectrumAnalyzer(fftSize);

            // build buffer containing both tones added (single channel)
            var buf = new float[fftSize];
            for (int n = 0; n < fftSize; n++)
            {
                double t = n / (double)sampleRate;
                buf[n] = (float)(amplitude * Math.Sin(2.0 * Math.PI * f1 * t) + amplitude * Math.Sin(2.0 * Math.PI * f2 * t));
            }

            analyzer.FeedInterleaved(buf, channels: 1, startChannel: 0);
            var mags = analyzer.GetLatestMagnitudes();
            Assert.IsNotNull(mags);

            int bin1 = (int)Math.Round(f1 * fftSize / (double)sampleRate);
            int bin2 = (int)Math.Round(f2 * fftSize / (double)sampleRate);
            bin1 = Math.Clamp(bin1, 0, mags.Length - 1);
            bin2 = Math.Clamp(bin2, 0, mags.Length - 1);

            // find local maxima near expected bins
            int found1 = Array.IndexOf(mags, mags.Skip(Math.Max(0, bin1 - 3)).Take(7).Max());
            int found2 = Array.IndexOf(mags, mags.Skip(Math.Max(0, bin2 - 3)).Take(7).Max());

            // ensure peaks are near expected locations
            Assert.LessOrEqual(Math.Abs(found1 - bin1), 3, $"First tone peak not near expected bin {bin1}");
            Assert.LessOrEqual(Math.Abs(found2 - bin2), 3, $"Second tone peak not near expected bin {bin2}");
        }
    }
}
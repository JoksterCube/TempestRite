using System;

namespace JoksterCube.TempestRite.Domain;

// The Plume-Ward Rite's landing, made in code so it sounds like nothing else in the game: one short, soft airy puff
// (noise that darkens as it fades), nothing after it. About a sixth of a second, stereo samples at SampleRate.
internal static class PlumeSound
{
    internal const int SampleRate = 44100;
    private const double Length = 0.16;
    private const uint Seed = 7;

    internal static void Samples(out float[] left, out float[] right)
    {
        int count = (int)(Length * SampleRate);
        double[] samples = new double[count];
        uint state = Seed;
        double low = 0;
        for (int i = 0; i < count; i++)
        {
            double t = (double)i / SampleRate;

            // The puff: white noise through a one-pole low-pass sweeping from 1.1 kHz down to 400 Hz (soft, not hissy).
            state = state * 1664525u + 1013904223u;
            double noise = (state >> 8) / (double)(1 << 24) * 2 - 1;
            double cutoff = 400 + 700 * Math.Exp(-t / 0.04);
            double a = 1 - Math.Exp(-2 * Math.PI * cutoff / SampleRate);
            low += a * (noise - low);
            samples[i] = low * Math.Min(1, t / 0.025) * Math.Exp(-t / 0.035);
        }

        double peak = 1e-6;
        foreach (double sample in samples) peak = Math.Max(peak, Math.Abs(sample));
        double gain = 0.7 / peak;
        // The last 40 ms fade to silence so it ends without a click.
        int tail = SampleRate / 25;
        left = new float[count];
        right = new float[count];
        for (int i = 0; i < count; i++)
        {
            double end = Math.Min(1, (count - 1 - i) / (double)tail);
            left[i] = right[i] = (float)(samples[i] * gain * end);
        }
    }
}

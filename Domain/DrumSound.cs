using System;

namespace JoksterCube.TempestRite.Domain;

// The kinship rites' call, heard by everyone on the server when one is burnt: a war-drum struck three times, the
// last hit lower, louder and ringing longest (DUM - DUM - DUUUM). Made in code (the game has no drum of its own).
// - Each hit is a deep skin: a low note that drops quickly to its pitch as the skin settles, two fainter overtones
//   that fade sooner, and a soft thump of air as the stick lands.
// - A little warmth (soft saturation), then a stereo room: a few early echoes and a darker, fading tail.
// Rendered once (plain math, a few milliseconds), as stereo samples at SampleRate.
internal static class DrumSound
{
    internal const int SampleRate = 44100;
    // From the first hit to the end of the tail.
    internal const double Length = 3.4;
    // The very end eases out over this long, so nothing is cut off.
    private const double FadeOut = 0.3;
    // When the last (big) hit lands: whatever follows the drum starts about then.
    internal const float LastHit = 0.8f;

    // Each hit: when it lands (s), how loud, how long the skin rings (s), the pitch it settles at (Hz).
    private static readonly (double At, double Loud, double Ring, double Low)[] Hits =
    [
        (0.00, 0.70, 0.28, 60),
        (0.40, 0.70, 0.28, 60),
        (LastHit, 1.00, 0.65, 50)
    ];

    // The skin starts this much higher and drops to its pitch over about DropTime.
    private const double PitchDrop = 1.8;
    private const double DropTime = 0.035;
    private const double Peak = 0.9;
    private const uint Seed = 7;

    internal static void Samples(out float[] left, out float[] right)
    {
        int length = (int)(Length * SampleRate);
        double[] dry = new double[length];
        uint seed = Seed;
        foreach ((double at, double loud, double ring, double low) in Hits)
        {
            int start = (int)(at * SampleRate);
            double phase = 0, air = 0;
            for (int i = start; i < length; i++)
            {
                double t = (i - start) / (double)SampleRate;
                double envelope = Math.Exp(-t / ring);
                if (envelope < 1e-4) break;
                double frequency = low * (1 + (PitchDrop - 1) * Math.Exp(-t / DropTime));
                phase += 2 * Math.PI * frequency / SampleRate;
                double body = Math.Sin(phase) * envelope;
                double skin = Math.Sin(phase * 1.59 + 0.3) * 0.35 * Math.Exp(-t / (ring * 0.35))
                    + Math.Sin(phase * 2.14) * 0.15 * Math.Exp(-t / (ring * 0.2));
                // The stick's thump: noise, softened, gone in a few hundredths of a second.
                seed = seed * 1664525u + 1013904223u;
                double white = (seed >> 8) / (double)(1 << 24) * 2 - 1;
                air += (white - air) * 0.25;
                double thump = air * 1.2 * Math.Exp(-t / 0.015);
                dry[i] += loud * (body + skin + thump);
            }
        }
        for (int i = 0; i < length; i++) dry[i] = Math.Tanh(1.4 * dry[i]);

        double[] l = Room(dry, [0.029, 0.061, 0.097, 0.143], [0.32, 0.22, 0.15, 0.09], 0.083);
        double[] r = Room(dry, [0.037, 0.071, 0.109, 0.157], [0.30, 0.20, 0.14, 0.08], 0.091);
        double peak = 1e-9;
        for (int i = 0; i < length; i++) peak = Math.Max(peak, Math.Max(Math.Abs(l[i]), Math.Abs(r[i])));
        double scale = Peak / peak;
        int fadeFrom = length - (int)(FadeOut * SampleRate);
        left = new float[length];
        right = new float[length];
        for (int i = 0; i < length; i++)
        {
            double fade = i < fadeFrom ? 1 : (length - 1 - i) / (double)(length - 1 - fadeFrom);
            left[i] = (float)(l[i] * scale * fade);
            right[i] = (float)(r[i] * scale * fade);
        }
    }

    // One side of the room: the drum, its early echoes, and a darker loop that fades into a tail.
    private static double[] Room(double[] dry, double[] delays, double[] gains, double loop)
    {
        int length = dry.Length;
        double[] wet = (double[])dry.Clone();
        for (int tap = 0; tap < delays.Length; tap++)
        {
            int delay = (int)(delays[tap] * SampleRate);
            for (int i = delay; i < length; i++) wet[i] += dry[i - delay] * gains[tap];
        }
        int back = (int)(loop * SampleRate);
        double[] tail = new double[length];
        double soft = 0;
        for (int i = 0; i < length; i++)
        {
            double fed = dry[i] * 0.3 + (i >= back ? tail[i - back] * 0.45 : 0);
            soft += (fed - soft) * 0.3;
            tail[i] = soft;
            wet[i] += soft * 0.6;
        }
        return wet;
    }
}

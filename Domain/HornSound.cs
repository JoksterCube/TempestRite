using System;
using System.Threading;

namespace JoksterCube.TempestRite.Domain;

// The Rally-Horn Rite's horn: a joyful brass call made in code (the game has no horn of its own), shaped after a
// recording of a Viking horn, measured frame by frame, then made a little lower and brassier.
// - It starts with a quiet, buzzy slide up, eases off for a moment, then pops into the open note a fifth above.
// - The open note swells for ~0.7 s, peaks, and fades to half before it ends; it opens up (brighter) as it gets loud.
// - Each overtone drifts a few dB on its own, slowly, as a player's lips do; the pitch holds almost still.
// - A faint breath runs through it; a wide stereo space gives it echoes off near rocks and a long tail all around.
// Rendered once (on a worker thread, it is plain math), as stereo samples at SampleRate.
internal static class HornSound
{
    internal const int SampleRate = 44100;

    // The style chosen: four semitones and a bit (5) below the recording, fuller middle and bottom, opening up when
    // loud, the recording's metal turned down a little, steadier and with less breath.
    private const double Pitch = 0.7491535384383408; // 2^(-5/12)
    private const double Body = 4, Low = 3, Blare = 3, Sheen = -4, Move = 0.75, AirScale = 0.7;
    private const uint Seed = 1;

    private const int Overtones = 16;
    private const double Length = 4.4;
    private const double SoundEnd = 2.2;
    private const double HoldFrom = 0.30, HoldBlend = 0.10;

    // Measured from the recording (its held note is 361 Hz); scaled by Pitch.
    private static readonly double[,] PitchCurve =
    {
        { 0.0, 208 }, { 0.06, 222 }, { 0.10, 225 }, { 0.15, 226 }, { 0.20, 236 }, { 0.235, 244 },
        // The lift: eased off, a quick slide up, then the open note lands a touch sharp and settles.
        { 0.25, 275 }, { 0.27, 300 }, { 0.28, 336 }, { 0.29, 362 }, { 0.30, 369 }, { 0.32, 365.5 }, { 0.36, 363 },
        { 0.40, 360 }, { 0.45, 362.5 }, { 0.6, 362 }, { 1.0, 360.7 }, { 1.8, 359.5 }, { 1.98, 358 }, { 2.06, 354 },
        { 2.2, 352 }
    };

    private static readonly double[,] LoudCurve =
    {
        { 0.0, 0.0 }, { 0.04, 0.05 }, { 0.08, 0.15 }, { 0.12, 0.21 }, { 0.20, 0.26 }, { 0.23, 0.22 },
        // The breath eases during the slide (hiding it), then the open note pops in and blooms.
        { 0.25, 0.11 }, { 0.27, 0.13 }, { 0.28, 0.22 }, { 0.30, 0.48 }, { 0.34, 0.47 }, { 0.38, 0.33 }, { 0.41, 0.36 },
        { 0.46, 0.52 }, { 0.52, 0.70 }, { 0.62, 0.82 }, { 0.72, 0.76 }, { 0.84, 0.90 }, { 0.96, 0.97 }, { 1.2, 1.0 },
        { 1.4, 0.87 }, { 1.6, 0.68 }, { 1.8, 0.57 }, { 1.95, 0.50 }, { 2.03, 0.16 }, { 2.12, 0.0 }
    };

    // Air, in dB below the note: none at the start, a small burst where the lips jump, faint breath through the call.
    private static readonly double[,] AirCurve =
    {
        { 0.0, -60 }, { 0.10, -50 }, { 0.20, -42 }, { 0.25, -30 }, { 0.29, -40 }, { 0.40, -44 }, { 1.0, -50 },
        { 1.9, -52 }, { 2.03, -38 }, { 2.15, -40 }
    };

    // Overtones 2..8 in dB below the note, over time: buzzy slide, then the call brightening and mellowing.
    private static readonly double[] ToneTimes = [0.00, 0.10, 0.23, 0.26, 0.29, 0.32, 0.40, 0.70, 1.00, 1.40, 1.90];
    private static readonly double[][] Tones =
    [
        [-8, -22, -34, -42, -44, -44, -48],
        [-1, -8, -20, -22, -29, -37, -39],
        [-1, -14, -24, -24, -36, -38, -42],
        [-12, -28, -22, -32, -38, -42, -50],
        [-14, -22, -33, -38, -40, -44, -52],
        [-18, -19, -30, -33, -36, -40, -48],
        [-10, -17, -26, -26, -27, -31, -40],
        [-15, -19, -24, -24, -25, -28, -38],
        [-20, -21, -24, -24, -26, -28, -38],
        [-20, -27, -26, -32, -30, -34, -48],
        [-19, -24, -30, -36, -36, -44, -48]
    ];

    // The held note's overtones 2..16 (dB below the note) from the recording, the high ones its metallic sheen; and
    // the correction found by measuring the result against it.
    private static readonly double[] Hold = [-20, -23, -25, -28, -29, -31, -44, -43, -52, -55, -41, -48, -49, -50, -53];
    private static readonly double[] Calibration = [3.2, 8.8, 2.4, 12.8, 0.8, 2.4, -0.8, 8.8, -4.8, 15.2, 9.6, 8.8, 10.4, 7.2, 12.0];

    private static float[]? _left, _right;
    private static volatile bool _ready;
    private static int _started;
    private static readonly object Lock = new();

    // Starts rendering on a worker thread (at game start), so it is long ready when the rite is first called.
    internal static void Prepare()
    {
        if (Interlocked.Exchange(ref _started, 1) == 1) return;
        ThreadPool.QueueUserWorkItem(_ =>
        {
            try { Samples(out float[] _, out float[] _); }
            catch (Exception exception) { Plugin.ModLogger.LogWarning($"The Rally-Horn Rite's horn could not be made: {exception.Message}"); }
        });
    }

    // The samples if they are rendered already; never waits.
    internal static bool TryGet(out float[] left, out float[] right)
    {
        left = right = [];
        if (!_ready) return false;
        left = _left!;
        right = _right!;
        return true;
    }

    // The horn's stereo samples (-1..1), rendering them now if not done yet (waits for a render under way).
    internal static void Samples(out float[] left, out float[] right)
    {
        lock (Lock)
        {
            if (!_ready)
            {
                Render(out _left, out _right);
                _ready = true;
            }
            left = _left!;
            right = _right!;
        }
    }

    private static void Render(out float[] left, out float[] right)
    {
        double[] dry = Call();
        OpenAir(dry, out double[] l, out double[] r);
        // Remove any offset, then bring both sides to the same peak.
        RemoveOffset(l);
        RemoveOffset(r);
        double peak = 1e-9;
        for (int i = 0; i < l.Length; i++) peak = Math.Max(peak, Math.Max(Math.Abs(l[i]), Math.Abs(r[i])));
        double scale = 0.885 / peak;
        left = new float[l.Length];
        right = new float[r.Length];
        for (int i = 0; i < l.Length; i++)
        {
            left[i] = (float)(l[i] * scale);
            right[i] = (float)(r[i] * scale);
        }
    }

    // ---- The horn itself (mono) ----

    private static double[] Call()
    {
        PythonRandom random = new(Seed);
        double[] buffer = new double[(int)(Length * SampleRate)];
        double[] phases = new double[Overtones];
        for (int k = 0; k < Overtones; k++) phases[k] = random.NextDouble() * 2 * Math.PI;
        double[] drift = new double[Overtones], driftTo = new double[Overtones];
        double[] tone = new double[7], toneMid = new double[7], held = new double[Overtones - 1];
        ToneAt(1.0, toneMid);
        double phase = 0, noise1 = 0, noise2 = 0;

        int count = (int)(SoundEnd * SampleRate);
        for (int i = 0; i < count; i++)
        {
            double t = (double)i / SampleRate;
            double loud = Curve(t, LoudCurve);
            if (loud <= 0 && t > 1) break;
            double f = Curve(t, PitchCurve) * Pitch;
            f *= 1 + 0.0013 * Math.Sin(2 * Math.PI * 3.4 * t);
            phase += 2 * Math.PI * f / SampleRate;
            // Each overtone's own slow drift (dB), eased toward a new aim every ~80 ms.
            if (i % 3528 == 0)
                for (int k = 1; k < Overtones; k++)
                    driftTo[k] = (random.NextDouble() * 2 - 1) * (1.5 + 0.35 * Math.Min(k, 10)) * Move;
            ToneAt(t, tone);
            // From the lift's tone into the held note's.
            double h = Math.Min(1.0, Math.Max(0.0, (t - HoldFrom) / HoldBlend));
            if (h > 0) HoldTone(tone, toneMid, loud, held);

            double s = Math.Sin(phase + phases[0]);
            for (int k = 2; k <= Overtones; k++)
            {
                if (f * k > 12000) break;
                drift[k - 1] += (driftTo[k - 1] - drift[k - 1]) * 0.0006;
                double db = k <= 8 ? tone[k - 2] : tone[6] - 6 * (k - 8);
                if (h > 0) db = db * (1 - h) + held[k - 2] * h;
                s += Math.Pow(10, (db + drift[k - 1]) / 20) * Math.Sin(k * phase + phases[k - 1]);
            }
            // Breath: noise in the 1-4 kHz band, buzzing with the note (air through the horn, not hiss).
            double w = random.NextDouble() * 2 - 1;
            noise1 += 0.45 * (w - noise1);
            noise2 += 0.12 * (noise1 - noise2);
            double air = (noise1 - noise2) * (0.6 + 0.4 * Math.Sin(phase)) * Math.Pow(10, Curve(t, AirCurve) / 20) * 5 * AirScale;
            buffer[i] = loud * (s + air);
        }
        // A soft rounding of the loudest peaks.
        double norm = Math.Tanh(1.1);
        for (int i = 0; i < buffer.Length; i++) buffer[i] = Math.Tanh(1.1 * buffer[i]) / norm;
        return buffer;
    }

    // The held note's overtones 2..16: the recording's, moving with the call as it brightens and mellows, made fuller
    // (Body on 3-7, Low on 2-3), its metal (9-16) set by Sheen, and opening up as it gets loud (Blare).
    private static void HoldTone(double[] toneNow, double[] toneMid, double loud, double[] held)
    {
        for (int k = 2; k <= Overtones; k++)
        {
            int j = Math.Min(k, 8) - 2;
            double db = Hold[k - 2] + Calibration[k - 2] + (toneNow[j] - toneMid[j]);
            if (k >= 3 && k <= 7) db += Body;
            if (k >= 9) db += Sheen;
            if (k == 2) db += Low;
            if (k == 3) db += Low / 2;
            if (k >= 3) db += Blare * (loud - 0.6) * 2.5 * Math.Min(1.0, k / 8.0);
            held[k - 2] = db;
        }
    }

    private static void ToneAt(double t, double[] into)
    {
        for (int i = 0; i < ToneTimes.Length - 1; i++)
        {
            if (t > ToneTimes[i + 1]) continue;
            double k = t >= ToneTimes[i] ? (t - ToneTimes[i]) / (ToneTimes[i + 1] - ToneTimes[i]) : 0.0;
            for (int j = 0; j < into.Length; j++) into[j] = Tones[i][j] + (Tones[i + 1][j] - Tones[i][j]) * k;
            return;
        }
        Array.Copy(Tones[Tones.Length - 1], into, into.Length);
    }

    // Linear between (time, value) points, flat outside.
    private static double Curve(double t, double[,] points)
    {
        int n = points.GetLength(0);
        if (t <= points[0, 0]) return points[0, 1];
        for (int i = 1; i < n; i++)
        {
            if (t > points[i, 0]) continue;
            double t0 = points[i - 1, 0], v0 = points[i - 1, 1];
            return v0 + (points[i, 1] - v0) * (t - t0) / (points[i, 0] - t0);
        }
        return points[n - 1, 1];
    }

    // ---- The space around it (stereo) ----

    private static readonly int[] Combs = [1116, 1188, 1277, 1356, 1422, 1491, 1557, 1617];
    private static readonly int[] Allpasses = [556, 441, 341, 225];
    private static readonly double[,] EarlyLeft = { { 0.017, 0.55 }, { 0.031, 0.40 }, { 0.047, 0.32 }, { 0.071, 0.22 } };
    private static readonly double[,] EarlyRight = { { 0.023, 0.55 }, { 0.037, 0.40 }, { 0.056, 0.30 }, { 0.083, 0.20 } };

    // A wide stereo space: the horn in the middle, its echo coming back from all around. Freeverb-style, eight combs
    // and four allpasses a side, the right side's a little longer so the two differ (what makes it sound open); plus
    // early echoes off near rocks, different on each side.
    private static void OpenAir(double[] dry, out double[] left, out double[] right)
    {
        const double Rt60 = 3.0, WetDb = -8.0, Early = 0.5, PreDelay = 0.035, Damp = 0.35;
        int n = dry.Length;
        int pre = (int)(PreDelay * SampleRate);
        double[] source = new double[n];
        Array.Copy(dry, 0, source, pre, n - pre);

        double[] wetLeft = Reverb(source, 0, Rt60, Damp);
        double[] wetRight = Reverb(source, 23, Rt60, Damp);
        double peakDry = 1e-9, peakWet = 1e-9;
        for (int i = 0; i < n; i++)
        {
            peakDry = Math.Max(peakDry, Math.Abs(dry[i]));
            peakWet = Math.Max(peakWet, Math.Max(Math.Abs(wetLeft[i]), Math.Abs(wetRight[i])));
        }
        double scale = Math.Pow(10, WetDb / 20) * peakDry / peakWet;
        AddEarly(dry, wetLeft, EarlyLeft, Early / scale);
        AddEarly(dry, wetRight, EarlyRight, Early / scale);

        left = new double[n];
        right = new double[n];
        for (int i = 0; i < n; i++)
        {
            left[i] = dry[i] + scale * wetLeft[i];
            right[i] = dry[i] + scale * wetRight[i];
        }
    }

    private static double[] Reverb(double[] source, int spread, double rt60, double damp)
    {
        int n = source.Length;
        double[] wet = new double[n];
        foreach (int length in Combs)
        {
            int d = (int)((length + spread) * 1.15);
            double gain = Math.Pow(10, -3.0 * d / (rt60 * SampleRate));
            double[] line = new double[d];
            int index = 0;
            double store = 0;
            for (int i = 0; i < n; i++)
            {
                double y = line[index];
                store = y * (1 - damp) + store * damp;
                line[index] = source[i] + store * gain;
                index = (index + 1) % d;
                wet[i] += y;
            }
        }
        foreach (int length in Allpasses)
        {
            int d = length + spread;
            double[] line = new double[d];
            int index = 0;
            for (int i = 0; i < n; i++)
            {
                double b = line[index];
                line[index] = wet[i] + b * 0.5;
                index = (index + 1) % d;
                wet[i] = b - wet[i];
            }
        }
        return wet;
    }

    private static void AddEarly(double[] dry, double[] side, double[,] taps, double scale)
    {
        for (int tap = 0; tap < taps.GetLength(0); tap++)
        {
            int d = (int)(taps[tap, 0] * SampleRate);
            double gain = taps[tap, 1] * scale;
            for (int i = d; i < dry.Length; i++) side[i] += dry[i - d] * gain;
        }
    }

    // Python's random number generator (Mersenne Twister, seeded and read as Python does), so the horn comes out
    // exactly as the one chosen from the Python sketches: its overtones' drift and breath follow these numbers.
    private sealed class PythonRandom
    {
        private readonly uint[] _state = new uint[624];
        private int _index = 625;

        internal PythonRandom(uint seed)
        {
            // random.seed(n) for a small positive int: init_by_array with the one key n.
            Init(19650218u);
            uint[] key = [seed];
            int i = 1, j = 0;
            for (int k = Math.Max(624, key.Length); k > 0; k--)
            {
                _state[i] = (_state[i] ^ ((_state[i - 1] ^ (_state[i - 1] >> 30)) * 1664525u)) + key[j] + (uint)j;
                i++;
                j++;
                if (i >= 624)
                {
                    _state[0] = _state[623];
                    i = 1;
                }
                if (j >= key.Length) j = 0;
            }
            for (int k = 623; k > 0; k--)
            {
                _state[i] = (_state[i] ^ ((_state[i - 1] ^ (_state[i - 1] >> 30)) * 1566083941u)) - (uint)i;
                i++;
                if (i >= 624)
                {
                    _state[0] = _state[623];
                    i = 1;
                }
            }
            _state[0] = 0x80000000u;
        }

        private void Init(uint seed)
        {
            _state[0] = seed;
            for (int i = 1; i < 624; i++) _state[i] = 1812433253u * (_state[i - 1] ^ (_state[i - 1] >> 30)) + (uint)i;
            _index = 624;
        }

        private uint Next()
        {
            if (_index >= 624)
            {
                for (int k = 0; k < 624; k++)
                {
                    uint y = (_state[k] & 0x80000000u) | (_state[(k + 1) % 624] & 0x7fffffffu);
                    _state[k] = _state[(k + 397) % 624] ^ (y >> 1) ^ ((y & 1u) != 0 ? 0x9908b0dfu : 0u);
                }
                _index = 0;
            }
            uint x = _state[_index++];
            x ^= x >> 11;
            x ^= (x << 7) & 0x9d2c5680u;
            x ^= (x << 15) & 0xefc60000u;
            x ^= x >> 18;
            return x;
        }

        // random.random(): 53 random bits in [0, 1).
        internal double NextDouble()
        {
            uint a = Next() >> 5, b = Next() >> 6;
            return (a * 67108864.0 + b) * (1.0 / 9007199254740992.0);
        }
    }

    // A gentle high-pass at ~20 Hz, so the sound doesn't click.
    private static void RemoveOffset(double[] buffer)
    {
        double x1 = 0, y1 = 0;
        for (int i = 0; i < buffer.Length; i++)
        {
            double x = buffer[i];
            y1 = x - x1 + 0.997 * y1;
            x1 = x;
            buffer[i] = y1;
        }
    }
}

using System;
using System.IO;
using JoksterCube.TempestRite.Domain;
using Xunit;

namespace JoksterCube.TempestRite.Tests;

// The kinship rites' war-drum: renders sanely, and (with TEMPESTRITE_HORN_OUT set to a folder) writes it as a WAV to
// listen to, beside the horn.
public class DrumPreview
{
    [Fact]
    public void Drum_renders()
    {
        DrumSound.Samples(out float[] left, out float[] right);

        Assert.Equal(left.Length, right.Length);
        Assert.Equal((int)(DrumSound.Length * DrumSound.SampleRate), left.Length);
        float peak = 0f;
        for (int i = 0; i < left.Length; i++)
        {
            Assert.False(float.IsNaN(left[i]) || float.IsNaN(right[i]));
            peak = Math.Max(peak, Math.Max(Math.Abs(left[i]), Math.Abs(right[i])));
        }
        Assert.InRange(peak, 0.85f, 0.91f);
        // The last hit is the loudest moment, and the tail has died down by the end.
        Assert.True(Loudest(left) >= (int)(DrumSound.LastHit * DrumSound.SampleRate), "the last hit is the biggest");
        Assert.True(Math.Abs(left[left.Length - 1]) < 0.02f, "the tail fades out");

        string? output = Environment.GetEnvironmentVariable("TEMPESTRITE_HORN_OUT");
        if (string.IsNullOrEmpty(output)) return;
        Directory.CreateDirectory(output);
        Write(Path.Combine(output, "kinship_drum.wav"), left, right);
    }

    private static int Loudest(float[] samples)
    {
        int at = 0;
        for (int i = 1; i < samples.Length; i++) if (Math.Abs(samples[i]) > Math.Abs(samples[at])) at = i;
        return at;
    }

    private static void Write(string path, float[] left, float[] right)
    {
        using BinaryWriter writer = new(File.Create(path));
        int bytes = left.Length * 4;
        writer.Write("RIFF"u8.ToArray());
        writer.Write(36 + bytes);
        writer.Write("WAVEfmt "u8.ToArray());
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)2);
        writer.Write(DrumSound.SampleRate);
        writer.Write(DrumSound.SampleRate * 4);
        writer.Write((short)4);
        writer.Write((short)16);
        writer.Write("data"u8.ToArray());
        writer.Write(bytes);
        for (int i = 0; i < left.Length; i++)
        {
            writer.Write((short)Math.Round(left[i] * 32767));
            writer.Write((short)Math.Round(right[i] * 32767));
        }
    }
}

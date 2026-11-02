using System;
using System.Diagnostics;
using System.IO;
using JoksterCube.TempestRite.Domain;
using Xunit;

namespace JoksterCube.TempestRite.Tests;

// The Rally-Horn Rite's horn: renders sanely, and (with TEMPESTRITE_HORN_OUT set to a folder) writes it as a WAV to
// listen to.
public class HornPreview
{
    [Fact]
    public void Horn_renders()
    {
        Stopwatch watch = Stopwatch.StartNew();
        HornSound.Samples(out float[] left, out float[] right);
        watch.Stop();

        Assert.Equal(left.Length, right.Length);
        Assert.True(left.Length > HornSound.SampleRate * 3, "about four seconds long");
        float peak = 0f;
        for (int i = 0; i < left.Length; i++) peak = Math.Max(peak, Math.Max(Math.Abs(left[i]), Math.Abs(right[i])));
        Assert.InRange(peak, 0.8f, 0.9f);
        foreach (float sample in left) Assert.False(float.IsNaN(sample));

        string? output = Environment.GetEnvironmentVariable("TEMPESTRITE_HORN_OUT");
        if (string.IsNullOrEmpty(output)) return;
        Directory.CreateDirectory(output);
        Write(Path.Combine(output, "rally_horn.wav"), left, right);
        File.WriteAllText(Path.Combine(output, "rally_horn_ms.txt"), watch.ElapsedMilliseconds.ToString());
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
        writer.Write(HornSound.SampleRate);
        writer.Write(HornSound.SampleRate * 4);
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

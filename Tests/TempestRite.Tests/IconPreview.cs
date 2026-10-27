using System;
using System.IO;
using JoksterCube.TempestRite.Domain;
using UnityEngine;
using Xunit;

namespace JoksterCube.TempestRite.Tests;

// Renders every rite's status icon with the mod's own drawing code, for the README. Runs only when
// TEMPESTRITE_ICON_ART (the game's pictures as .rgba files, see Tools/Icon/rite_previews.py) and TEMPESTRITE_ICON_OUT
// are set; otherwise it does nothing.
// .rgba: width and height (int32), then width x height RGBA bytes, row 0 at the top.
public class IconPreview
{
    [Fact]
    public void Render_rite_icons()
    {
        string? art = Environment.GetEnvironmentVariable("TEMPESTRITE_ICON_ART");
        string? output = Environment.GetEnvironmentVariable("TEMPESTRITE_ICON_OUT");
        if (string.IsNullOrEmpty(art) || string.IsNullOrEmpty(output)) return;
        Directory.CreateDirectory(output);

        WeatherIcons.PreviewArt = name =>
        {
            string path = Path.Combine(art, name.Replace("item:", "item_") + ".rgba");
            if (!File.Exists(path)) return null;
            (int width, int height, byte[] bytes) = Read(path);
            Color[] colors = new Color[width * height];
            for (int i = 0; i < colors.Length; i++)
                colors[i] = new Color(bytes[i * 4] / 255f, bytes[i * 4 + 1] / 255f, bytes[i * 4 + 2] / 255f, bytes[i * 4 + 3] / 255f);
            return (width, height, colors);
        };
        try
        {
            foreach (WeatherKind kind in WeatherKinds.All)
            {
                Color[]? colors = WeatherIcons.Render(kind);
                if (colors == null) continue;
                byte[] bytes = new byte[colors.Length * 4];
                for (int i = 0; i < colors.Length; i++)
                {
                    bytes[i * 4] = Byte(colors[i].r);
                    bytes[i * 4 + 1] = Byte(colors[i].g);
                    bytes[i * 4 + 2] = Byte(colors[i].b);
                    bytes[i * 4 + 3] = Byte(colors[i].a);
                }
                using BinaryWriter writer = new(File.Create(Path.Combine(output, kind + ".rgba")));
                writer.Write(64);
                writer.Write(64);
                writer.Write(bytes);
            }
        }
        finally { WeatherIcons.PreviewArt = null; }
    }

    private static byte Byte(float value) => (byte)Math.Round(Math.Clamp(value, 0f, 1f) * 255f);

    private static (int, int, byte[]) Read(string path)
    {
        using BinaryReader reader = new(File.OpenRead(path));
        int width = reader.ReadInt32(), height = reader.ReadInt32();
        return (width, height, reader.ReadBytes(width * height * 4));
    }
}

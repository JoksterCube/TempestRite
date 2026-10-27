using System;
using System.Collections.Generic;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// Status icons put together when first needed, in the game's own painted style and mostly from its own art, so the
// mod ships no image files:
// - Clear skies: a glowing sun, painted here like the game's.
// - Rain: a cloud filled with the game's smoke texture, over the game's rain drops (the Wet icon).
// - Thunderstorm: a darker cloud with the game's glowing lightning bolt.
// - Mist: soft bands of the game's smoke texture.
// - Polar day: the sun over a snowy hill. Polar night: a crescent moon on the game's starry Cold icon.
// - True Sight: the game's own wisp icon. Sootfall: bands of ash with glowing embers.
// - Tailwind: the game's sail and wind lines. Crypt Delver: its water drops under a "no" sign. Verdant: a glowing
//   seedling with sparkles. Packward: a wolf running off. Carrion: a boar under a green smell.
// - Vat-Boil: the fermenter with green bubbles. Amber-Mend: the hammer over glowing resin.
//   Abyssal Surge: the serpent trophy over a fish and waves.
// - Glacial Satiety: a stew under frost. Vein-Shatter: the antler pickaxe over cracked copper ore. Carapace Temper:
//   the carapace breastplate in an amber glow. Grave-Bind: the torn spirit under a beam of light. Fortune's Boon: the wishbone
//   over coins, with golden sparkles. Luminary: a wisp in a wide warm glow. Abyssal Draw: chitin with sparkles drawn
//   in. Sovereign Ward: the majestic carapace under a golden dome. Unmaking: the thunderstone with the game's
//   lightning.
// If one of the game's sprites can't be found (renamed in an update), a plain painted shape stands in.
internal static class WeatherIcons
{
    private const int Size = 64;

    private static readonly Dictionary<WeatherKind, Sprite> Cache = [];

    // Outside the game (README previews): the game's pictures by name ("item:Wisp" for an item's icon), as width,
    // height and colors with row 0 at the top. Null in the game.
    internal static Func<string, (int Width, int Height, Color[] Colors)?>? PreviewArt;

    internal static Sprite? For(WeatherKind kind)
    {
        if (Cache.TryGetValue(kind, out Sprite sprite) && sprite) return sprite;
        Canvas? canvas = Picture(kind);
        if (canvas == null) return null;
        sprite = canvas.ToSprite($"TempestRite_{kind}");
        Cache[kind] = sprite;
        return sprite;
    }

    // The icon's 64 x 64 colors, row 0 at the top, without making a sprite (README previews).
    internal static Color[]? Render(WeatherKind kind) => Picture(kind)?.Colors;

    private static Canvas? Picture(WeatherKind kind)
    {
        Canvas canvas = new();
        switch (kind)
        {
            case WeatherKind.Clear:
                Sun(canvas);
                break;
            case WeatherKind.Rain:
                Cloud(canvas, 32f, 20f, new Color(0.86f, 0.89f, 0.94f));
                if (GameArt.Get("Wet") is { } wet) canvas.Draw(wet, 18f, 35f, 29f, 30f);
                else Drops(canvas);
                break;
            case WeatherKind.Thunderstorm:
                Cloud(canvas, 32f, 20f, new Color(0.46f, 0.50f, 0.60f));
                if (GameArt.Get("Lightning") is { } bolt) canvas.Draw(bolt, 20f, 24f, 30f, 38f);
                else Bolt(canvas);
                break;
            case WeatherKind.Mist:
                MistBands(canvas, new Color(0.80f, 0.84f, 0.90f));
                break;
            case WeatherKind.Sootfall:
                MistBands(canvas, new Color(0.62f, 0.57f, 0.53f));
                Embers(canvas);
                break;
            case WeatherKind.PolarDay:
                Sun(canvas, 32f, 26f, 0.78f);
                SnowyHill(canvas);
                break;
            case WeatherKind.PolarNight:
                if (GameArt.Get("Cold") is { } sky) canvas.Draw(sky, 0f, 0f, 64f, 64f);
                Moon(canvas);
                break;
            case WeatherKind.NoMist:
                // The wisp light, which clears the mist in the game too.
                if (GameArt.ItemIconPixels("Wisp") is { } wisp) canvas.Draw(wisp, 0f, 0f, 64f, 64f);
                else MistBands(canvas, new Color(0.80f, 0.84f, 0.90f));
                break;
            case WeatherKind.Aurora:
                if (GameArt.Get("Cold") is { } night) canvas.Draw(night, 0f, 0f, 64f, 64f);
                AuroraBands(canvas);
                break;
            case WeatherKind.Crimson:
                if (GameArt.Get("Cold") is { } moonSky) canvas.Draw(moonSky, 0f, 0f, 64f, 64f);
                BloodMoon(canvas);
                break;
            case WeatherKind.Tailwind:
                // The game's sail, with its wind lines blowing into it.
                if (GameArt.Get("ship_fullsail") is { } sail) canvas.Draw(sail, 26f, 6f, 34f, 52f);
                if (GameArt.Get("windicon") is { } wind) canvas.Draw(wind, 2f, 14f, 30f, 30f, new Color(0.9f, 0.95f, 1f, 0.95f));
                break;
            case WeatherKind.CryptDelver:
                // The game's water drops under a "no" sign: never wet.
                if (GameArt.Get("Wet") is { } drops) canvas.Draw(drops, 8f, 8f, 48f, 48f);
                NoSign(canvas);
                break;
            case WeatherKind.Verdant:
                // A seedling glowing green, with sparkles.
                Glow(canvas, 32f, 38f, 30f, new Color(0.4f, 1f, 0.4f), 0.55f);
                if (GameArt.Get("kaleseedling") is { } seedling) canvas.Draw(seedling, 10f, 8f, 44f, 52f);
                Sparkle(canvas, 14f, 16f, 4.5f);
                Sparkle(canvas, 50f, 12f, 3.5f);
                Sparkle(canvas, 48f, 40f, 4f);
                break;
            case WeatherKind.Packward:
                // A wolf running off, with speed streaks behind it.
                foreach ((float y, float x0, float x1) in new[] { (22f, 36f, 58f), (32f, 40f, 62f), (42f, 36f, 58f) })
                    canvas.Paint((px, py) => new Color(0.92f, 0.95f, 1f, Mathf.Clamp01(0.5f - Capsule(px, py, x0, y, x1, y, 1.6f)) * 0.85f));
                if (GameArt.Get("TrophyWolf") is { } wolf) canvas.Draw(wolf, 46f, 6f, -42f, 50f);
                break;
            case WeatherKind.Carrion:
                // A boar following a smell: the game's heat waves, turned sickly green.
                if (GameArt.Get("TrophyBoar") is { } boar) canvas.Draw(boar, 8f, 18f, 48f, 46f);
                if (GameArt.Get("Warm") is { } waves) canvas.Draw(waves, 18f, 0f, 28f, 26f, new Color(0.55f, 1f, 0.25f, 0.95f));
                break;
            case WeatherKind.VatBoil:
                Glow(canvas, 32f, 34f, 30f, new Color(0.7f, 0.9f, 0.2f), 0.45f);
                if (GameArt.Get("fermenter") is { } vat) canvas.Draw(vat, 6f, 12f, 46f, 48f);
                Bubble(canvas, 46f, 18f, 5f);
                Bubble(canvas, 54f, 30f, 3.5f);
                Bubble(canvas, 40f, 8f, 3f);
                break;
            case WeatherKind.AmberMend:
                Glow(canvas, 24f, 42f, 26f, new Color(1f, 0.65f, 0.15f), 0.6f);
                if (GameArt.Get("resin") is { } resin) canvas.Draw(resin, 4f, 26f, 36f, 36f);
                if (GameArt.Get("hammer") is { } hammer) canvas.Draw(hammer, 22f, 2f, 40f, 40f);
                Sparkle(canvas, 50f, 46f, 4f, new Color(1f, 0.75f, 0.3f));
                break;
            case WeatherKind.AbyssalSurge:
                Waves(canvas);
                if (GameArt.Get("fish1") is { } fish) canvas.Draw(fish, 34f, 30f, 28f, 28f);
                if (GameArt.Get("TrophySerpent") is { } serpent) canvas.Draw(serpent, 2f, 2f, 40f, 40f);
                break;
            case WeatherKind.GlacialSatiety:
                Glow(canvas, 32f, 36f, 30f, new Color(0.55f, 0.85f, 1f), 0.5f);
                if (GameArt.Get("SerpentStew") is { } stew) canvas.Draw(stew, 6f, 14f, 44f, 44f);
                if (GameArt.Get("Frost") is { } frost) canvas.Draw(frost, 34f, 2f, 28f, 28f);
                break;
            case WeatherKind.VeinShatter:
                if (GameArt.Get("copperore") is { } ore) canvas.Draw(ore, 4f, 22f, 40f, 40f);
                Cracks(canvas);
                if (GameArt.Get("pickaxe_antler") is { } pick) canvas.Draw(pick, 24f, 2f, 38f, 38f);
                break;
            case WeatherKind.CarapaceTemper:
                Glow(canvas, 32f, 32f, 31f, new Color(1f, 0.75f, 0.3f), 0.5f);
                if (GameArt.Get("ArmorCarapaceChest") is { } carapace) canvas.Draw(carapace, 6f, 6f, 52f, 52f);
                else if (GameArt.Get("carapace") is { } shell) canvas.Draw(shell, 6f, 6f, 52f, 52f);
                break;
            case WeatherKind.GraveBind:
                Beam(canvas);
                if (GameArt.ItemIconPixels("YagluthDrop") is { } spirit) canvas.Draw(spirit, 4f, 12f, 56f, 56f);
                break;
            case WeatherKind.FortunesBoon:
                if (GameArt.Get("coins") is { } coins) canvas.Draw(coins, 22f, 30f, 38f, 32f);
                if (GameArt.Get("wishbone") is { } wishbone) canvas.Draw(wishbone, 4f, 4f, 42f, 42f);
                Sparkle(canvas, 52f, 14f, 4.5f, new Color(1f, 0.85f, 0.3f));
                Sparkle(canvas, 10f, 52f, 3.5f, new Color(1f, 0.85f, 0.3f));
                break;
            case WeatherKind.Luminary:
                Glow(canvas, 32f, 32f, 32f, new Color(1f, 0.8f, 0.45f), 0.9f);
                if (GameArt.ItemIconPixels("Wisp") is { } lightWisp) canvas.Draw(lightWisp, -6f, -6f, 76f, 76f);
                break;
            case WeatherKind.AbyssalDraw:
                Glow(canvas, 32f, 32f, 26f, new Color(0.35f, 0.75f, 1f), 0.5f);
                if (GameArt.ItemIconPixels("Chitin") is { } chitin) canvas.Draw(chitin, 14f, 14f, 36f, 36f);
                foreach ((float x, float y) in new[] { (6f, 8f), (56f, 10f), (8f, 54f), (56f, 54f) })
                    Sparkle(canvas, x, y, 3.5f, new Color(0.4f, 0.8f, 1f));
                break;
            case WeatherKind.SovereignWard:
                Dome(canvas);
                if (GameArt.ItemIconPixels("QueenDrop") is { } queen) canvas.Draw(queen, 12f, 18f, 40f, 40f);
                break;
            case WeatherKind.Unmaking:
                Glow(canvas, 32f, 36f, 28f, new Color(0.6f, 0.75f, 1f), 0.5f);
                if (GameArt.ItemIconPixels("Thunderstone") is { } stone) canvas.Draw(stone, 6f, 18f, 42f, 42f);
                if (GameArt.Get("Lightning") is { } strike) canvas.Draw(strike, 32f, 2f, 28f, 34f);
                break;
            // Rites that happen at once have no status; these only show on the rites page and in the README.
            case WeatherKind.Grounding:
                Glow(canvas, 32f, 34f, 30f, new Color(0.55f, 0.9f, 0.4f), 0.5f);
                if (GameArt.ItemIconPixels("Flint") is { } flint) canvas.Draw(flint, 6f, 6f, 52f, 52f);
                break;
            case WeatherKind.Sanctuary:
                Glow(canvas, 32f, 34f, 30f, new Color(1f, 0.45f, 0.35f), 0.5f);
                if (GameArt.ItemIconPixels("IronScrap") is { } scrap) canvas.Draw(scrap, 6f, 6f, 52f, 52f);
                break;
            case WeatherKind.Reset:
                Glow(canvas, 32f, 34f, 30f, new Color(0.9f, 0.85f, 0.7f), 0.5f);
                if (GameArt.ItemIconPixels("BoneFragments") is { } bones) canvas.Draw(bones, 6f, 6f, 52f, 52f);
                break;
            default:
                return null;
        }
        return canvas;
    }

    // ---- Pictures ----

    private static void Sun(Canvas canvas, float cx = 32f, float cy = 32f, float scale = 1f)
    {
        Color halo = new(1f, 0.6f, 0.1f);
        Color core = new(1f, 0.97f, 0.75f), mid = new(1f, 0.85f, 0.15f), rim = new(0.98f, 0.55f, 0.05f);
        canvas.Paint((x, y) =>
        {
            float dx = (x - cx) / scale, dy = (y - cy) / scale;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float spike = Mathf.Abs(Mathf.Cos(Mathf.Atan2(dy, dx) * 6f));
            float edge = 14f + 9f * Mathf.Pow(spike, 2.2f);
            float body = Mathf.Clamp01((edge - r) * scale / 1.2f + 0.5f);
            float t = Mathf.Clamp01(r / edge);
            Color fill = t < 0.5f ? Color.Lerp(core, mid, t / 0.5f) : Color.Lerp(mid, rim, (t - 0.5f) / 0.5f);
            float glow = Mathf.Pow(Mathf.Clamp01(1f - r / 31f), 2f) * 0.85f;
            return Canvas.Over(new Color(halo.r, halo.g, halo.b, glow), new Color(fill.r, fill.g, fill.b, body));
        });
    }

    // Three soft drifting bands of the game's smoke texture.
    private static void MistBands(Canvas canvas, Color tint)
    {
        Pixels? smoke = GameArt.Get("Smoked");
        foreach ((float y, float x0, float x1, float r, float offset) in new[] { (18f, 10f, 46f, 6.5f, 0f), (32f, 18f, 56f, 7f, 8f), (46f, 6f, 44f, 6.5f, -6f) })
        {
            float middle = (x0 + x1) / 2f, half = (x1 - x0) / 2f + r;
            canvas.Paint((x, py) =>
            {
                float d = Capsule(x, py, x0, y, x1, y, r);
                float taper = Mathf.Clamp01(1f - Mathf.Pow(Mathf.Abs((x - middle) / half), 3f));
                float alpha = Mathf.Clamp01(0.5f - d / 5f) * 0.92f * taper;
                if (alpha <= 0f) return Color.clear;
                float lum = 0.45f;
                if (smoke != null)
                {
                    Color texel = smoke.Sample((x - (offset - 4f)) / 60f, py / 64f);
                    if (texel.a > 0.05f) lum = (texel.r + texel.g + texel.b) / 3f;
                }
                float k = Mathf.Clamp(1f + 1.4f * (lum - 0.45f), 0f, 1.4f);
                return new Color(Mathf.Clamp01(tint.r * k), Mathf.Clamp01(tint.g * k), Mathf.Clamp01(tint.b * k), alpha);
            });
        }
    }

    // Glowing embers drifting through ash.
    private static void Embers(Canvas canvas)
    {
        foreach ((float x, float y, float r) in new[] { (16f, 12f, 2.6f), (47f, 24f, 2.2f), (27f, 40f, 2.8f), (50f, 52f, 2.4f), (12f, 54f, 2f) })
            canvas.Paint((px, py) =>
            {
                float d = Circle(px, py, x, y, r);
                float glow = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Max(d, 0f) / 4f), 2f) * 0.7f;
                return Canvas.Over(new Color(1f, 0.45f, 0.1f, glow), new Color(1f, 0.82f, 0.45f, Mathf.Clamp01(0.5f - d)));
            });
    }

    // A snowy hill along the bottom, under the sun that won't set.
    private static void SnowyHill(Canvas canvas)
    {
        float[] alpha = new float[Size * Size];
        Color[] fill = new Color[Size * Size];
        for (int py = 0; py < Size; py++)
        {
            for (int px = 0; px < Size; px++)
            {
                float x = px + 0.5f, y = py + 0.5f;
                int i = py * Size + px;
                alpha[i] = Mathf.Clamp01(0.5f - Circle(x, y, 32f, 112f, 62f) / 1.2f);
                float shade = Mathf.Clamp(1.05f - (y - 50f) / 20f * 0.25f, 0.75f, 1.05f);
                fill[i] = new Color(Mathf.Clamp01(0.88f * shade), Mathf.Clamp01(0.93f * shade), Mathf.Clamp01(shade), alpha[i]);
            }
        }
        float[] shadow = Canvas.Blur(alpha, 2);
        for (int i = 0; i < shadow.Length; i++) canvas.Blend(i, new Color(0f, 0f, 0f, shadow[i] * 0.5f));
        for (int i = 0; i < fill.Length; i++) if (alpha[i] > 0f) canvas.Blend(i, fill[i]);
    }

    // A pale crescent moon with a faint blue glow.
    private static void Moon(Canvas canvas)
    {
        canvas.Paint((x, y) =>
        {
            float outer = Circle(x, y, 34f, 30f, 17f);
            float bite = Circle(x, y, 42f, 24f, 15f);
            float d = Mathf.Max(outer, -bite);
            float glow = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Max(outer, 0f) / 10f), 2f) * 0.5f * Mathf.Clamp01(bite / 4f);
            float t = Mathf.Clamp01((x - 17f) / 30f);
            Color moon = new(0.97f - 0.1f * t, 0.97f - 0.05f * t, 0.88f + 0.08f * t, Mathf.Clamp01(0.5f - d / 1.2f));
            return Canvas.Over(new Color(0.6f, 0.8f, 1f, glow), moon);
        });
    }

    // A soft round glow.
    private static void Glow(Canvas canvas, float cx, float cy, float radius, Color color, float strength) =>
        canvas.Paint((x, y) =>
        {
            float r = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            return new Color(color.r, color.g, color.b, Mathf.Pow(Mathf.Clamp01(1f - r / radius), 2f) * strength);
        });

    // A small four-pointed sparkle with a glow, green unless told otherwise.
    private static void Sparkle(Canvas canvas, float cx, float cy, float size, Color? glow = null)
    {
        Glow(canvas, cx, cy, size * 1.6f, glow ?? new Color(0.5f, 1f, 0.5f), 0.6f);
        canvas.Paint((x, y) =>
        {
            float d = Mathf.Min(Capsule(x, y, cx - size, cy, cx + size, cy, 0.7f), Capsule(x, y, cx, cy - size, cx, cy + size, 0.7f));
            return new Color(1f, 1f, 0.9f, Mathf.Clamp01(0.5f - d));
        });
    }

    // A bubble: a pale green ring with a glint.
    private static void Bubble(Canvas canvas, float cx, float cy, float r) =>
        canvas.Paint((x, y) =>
        {
            float ring = Mathf.Abs(Circle(x, y, cx, cy, r)) - 0.8f;
            float glint = Circle(x, y, cx - r * 0.35f, cy - r * 0.35f, r * 0.25f);
            Color edge = new(0.8f, 1f, 0.45f, Mathf.Clamp01(0.5f - ring) * 0.95f);
            Color inside = new(0.6f, 0.9f, 0.3f, Circle(x, y, cx, cy, r) < 0f ? 0.25f : 0f);
            return Canvas.Over(Canvas.Over(inside, edge), new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - glint)));
        });

    // Three rolling blue waves along the bottom.
    private static void Waves(Canvas canvas) =>
        canvas.Paint((x, y) =>
        {
            float alpha = 0f;
            foreach ((float baseY, float shade) in new[] { (44f, 0.55f), (52f, 0.75f), (60f, 0.95f) })
            {
                float crest = baseY + Mathf.Sin(x / 6f + baseY) * 2.5f;
                alpha = Mathf.Max(alpha, Mathf.Clamp01(0.5f - Mathf.Abs(y - crest) + 1.2f) * shade);
            }
            return new Color(0.3f, 0.6f, 0.9f, alpha * 0.9f);
        });

    // Bright cracks across the ore.
    private static void Cracks(Canvas canvas)
    {
        Vector2[][] lines =
        [
            [new(10f, 40f), new(20f, 46f), new(26f, 40f), new(36f, 50f)],
            [new(20f, 46f), new(18f, 58f)],
            [new(26f, 40f), new(30f, 30f)]
        ];
        canvas.Paint((x, y) =>
        {
            float d = float.MaxValue;
            foreach (Vector2[] line in lines)
                for (int i = 0; i + 1 < line.Length; i++)
                    d = Mathf.Min(d, Capsule(x, y, line[i].x, line[i].y, line[i + 1].x, line[i + 1].y, 0.9f));
            Color glow = new(1f, 0.8f, 0.4f, Mathf.Clamp01(0.5f - (d - 2f) / 2f) * 0.6f);
            return Canvas.Over(glow, new Color(1f, 0.97f, 0.85f, Mathf.Clamp01(0.5f - d)));
        });
    }

    // A golden dome over the ground: a glowing arc with a faint fill.
    private static void Dome(Canvas canvas) =>
        canvas.Paint((x, y) =>
        {
            if (y > 58f) return Color.clear;
            float r = Mathf.Sqrt((x - 32f) * (x - 32f) + (y - 58f) * (y - 58f));
            float edge = Mathf.Clamp01(0.5f - (Mathf.Abs(r - 28f) - 1.2f));
            float fill = r < 28f ? 0.18f : 0f;
            float glow = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(r - 28f) / 6f), 2f) * 0.5f;
            return new Color(1f, 0.85f, 0.4f, Mathf.Clamp01(Mathf.Max(edge, glow) + fill));
        });

    // A pale vertical beam of light, brightest in the middle.
    private static void Beam(Canvas canvas) =>
        canvas.Paint((x, y) =>
        {
            float across = Mathf.Abs(x - 32f);
            float core = Mathf.Clamp01(1f - across / 4f);
            float halo = Mathf.Pow(Mathf.Clamp01(1f - across / 14f), 2f) * 0.5f;
            float fade = Mathf.Clamp01(1.1f - y / 64f);
            return new Color(0.75f, 0.95f, 1f, Mathf.Clamp01(core + halo) * fade);
        });

    // A red "no" sign: a ring with a slash, edged dark.
    private static void NoSign(Canvas canvas)
    {
        canvas.Paint((x, y) =>
        {
            float ring = Mathf.Abs(Circle(x, y, 32f, 32f, 23f)) - 2.4f;
            float bar = Capsule(x, y, 16f, 16f, 48f, 48f, 2.4f);
            float d = Mathf.Min(ring, bar);
            Color edge = new(0.1f, 0.05f, 0.04f, Mathf.Clamp01(0.5f - (d - 1.5f)) * 0.9f);
            return Canvas.Over(edge, new Color(0.9f, 0.15f, 0.1f, Mathf.Clamp01(0.5f - d)));
        });
    }

    // Two soft waving curtains of green and violet light.
    private static void AuroraBands(Canvas canvas)
    {
        foreach ((float baseY, float amplitude, float phase, Color color) in new[]
                 {
                     (24f, 6f, 0f, new Color(0.35f, 1f, 0.6f)),
                     (36f, 5f, 1.7f, new Color(0.6f, 0.45f, 1f))
                 })
            canvas.Paint((x, y) =>
            {
                float center = baseY + Mathf.Sin(x / 64f * Mathf.PI * 2f + phase) * amplitude;
                float d = y - center;
                // Bright at the lower edge, fading upwards, like a curtain.
                float alpha = d < 0f ? Mathf.Clamp01(1f + d / 14f) : Mathf.Clamp01(1f - d / 2.5f);
                alpha *= Mathf.Clamp01(Mathf.Min(x, 64f - x) / 8f) * 0.85f;
                return new Color(color.r, color.g, color.b, alpha);
            });
    }

    // A full moon glowing blood red.
    private static void BloodMoon(Canvas canvas)
    {
        canvas.Paint((x, y) =>
        {
            float d = Circle(x, y, 32f, 30f, 15f);
            float glow = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Max(d, 0f) / 12f), 2f) * 0.7f;
            float shade = Mathf.Clamp01((x - 18f) / 28f);
            Color moon = new(0.85f - 0.25f * shade, 0.12f, 0.1f, Mathf.Clamp01(0.5f - d / 1.2f));
            return Canvas.Over(new Color(1f, 0.15f, 0.1f, glow), moon);
        });
    }

    // A cloud shape filled with the game's smoke texture, lighter on top, with a soft shadow behind it.
    private static void Cloud(Canvas canvas, float cx, float cy, Color tint)
    {
        const float scale = 1.05f;
        Pixels? smoke = GameArt.Get("Smoked");
        float meanLum = smoke?.MeanLuminance ?? 0.45f;
        float texX = cx - 30f * scale, texY = cy - 24f * scale, texW = 60f * scale, texH = 40f * scale;

        float[] alpha = new float[Size * Size];
        Color[] fill = new Color[Size * Size];
        for (int py = 0; py < Size; py++)
        {
            for (int px = 0; px < Size; px++)
            {
                float x = px + 0.5f, y = py + 0.5f;
                int i = py * Size + px;
                alpha[i] = Mathf.Clamp01(0.5f - CloudDistance(x - cx, y - cy, scale) / 1.6f);
                if (alpha[i] <= 0f) continue;

                float lum = meanLum;
                if (smoke != null)
                {
                    Color texel = smoke.Sample((x - texX) / texW, (y - texY) / texH);
                    if (texel.a > 0.05f) lum = (texel.r + texel.g + texel.b) / 3f;
                }
                float shade = Mathf.Clamp(1f + 1.6f * (lum - 0.45f), 0f, 1.4f);
                float light = Mathf.Clamp(1.15f - (y - (cy - 18f * scale)) / (36f * scale) * 0.35f, 0.7f, 1.15f);
                float k = shade * light;
                fill[i] = new Color(Mathf.Clamp01(tint.r * k), Mathf.Clamp01(tint.g * k), Mathf.Clamp01(tint.b * k), alpha[i]);
            }
        }

        float[] shadow = Canvas.Blur(alpha, 2);
        for (int i = 0; i < shadow.Length; i++) canvas.Blend(i, new Color(0f, 0f, 0f, shadow[i] * 0.55f));
        for (int i = 0; i < fill.Length; i++) if (alpha[i] > 0f) canvas.Blend(i, fill[i]);
    }

    private static float CloudDistance(float x, float y, float s) => Mathf.Min(
        Mathf.Min(Circle(x, y, -11f * s, 2f * s, 9f * s), Circle(x, y, 0f, -5f * s, 12.5f * s)),
        Mathf.Min(Circle(x, y, 12f * s, 1f * s, 9.5f * s), Capsule(x, y, -14f * s, 7f * s, 15f * s, 7f * s, 6.5f * s)));

    // Stand-ins for the game's drops and bolt.
    private static void Drops(Canvas canvas)
    {
        Color blue = new(0.2f, 0.62f, 0.95f);
        foreach ((float x, float y) in new[] { (23f, 45f), (41f, 45f), (32f, 54f) })
            canvas.Paint((px, py) =>
            {
                float d = Mathf.Min(Circle(px, py, x, y + 2f, 4.2f), Capsule(px, py, x, y - 5f, x, y + 2f, 1.2f + Mathf.Clamp01((py - (y - 5f)) / 7f) * 3f));
                return new Color(blue.r, blue.g, blue.b, Mathf.Clamp01(0.5f - d));
            });
    }

    private static void Bolt(Canvas canvas)
    {
        Vector2[] points = [new(35f, 27f), new(25f, 44f), new(32f, 44f), new(27f, 60f), new(42f, 38f), new(35f, 38f), new(40f, 27f)];
        canvas.Paint((x, y) =>
        {
            float d = Polygon(x, y, points);
            float glow = Mathf.Clamp01(1f - Mathf.Max(d, 0f) / 5f) * 0.6f;
            return Canvas.Over(new Color(0.3f, 0.8f, 1f, glow), new Color(0.95f, 0.98f, 1f, Mathf.Clamp01(0.5f - d)));
        });
    }

    // ---- Distance functions (negative inside), in pixels with y going down ----

    private static float Circle(float x, float y, float cx, float cy, float r) =>
        Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r;

    private static float Capsule(float x, float y, float ax, float ay, float bx, float by, float r)
    {
        float dx = bx - ax, dy = by - ay;
        float length = dx * dx + dy * dy;
        float t = length > 0f ? Mathf.Clamp01(((x - ax) * dx + (y - ay) * dy) / length) : 0f;
        float px = ax + t * dx - x, py = ay + t * dy - y;
        return Mathf.Sqrt(px * px + py * py) - r;
    }

    private static float Polygon(float x, float y, Vector2[] points)
    {
        float d = float.MaxValue;
        bool inside = false;
        for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
        {
            Vector2 a = points[j], b = points[i];
            d = Math.Min(d, Capsule(x, y, a.x, a.y, b.x, b.y, 0f));
            if ((b.y > y) != (a.y > y) && x < (a.x - b.x) * (y - b.y) / (a.y - b.y) + b.x) inside = !inside;
        }
        return inside ? -d : d;
    }

    // ---- Drawing ----

    // A 64 x 64 picture, straight alpha, row 0 at the top.
    private sealed class Canvas
    {
        private readonly Color[] _pixels = new Color[Size * Size];

        internal Color[] Colors => _pixels;

        internal void Blend(int index, Color color) => _pixels[index] = Over(_pixels[index], color);

        internal void Paint(Func<float, float, Color> shade)
        {
            for (int py = 0; py < Size; py++)
                for (int px = 0; px < Size; px++)
                    Blend(py * Size + px, shade(px + 0.5f, py + 0.5f));
        }

        // Draws a picture stretched into the box at (x, y) of size w x h.
        // A negative width draws it mirrored. tint multiplies its colors (and alpha).
        internal void Draw(Pixels source, float x, float y, float w, float h, Color? tint = null)
        {
            Color t = tint ?? Color.white;
            for (int py = 0; py < Size; py++)
            {
                for (int px = 0; px < Size; px++)
                {
                    float u = (px + 0.5f - x) / w, v = (py + 0.5f - y) / h;
                    if (u < 0f || u > 1f || v < 0f || v > 1f) continue;
                    Color c = source.Sample(u, v);
                    Blend(py * Size + px, new Color(c.r * t.r, c.g * t.g, c.b * t.b, c.a * t.a));
                }
            }
        }

        internal static Color Over(Color below, Color above)
        {
            float a = above.a + below.a * (1f - above.a);
            if (a <= 0f) return Color.clear;
            float k = below.a * (1f - above.a);
            return new Color(
                (above.r * above.a + below.r * k) / a,
                (above.g * above.a + below.g * k) / a,
                (above.b * above.a + below.b * k) / a,
                a);
        }

        // Two box blurs: close enough to a soft shadow.
        internal static float[] Blur(float[] values, int radius)
        {
            float[] result = values;
            for (int pass = 0; pass < 2; pass++)
            {
                float[] horizontal = new float[result.Length];
                float[] vertical = new float[result.Length];
                for (int y = 0; y < Size; y++)
                    for (int x = 0; x < Size; x++)
                    {
                        float sum = 0f;
                        for (int k = -radius; k <= radius; k++) sum += result[y * Size + Mathf.Clamp(x + k, 0, Size - 1)];
                        horizontal[y * Size + x] = sum / (2 * radius + 1);
                    }
                for (int y = 0; y < Size; y++)
                    for (int x = 0; x < Size; x++)
                    {
                        float sum = 0f;
                        for (int k = -radius; k <= radius; k++) sum += horizontal[Mathf.Clamp(y + k, 0, Size - 1) * Size + x];
                        vertical[y * Size + x] = sum / (2 * radius + 1);
                    }
                result = vertical;
            }
            return result;
        }

        internal Sprite ToSprite(string name)
        {
            Color[] flipped = new Color[_pixels.Length];
            // Textures start at the bottom row.
            for (int y = 0; y < Size; y++) Array.Copy(_pixels, y * Size, flipped, (Size - 1 - y) * Size, Size);
            Texture2D texture = new(Size, Size, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixels(flipped);
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }

    // A copy of a game sprite's pixels, row 0 at the top.
    private sealed class Pixels(int width, int height, Color[] colors)
    {
        internal float MeanLuminance { get; } = Mean(colors);

        // Bilinear, premultiplied so transparent edges don't darken; u, v from 0 to 1, v going down.
        internal Color Sample(float u, float v)
        {
            float x = u * width - 0.5f, y = v * height - 0.5f;
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            Vector4 sum = Texel(x0, y0) * ((1 - fx) * (1 - fy)) + Texel(x0 + 1, y0) * (fx * (1 - fy))
                        + Texel(x0, y0 + 1) * ((1 - fx) * fy) + Texel(x0 + 1, y0 + 1) * (fx * fy);
            return sum.w > 0f ? new Color(sum.x / sum.w, sum.y / sum.w, sum.z / sum.w, sum.w) : Color.clear;
        }

        private Vector4 Texel(int x, int y)
        {
            if (x < 0 || y < 0 || x >= width || y >= height) return Vector4.zero;
            Color c = colors[y * width + x];
            return new Vector4(c.r * c.a, c.g * c.a, c.b * c.a, c.a);
        }

        private static float Mean(Color[] colors)
        {
            float sum = 0f, weight = 0f;
            foreach (Color c in colors)
            {
                sum += (c.r + c.g + c.b) / 3f * c.a;
                weight += c.a;
            }
            return weight > 0f ? sum / weight : 0.45f;
        }
    }

    // The game's sprites, found by name and copied out of the GPU (their textures can't be read directly).
    private static class GameArt
    {
        private static readonly Dictionary<string, Pixels?> Copies = [];
        private static Dictionary<string, Sprite>? _sprites;

        internal static Pixels? Get(string name)
        {
            if (Copies.TryGetValue(name, out Pixels? copy)) return copy;
            if (PreviewArt != null) return Copies[name] = FromPreview(name);
            return FromGame(name);
        }

        // Kept apart from Get, so previews outside the game never touch the game's (and BepInEx's) code.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static Pixels? FromGame(string name)
        {
            Pixels? copy;
            try
            {
                _sprites ??= FindSprites();
                copy = _sprites.TryGetValue(name, out Sprite sprite) ? Copy(sprite) : null;
            }
            catch (Exception exception)
            {
                Plugin.ModLogger.LogWarning($"Couldn't copy the game's {name} icon, drawing a stand-in: {exception.Message}");
                copy = null;
            }
            if (copy == null) Plugin.ModLogger.LogDebug($"Game icon {name} not found, drawing a stand-in.");
            Copies[name] = copy;
            return copy;
        }

        // An item's own icon, copied to draw on.
        internal static Pixels? ItemIconPixels(string prefabName)
        {
            string key = "item:" + prefabName;
            if (Copies.TryGetValue(key, out Pixels? copy)) return copy;
            if (PreviewArt != null) return Copies[key] = FromPreview(key);
            return ItemFromGame(key, prefabName);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static Pixels? ItemFromGame(string key, string prefabName)
        {
            Pixels? copy;
            Sprite? icon = ItemIcon(prefabName);
            try { copy = icon ? Copy(icon!) : null; }
            catch (Exception exception)
            {
                Plugin.ModLogger.LogWarning($"Couldn't copy the {prefabName} icon: {exception.Message}");
                copy = null;
            }
            // Not kept while the item list isn't loaded yet, so it is tried again.
            if (copy != null || ObjectDB.instance) Copies[key] = copy;
            return copy;
        }

        private static Pixels? FromPreview(string name) =>
            PreviewArt!(name) is { } art ? new Pixels(art.Width, art.Height, art.Colors) : null;

        // An item's own icon, used as it is.
        internal static Sprite? ItemIcon(string prefabName)
        {
            if (!ObjectDB.instance) return null;
            GameObject? prefab = ObjectDB.instance.GetItemPrefab(prefabName);
            ItemDrop? drop = prefab ? prefab!.GetComponent<ItemDrop>() : null;
            return drop ? drop!.m_itemData.GetIcon() : null;
        }

        private static Dictionary<string, Sprite> FindSprites()
        {
            Dictionary<string, Sprite> sprites = [];
            foreach (Sprite sprite in Resources.FindObjectsOfTypeAll<Sprite>())
                if (sprite && sprite.texture && !sprites.ContainsKey(sprite.name)) sprites[sprite.name] = sprite;
            return sprites;
        }

        private static Pixels Copy(Sprite sprite)
        {
            Texture2D source = sprite.texture;
            Rect rect = sprite.textureRect;
            RenderTexture target = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            RenderTexture previous = RenderTexture.active;
            try
            {
                Graphics.Blit(source, target);
                RenderTexture.active = target;
                int width = Mathf.RoundToInt(rect.width), height = Mathf.RoundToInt(rect.height);
                Texture2D readable = new(width, height, TextureFormat.RGBA32, false);
                readable.ReadPixels(new Rect(rect.x, rect.y, width, height), 0, 0);
                readable.Apply();
                Color[] bottomUp = readable.GetPixels();
                UnityEngine.Object.Destroy(readable);

                Color[] topDown = new Color[bottomUp.Length];
                for (int y = 0; y < height; y++) Array.Copy(bottomUp, y * width, topDown, (height - 1 - y) * width, width);
                return new Pixels(width, height, topDown);
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
            }
        }
    }
}

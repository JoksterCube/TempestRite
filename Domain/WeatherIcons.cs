using System;
using System.Collections.Generic;
using UnityEngine;

namespace JoksterCube.TempestRite.Domain;

// Status icons put together when first needed, in the game's own painted style and mostly from its own art, so the
// mod ships no image files:
// - Clear skies: a glowing sun, painted here like the game's.
// - Rain: a cloud filled with the game's smoke texture, over the game's rain drops (the Wet icon).
// - Thunderstorm: a darker cloud with the game's glowing lightning bolt.
// - Mist: spruces fading into fog, the far ones pale, fog drifting between them.
// - Polar day: the sun over a snowy hill. Polar night: a crescent moon on the game's starry Cold icon.
// - Gale-Breath: icy gusts sweeping across and curling over, frost glinting in them.
// - True Sight: an eye glowing, the Mistlands mist pushed back around it. Sootfall: the red Ashlands sun breaking
//   through as the smoke parts, ash falling away.
// - Tailwind: a ship's striped sail filled by gusts blowing in from behind. Crypt Delver: the wooden chest in a crypt's stone doorway, lit green,
//   with gold spilling out. Verdant: a glowing seedling with sparkles.
// - Abyssal Surge: a sea serpent rearing out of the night sea over a school of fish, the sea fading out slightly at the
//   icon's sides and bottom.
// - Domain: Packward: wolf tracks leading away from a warded fire. Carrion: boar tracks leading in to rotting meat in
//   a green ring, its stench rising. Vat-Boil: the fermenter over a log fire, foam boiling
//   over. Pitch-Bellows: tar-black bellows blowing embers into a stone furnace's glowing mouth, flames leaping from
//   its chimney. Amber-Mend: a cracked wall sealed with glowing amber, the hammer striking it. Sovereign Ward: a longhouse
//   under a crowned golden dome, a fireball shattering on it. Grounding: an iron stake draining the green rings into
//   the earth.
// - Blessings: Glacial Satiety: meat frozen in a block of ice. Vein-Shatter: a rock of ore split apart under a
//   pickaxe, glowing through its cracks. Carapace Temper: a sword on an anvil in a glowing amber shell. Grave-Bind: a chained tombstone under a beam of light.
//   Fortune's Boon: the rune Fehu over treasure. Luminary: a lantern shining. Abyssal Draw: a whirlpool, glowing motes
//   streaming in along its arms.
// - Kinship: Skål: two tankards clinking. Sky-Call: lightning striking a runestone lit with the rune Jera.
//   Anvil-Spark: a hammer striking an anvil, sparks bursting from it.
// - Reckoning: Crimson: a blood moon over a horde with red eyes. Ragnarök: Surtr's flaming sword under a black sun
//   (both fading out toward their edges).
//   Sanctuary: a shield shattering a raid's red ring. Equilibrium: a balance scale. Unmaking: a sword breaking apart
//   over an iron bar.
// No picture shows a rite's key item: those can be changed in the settings.
// If one of the game's sprites can't be found (renamed in an update), a plain painted shape stands in or it is left out.
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

    // ---- The rites flap's button ----
    // A fire burning on an altar stone, the rune Othala (ᛟ) cut in it and glowing: every rite is a burning.

    private static Sprite? _flap;

    internal static Sprite Flap() => _flap ? _flap! : _flap = FlapPicture().ToSprite("TempestRite_Flap");

    // The picture's 64 x 64 colors, row 0 at the top (previews).
    internal static Color[] RenderFlap() => FlapPicture().Colors;

    private static readonly Vector2[] FlameMain =
        [new(20f, 30f), new(23f, 20f), new(28f, 11f), new(32f, 2f), new(37f, 12f), new(41f, 20f), new(44f, 30f)];
    private static readonly Vector2[] FlameLeft = [new(19f, 36f), new(16f, 25f), new(19f, 14f), new(26f, 27f)];
    private static readonly Vector2[] FlameRight = [new(45f, 36f), new(48f, 24f), new(45f, 11f), new(38f, 26f)];
    private static readonly Vector2[] FlameCore =
        [new(26f, 34f), new(28f, 25f), new(32.5f, 14f), new(36f, 25f), new(38f, 34f)];

    private static Canvas FlapPicture()
    {
        Canvas canvas = new();
        Glow(canvas, 32f, 28f, 30f, new Color(1f, 0.5f, 0.12f), 0.55f);

        // The stone, with a soft shadow, lit from the flame above.
        float[] alpha = new float[Size * Size];
        float[] stone = new float[Size * Size];
        for (int py = 0; py < Size; py++)
            for (int px = 0; px < Size; px++)
            {
                int i = py * Size + px;
                stone[i] = Box(px + 0.5f, py + 0.5f, 9f, 39f, 55f, 62f, 3f);
                alpha[i] = Mathf.Clamp01(0.5f - stone[i]);
            }
        float[] shadow = Canvas.Blur(alpha, 2);
        for (int i = Size; i < shadow.Length; i++) canvas.Blend(i, new Color(0f, 0f, 0f, shadow[i - Size] * 0.6f));
        for (int i = 0; i < alpha.Length; i++)
        {
            if (alpha[i] <= 0f) continue;
            float y = i / Size + 0.5f;
            float light = Mathf.Lerp(0.62f, 0.34f, Mathf.Clamp01((y - 39f) / 23f));
            light *= 0.75f + 0.25f * Mathf.Clamp01(-stone[i] / 2.5f);
            canvas.Blend(i, new Color(light * 1.05f, light, light * 0.92f, alpha[i]));
        }

        // The rune, cut in and glowing.
        canvas.Paint((x, y) =>
        {
            float d = Mathf.Min(Mathf.Min(Capsule(x, y, 32f, 43f, 26f, 49f, 1.5f), Capsule(x, y, 32f, 43f, 38f, 49f, 1.5f)),
                Mathf.Min(Capsule(x, y, 26f, 49f, 36.5f, 59f, 1.5f), Capsule(x, y, 38f, 49f, 27.5f, 59f, 1.5f)));
            float glow = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Max(d, 0f) / 4f), 2f) * 0.75f;
            return Canvas.Over(new Color(1f, 0.45f, 0.1f, glow), new Color(1f, 0.82f, 0.45f, Mathf.Clamp01(0.5f - d)));
        });

        // The flame: orange to red at its edge, a pale yellow heart.
        canvas.Paint((x, y) =>
        {
            float outer = Mathf.Min(Mathf.Min(Polygon(x, y, FlameMain), Circle(x, y, 32f, 30f, 12f)),
                Mathf.Min(Polygon(x, y, FlameLeft), Polygon(x, y, FlameRight)));
            float core = Mathf.Min(Polygon(x, y, FlameCore), Circle(x, y, 32f, 34f, 6f));
            float edge = Mathf.Clamp01(-outer / 6f);
            Color flame = new(1f, Mathf.Lerp(0.3f, 0.62f, edge), Mathf.Lerp(0.06f, 0.14f, edge), Mathf.Clamp01(0.5f - outer));
            Color heart = new(1f, 0.93f, 0.62f, Mathf.Clamp01(0.5f - core / 1.5f));
            return Canvas.Over(flame, heart);
        });
        return canvas;
    }

    // ---- The rites page's button ----
    // An open book in leather covers: lines of writing on the left page, the rune Ansuz (ᚨ, Odin's wisdom) glowing on
    // the right one.

    private static Sprite? _ritesBook;

    internal static Sprite RitesBook() => _ritesBook ? _ritesBook! : _ritesBook = RitesBookPicture().ToSprite("TempestRite_RitesBook");

    // The picture's 64 x 64 colors, row 0 at the top (previews).
    internal static Color[] RenderRitesBook() => RitesBookPicture().Colors;

    // The left page, its top and bottom edges curving down into the spine; the right one is its mirror.
    private static readonly Vector2[] BookPage =
    [
        new(5f, 20f), new(11f, 17.5f), new(19f, 17.5f), new(26f, 20f), new(31.5f, 24f),
        new(31.5f, 54f), new(26f, 50.5f), new(19f, 48f), new(11f, 48f), new(5f, 50f)
    ];

    private static float PageShape(float x, float y, float dy) =>
        Mathf.Min(Polygon(x, y - dy, BookPage), Polygon(64f - x, y - dy, BookPage));

    private static Canvas RitesBookPicture()
    {
        Canvas canvas = new();
        Glow(canvas, 46f, 38f, 22f, new Color(1f, 0.55f, 0.15f), 0.45f);

        // The covers, a little lower and wider than the pages, with a soft shadow.
        float[] alpha = new float[Size * Size];
        float[] cover = new float[Size * Size];
        for (int py = 0; py < Size; py++)
            for (int px = 0; px < Size; px++)
            {
                int i = py * Size + px;
                cover[i] = PageShape(px + 0.5f, py + 0.5f, 3.5f) - 2f;
                alpha[i] = Mathf.Clamp01(0.5f - cover[i]);
            }
        float[] shadow = Canvas.Blur(alpha, 2);
        for (int i = Size; i < shadow.Length; i++) canvas.Blend(i, new Color(0f, 0f, 0f, shadow[i - Size] * 0.6f));
        for (int i = 0; i < alpha.Length; i++)
        {
            if (alpha[i] <= 0f) continue;
            float light = 0.8f + 0.2f * Mathf.Clamp01(-cover[i] / 2f);
            canvas.Blend(i, new Color(0.45f * light, 0.25f * light, 0.13f * light, alpha[i]));
        }

        // The pages, shaded darker into the spine.
        canvas.Paint((x, y) =>
        {
            float d = PageShape(x, y, 0f);
            float shade = Mathf.Lerp(0.72f, 1f, Mathf.Clamp01(Mathf.Abs(x - 32f) / 9f));
            return new Color(0.95f * shade, 0.88f * shade, 0.72f * shade, Mathf.Clamp01(0.5f - d));
        });
        canvas.Paint((x, y) => new Color(0.35f, 0.24f, 0.15f, Mathf.Clamp01(0.5f - Capsule(x, y, 32f, 24f, 32f, 54f, 0.6f)) * 0.8f));

        // Writing on the left page.
        canvas.Paint((x, y) =>
        {
            float d = Mathf.Min(Mathf.Min(Capsule(x, y, 10f, 25f, 27f, 28.5f, 0.8f), Capsule(x, y, 10f, 31f, 27f, 34.5f, 0.8f)),
                Mathf.Min(Capsule(x, y, 10f, 37f, 27f, 40.5f, 0.8f), Capsule(x, y, 10f, 43f, 22f, 45f, 0.8f)));
            return new Color(0.4f, 0.3f, 0.22f, Mathf.Clamp01(0.5f - d) * 0.85f);
        });

        // Ansuz on the right page, glowing.
        canvas.Paint((x, y) =>
        {
            float d = Mathf.Min(Capsule(x, y, 42f, 26f, 42f, 47f, 1.6f),
                Mathf.Min(Capsule(x, y, 42f, 27f, 51f, 33f, 1.6f), Capsule(x, y, 42f, 35f, 51f, 41f, 1.6f)));
            float glow = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Max(d, 0f) / 4f), 2f) * 0.7f;
            return Canvas.Over(new Color(1f, 0.5f, 0.12f, glow), new Color(0.95f, 0.45f, 0.08f, Mathf.Clamp01(0.5f - d)));
        });
        return canvas;
    }

    // ---- Tab emblems of the rites page ----
    // Flat silhouettes like the game's menu icons, painted pale (lighter on top, a darker rim, a soft shadow) so the
    // button's image color makes them gold, or white for the selected tab. Atmospheric: a storm cloud with a bolt.
    // Celestial: a crescent moon and a star. Biome Mastery: snowy mountains. Domain: a longhouse. Blessings: a
    // radiant star. Kinship: two drinking horns raised together. Reckoning: a skull.

    private static readonly Dictionary<RiteGroup, Sprite> TabCache = [];

    internal static Sprite Tab(RiteGroup group)
    {
        if (TabCache.TryGetValue(group, out Sprite sprite) && sprite) return sprite;
        sprite = TabPicture(group).ToSprite($"TempestRite_Tab{group}");
        TabCache[group] = sprite;
        return sprite;
    }

    // The emblem's 64 x 64 colors, row 0 at the top (previews).
    internal static Color[] RenderTab(RiteGroup group) => TabPicture(group).Colors;

    private static Canvas TabPicture(RiteGroup group)
    {
        Func<float, float, float> shape = group switch
        {
            RiteGroup.Atmospheric => StormShape,
            RiteGroup.Celestial => MoonShape,
            RiteGroup.BiomeMastery => MountainShape,
            RiteGroup.Domain => LonghouseShape,
            RiteGroup.Blessings => BlessingShape,
            RiteGroup.Kinship => HornsShape,
            _ => SkullShape
        };
        Canvas canvas = new();
        Emblem(canvas, shape);
        return canvas;
    }

    private static void Emblem(Canvas canvas, Func<float, float, float> shape)
    {
        float[] alpha = new float[Size * Size];
        float[] distance = new float[Size * Size];
        for (int py = 0; py < Size; py++)
            for (int px = 0; px < Size; px++)
            {
                int i = py * Size + px;
                distance[i] = shape(px + 0.5f, py + 0.5f);
                alpha[i] = Mathf.Clamp01(0.5f - distance[i]);
            }

        // The shadow falls a pixel down.
        float[] shadow = Canvas.Blur(alpha, 1);
        for (int i = Size; i < shadow.Length; i++) canvas.Blend(i, new Color(0f, 0f, 0f, shadow[i - Size] * 0.7f));

        for (int i = 0; i < alpha.Length; i++)
        {
            if (alpha[i] <= 0f) continue;
            float y = i / Size + 0.5f;
            float light = Mathf.Lerp(1f, 0.78f, Mathf.Clamp01((y - 8f) / 48f));
            light *= 0.8f + 0.2f * Mathf.Clamp01(-distance[i] / 2.5f);
            canvas.Blend(i, new Color(light, light, light, alpha[i]));
        }
    }

    private static readonly Vector2[] TabBolt =
        [new(37f, 27f), new(25f, 45f), new(32f, 45f), new(27f, 61f), new(44f, 38f), new(36f, 38f), new(42f, 27f)];

    // A cloud with a bolt coming out of it, a gap cut around the bolt.
    private static float StormShape(float x, float y)
    {
        float bolt = Polygon(x, y, TabBolt);
        float cloud = CloudDistance(x - 32f, y - 23f, 1.2f);
        return Mathf.Min(Mathf.Max(cloud, -(bolt - 2.5f)), bolt);
    }

    private static readonly Vector2[] TabMoonStar = StarPoints(45f, 21f, [10f, 10f, 10f, 10f], 3f);

    // A crescent opening to the right, with a star in it.
    private static float MoonShape(float x, float y)
    {
        float moon = Mathf.Max(Circle(x, y, 28f, 35f, 22f), -Circle(x, y, 41f, 28f, 18f));
        return Mathf.Min(moon, Polygon(x, y, TabMoonStar));
    }

    private static readonly Vector2[] TabPeaks =
        [new(3f, 55f), new(25f, 12f), new(37f, 33f), new(45f, 22f), new(61f, 55f)];

    // Two peaks, the snow line cut across the big one.
    private static float MountainShape(float x, float y)
    {
        float snowLine = Mathf.Min(
            Mathf.Min(Capsule(x, y, 12f, 33f, 18f, 29f, 1.6f), Capsule(x, y, 18f, 29f, 23f, 34f, 1.6f)),
            Mathf.Min(Capsule(x, y, 23f, 34f, 28f, 28f, 1.6f), Capsule(x, y, 28f, 28f, 35f, 34f, 1.6f)));
        return Mathf.Max(Polygon(x, y, TabPeaks), -snowLine);
    }

    private static readonly Vector2[] TabRoof = [new(3f, 37f), new(32f, 13f), new(61f, 37f)];

    // A longhouse: the roof with crossed gable ends over the walls, with a door.
    private static float LonghouseShape(float x, float y)
    {
        float roof = Mathf.Min(Polygon(x, y, TabRoof),
            Mathf.Min(Capsule(x, y, 37f, 19f, 25f, 4f, 2.4f), Capsule(x, y, 27f, 19f, 39f, 4f, 2.4f)));
        float walls = Mathf.Max(Box(x, y, 12f, 40f, 52f, 58f), -Box(x, y, 27f, 44f, 37f, 59f, 3f));
        return Mathf.Min(roof, walls);
    }

    private static readonly Vector2[] TabBlessing = StarPoints(29f, 35f, [25f, 14f, 25f, 14f, 25f, 14f, 25f, 14f], 7.5f);
    private static readonly Vector2[] TabBlessingSmall = StarPoints(52f, 11f, [10f, 10f, 10f, 10f], 3.5f);

    // A big radiant star with a small one beside it.
    private static float BlessingShape(float x, float y) =>
        Mathf.Min(Polygon(x, y, TabBlessing), Polygon(x, y, TabBlessingSmall));

    // A drinking horn: from its wide mouth at the top down a curve to its tip, getting thinner, a rim band cut near the
    // mouth. mirror: bends the other way.
    private static float Horn(float x, float y, bool mirror)
    {
        if (mirror) x = 64f - x;
        const int Steps = 8;
        float horn = float.MaxValue;
        Vector2 previous = HornPoint(0f);
        for (int k = 1; k <= Steps; k++)
        {
            float t = k / (float)Steps;
            Vector2 point = HornPoint(t);
            float radius = Mathf.Lerp(6.5f, 1.2f, t - 0.5f / Steps);
            horn = Mathf.Min(horn, Capsule(x, y, previous.x, previous.y, point.x, point.y, radius));
            previous = point;
        }
        Vector2 a = HornPoint(0.12f), b = HornPoint(0.2f);
        Vector2 across = new Vector2(b.y - a.y, a.x - b.x).normalized * 9f;
        Vector2 middle = (a + b) / 2f;
        float band = Capsule(x, y, middle.x - across.x, middle.y - across.y, middle.x + across.x, middle.y + across.y, 0.9f);
        return Mathf.Max(horn, -band);
    }

    // Along the horn: its mouth up and to the left of the middle, curving down to the tip at the bottom right.
    private static Vector2 HornPoint(float t) =>
        new(Mathf.Lerp(22f, 50f, t) - Mathf.Sin(t * Mathf.PI) * 6f, Mathf.Lerp(9f, 57f, t) + Mathf.Sin(t * Mathf.PI) * 2f);

    // Two drinking horns, their mouths raised together at the top, the left one cut away where they cross.
    private static float HornsShape(float x, float y)
    {
        float left = Horn(x, y, mirror: false), right = Horn(x, y, mirror: true);
        return Mathf.Min(Mathf.Max(left, -(right - 1.6f)), right);
    }

    private static readonly Vector2[] TabNose = [new(32f, 35f), new(28.5f, 42f), new(35.5f, 42f)];

    // A skull: the cranium and jaw, with eyes, nose and the gaps between the teeth cut out.
    private static float SkullShape(float x, float y)
    {
        float head = Mathf.Min(Circle(x, y, 32f, 27f, 20f), Box(x, y, 20f, 34f, 44f, 58f, 4f));
        float holes = Mathf.Min(Circle(x, y, 24.5f, 30f, 6f), Circle(x, y, 39.5f, 30f, 6f));
        holes = Mathf.Min(holes, Polygon(x, y, TabNose));
        holes = Mathf.Min(holes, Mathf.Min(Capsule(x, y, 26.5f, 49f, 26.5f, 60f, 1.4f),
            Mathf.Min(Capsule(x, y, 32f, 49f, 32f, 60f, 1.4f), Capsule(x, y, 37.5f, 49f, 37.5f, 60f, 1.4f))));
        return Mathf.Max(head, -holes);
    }

    // A star's outline: a tip at each of the given lengths, the first pointing up, with dents between them.
    private static Vector2[] StarPoints(float cx, float cy, float[] tips, float inner)
    {
        int count = tips.Length;
        Vector2[] points = new Vector2[count * 2];
        for (int k = 0; k < count; k++)
        {
            float angle = k * Mathf.PI * 2f / count - Mathf.PI / 2f;
            points[k * 2] = new Vector2(cx + Mathf.Cos(angle) * tips[k], cy + Mathf.Sin(angle) * tips[k]);
            float between = angle + Mathf.PI / count;
            points[k * 2 + 1] = new Vector2(cx + Mathf.Cos(between) * inner, cy + Mathf.Sin(between) * inner);
        }
        return points;
    }

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
                MistPicture(canvas);
                break;
            case WeatherKind.Sootfall:
                SootfallPicture(canvas);
                break;
            case WeatherKind.PolarDay:
                Sun(canvas, 32f, 26f, 0.78f);
                SnowyHill(canvas);
                break;
            case WeatherKind.PolarNight:
                if (GameArt.Get("Cold") is { } sky) canvas.Draw(sky, 0f, 0f, 64f, 64f);
                Moon(canvas);
                break;
            case WeatherKind.GaleBreath:
                GaleBreathPicture(canvas);
                break;
            case WeatherKind.NoMist:
                TrueSightPicture(canvas);
                break;
            case WeatherKind.Aurora:
                if (GameArt.Get("Cold") is { } night) canvas.Draw(night, 0f, 0f, 64f, 64f);
                AuroraBands(canvas);
                break;
            case WeatherKind.Crimson:
                CrimsonPicture(canvas);
                break;
            case WeatherKind.Ragnarok:
                RagnarokPicture(canvas);
                break;
            case WeatherKind.Tailwind:
                TailwindPicture(canvas);
                break;
            case WeatherKind.CryptDelver:
                // A chest in a crypt's stone doorway, lit green from within, with gold spilling out in front.
                CryptArch(canvas);
                Glow(canvas, 32f, 40f, 22f, new Color(0.35f, 1f, 0.45f), 0.55f);
                if (GameArt.Get("chest_wood") is { } chest) canvas.Draw(chest, 12f, 18f, 40f, 40f);
                else Glow(canvas, 32f, 44f, 14f, new Color(0.6f, 0.4f, 0.2f), 0.9f);
                if (GameArt.Get("treasure_pile") is { } gold) canvas.Draw(gold, 24f, 37f, 28f, 26f);
                Sparkle(canvas, 52f, 44f, 4f, new Color(1f, 0.85f, 0.3f));
                Sparkle(canvas, 15f, 52f, 3f, new Color(1f, 0.85f, 0.3f));
                break;
            case WeatherKind.Verdant:
                // A seedling glowing green, with sparkles.
                Glow(canvas, 32f, 36f, 30f, new Color(0.4f, 1f, 0.4f), 0.6f);
                if (GameArt.Get("kaleseedling") is { } seedling) canvas.Draw(seedling, 4f, 0f, 56f, 66f);
                Sparkle(canvas, 14f, 16f, 4.5f);
                Sparkle(canvas, 50f, 12f, 3.5f);
                Sparkle(canvas, 48f, 40f, 4f);
                break;
            case WeatherKind.Packward:
                PackwardPicture(canvas);
                break;
            case WeatherKind.Carrion:
                CarrionPicture(canvas);
                break;
            case WeatherKind.VatBoil:
                VatBoilPicture(canvas);
                break;
            case WeatherKind.PitchBellows:
                PitchBellowsPicture(canvas);
                break;
            case WeatherKind.AmberMend:
                AmberMendPicture(canvas);
                break;
            case WeatherKind.AbyssalSurge:
                AbyssalSurgePicture(canvas);
                break;
            case WeatherKind.GlacialSatiety:
                GlacialSatietyPicture(canvas);
                break;
            case WeatherKind.VeinShatter:
                VeinShatterPicture(canvas);
                break;
            case WeatherKind.CarapaceTemper:
                CarapaceTemperPicture(canvas);
                break;
            case WeatherKind.GraveBind:
                GraveBindPicture(canvas);
                break;
            case WeatherKind.FortunesBoon:
                FortunesBoonPicture(canvas);
                break;
            case WeatherKind.Luminary:
                LuminaryPicture(canvas);
                break;
            case WeatherKind.AbyssalDraw:
                AbyssalDrawPicture(canvas);
                break;
            case WeatherKind.SovereignWard:
                SovereignWardPicture(canvas);
                break;
            case WeatherKind.Skal:
                // Its status is the game's own Rested, but the rites page shows this.
                SkalPicture(canvas);
                break;
            case WeatherKind.SkyCall:
                SkyCallPicture(canvas);
                break;
            case WeatherKind.AnvilSpark:
                AnvilSparkPicture(canvas);
                break;
            case WeatherKind.Grounding:
                GroundingPicture(canvas);
                break;
            case WeatherKind.Sanctuary:
                SanctuaryPicture(canvas);
                break;
            case WeatherKind.Reset:
                EquilibriumPicture(canvas);
                break;
            case WeatherKind.Unmaking:
                UnmakingPicture(canvas);
                break;
            default:
                return null;
        }
        return canvas;
    }

    // ---- Rite pictures ----

    // Abyssal Surge: a sea serpent rearing out of the sea at night, its head, a spiked hump and its tail fin above the
    // waves, a school of fish below.
    private static void AbyssalSurgePicture(Canvas canvas)
    {
        if (GameArt.Get("Cold") is { } night) canvas.Draw(night, 0f, 0f, 64f, 64f);
        Glow(canvas, 34f, 24f, 28f, new Color(0.3f, 0.75f, 0.8f), 0.3f);
        Shape(canvas, SerpentShape, (x, y, depth) =>
        {
            float light = Mathf.Lerp(0.55f, 1f, Mathf.Clamp01(depth / 2.5f)) * Mathf.Lerp(1.2f, 0.75f, Mathf.Clamp01((y - 10f) / 36f));
            return new Color(0.16f * light, 0.5f * light, 0.44f * light, 1f);
        });
        // Its eye, glowing.
        canvas.Paint((x, y) =>
        {
            float d = Circle(x, y, 20.5f, 14f, 1.2f);
            float glow = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Max(d, 0f) / 3.5f), 2f) * 0.8f;
            return Canvas.Over(new Color(1f, 0.8f, 0.2f, glow), new Color(1f, 0.95f, 0.6f, Mathf.Clamp01(0.5f - d)));
        });
        Sea(canvas);
        canvas.FadeEdges(4f, top: false, corner: 10f);
    }

    private static readonly Vector2[] SerpentHead =
        [new(13f, 15f), new(19f, 10.5f), new(26f, 12f), new(31.5f, 15.5f), new(24f, 17f), new(30f, 20.5f), new(22f, 22f), new(15f, 22f)];
    private static readonly Vector2[] SerpentFin = [new(54f, 37f), new(62f, 27f), new(61.5f, 37f), new(57f, 41f)];
    private static readonly Vector2[][] SerpentSpikes =
    [
        [new(29.5f, 31f), new(31f, 22.5f), new(34f, 28f)],
        [new(34.5f, 27f), new(38f, 19.5f), new(39f, 28f)],
        [new(40f, 30f), new(44.5f, 25f), new(43.5f, 33f)]
    ];

    private static float SerpentShape(float x, float y)
    {
        float neck = Mathf.Min(Capsule(x, y, 9f, 48f, 9f, 34f, 4f),
            Mathf.Min(Capsule(x, y, 9f, 34f, 12f, 25f, 3.6f), Capsule(x, y, 12f, 25f, 17f, 18f, 3.3f)));
        float head = Polygon(x, y, SerpentHead) - 0.8f;
        float hump = Mathf.Min(Mathf.Min(Capsule(x, y, 26f, 48f, 28.5f, 35f, 4f), Capsule(x, y, 28.5f, 35f, 35f, 29f, 4f)),
            Mathf.Min(Capsule(x, y, 35f, 29f, 41.5f, 34f, 4f), Capsule(x, y, 41.5f, 34f, 44f, 48f, 4f)));
        float spikes = float.MaxValue;
        foreach (Vector2[] spike in SerpentSpikes) spikes = Mathf.Min(spikes, Polygon(x, y, spike));
        float tail = Mathf.Min(Capsule(x, y, 50f, 48f, 53f, 41f, 2.8f), Capsule(x, y, 53f, 41f, 57f, 37f, 2f));
        return Mathf.Min(Mathf.Min(Mathf.Min(neck, head), Mathf.Min(hump, spikes)), Mathf.Min(tail, Polygon(x, y, SerpentFin)));
    }

    // Deep water over the bottom, its surface foaming, a school of pale fish in it.
    private static void Sea(Canvas canvas)
    {
        canvas.Paint((x, y) =>
        {
            float below = y - (43f + Mathf.Sin(x / 4.5f) * 1.3f);
            if (below < -1.5f) return Color.clear;
            float depth = Mathf.Clamp01(below / 20f);
            Color water = new(Mathf.Lerp(0.1f, 0.03f, depth), Mathf.Lerp(0.36f, 0.12f, depth), Mathf.Lerp(0.58f, 0.3f, depth),
                Mathf.Clamp01(below + 0.5f) * 0.92f);
            return Canvas.Over(water, new Color(0.85f, 0.96f, 1f, Mathf.Clamp01(1.2f - Mathf.Abs(below)) * 0.9f));
        });
        foreach ((float fx, float fy, float s) in new[]
                 {
                     (16f, 51f, 1f), (25f, 54.5f, 0.9f), (34f, 50.5f, 1f), (43f, 55f, 0.9f), (30f, 59.5f, 0.8f),
                     (20f, 59f, 0.75f), (40f, 61f, 0.75f), (51f, 51.5f, 0.9f)
                 })
            canvas.Paint((x, y) =>
            {
                float ex = (x - fx) / (2.6f * s), ey = (y - fy) / (1.1f * s);
                float body = (Mathf.Sqrt(ex * ex + ey * ey) - 1f) * 1.1f * s;
                float tail = Polygon(x, y, [new(fx - 2.2f * s, fy), new(fx - 4.4f * s, fy - 1.6f * s), new(fx - 4.4f * s, fy + 1.6f * s)]);
                return new Color(0.62f, 0.9f, 1f, Mathf.Clamp01(0.5f - Mathf.Min(body, tail)) * 0.85f);
            });
    }

    // Carrion: boar tracks leading in to a green ring around rotting meat, its stench rising.
    private static void CarrionPicture(Canvas canvas)
    {
        Color green = new(0.55f, 1f, 0.3f);
        Glow(canvas, 32f, 34f, 20f, green, 0.4f);
        canvas.Paint((x, y) =>
        {
            // A ring of dashes.
            float angle = Mathf.Atan2(y - 34f, x - 32f);
            float dash = Mathf.Abs(Mathf.Repeat(angle / (Mathf.PI * 2f) * 16f, 1f) - 0.5f);
            float d = Mathf.Abs(Circle(x, y, 32f, 34f, 11f)) - 0.9f;
            float glow = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Max(d, 0f) / 4f), 2f) * 0.5f;
            return Canvas.Over(new Color(green.r, green.g, green.b, glow), new Color(0.8f, 1f, 0.6f, Mathf.Clamp01(0.5f - d) * Mathf.Clamp01(dash * 8f - 1f)));
        });
        if (GameArt.Get("meat_rotten") is { } meat) canvas.Draw(meat, 16f, 18f, 32f, 32f);
        // The stench, rising in waves.
        foreach (float sx in new[] { 26f, 32f, 38f })
            canvas.Paint((x, y) =>
            {
                if (y > 26f || y < 3f) return Color.clear;
                float wave = sx + Mathf.Sin(y / 3f + sx) * 1.8f;
                float fade = Mathf.Clamp01((y - 3f) / 8f) * Mathf.Clamp01((26f - y) / 4f);
                return new Color(green.r, green.g, green.b, Mathf.Clamp01(1.3f - Mathf.Abs(x - wave)) * fade * 0.9f);
            });
        // Two prints along each track, the farther fainter, all heading in.
        foreach (float direction in new[] { -150f, -30f, 90f })
        {
            float rad = direction * Mathf.Deg2Rad;
            for (int step = 0; step < 2; step++)
            {
                float distance = 20.5f + step * 8.5f, side = step == 0 ? 2f : -2f;
                float cx = 32f + Mathf.Cos(rad) * distance - Mathf.Sin(rad) * side;
                float cy = 34f + Mathf.Sin(rad) * distance + Mathf.Cos(rad) * side;
                float alpha = step == 0 ? 1f : 0.6f, scale = step == 0 ? 1.5f : 1.3f;
                canvas.Paint((x, y) => new Color(0.75f, 1f, 0.6f, Mathf.Clamp01(0.5f - Hoof(x, y, cx, cy, direction + 180f, scale)) * alpha));
            }
        }
    }

    // A boar's cloven hoof print pointing along degrees: two halves, two dewclaws behind.
    private static float Hoof(float x, float y, float cx, float cy, float degrees, float scale)
    {
        Vector2 local = Unturn(x, y, cx, cy, degrees);
        float lx = (local.x - cx) / scale, ly = (local.y - cy) / scale;
        float halves = Mathf.Min(Capsule(lx, ly, -1.2f, -1.25f, 1.8f, -0.75f, 0.95f), Capsule(lx, ly, -1.2f, 1.25f, 1.8f, 0.75f, 0.95f));
        float claws = Mathf.Min(Circle(lx, ly, -3.6f, -1.9f, 0.6f), Circle(lx, ly, -3.6f, 1.9f, 0.6f));
        return Mathf.Min(halves, claws) * scale;
    }

    // Vein-Shatter: a rock of ore split apart by a pickaxe's blow, light glowing through the cracks, chips flying.
    private static readonly float[] RockCracks = [-105f, -20f, 62f, 150f];

    private static void VeinShatterPicture(Canvas canvas)
    {
        const float x0 = 2f, y0 = 10f, size = 56f, cx = 30f, cy = 40f;
        Glow(canvas, cx, cy, 22f, new Color(1f, 0.6f, 0.2f), 0.8f);
        Pixels? rock = GameArt.Get("TinOre");
        for (int k = 0; k < RockCracks.Length; k++)
        {
            float a0 = RockCracks[k] * Mathf.Deg2Rad;
            float a1 = (k + 1 < RockCracks.Length ? RockCracks[k + 1] : RockCracks[0] + 360f) * Mathf.Deg2Rad;
            Vector2 from = new(Mathf.Cos(a0), Mathf.Sin(a0)), to = new(Mathf.Cos(a1), Mathf.Sin(a1));
            Vector2 push = (from + to).normalized * 2.2f;
            // The piece: the part of the rock between two cracks (a jagged edge along each), pushed out.
            float Piece(float u, float v)
            {
                float px = x0 + u * size - cx, py = y0 + v * size - cy;
                float jag = Mathf.Sin((px + py) * 1.3f) * 0.5f;
                return Mathf.Clamp01(Mathf.Min(from.x * py - from.y * px, px * to.y - py * to.x) + jag - 0.5f);
            }
            if (rock != null) canvas.Draw(rock, x0 + push.x, y0 + push.y, size, size, null, 0f, Piece);
        }
        GlowLine(canvas, (x, y) => Strokes(x, y, 0.5f, new Vector4(cx, cy, cx - 4f, cy - 20f), new Vector4(cx, cy, cx + 20f, cy - 7f)),
            new Color(1f, 0.6f, 0.2f), new Color(1f, 0.9f, 0.6f), 3f, 0.5f);
        foreach ((float x, float y, float r) in new[] { (11f, 22f, 1.6f), (50f, 20f, 1.3f), (5f, 34f, 1.1f), (59f, 54f, 1.4f), (8f, 58f, 1.2f) })
            canvas.Paint((px, py) =>
            {
                float d = Circle(px, py, x, y, r);
                float glow = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Max(d, 0f) / 2.5f), 2f) * 0.5f;
                return Canvas.Over(new Color(1f, 0.6f, 0.2f, glow), new Color(0.6f, 0.55f, 0.5f, Mathf.Clamp01(0.5f - d)));
            });
        if (GameArt.Get("pickaxe_iron") is { } pick) canvas.Draw(pick, 26f, -4f, 38f, 38f);
        Sparkle(canvas, 31f, 22f, 4.5f, new Color(1f, 0.75f, 0.3f));
    }

    // Carapace Temper: a sword on an anvil, wrapped in a glowing amber shell of plates, sparks flying.
    private static readonly Vector2[] Anvil =
    [
        new(3f, 38f), new(15f, 35f), new(52f, 35f), new(57f, 37f), new(57f, 42f), new(45f, 43.5f), new(40f, 48f), new(47f, 54f),
        new(47f, 59f), new(17f, 59f), new(17f, 54f), new(24f, 48f), new(19f, 43.5f), new(11f, 42f)
    ];

    private static void CarapaceTemperPicture(Canvas canvas)
    {
        Glow(canvas, 32f, 28f, 30f, new Color(1f, 0.7f, 0.25f), 0.5f);
        Shape(canvas, (x, y) => Polygon(x, y, Anvil) - 0.5f, (x, y, depth) =>
        {
            float top = Mathf.Clamp01(1f - (y - 34.5f) / 2.5f);
            float light = Mathf.Lerp(0.55f, 0.32f, Mathf.Clamp01((y - 35f) / 24f)) * (0.8f + 0.2f * Mathf.Clamp01(depth / 2f)) + top * 0.35f;
            return new Color(light * 0.95f, light, light * 1.08f, 1f);
        });
        if (GameArt.Get("SwordIron") is { } sword)
        {
            // An amber shell around the blade: the sword once more, bigger and glowing, behind it.
            Glow(canvas, 32f, 33f, 26f, new Color(1f, 0.6f, 0.15f), 0.5f);
            canvas.Draw(sword, -2f, 0f, 68f, 68f, new Color(1f, 0.55f, 0.1f, 0.8f), 45f);
            canvas.Draw(sword, 2f, 4f, 60f, 60f, new Color(1f, 0.85f, 0.45f, 1f), 45f);
            canvas.Draw(sword, 6f, 8f, 52f, 52f, new Color(1f, 0.92f, 0.8f), 45f);
            // Plates of the shell, faint across the blade.
            canvas.Paint((x, y) =>
            {
                float ex = (x - 32f) / 27f, ey = (y - 33.5f) / 4.5f;
                float inside = Mathf.Clamp01((1f - Mathf.Sqrt(ex * ex + ey * ey)) * 3f);
                if (inside <= 0f) return Color.clear;
                float lattice = 0f;
                for (int k = 0; k < 3; k++)
                {
                    float angle = k * Mathf.PI / 3f + Mathf.PI / 6f;
                    float t = (x * Mathf.Cos(angle) + y * Mathf.Sin(angle)) / 4f;
                    lattice = Mathf.Max(lattice, Mathf.Clamp01(0.5f - (Mathf.Abs(t - Mathf.Round(t)) * 4f - 0.25f)));
                }
                return new Color(1f, 0.6f, 0.15f, inside * lattice * 0.5f);
            });
        }
        foreach ((float x, float y) in new[] { (22f, 27f), (40f, 24f), (30f, 21f), (46f, 28f) })
            canvas.Paint((px, py) =>
            {
                float d = Circle(px, py, x, y, 0.8f);
                float glow = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Max(d, 0f) / 2.5f), 2f) * 0.7f;
                return Canvas.Over(new Color(1f, 0.55f, 0.15f, glow), new Color(1f, 0.9f, 0.6f, Mathf.Clamp01(0.5f - d)));
            });
        Sparkle(canvas, 50f, 15f, 4f, new Color(1f, 0.75f, 0.3f));
        Sparkle(canvas, 13f, 18f, 3f, new Color(1f, 0.75f, 0.3f));
        Sparkle(canvas, 56f, 30f, 2.5f, new Color(1f, 0.75f, 0.3f));
    }

    // Packward: a blue ward ring around a fire, wolf tracks leading away from it.
    private static void PackwardPicture(Canvas canvas)
    {
        Color ward = new(0.5f, 0.78f, 1f);
        Glow(canvas, 32f, 32f, 20f, ward, 0.45f);
        Vector4[] ticks = new Vector4[12];
        for (int k = 0; k < ticks.Length; k++)
        {
            float angle = k * Mathf.PI / 6f;
            float cos = Mathf.Cos(angle), sin = Mathf.Sin(angle);
            ticks[k] = new Vector4(32f + cos * 11.5f, 32f + sin * 11.5f, 32f + cos * 13.5f, 32f + sin * 13.5f);
        }
        GlowLine(canvas, (x, y) => Mathf.Min(Mathf.Abs(Circle(x, y, 32f, 32f, 9f)) - 0.9f, Strokes(x, y, 0.7f, ticks)),
            ward, new Color(0.85f, 0.95f, 1f), 4f, 0.7f);
        Flame(canvas, 32f, 37f, 7f, 11f);
        // Two prints along each track, fainter the farther.
        foreach (float direction in new[] { -135f, -45f, 90f })
        {
            float rad = direction * Mathf.Deg2Rad;
            for (int step = 0; step < 2; step++)
            {
                float distance = 20f + step * 9f, side = step == 0 ? -2.2f : 2.2f;
                float cx = 32f + Mathf.Cos(rad) * distance - Mathf.Sin(rad) * side;
                float cy = 32f + Mathf.Sin(rad) * distance + Mathf.Cos(rad) * side;
                float alpha = step == 0 ? 1f : 0.6f, scale = step == 0 ? 1.5f : 1.3f;
                canvas.Paint((x, y) => new Color(0.75f, 0.9f, 1f, Mathf.Clamp01(0.5f - Paw(x, y, cx, cy, direction, scale)) * alpha));
            }
        }
    }

    // A paw print pointing along degrees (0: right, turning clockwise): the pad, four toes in front.
    private static float Paw(float x, float y, float cx, float cy, float degrees, float scale)
    {
        Vector2 local = Unturn(x, y, cx, cy, degrees);
        float lx = (local.x - cx) / scale, ly = (local.y - cy) / scale;
        float pad = Circle(lx * 1.2f, ly, -0.9f, 0f, 2.2f);
        float toes = Mathf.Min(Mathf.Min(Circle(lx, ly, 2.7f, -2.6f, 0.95f), Circle(lx, ly, 3.9f, -0.95f, 1f)),
            Mathf.Min(Circle(lx, ly, 3.9f, 0.95f, 1f), Circle(lx, ly, 2.7f, 2.6f, 0.95f)));
        return Mathf.Min(pad, toes) * scale;
    }

    // Vat-Boil: the fermenter over a small log fire, foam boiling over its top and bubbles rising, all of it inside the
    // icon (nothing cut off at the top or bottom).
    private static void VatBoilPicture(Canvas canvas)
    {
        // The vat drawn a little smaller and higher than the sprite's box; the foam follows it.
        const float k = 0.88f;
        static float X(float x) => 9f + (x - 6f) * k;
        static float Y(float y) => 11f + (y - 14f) * k;
        Glow(canvas, 32f, 56f, 24f, new Color(1f, 0.45f, 0.1f), 0.55f);
        Glow(canvas, 30f, 16f, 20f, new Color(0.75f, 1f, 0.3f), 0.35f);
        if (GameArt.Get("fermenter") is { } vat) canvas.Draw(vat, X(6f), Y(14f), 46f * k, 46f * k);
        else if (GameArt.Get("Barrel") is { } barrel) canvas.Draw(barrel, X(6f), Y(14f), 46f * k, 46f * k);
        // Two crossed logs under it, glowing at the ends, the flames standing on them.
        Shape(canvas, (x, y) => Mathf.Min(Capsule(x, y, 15f, 60f, 47f, 56.5f, 2f), Capsule(x, y, 17f, 56.5f, 49f, 60f, 2f)), (x, y, depth) =>
        {
            float ember = Mathf.Clamp01(1f - Mathf.Min(Mathf.Abs(x - 15.5f), Mathf.Abs(x - 48.5f)) / 3f);
            float light = 0.7f + 0.3f * Mathf.Clamp01(depth / 1.2f);
            return Color.Lerp(new Color(0.42f * light, 0.27f * light, 0.15f * light, 1f), new Color(1f, 0.55f, 0.15f, 1f), ember * 0.8f);
        }, 0.4f);
        Flame(canvas, 22f, 59f, 6f, 10f, -2f);
        Flame(canvas, 41f, 59f, 6f, 10f, 2f);
        Flame(canvas, 31.5f, 59f, 8f, 13f);
        // Foam heaped over the rim and running down its side.
        canvas.Paint((x, y) => FoamColor(Capsule(x, y, X(41f), Y(18f), X(41.5f), Y(27f), (1.6f - Mathf.Clamp01((y - Y(18f)) / (9f * k)) * 0.6f) * k),
            x, y, X(41f), Y(18f), 3f * k));
        foreach ((float bx, float by, float r) in new[]
                 {
                     (18f, 19f, 3.4f), (40f, 18f, 3.6f), (23.5f, 16f, 4.4f), (35f, 15.5f, 4.5f), (29.5f, 13.5f, 5f),
                     (26f, 19.5f, 3.4f), (32.5f, 19.5f, 3.3f)
                 })
            canvas.Paint((x, y) => FoamColor(Circle(x, y, X(bx), Y(by), r * k), x, y, X(bx), Y(by), r * k));
        foreach ((float bx, float by, float r) in new[] { (47f, 10f, 3.2f), (54f, 18f, 2.3f), (41f, 5.5f, 1.8f), (56f, 9f, 1.6f) })
            Bubble(canvas, bx, by, r);
    }

    // A foam bubble: pale green, lit from the top left, a darker rim and a glint.
    private static Color FoamColor(float d, float x, float y, float cx, float cy, float r)
    {
        if (d > 1f) return Color.clear;
        float t = Mathf.Clamp01((x - cx + y - cy) / (2.5f * r) + 0.5f);
        Color fill = Color.Lerp(new Color(0.95f, 1f, 0.72f), new Color(0.55f, 0.78f, 0.2f), t);
        fill.a = Mathf.Clamp01(0.5f - d);
        Color rim = new(0.3f, 0.45f, 0.1f, Mathf.Clamp01(0.5f - (Mathf.Abs(d + 0.5f) - 0.45f)) * 0.75f);
        Color glint = new(1f, 1f, 1f, Mathf.Clamp01(0.5f - Circle(x, y, cx - r * 0.4f, cy - r * 0.4f, r * 0.2f)));
        return Canvas.Over(Canvas.Over(fill, rim), glint);
    }

    // Pitch-Bellows: a stone furnace, its mouth glowing white-hot and flames leaping from its chimney, the tar-black
    // bellows at its side blowing a stream of embers in.
    private static readonly Vector2[] BellowsTop = [new(4f, 37f), new(27f, 45.5f), new(27f, 47.5f), new(4f, 42f)];
    private static readonly Vector2[] BellowsBottom = [new(4f, 54f), new(27f, 49.5f), new(27f, 51.5f), new(4f, 59f)];
    private static readonly Vector2[] BellowsLeather = [new(6f, 41f), new(26f, 47f), new(26f, 50f), new(6f, 55f)];

    private static void PitchBellowsPicture(Canvas canvas)
    {
        Color ember = new(1f, 0.45f, 0.12f);
        Color white = new(1f, 0.9f, 0.6f);
        Glow(canvas, 44f, 46f, 30f, new Color(1f, 0.4f, 0.1f), 0.55f);

        // The furnace: a domed stone body with a short chimney, lit from the left and by its own mouth.
        static float Furnace(float x, float y) =>
            Mathf.Min(Mathf.Min(Box(x, y, 28f, 40f, 62f, 62f, 2f), Ellipse(x, y, 45f, 41f, 17f, 14f)), Box(x, y, 40.5f, 21f, 49.5f, 31f, 1f));
        static float Mouth(float x, float y) => Mathf.Min(Box(x, y, 38f, 50f, 52f, 62f, 0f), Circle(x, y, 45f, 50f, 7f));
        Shape(canvas, Furnace, (x, y, depth) =>
        {
            float light = Mathf.Lerp(0.62f, 0.34f, Mathf.Clamp01((x - 30f) / 30f)) * (0.8f + 0.2f * Mathf.Clamp01(depth / 2f));
            // Rough stone courses.
            float course = Mathf.Abs(Mathf.Repeat(y, 6f) - 3f) < 0.45f ? 0.75f : 1f;
            float heat = Mathf.Clamp01(1f - Mathf.Max(0f, Mouth(x, y)) / 8f) * 0.65f;
            Color stone = new(light * 0.95f * course, light * 0.92f * course, light * 0.88f * course, 1f);
            return Color.Lerp(stone, new Color(1f, 0.5f, 0.18f, 1f), heat);
        });
        canvas.Paint((x, y) =>
        {
            float d = Mouth(x, y) + 1.2f;
            if (d > 1f) return Color.clear;
            float core = Mathf.Clamp01(1f - Circle(x, y, 45f, 56f, 0f) / 9f);
            Color fire = Color.Lerp(new Color(1f, 0.4f, 0.08f), new Color(1f, 0.95f, 0.7f), core);
            fire.a = Mathf.Clamp01(0.5f - d);
            return fire;
        });
        Flame(canvas, 41.5f, 24f, 7f, 13f, -2f);
        Flame(canvas, 48.5f, 24f, 6f, 11f, 2.5f);
        Flame(canvas, 45f, 24f, 8f, 17f);

        // The bellows: two tar-black boards, pleated leather between them, an iron nozzle at the furnace's mouth.
        Shape(canvas, (x, y) => Polygon(x, y, BellowsLeather), (x, y, depth) =>
        {
            float pleat = 0.75f + 0.25f * Mathf.Abs(Mathf.Sin(x * 1.1f));
            return new Color(0.42f * pleat, 0.27f * pleat, 0.16f * pleat, 1f);
        }, 0.4f);
        foreach (Vector2[] board in new[] { BellowsTop, BellowsBottom })
            Shape(canvas, (x, y) => Polygon(x, y, board) - 0.3f, (x, y, depth) =>
            {
                float sheen = Mathf.Clamp01(1f - Mathf.Abs(depth - 0.8f)) * 0.12f;
                return new Color(0.13f + sheen, 0.12f + sheen, 0.12f + sheen, 1f);
            });
        Shape(canvas, (x, y) => Mathf.Min(Capsule(x, y, 4.5f, 39.5f, 1.5f, 34f, 1.1f), Capsule(x, y, 4.5f, 56.5f, 1.5f, 62f, 1.1f)),
            (x, y, depth) => new Color(0.36f, 0.24f, 0.14f, 1f), 0.4f);
        Shape(canvas, (x, y) => Capsule(x, y, 26f, 48.5f, 37f, 52f, 1.5f), (x, y, depth) =>
        {
            float light = 0.35f + 0.25f * Mathf.Clamp01(depth / 1.2f);
            return new Color(light, light * 0.98f, light * 1.05f, 1f);
        });

        // Embers streaming from the nozzle into the mouth and rising off the furnace.
        foreach ((float ex, float ey, float r) in new[] { (39f, 50.5f, 1.1f), (35.5f, 46f, 0.9f), (40f, 45.5f, 0.8f), (33f, 42f, 0.7f) })
            canvas.Paint((x, y) =>
            {
                float d = Circle(x, y, ex, ey, r);
                float halo = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Max(d, 0f) / 3f), 2f) * 0.7f;
                return Canvas.Over(new Color(ember.r, ember.g, ember.b, halo), new Color(white.r, white.g, white.b, Mathf.Clamp01(0.5f - d)));
            });
        Sparkle(canvas, 54f, 6f, 3f, ember);
        Sparkle(canvas, 36f, 7f, 2.5f, ember);
        Sparkle(canvas, 21f, 28f, 3f, ember);
    }

    // Amber-Mend: a cracked wooden wall, the crack sealed with glowing amber, the hammer striking it.
    private static readonly Vector2[] AmberCrack =
        [new(28f, 15f), new(26.5f, 19f), new(27.5f, 23f), new(24.5f, 28f), new(26f, 33f), new(23.5f, 38f), new(25.5f, 44f), new(24f, 50f), new(25f, 57f)];
    private static readonly Vector2[] AmberBranch = [new(26f, 33f), new(30f, 36f), new(31f, 41f)];

    private static void AmberMendPicture(Canvas canvas)
    {
        Glow(canvas, 28f, 36f, 30f, new Color(1f, 0.6f, 0.12f), 0.45f);
        if (GameArt.Get("wood_wall") is { } wall) canvas.Draw(wall, 4f, 12f, 44f, 48f);
        else Shape(canvas, (x, y) => Box(x, y, 7f, 14f, 46f, 58f, 2f), (x, y, depth) => new Color(0.5f, 0.33f, 0.18f, 1f));
        GlowLine(canvas, (x, y) => Mathf.Min(Polyline(x, y, AmberCrack, 1.1f), Polyline(x, y, AmberBranch, 0.8f)),
            new Color(1f, 0.55f, 0.08f), new Color(1f, 0.85f, 0.35f), 4.5f, 0.85f);
        if (GameArt.Get("hammer") is { } hammer) canvas.Draw(hammer, 27f, 0f, 36f, 36f);
        Sparkle(canvas, 28.5f, 14f, 4.5f, new Color(1f, 0.7f, 0.25f));
        Sparkle(canvas, 16f, 44f, 3f, new Color(1f, 0.7f, 0.25f));
        Sparkle(canvas, 37f, 50f, 2.5f, new Color(1f, 0.7f, 0.25f));
    }

    // Sovereign Ward: a longhouse under a golden latticed dome with a crown on top, a fireball shattering against it.
    private static void SovereignWardPicture(Canvas canvas)
    {
        canvas.Paint((x, y) =>
        {
            if (y > 59f) return Color.clear;
            float r = Mathf.Sqrt((x - 32f) * (x - 32f) + (y - 59f) * (y - 59f));
            float edge = Mathf.Clamp01(0.5f - (Mathf.Abs(r - 28f) - 1.1f));
            float glow = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(r - 28f) / 6f), 2f) * 0.5f;
            float fill = 0f;
            if (r < 28f)
            {
                float lattice = 0f;
                for (int k = 0; k < 3; k++)
                {
                    float angle = k * Mathf.PI / 3f;
                    float t = (x * Mathf.Cos(angle) + y * Mathf.Sin(angle)) / 7f;
                    lattice = Mathf.Max(lattice, Mathf.Clamp01(0.5f - (Mathf.Abs(t - Mathf.Round(t)) * 7f - 0.35f)));
                }
                fill = 0.14f + lattice * 0.22f * Mathf.Clamp01(r / 14f);
            }
            return new Color(1f, 0.85f, 0.4f, Mathf.Clamp01(Mathf.Max(edge, glow) + fill));
        });
        Shape(canvas, (x, y) => LonghouseShape((x - 16f) * 2f, (y - 30f) * 2f) * 0.5f, (x, y, depth) =>
        {
            float light = Mathf.Lerp(1.1f, 0.75f, Mathf.Clamp01((y - 32f) / 27f)) * (0.8f + 0.2f * Mathf.Clamp01(depth / 1.5f));
            return new Color(0.52f * light, 0.34f * light, 0.2f * light, 1f);
        });
        Shape(canvas, (x, y) => Polygon(x, y, WardCrown), (x, y, depth) =>
        {
            float light = 0.8f + 0.3f * Mathf.Clamp01(depth / 1.5f);
            return new Color(1f * light, 0.8f * light, 0.3f * light, 1f);
        });
        canvas.Paint((x, y) => new Color(0.9f, 0.15f, 0.15f, Mathf.Clamp01(0.5f - Circle(x, y, 32f, 28.5f, 1.2f))));
        // The fireball, broken where it meets the dome, its pieces flying back.
        canvas.Paint((x, y) =>
        {
            float trail = Capsule(x, y, 53f, 34f, 62f, 23f, 2.6f - Mathf.Clamp01((53f - x) / -9f) * 2f);
            float alpha = Mathf.Clamp01(0.5f - trail) * Mathf.Lerp(0.9f, 0.2f, Mathf.Clamp01((x - 53f) / 9f));
            return new Color(1f, 0.5f, 0.12f, alpha);
        });
        Glow(canvas, 51f, 36f, 8f, new Color(1f, 0.8f, 0.4f), 0.9f);
        foreach (Vector2[] shard in WardShards)
            canvas.Paint((x, y) => new Color(1f, 0.55f, 0.15f, Mathf.Clamp01(0.5f - Polygon(x, y, shard))));
    }

    private static readonly Vector2[] WardCrown =
        [new(25.5f, 31.5f), new(25f, 24.5f), new(28.7f, 28f), new(32f, 21.5f), new(35.3f, 28f), new(39f, 24.5f), new(38.5f, 31.5f)];
    private static readonly Vector2[][] WardShards =
    [
        [new(56f, 39f), new(59.5f, 40f), new(57f, 42.5f)],
        [new(55f, 29f), new(58f, 27f), new(57.5f, 30.5f)],
        [new(46f, 29f), new(48f, 26.5f), new(49f, 29.5f)],
        [new(59f, 34f), new(62f, 33.5f), new(60.5f, 36.5f)]
    ];

    // Glacial Satiety: cooked meat frozen in a block of ice, frost on its corners.
    private static void GlacialSatietyPicture(Canvas canvas)
    {
        Glow(canvas, 32f, 34f, 30f, new Color(0.55f, 0.85f, 1f), 0.45f);
        canvas.Paint((x, y) =>
        {
            float shade = Mathf.Clamp01((x - 8f + y - 12f) / 96f);
            return new Color(0.6f, 0.82f, 0.95f, Mathf.Clamp01(0.5f - Box(x, y, 8f, 12f, 56f, 56f, 6f)) * Mathf.Lerp(0.45f, 0.3f, shade));
        });
        if (GameArt.Get("meat_cooked") is { } meat) canvas.Draw(meat, 11f, 11f, 42f, 42f, new Color(0.92f, 0.96f, 1f));
        canvas.Paint((x, y) =>
        {
            float d = Box(x, y, 8f, 12f, 56f, 56f, 6f);
            if (d > 1f) return Color.clear;
            Color ice = new(0.75f, 0.92f, 1f, Mathf.Clamp01(0.5f - d) * 0.12f);
            Color rim = new(0.88f, 0.97f, 1f, Mathf.Clamp01(0.5f - (Mathf.Abs(d + 1f) - 1f)) * 0.9f);
            float shine = Mathf.Min(Mathf.Min(Capsule(x, y, 14f, 16.5f, 38f, 16.5f, 0.8f), Capsule(x, y, 12.5f, 19f, 12.5f, 38f, 0.8f)),
                Mathf.Min(Capsule(x, y, 41f, 22f, 50f, 31f, 0.7f), Capsule(x, y, 45f, 21f, 51f, 27f, 0.55f)));
            return Canvas.Over(Canvas.Over(ice, rim), new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - shine) * 0.75f));
        });
        if (GameArt.Get("Frost") is { } frost)
        {
            canvas.Draw(frost, 42f, 0f, 22f, 22f);
            canvas.Draw(frost, 1f, 45f, 15f, 15f);
        }
        Sparkle(canvas, 50f, 50f, 3f, new Color(0.6f, 0.85f, 1f));
    }

    // Abyssal Draw: a whirlpool from the deep, pulling things in along its arms.
    private static void AbyssalDrawPicture(Canvas canvas)
    {
        Whirlpool(canvas);
        foreach ((int arm, float r) in new[] { (0, 9f), (0, 21f), (1, 13f), (1, 25f), (2, 10f), (2, 19f) })
        {
            ArmTrail(canvas, arm, r, 9f, 0.7f, 0.9f);
            Vector2 p = ArmPoint(arm, r);
            canvas.Paint((x, y) =>
            {
                float d = Circle(x, y, p.x, p.y, 1.3f);
                float glow = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Max(d, 0f) / 3.5f), 2f) * 0.8f;
                return Canvas.Over(new Color(0.4f, 0.85f, 1f, glow), new Color(0.92f, 1f, 1f, Mathf.Clamp01(0.5f - d)));
            });
        }
        Sparkle(canvas, 52f, 12f, 3f, new Color(0.4f, 0.85f, 1f));
    }

    // A streak along one of the whirlpool's arms, from r out to r + length, brightest at r.
    private static void ArmTrail(Canvas canvas, int arm, float r, float length, float width, float strength)
    {
        Vector2[] points = new Vector2[8];
        for (int k = 0; k < points.Length; k++) points[k] = ArmPoint(arm, r + length * k / (points.Length - 1f));
        Vector2 head = points[0];
        canvas.Paint((x, y) =>
        {
            float d = Polyline(x, y, points, width);
            float along = Mathf.Clamp01(Vector2.Distance(new Vector2(x, y), head) / length);
            float halo = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Max(d, 0f) / 2.5f), 2f) * 0.45f;
            return Canvas.Over(new Color(0.4f, 0.85f, 1f, halo * (1f - along) * strength),
                new Color(0.8f, 0.97f, 1f, Mathf.Clamp01(0.5f - d) * (1f - along) * strength));
        });
    }

    // The whirlpool itself: three arms spiralling in to a dark eye.
    private static void Whirlpool(Canvas canvas)
    {
        canvas.Paint((x, y) =>
        {
            float dx = x - 32f, dy = y - 32f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float back = Mathf.Clamp01(1f - r / 31f);
            float arms = Mathf.Pow(0.5f + 0.5f * Mathf.Cos(3f * Mathf.Atan2(dy, dx) + 5.5f * Mathf.Log(Mathf.Max(r, 1f) / 4f)), 2f);
            float alpha = arms * Mathf.Clamp01(1f - r / 30f) * Mathf.Clamp01(r / 5f);
            float t = Mathf.Clamp01(r / 28f);
            Color arm = new(Mathf.Lerp(0.55f, 0.15f, t), Mathf.Lerp(0.95f, 0.35f, t), Mathf.Lerp(1f, 0.75f, t), alpha);
            return Canvas.Over(new Color(0.05f, 0.08f, 0.2f, Mathf.Pow(back, 0.6f) * 0.85f), arm);
        });
        // The dark eye at its heart.
        canvas.Paint((x, y) =>
        {
            float d = Circle(x, y, 32f, 32f, 4.5f);
            float glow = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(d) / 3f), 2f) * 0.9f;
            return Canvas.Over(new Color(0.01f, 0.02f, 0.06f, Mathf.Clamp01(0.5f - d)), new Color(0.5f, 0.95f, 1f, glow));
        });
    }

    // A point on the crest of one of the whirlpool's three arms (0 to 2), r pixels from its eye.
    private static Vector2 ArmPoint(int arm, float r)
    {
        float angle = (2f * Mathf.PI * arm - 5.5f * Mathf.Log(r / 4f)) / 3f;
        return new Vector2(32f + r * Mathf.Cos(angle), 32f + r * Mathf.Sin(angle));
    }

    // Ragnarök: Surtr's flaming sword plunged into burning ground under a black, eclipsed sun.
    private static void RagnarokPicture(Canvas canvas)
    {
        canvas.Paint((x, y) =>
        {
            float r = Mathf.Sqrt((x - 32f) * (x - 32f) + (y - 22f) * (y - 22f)) / 40f;
            Color near = new(1f, 0.5f, 0.1f), mid = new(0.55f, 0.07f, 0.03f), far = new(0.1f, 0.02f, 0.02f);
            Color sky = r < 0.45f ? Color.Lerp(near, mid, r / 0.45f) : Color.Lerp(mid, far, Mathf.Clamp01((r - 0.45f) / 0.55f));
            sky.a = 1f;
            return sky;
        });
        // The eclipse: a black disc, a burning ring around it.
        canvas.Paint((x, y) =>
        {
            float d = Circle(x, y, 32f, 21f, 10f);
            float corona = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Max(d, 0f) / 9f), 2f);
            Color ring = Color.Lerp(new Color(1f, 0.55f, 0.1f), new Color(1f, 0.95f, 0.7f), corona);
            ring.a = corona;
            return Canvas.Over(ring, new Color(0.04f, 0.01f, 0.01f, Mathf.Clamp01(0.5f - d)));
        });
        Flame(canvas, 26.5f, 55f, 9f, 28f, -3f);
        Flame(canvas, 37.5f, 54f, 8f, 26f, 3f);
        Flame(canvas, 32f, 52f, 7f, 34f);
        if (GameArt.Get("SwordIron") is { } sword)
        {
            canvas.Draw(sword, 5f, 5f, 54f, 54f, new Color(1f, 0.5f, 0.1f, 0.6f), 135f);
            canvas.Draw(sword, 7f, 7f, 50f, 50f, new Color(1f, 0.85f, 0.65f), 135f);
        }
        // The burning ground, lava in its cracks.
        canvas.Paint((x, y) =>
        {
            float ground = Polygon(x, y, RagnarokGround);
            float cracks = Mathf.Min(Polyline(x, y, [new(4f, 64f), new(9f, 58f), new(15f, 59f), new(22f, 55f)], 0.6f),
                Polyline(x, y, [new(62f, 63f), new(52f, 58f), new(47f, 59f), new(41f, 55f)], 0.6f));
            Color earth = new(0.09f, 0.03f, 0.02f, Mathf.Clamp01(0.5f - ground));
            float lava = Mathf.Clamp01(0.5f - cracks) * Mathf.Clamp01(-ground);
            return Canvas.Over(earth, new Color(1f, 0.55f, 0.12f, lava));
        });
        Embers(canvas);
        canvas.Fade(12f);
    }

    private static readonly Vector2[] RagnarokGround =
    [
        new(-1f, 65f), new(-1f, 53f), new(7f, 50f), new(13f, 53f), new(21f, 49.5f), new(28f, 52.5f), new(36f, 51f), new(44f, 53.5f),
        new(52f, 48.5f), new(58f, 52f), new(65f, 50f), new(65f, 65f)
    ];

    // Crimson: a blood moon behind dark clouds over a horde of horned raiders, their eyes glowing red.
    private static void CrimsonPicture(Canvas canvas)
    {
        canvas.Paint((x, y) =>
        {
            float t = Mathf.Clamp01(y / 60f);
            return new Color(Mathf.Lerp(0.1f, 0.5f, t), Mathf.Lerp(0.01f, 0.04f, t), Mathf.Lerp(0.02f, 0.04f, t), 1f);
        });
        canvas.Paint((x, y) =>
        {
            float d = Circle(x, y, 32f, 25f, 16f);
            float glow = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Max(d, 0f) / 14f), 2f) * 0.75f;
            float shade = Mathf.Clamp01((x - 18f + y - 10f) / 50f);
            float maria = Mathf.Max(Mathf.Clamp01(0.5f - Circle(x, y, 26f, 21f, 5f) / 2f), Mathf.Clamp01(0.5f - Circle(x, y, 38f, 31f, 4f) / 2f));
            maria = Mathf.Max(maria, Mathf.Clamp01(0.5f - Circle(x, y, 37f, 17f, 2.6f) / 1.5f));
            float k = (1f - 0.35f * shade) * (1f - 0.3f * maria);
            Color moon = new(0.9f * k, 0.12f * k, 0.08f * k, Mathf.Clamp01(0.5f - d / 1.2f));
            return Canvas.Over(new Color(1f, 0.12f, 0.08f, glow), moon);
        });
        canvas.Paint((x, y) =>
        {
            float alpha = 0f;
            foreach ((float x0, float x1, float cy, float r) in new[] { (0f, 38f, 28f, 2.2f), (26f, 64f, 35f, 1.8f), (38f, 62f, 16f, 1.2f) })
            {
                float t = Mathf.Clamp01((x - x0) / (x1 - x0));
                float thick = r * Mathf.Sin(t * Mathf.PI) + Mathf.Sin(x * 0.9f + cy) * 0.35f;
                float wave = cy + Mathf.Sin(x * 0.25f + cy) * 0.8f;
                alpha = Mathf.Max(alpha, Mathf.Clamp01((thick - Mathf.Abs(y - wave)) / 1.2f + 0.5f));
            }
            return new Color(0.07f, 0.01f, 0.02f, alpha * 0.9f);
        });
        // The horde: horned heads along the bottom, spears raised between them.
        Shape(canvas, HordeShape, (x, y, depth) => new Color(0.05f, 0.01f, 0.01f, 1f), 0f);
        foreach (float hx in HordeHeads)
        {
            float hy = HordeHeadY(hx);
            canvas.Paint((x, y) =>
            {
                float d = Mathf.Min(Circle(x, y, hx - 1.6f, hy + 0.5f, 0.75f), Circle(x, y, hx + 1.6f, hy + 0.5f, 0.75f));
                float glow = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Max(d, 0f) / 2.5f), 2f) * 0.8f;
                return Canvas.Over(new Color(1f, 0.1f, 0.05f, glow), new Color(1f, 0.45f, 0.3f, Mathf.Clamp01(0.5f - d)));
            });
        }
        canvas.Fade(12f);
    }

    private static readonly float[] HordeHeads = [7f, 20f, 33f, 46f, 58f];

    private static float HordeHeadY(float x) => 52f + Mathf.Sin(x * 0.7f) * 1.5f;

    private static float HordeShape(float x, float y)
    {
        float d = Box(x, y, -2f, 57f, 66f, 66f);
        foreach (float hx in HordeHeads)
        {
            float hy = HordeHeadY(hx);
            float head = Mathf.Min(Circle(x, y, hx, hy, 4.2f), Box(x, y, hx - 4.6f, hy, hx + 4.6f, 66f));
            float horns = Mathf.Min(Polyline(x, y, [new(hx - 3.5f, hy - 1.5f), new(hx - 6.5f, hy - 4.5f), new(hx - 6.5f, hy - 8f)], 0.9f),
                Polyline(x, y, [new(hx + 3.5f, hy - 1.5f), new(hx + 6.5f, hy - 4.5f), new(hx + 6.5f, hy - 8f)], 0.9f));
            d = Mathf.Min(d, Mathf.Min(head, horns));
        }
        foreach (float sx in new[] { 13.5f, 27f, 39.5f, 52f })
        {
            float tip = 38f + Mathf.Sin(sx) * 2f;
            float shaft = Capsule(x, y, sx, tip + 3f, sx, 60f, 0.6f);
            float head = Polygon(x, y, [new(sx, tip - 3f), new(sx + 1.6f, tip + 1f), new(sx, tip + 3f), new(sx - 1.6f, tip + 1f)]);
            d = Mathf.Min(d, Mathf.Min(shaft, head));
        }
        return d;
    }

    // Grave-Bind: a tombstone in chains, a rune of protection glowing on it, a beam of light rising behind it.
    private static void GraveBindPicture(Canvas canvas)
    {
        Beam(canvas);
        Glow(canvas, 32f, 36f, 22f, new Color(0.5f, 0.9f, 1f), 0.35f);
        canvas.Paint((x, y) =>
        {
            float ex = (x - 32f) / 22f, ey = (y - 61f) / 5f;
            return new Color(0.22f, 0.17f, 0.12f, Mathf.Clamp01((1f - Mathf.Sqrt(ex * ex + ey * ey)) * 5f));
        });
        Shape(canvas, (x, y) => Mathf.Min(Box(x, y, 21f, 30f, 43f, 60f, 1f), Circle(x, y, 32f, 30f, 11f)), (x, y, depth) =>
        {
            float light = Mathf.Lerp(1.05f, 0.7f, Mathf.Clamp01((y - 19f) / 41f)) * (0.75f + 0.25f * Mathf.Clamp01(depth / 2f));
            return new Color(0.56f * light, 0.58f * light, 0.62f * light, 1f);
        });
        if (GameArt.Get("chain") is { } chain)
        {
            Color iron = new(0.75f, 0.72f, 0.7f);
            canvas.Draw(chain, 4f, 18f, 56f, 56f, iron, 60f);
            canvas.Draw(chain, 4f, 18f, 56f, 56f, iron, -60f);
        }
        GlowLine(canvas, (x, y) => Strokes(x, y, 1.1f, new Vector4(32f, 26f, 32f, 42f), new Vector4(32f, 32f, 26.5f, 25f),
            new Vector4(32f, 32f, 37.5f, 25f)), new Color(0.4f, 0.85f, 1f), new Color(0.85f, 1f, 1f), 4f, 0.8f);
    }

    // Fortune's Boon: the rune Fehu (wealth) glowing gold over a heap of treasure.
    private static void FortunesBoonPicture(Canvas canvas)
    {
        Glow(canvas, 32f, 26f, 30f, new Color(1f, 0.75f, 0.2f), 0.55f);
        GlowLine(canvas, (x, y) => Strokes(x, y, 2f, new Vector4(25f, 6f, 25f, 40f), new Vector4(25f, 17f, 38f, 7f),
            new Vector4(25f, 27f, 40f, 16f)), new Color(1f, 0.6f, 0.1f), new Color(1f, 0.88f, 0.4f), 5f, 0.85f);
        if (GameArt.Get("treasure_pile") is { } pile) canvas.Draw(pile, 4f, 14f, 56f, 56f);
        else if (GameArt.Get("coins") is { } coins) canvas.Draw(coins, 14f, 34f, 36f, 30f);
        Sparkle(canvas, 50f, 26f, 4.5f, new Color(1f, 0.85f, 0.3f));
        Sparkle(canvas, 11f, 22f, 3.5f, new Color(1f, 0.85f, 0.3f));
        Sparkle(canvas, 46f, 8f, 3f, new Color(1f, 0.85f, 0.3f));
    }

    // Luminary: a lantern shining, rays of warm light all around it.
    private static void LuminaryPicture(Canvas canvas)
    {
        canvas.Paint((x, y) =>
        {
            float dx = x - 32f, dy = y - 33f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float rays = Mathf.Pow(Mathf.Abs(Mathf.Cos(Mathf.Atan2(dy, dx) * 6f)), 6f);
            return new Color(1f, 0.85f, 0.5f, rays * Mathf.Clamp01(1f - r / 32f) * Mathf.Clamp01(r / 10f) * 0.6f);
        });
        Glow(canvas, 32f, 33f, 30f, new Color(1f, 0.75f, 0.35f), 0.8f);
        Glow(canvas, 32f, 33f, 13f, new Color(1f, 0.95f, 0.7f), 0.6f);
        if (GameArt.Get("lantern") is { } lantern) canvas.Draw(lantern, 9f, 1f, 46f, 60f);
        else Glow(canvas, 32f, 33f, 8f, new Color(1f, 1f, 0.85f), 1f);
    }

    // Skål: two tankards clinking, mead splashing up between them, golden sparks.
    private static void SkalPicture(Canvas canvas)
    {
        Glow(canvas, 32f, 32f, 30f, new Color(1f, 0.68f, 0.2f), 0.6f);
        if (GameArt.Get("tankard") is { } tankard)
        {
            canvas.Draw(tankard, 37f, 20f, -36f, 36f, null, 30f);
            canvas.Draw(tankard, 27f, 20f, 36f, 36f, null, -30f);
        }
        foreach ((float x, float y, float r, bool foam) in new[]
                 {
                     (32f, 25f, 2.4f, true), (28f, 20.5f, 1.6f, true), (36.5f, 20f, 1.8f, true), (31.5f, 16f, 1.3f, false),
                     (24.5f, 23f, 1.1f, false), (40f, 23f, 1.2f, false), (34.5f, 13f, 1f, true)
                 })
        {
            Color color = foam ? new Color(1f, 0.96f, 0.82f) : new Color(1f, 0.7f, 0.2f);
            canvas.Paint((px, py) => new Color(color.r, color.g, color.b, Mathf.Clamp01(0.5f - Circle(px, py, x, y, r))));
        }
        foreach ((float x, float y, float size) in new[] { (10f, 12f, 4f), (55f, 10f, 3.5f), (52f, 50f, 3f), (11f, 50f, 3f) })
            Sparkle(canvas, x, y, size, new Color(1f, 0.7f, 0.25f));
    }

    // Sky-Call: lightning from a dark cloud striking a runestone, the rune Jera (the turning year: renewal) lit on it.
    private static void SkyCallPicture(Canvas canvas)
    {
        Glow(canvas, 32f, 42f, 26f, new Color(0.45f, 0.75f, 1f), 0.55f);
        Shape(canvas, (x, y) => Polygon(x, y, SkyStone) - 1f, (x, y, depth) =>
        {
            float light = Mathf.Lerp(1.1f, 0.65f, Mathf.Clamp01((y - 36f) / 26f)) * (0.75f + 0.25f * Mathf.Clamp01(depth / 2f));
            return new Color(0.5f * light, 0.53f * light, 0.58f * light, 1f);
        });
        GlowLine(canvas, (x, y) => Strokes(x, y, 1.1f, new Vector4(31f, 44f, 27.5f, 48.5f), new Vector4(27.5f, 48.5f, 31f, 53f),
            new Vector4(33f, 48f, 36.5f, 52.5f), new Vector4(36.5f, 52.5f, 33f, 57f)), new Color(0.35f, 0.75f, 1f), new Color(0.85f, 0.97f, 1f), 4f, 0.85f);
        Cloud(canvas, 32f, 12f, new Color(0.38f, 0.42f, 0.52f));
        if (GameArt.Get("Lightning") is { } bolt) canvas.Draw(bolt, 21f, 14f, 24f, 28f);
        else Bolt(canvas);
        Sparkle(canvas, 22f, 38f, 3f, new Color(0.45f, 0.8f, 1f));
        Sparkle(canvas, 43f, 40f, 3.5f, new Color(0.45f, 0.8f, 1f));
    }

    // Anvil-Spark: a hammer striking an anvil, a fan of sparks bursting up from where it hits.
    private static void AnvilSparkPicture(Canvas canvas)
    {
        Color hot = new(1f, 0.6f, 0.15f);
        Color white = new(1f, 0.93f, 0.7f);
        const float hitX = 28f, hitY = 34f;
        Glow(canvas, hitX, hitY, 30f, new Color(1f, 0.62f, 0.2f), 0.55f);
        Shape(canvas, (x, y) => Polygon(x, y, Anvil) - 0.5f, (x, y, depth) =>
        {
            float top = Mathf.Clamp01(1f - (y - 34.5f) / 2.5f);
            float light = Mathf.Lerp(0.55f, 0.32f, Mathf.Clamp01((y - 35f) / 24f)) * (0.8f + 0.2f * Mathf.Clamp01(depth / 2f)) + top * 0.35f;
            // The face glows where the hammer hits.
            float heat = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(x, y), new Vector2(hitX, hitY)) / 9f);
            return Color.Lerp(new Color(light * 0.95f, light, light * 1.08f, 1f), new Color(1f, 0.6f, 0.2f, 1f), heat * 0.8f);
        });
        // Sparks flying up and out in streaks, each hotter at its head.
        foreach ((float angle, float from, float to) in new[]
                 {
                     (-168f, 4f, 15f), (-150f, 3f, 21f), (-128f, 4f, 17f), (-108f, 3f, 24f), (-88f, 5f, 19f), (-68f, 3f, 25f),
                     (-48f, 4f, 20f), (-30f, 3f, 23f), (-12f, 5f, 15f)
                 })
        {
            float ax = Mathf.Cos(angle * Mathf.Deg2Rad), ay = Mathf.Sin(angle * Mathf.Deg2Rad);
            float sx = hitX + ax * from, sy = hitY + ay * from, ex = hitX + ax * to, ey = hitY + ay * to;
            canvas.Paint((x, y) =>
            {
                float d = Capsule(x, y, sx, sy, ex, ey, 0.55f);
                float along = Mathf.Clamp01(((x - sx) * ax + (y - sy) * ay) / (to - from));
                float glow = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Max(d, 0f) / 2.2f), 2f) * 0.6f * along;
                return Canvas.Over(new Color(hot.r, hot.g, hot.b, glow), new Color(white.r, white.g, white.b, Mathf.Clamp01(0.5f - d) * (0.35f + 0.65f * along)));
            });
        }
        Glow(canvas, hitX, hitY, 7f, white, 0.9f);
        // The hammer raised over it, about to strike again.
        if (GameArt.Get("hammer") is { } hammer) canvas.Draw(hammer, 24f, 10f, 34f, 34f);
        Sparkle(canvas, hitX, hitY - 1f, 5f, hot);
        Sparkle(canvas, 9f, 14f, 3f, hot);
        Sparkle(canvas, 52f, 12f, 3.5f, hot);
    }

    // Gale-Breath: three icy gusts sweeping across, each curling over at its head, frost glinting in them.
    private static void GaleBreathPicture(Canvas canvas)
    {
        Color frost = new(0.5f, 0.82f, 1f);
        Color white = new(0.93f, 0.98f, 1f);
        Glow(canvas, 32f, 32f, 34f, new Color(0.35f, 0.6f, 0.95f), 0.45f);
        foreach ((float y, float x0, float x1, float r, float width) in new[]
                 { (21f, 5f, 38f, 7f, 1.6f), (38f, 3f, 46f, 8.5f, 2f), (54f, 10f, 34f, 5.5f, 1.4f) })
            GlowLine(canvas, (px, py) => Gust(px, py, y, x0, x1, r, width), frost, white, 4f, 0.7f);
        Sparkle(canvas, 52f, 12f, 3f, frost);
        Sparkle(canvas, 22f, 29f, 2.5f, frost);
        Sparkle(canvas, 50f, 52f, 3f, frost);
    }

    // Tailwind: a ship's striped sail filled by the wind, gusts like the Gale-Breath's blowing into it from behind.
    private static void TailwindPicture(Canvas canvas)
    {
        Color air = new(0.6f, 0.85f, 1f);
        Color white = new(0.95f, 0.98f, 1f);
        Glow(canvas, 38f, 30f, 30f, new Color(0.5f, 0.75f, 1f), 0.4f);
        // The hull, its prow raised at the front (right), lit along the gunwale.
        Shape(canvas, (x, y) => Polygon(x, y, TailwindHull), (x, y, depth) =>
        {
            float light = Mathf.Lerp(1f, 0.6f, Mathf.Clamp01((y - 49f) / 9f)) * (0.8f + 0.2f * Mathf.Clamp01(depth / 1.5f));
            return new Color(0.5f * light, 0.32f * light, 0.18f * light, 1f);
        });
        // The mast and the yard the sail hangs from.
        Shape(canvas, (x, y) => Mathf.Min(Capsule(x, y, 40f, 5f, 40f, 51f, 1.4f), Capsule(x, y, 23f, 10f, 57f, 10f, 1.3f)), (x, y, depth) =>
        {
            float light = 0.75f + 0.25f * Mathf.Clamp01(depth / 1f);
            return new Color(0.45f * light, 0.3f * light, 0.17f * light, 1f);
        });
        // The sail, bellied out: lighter in the middle, red and cream stripes bending with it.
        Shape(canvas, TailwindSail, (x, y, depth) =>
        {
            float u = Mathf.Clamp((x - 40f) / TailwindSailHalf(y), -1f, 1f);
            float t = (u + 1f) * 2.5f;
            float stripe = Mathf.Repeat(t, 2f);
            float red = Mathf.Clamp01(Mathf.Min(stripe - 1f, 2f - stripe) * 6f + 0.5f);
            Color cloth = Color.Lerp(new Color(0.93f, 0.87f, 0.74f), new Color(0.74f, 0.2f, 0.13f), red);
            float light = (0.7f + 0.35f * (1f - u * u)) * Mathf.Lerp(1.05f, 0.85f, Mathf.Clamp01((y - 12f) / 36f));
            light *= 0.85f + 0.15f * Mathf.Clamp01(depth / 1.5f);
            return new Color(cloth.r * light, cloth.g * light, cloth.b * light, 1f);
        }, 0.5f);
        foreach ((float y, float x0, float x1, float r, float width) in new[] { (24f, 4f, 16f, 3.5f, 1.2f), (37f, 3f, 19f, 4.5f, 1.5f) })
            GlowLine(canvas, (px, py) => Gust(px, py, y, x0, x1, r, width), air, white, 3.5f, 0.7f);
        Sparkle(canvas, 9f, 49f, 2.5f, air);
        Sparkle(canvas, 58f, 21f, 3f, air);
    }

    private static readonly Vector2[] TailwindHull =
        [new(9f, 46f), new(14f, 50f), new(54f, 50f), new(59f, 43f), new(61f, 44f), new(57f, 56f), new(50f, 59f), new(20f, 59f), new(12f, 54f)];

    // Half the sail's width at y: it bellies out most halfway down.
    private static float TailwindSailHalf(float y) => 14f + 3f * Mathf.Sin(Mathf.PI * Mathf.Clamp01((y - 11f) / 34f));

    // The sail hanging from the yard, its foot sagging in the middle.
    private static float TailwindSail(float x, float y)
    {
        float u = (x - 40f) / 17f;
        float bottom = 44f + 3f * (1f - u * u);
        return Mathf.Max(Mathf.Max(11f - y, y - bottom), Mathf.Abs(x - 40f) - TailwindSailHalf(y));
    }

    // How far (x, y) is from a gust: a stroke from x0 to x1 along y, thin at its tail and full width at its head, which
    // curls up and back over itself in a loop of radius r.
    private static float Gust(float x, float y, float baseY, float x0, float x1, float r, float width)
    {
        float t = Mathf.Clamp01((x - x0) / (x1 - x0));
        float stroke = Mathf.Max(Mathf.Abs(y - baseY), Mathf.Max(x0 - x, x - x1)) - width * Mathf.Lerp(0.3f, 1f, t);
        // The curl: around a circle above the head, from its bottom over the right and top to its left, thinning out.
        float cx = x1, cy = baseY - r;
        float angle = Mathf.Atan2(y - cy, x - cx) * Mathf.Rad2Deg;
        float curl = float.MaxValue;
        if (angle <= 90f && angle >= -180f)
        {
            float along = (90f - angle) / 270f;
            curl = Mathf.Abs(Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r) - width * Mathf.Lerp(1f, 0.35f, along);
        }
        return Mathf.Min(stroke, curl);
    }

    private static readonly Vector2[] SkyStone = [new(24f, 63f), new(23.5f, 46f), new(26.5f, 38.5f), new(32f, 36f), new(38f, 38.5f), new(41f, 46f), new(40f, 63f)];

    // Grounding: an iron stake driven into the earth, the green rings of the Domain rites draining down it into the ground.
    private static void GroundingPicture(Canvas canvas)
    {
        Color green = new(0.5f, 0.95f, 0.35f);
        Glow(canvas, 32f, 48f, 26f, green, 0.4f);
        // The earth, cracks glowing green where the rites run into it.
        Shape(canvas, (x, y) => Polygon(x, y, GroundingEarth), (x, y, depth) =>
        {
            float light = Mathf.Lerp(1f, 0.6f, Mathf.Clamp01((y - 44f) / 20f));
            return new Color(0.36f * light, 0.28f * light, 0.2f * light, 1f);
        });
        GlowLine(canvas, (x, y) => Mathf.Max(Mathf.Min(Mathf.Min(Polyline(x, y, [new(32f, 47f), new(26f, 52f), new(22f, 51f), new(15f, 57f)], 0.6f),
                Polyline(x, y, [new(32f, 47f), new(38f, 53f), new(44f, 52f), new(50f, 58f)], 0.6f)), Polyline(x, y, [new(32f, 47f), new(31f, 55f), new(34f, 62f)], 0.6f)),
            Polygon(x, y, GroundingEarth) + 0.5f), green, new Color(0.85f, 1f, 0.7f), 3.5f, 0.7f);
        Ellipses(canvas, 32f, 46f, 22f, 5.5f, green);
        // Motes falling into the earth, streaking behind them.
        foreach ((float mx, float my) in new[] { (24f, 18f), (40f, 24f), (22f, 32f), (42f, 36f), (27f, 41f), (37f, 14f) })
            canvas.Paint((x, y) =>
            {
                float streak = Capsule(x, y, mx, my - 7f, mx, my, 0.9f);
                float fade = Mathf.Clamp01((y - (my - 7f)) / 7f);
                float glow = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Max(Circle(x, y, mx, my, 1.2f), 0f) / 3f), 2f) * 0.7f;
                return Canvas.Over(new Color(green.r, green.g, green.b, Mathf.Max(Mathf.Clamp01(0.5f - streak) * fade * 0.8f, glow)),
                    new Color(0.85f, 1f, 0.7f, Mathf.Clamp01(0.5f - Circle(x, y, mx, my, 1.1f))));
            });
        Shape(canvas, (x, y) => Mathf.Min(Polygon(x, y, GroundingStake), Box(x, y, 26.5f, 6f, 37.5f, 11.5f, 1.5f)), (x, y, depth) =>
        {
            float sheen = Mathf.Clamp01(1f - Mathf.Abs(x - 30.5f) / 2.5f) * 0.35f;
            float light = 0.6f + 0.25f * Mathf.Clamp01(depth / 1.5f) + sheen;
            return new Color(0.5f * light, 0.52f * light, 0.56f * light, 1f);
        });
    }

    private static readonly Vector2[] GroundingEarth =
        [new(2f, 64f), new(4f, 52f), new(12f, 47f), new(22f, 45.5f), new(32f, 46f), new(43f, 45f), new(53f, 47.5f), new(60f, 52f), new(62f, 64f)];
    private static readonly Vector2[] GroundingStake = [new(28.5f, 11f), new(35.5f, 11f), new(34.3f, 44f), new(32f, 52f), new(29.7f, 44f)];

    // A glowing ring lying on the ground, seen from the side.
    private static void Ellipses(Canvas canvas, float cx, float cy, float rx, float ry, Color color) =>
        canvas.Paint((x, y) =>
        {
            float d = Mathf.Abs(Ellipse(x, y, cx, cy, rx, ry)) - 0.9f;
            float glow = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Max(d, 0f) / 3f), 2f) * 0.6f;
            return Canvas.Over(new Color(color.r, color.g, color.b, glow), new Color(0.85f, 1f, 0.7f, Mathf.Clamp01(0.5f - d)));
        });

    // Sanctuary: a shield before the red bubble of a raid, shattering it.
    private static void SanctuaryPicture(Canvas canvas)
    {
        Glow(canvas, 32f, 32f, 30f, new Color(1f, 0.92f, 0.7f), 0.55f);
        canvas.Paint((x, y) =>
        {
            float dx = x - 32f, dy = y - 32f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float angle = Mathf.Atan2(dy, dx);
            // Pieces of the ring, each pushed a little outward.
            float piece = Mathf.Repeat(angle / (Mathf.PI * 2f) * 7f, 1f);
            float push = Mathf.Abs(Mathf.Sin(Mathf.Floor(angle / (Mathf.PI * 2f) * 7f) * 2.3f)) * 3f;
            float d = Mathf.Abs(r - 26f - push) - 1.1f;
            float gap = Mathf.Clamp01(Mathf.Min(piece, 1f - piece) * 30f - 1.5f);
            float glow = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Max(d, 0f) / 4f), 2f) * 0.6f;
            return new Color(1f, 0.2f, 0.15f, Mathf.Max(Mathf.Clamp01(0.5f - d), glow) * gap);
        });
        if (GameArt.Get("shield_banded0") is { } shield) canvas.Draw(shield, 10f, 10f, 44f, 44f);
        else if (GameArt.Get("shield_wood0") is { } wood) canvas.Draw(wood, 10f, 10f, 44f, 44f);
        Sparkle(canvas, 52f, 12f, 3.5f, new Color(1f, 0.9f, 0.6f));
        Sparkle(canvas, 12f, 52f, 3f, new Color(1f, 0.9f, 0.6f));
    }

    // Equilibrium: a balance scale, level, between a cool and a warm glow.
    private static void EquilibriumPicture(Canvas canvas)
    {
        Glow(canvas, 14f, 36f, 22f, new Color(0.45f, 0.7f, 1f), 0.6f);
        Glow(canvas, 50f, 36f, 22f, new Color(1f, 0.7f, 0.3f), 0.6f);
        Shape(canvas, ScaleShape, (x, y, depth) =>
        {
            float light = Mathf.Lerp(1.05f, 0.75f, Mathf.Clamp01((y - 8f) / 52f)) * (0.8f + 0.2f * Mathf.Clamp01(depth / 1.2f));
            return new Color(0.86f * light, 0.87f * light, 0.92f * light, 1f);
        });
    }

    private static float ScaleShape(float x, float y)
    {
        float frame = Mathf.Min(Mathf.Min(Capsule(x, y, 32f, 12f, 32f, 54f, 1.7f), Circle(x, y, 32f, 9.5f, 2.6f)),
            Mathf.Min(Capsule(x, y, 9f, 19f, 55f, 19f, 1.4f), Mathf.Min(Box(x, y, 21f, 55f, 43f, 59f, 1.5f), Box(x, y, 26f, 51f, 38f, 56f, 1f))));
        float strings = Mathf.Min(Mathf.Min(Capsule(x, y, 10f, 19f, 4f, 37f, 0.5f), Capsule(x, y, 10f, 19f, 16f, 37f, 0.5f)),
            Mathf.Min(Capsule(x, y, 54f, 19f, 48f, 37f, 0.5f), Capsule(x, y, 54f, 19f, 60f, 37f, 0.5f)));
        float pans = Mathf.Min(Mathf.Max(Circle(x, y, 10f, 31f, 8f), 37f - y), Mathf.Max(Circle(x, y, 54f, 31f, 8f), 37f - y));
        return Mathf.Min(Mathf.Min(frame, strings), pans);
    }

    // Unmaking: a sword breaking apart over the fire, an iron bar glowing below what it is turned back into.
    private static void UnmakingPicture(Canvas canvas)
    {
        Glow(canvas, 40f, 48f, 28f, new Color(1f, 0.45f, 0.1f), 0.65f);
        if (GameArt.Get("iron") is { } bar) canvas.Draw(bar, 28f, 32f, 34f, 34f, new Color(1f, 0.82f, 0.65f));
        if (GameArt.Get("SwordIron") is { } sword)
        {
            // Along the blade, from the pommel (0) to the tip (1).
            static float Along(float u, float v) => (u + 1f - v) / 2f;
            canvas.Draw(sword, 0f, 6f, 50f, 50f, null, 0f, (u, v) => Mathf.Clamp01((0.52f - Along(u, v)) * 60f));
            canvas.Draw(sword, 7f, -3f, 50f, 50f, null, 16f, (u, v) => Mathf.Clamp01((Along(u, v) - 0.57f) * 60f));
        }
        foreach (Vector2[] shard in UnmakingShards)
            canvas.Paint((x, y) =>
            {
                float d = Polygon(x, y, shard);
                float glow = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Max(d, 0f) / 2.5f), 2f) * 0.6f;
                return Canvas.Over(new Color(1f, 0.55f, 0.15f, glow), new Color(0.85f, 0.88f, 0.92f, Mathf.Clamp01(0.5f - d)));
            });
        Embers(canvas);
    }

    private static readonly Vector2[][] UnmakingShards =
    [
        [new(29f, 27f), new(32f, 25f), new(31f, 29f)],
        [new(33f, 33f), new(36f, 32f), new(34f, 35.5f)],
        [new(26f, 33f), new(28f, 31.5f), new(28.5f, 34.5f)]
    ];

    // ---- Pictures ----

    // A shape filled by fill(x, y, depth), depth being how far inside it the pixel is, over a soft shadow a pixel down.
    private static void Shape(Canvas canvas, Func<float, float, float> shape, Func<float, float, float, Color> fill, float shadow = 0.6f)
    {
        float[] alpha = new float[Size * Size];
        float[] depth = new float[Size * Size];
        for (int py = 0; py < Size; py++)
            for (int px = 0; px < Size; px++)
            {
                int i = py * Size + px;
                float d = shape(px + 0.5f, py + 0.5f);
                depth[i] = -d;
                alpha[i] = Mathf.Clamp01(0.5f - d);
            }
        if (shadow > 0f)
        {
            float[] blurred = Canvas.Blur(alpha, 1);
            for (int i = Size; i < blurred.Length; i++) canvas.Blend(i, new Color(0f, 0f, 0f, blurred[i - Size] * shadow));
        }
        for (int i = 0; i < alpha.Length; i++)
        {
            if (alpha[i] <= 0f) continue;
            Color c = fill(i % Size + 0.5f, i / Size + 0.5f, depth[i]);
            canvas.Blend(i, new Color(c.r, c.g, c.b, c.a * alpha[i]));
        }
    }

    // A line (distance function) in core, with a soft glow out to width pixels around it.
    private static void GlowLine(Canvas canvas, Func<float, float, float> line, Color glow, Color core, float width, float strength) =>
        canvas.Paint((x, y) =>
        {
            float d = line(x, y);
            float halo = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Max(d, 0f) / width), 2f) * strength;
            return Canvas.Over(new Color(glow.r, glow.g, glow.b, halo), new Color(core.r, core.g, core.b, Mathf.Clamp01(0.5f - d)));
        });

    // A flame standing on (cx, by): orange to red at its edge, a pale heart. lean tips its point sideways.
    private static void Flame(Canvas canvas, float cx, float by, float width, float height, float lean = 0f)
    {
        float half = width / 2f;
        Vector2[] outer =
        [
            new(cx - half, by - half), new(cx + lean * 0.5f - width * 0.28f, by - height * 0.6f), new(cx + lean, by - height),
            new(cx + lean * 0.5f + width * 0.28f, by - height * 0.6f), new(cx + half, by - half)
        ];
        canvas.Paint((x, y) =>
        {
            float shape = Mathf.Min(Polygon(x, y, outer), Circle(x, y, cx, by - half, half));
            if (shape > 1f) return Color.clear;
            float heart = Mathf.Min(Circle(x, y, cx, by - width * 0.4f, width * 0.25f),
                Capsule(x, y, cx, by - width * 0.4f, cx + lean * 0.4f, by - height * 0.5f, width * 0.12f));
            float edge = Mathf.Clamp01(-shape / (width * 0.35f));
            Color flame = new(1f, Mathf.Lerp(0.3f, 0.62f, edge), Mathf.Lerp(0.06f, 0.14f, edge), Mathf.Clamp01(0.5f - shape));
            return Canvas.Over(flame, new Color(1f, 0.93f, 0.62f, Mathf.Clamp01(0.5f - heart)));
        });
    }

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

    // Shrouding: spruces fading into the fog, the far ones pale, fog drifting between them.
    private static void MistPicture(Canvas canvas)
    {
        Color far = new(0.58f, 0.63f, 0.71f), near = new(0.2f, 0.27f, 0.3f);
        Glow(canvas, 32f, 36f, 32f, new Color(0.7f, 0.78f, 0.9f), 0.35f);
        canvas.Paint((x, y) => new Color(far.r, far.g, far.b,
            Mathf.Clamp01(0.5f - Mathf.Min(Spruce(x, y, 44f, 12f, 28f, 18f), Spruce(x, y, 56f, 19f, 22f, 14f))) * 0.8f));
        FogLayer(canvas, 37f, 6f, new Color(0.85f, 0.88f, 0.93f), 0.85f, 0f);
        Shape(canvas, (x, y) => Spruce(x, y, 21f, 8f, 44f, 28f), (x, y, depth) =>
        {
            float light = Mathf.Lerp(1.25f, 0.85f, Mathf.Clamp01((x - 9f) / 24f)) * (0.85f + 0.15f * Mathf.Clamp01(depth / 1.5f));
            return new Color(near.r * light, near.g * light, near.b * light, 1f);
        }, 0.3f);
        FogLayer(canvas, 54f, 7f, new Color(0.9f, 0.92f, 0.96f), 0.95f, 2f);
        FogLayer(canvas, 22f, 4f, new Color(0.88f, 0.9f, 0.95f), 0.4f, 4f);
    }

    // A spruce: three tiers of branches on a short trunk, top at (cx, top), h tall and w wide at its foot.
    private static float Spruce(float x, float y, float cx, float top, float h, float w)
    {
        float d = Box(x, y, cx - w * 0.06f, top + h * 0.9f, cx + w * 0.06f, top + h + 3f);
        for (int k = 0; k < 3; k++)
        {
            float apex = top + k * 0.24f * h, bottom = top + (0.4f + k * 0.3f) * h, half = w * (0.26f + 0.12f * k);
            d = Mathf.Min(d, Polygon(x, y, [new(cx, apex), new(cx + half, bottom), new(cx - half, bottom)]));
        }
        return d;
    }

    // A soft layer of fog along cy, about thick pixels each way, fading out toward the sides.
    private static void FogLayer(Canvas canvas, float cy, float thick, Color tint, float strength, float phase)
    {
        Pixels? smoke = GameArt.Get("Smoked");
        canvas.Paint((x, y) =>
        {
            float v = (y - (cy + Mathf.Sin(x / 7f + phase) * 1.5f)) / thick;
            float alpha = Mathf.Exp(-v * v * 2f) * strength * Mathf.SmoothStep(0f, 1f, Mathf.Min(x, 64f - x) / 10f);
            float k = SmokeShade(smoke, (x + phase * 10f) / 64f, y / 64f);
            return new Color(Mathf.Clamp01(tint.r * k), Mathf.Clamp01(tint.g * k), Mathf.Clamp01(tint.b * k), Mathf.Clamp01(alpha));
        });
    }

    // A bank of billows (x, y, radius), rounded on top and fading out down to bottom and toward the sides, top color at
    // its crest and under at its foot, filled with the game's smoke.
    private static void FogBank(Canvas canvas, Vector3[] puffs, float bottom, Color top, Color under, float strength, float shift)
    {
        Pixels? smoke = GameArt.Get("Smoked");
        float crest = float.MaxValue;
        foreach (Vector3 p in puffs) crest = Mathf.Min(crest, p.y - p.z);
        canvas.Paint((x, y) =>
        {
            float d = float.MaxValue;
            foreach (Vector3 p in puffs) d = Mathf.Min(d, Circle(x, y, p.x, p.y, p.z));
            float alpha = Mathf.Clamp01(0.5f - d / 4f) * Mathf.Clamp01((bottom - y) / 10f) *
                          Mathf.SmoothStep(0f, 1f, Mathf.Min(x, 64f - x) / 9f) * strength;
            if (alpha <= 0f) return Color.clear;
            float k = SmokeShade(smoke, (x + shift) / 64f, y / 64f) * (1f + 0.08f * Mathf.Clamp01(1f + d / 4f));
            Color c = Color.Lerp(top, under, Mathf.Clamp01((y - crest) / (bottom - crest)));
            return new Color(Mathf.Clamp01(c.r * k), Mathf.Clamp01(c.g * k), Mathf.Clamp01(c.b * k), alpha);
        });
    }

    // How much lighter or darker the game's smoke texture makes a spot (1 without it).
    private static float SmokeShade(Pixels? smoke, float u, float v)
    {
        if (smoke == null) return 1f;
        Color texel = smoke.Sample(Mathf.Repeat(u, 1f), Mathf.Repeat(v, 1f));
        if (texel.a <= 0.05f) return 1f;
        return Mathf.Clamp(1f + 1.4f * ((texel.r + texel.g + texel.b) / 3f - smoke.MeanLuminance), 0.65f, 1.35f);
    }

    // True Sight: an eye of true sight, its iris glowing, the Mistlands mist pushed back around it.
    private static void TrueSightPicture(Canvas canvas)
    {
        Color cyan = new(0.45f, 0.85f, 1f);
        Color mist = new(0.6f, 0.55f, 0.72f);
        Pixels? smoke = GameArt.Get("Smoked");
        // The mist: a ring, wavy where it was pushed back, gone before the icon's edge.
        canvas.Paint((x, y) =>
        {
            float dx = x - 32f, dy = y - 32f;
            float wavy = Mathf.Sqrt(dx * dx + dy * dy) + Mathf.Sin(Mathf.Atan2(dy, dx) * 6f) * 1.5f;
            float alpha = Mathf.SmoothStep(0f, 1f, (wavy - 13f) / 7f) * Mathf.SmoothStep(0f, 1f, (34f - wavy) / 9f) * 0.95f;
            float k = SmokeShade(smoke, x / 64f, y / 64f);
            return new Color(Mathf.Clamp01(mist.r * k), Mathf.Clamp01(mist.g * k), Mathf.Clamp01(mist.b * k), alpha);
        });
        Glow(canvas, 32f, 32f, 22f, cyan, 0.5f);
        canvas.Paint((x, y) =>
        {
            float eye = Mathf.Max(Circle(x, y, 32f, 46f, 20f), Circle(x, y, 32f, 18f, 20f));
            if (eye > 2f) return Color.clear;
            float iris = Circle(x, y, 32f, 32f, 6.5f);
            float t = Mathf.Clamp01((iris + 6.5f) / 6.5f);
            Color white = new(0.9f, 0.92f, 0.95f, Mathf.Clamp01(0.5f - eye));
            Color ring = new(Mathf.Lerp(0.75f, 0.15f, t), Mathf.Lerp(1f, 0.6f, t), 1f, Mathf.Clamp01(0.5f - iris) * Mathf.Clamp01(0.5f - eye));
            Color pupil = new(0.04f, 0.08f, 0.16f, Mathf.Clamp01(0.5f - Circle(x, y, 32f, 32f, 2.6f)));
            Color glint = new(1f, 1f, 1f, Mathf.Clamp01(0.5f - Circle(x, y, 30f, 30f, 1.2f)));
            Color lid = new(0.2f, 0.25f, 0.38f, Mathf.Clamp01(0.5f - (Mathf.Abs(eye) - 0.8f)));
            return Canvas.Over(Canvas.Over(Canvas.Over(Canvas.Over(white, ring), pupil), glint), lid);
        });
        Sparkle(canvas, 50f, 14f, 3f, cyan);
        Sparkle(canvas, 14f, 50f, 2.5f, cyan);
    }

    // Sootfall: the Ashlands' red sun breaking through as the smoke parts below it, ash falling away.
    private static void SootfallPicture(Canvas canvas)
    {
        canvas.Paint((x, y) =>
        {
            float d = Circle(x, y, 32f, 24f, 11f);
            float glow = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Max(d, 0f) / 16f), 2f) * 0.8f;
            float t = Mathf.Clamp01((y - 13f) / 22f);
            Color sun = new(1f, Mathf.Lerp(0.75f, 0.35f, t), Mathf.Lerp(0.4f, 0.12f, t), Mathf.Clamp01(0.5f - d / 1.2f));
            return Canvas.Over(new Color(1f, 0.35f, 0.1f, glow), sun);
        });
        Color smoke = new(0.55f, 0.5f, 0.48f), lit = new(0.85f, 0.42f, 0.2f);
        FogBank(canvas, [new(6f, 40f, 8f), new(15f, 35f, 8f), new(23f, 41f, 6f)], 56f, smoke, lit, 0.95f, 0f);
        FogBank(canvas, [new(41f, 41f, 6f), new(49f, 35f, 8f), new(58f, 40f, 8f)], 56f, smoke, lit, 0.95f, 30f);
        // Flakes of grey ash.
        foreach ((float fx, float fy, float s) in new[] { (30f, 46f, 1.1f), (36f, 52f, 1f), (27f, 56f, 0.9f), (12f, 18f, 1f), (52f, 14f, 1.1f) })
            canvas.Paint((x, y) =>
            {
                float d = (Mathf.Abs(x - fx) + Mathf.Abs(y - fy) * 1.6f) / 1.4f - s;
                return new Color(0.62f, 0.58f, 0.56f, Mathf.Clamp01(0.5f - d) * 0.9f);
            });
        Embers(canvas);
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

    // A snowy hill along the bottom, under the sun that won't set, fading out toward the icon's sides.
    private static void SnowyHill(Canvas canvas)
    {
        float[] alpha = new float[Size * Size];
        float[] fade = new float[Size * Size];
        Color[] fill = new Color[Size * Size];
        for (int py = 0; py < Size; py++)
        {
            for (int px = 0; px < Size; px++)
            {
                float x = px + 0.5f, y = py + 0.5f;
                int i = py * Size + px;
                alpha[i] = Mathf.Clamp01(0.5f - Circle(x, y, 32f, 112f, 62f) / 1.2f);
                fade[i] = Mathf.SmoothStep(0f, 1f, (Mathf.Min(x, 64f - x) - 1f) / 22f);
                float shade = Mathf.Clamp(1.05f - (y - 50f) / 20f * 0.25f, 0.75f, 1.05f);
                fill[i] = new Color(Mathf.Clamp01(0.88f * shade), Mathf.Clamp01(0.93f * shade), Mathf.Clamp01(shade), alpha[i] * fade[i]);
            }
        }
        // The shadow only outside the hill, so it doesn't darken the snow where it fades.
        float[] shadow = Canvas.Blur(alpha, 2);
        for (int i = 0; i < shadow.Length; i++) canvas.Blend(i, new Color(0f, 0f, 0f, shadow[i] * 0.5f * (1f - alpha[i]) * fade[i]));
        for (int i = 0; i < fill.Length; i++) if (fill[i].a > 0f) canvas.Blend(i, fill[i]);
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

    // A crypt's doorway: a stone arch of blocks around a dark opening.
    private static void CryptArch(Canvas canvas) =>
        canvas.Paint((x, y) =>
        {
            float outer = Arch(x, y, 4f, 60f, 28f, 62f);
            float inner = Arch(x, y, 14f, 50f, 30f, 62f);
            if (outer > 1f) return Color.clear;
            if (inner < 0f)
            {
                // The opening: dark, a little lighter toward the floor.
                float depth = Mathf.Clamp01((y - 14f) / 48f);
                return new Color(0.05f + depth * 0.03f, 0.07f + depth * 0.05f, 0.06f + depth * 0.03f, 1f);
            }
            // The stone: lit from above, darker at its edges, with mortar lines between the blocks.
            float edge = Mathf.Clamp01(0.5f - outer);
            float light = Mathf.Lerp(0.55f, 0.32f, y / 64f);
            float rim = Mathf.Clamp01(1f - Mathf.Min(-outer, inner) / 2.5f) * 0.35f;
            bool mortar = Mortar(x, y);
            float shade = (light - rim) * (mortar ? 0.55f : 1f);
            return new Color(shade, shade * 0.98f, shade * 0.92f, edge);
        });

    // An arch from x0 to x1: square sides from y down, a round top above.
    private static float Arch(float x, float y, float x0, float x1, float top, float bottom)
    {
        float cx = (x0 + x1) / 2f, r = (x1 - x0) / 2f;
        return Mathf.Min(Box(x, y, x0, top, x1, bottom), Circle(x, y, cx, top, r));
    }

    // The joints between an arch's blocks: rings of blocks around its top, courses down its sides.
    private static bool Mortar(float x, float y)
    {
        const float cx = 32f, cy = 28f;
        if (y < cy)
        {
            // How far (in pixels, along the ring) from the nearest joint, one every 22.5 degrees.
            float angle = Mathf.Atan2(cy - y, x - cx) * Mathf.Rad2Deg;
            float off = Mathf.Abs(Mathf.Repeat(angle + 11.25f, 22.5f) - 11.25f) * Mathf.Deg2Rad;
            return off * Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)) < 0.6f;
        }
        float course = Mathf.Repeat(y - cy, 9f);
        return course < 0.9f;
    }

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

    // A box from (x0, y0) to (x1, y1), its corners rounded by r.
    private static float Box(float x, float y, float x0, float y0, float x1, float y1, float r = 0f)
    {
        float qx = Mathf.Abs(x - (x0 + x1) / 2f) - ((x1 - x0) / 2f - r);
        float qy = Mathf.Abs(y - (y0 + y1) / 2f) - ((y1 - y0) / 2f - r);
        float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
        return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
    }

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

    // Strokes r thick, each from (x, y) to (z, w) of a segment.
    private static float Strokes(float x, float y, float r, params Vector4[] segments)
    {
        float d = float.MaxValue;
        foreach (Vector4 s in segments) d = Mathf.Min(d, Capsule(x, y, s.x, s.y, s.z, s.w, r));
        return d;
    }

    // A line r thick through the points.
    private static float Polyline(float x, float y, Vector2[] points, float r)
    {
        float d = float.MaxValue;
        for (int i = 0; i + 1 < points.Length; i++) d = Mathf.Min(d, Capsule(x, y, points[i].x, points[i].y, points[i + 1].x, points[i + 1].y, r));
        return d;
    }

    // An ellipse: close to the distance near its outline.
    private static float Ellipse(float x, float y, float cx, float cy, float rx, float ry)
    {
        float dx = (x - cx) / rx, dy = (y - cy) / ry;
        float k = Mathf.Sqrt(dx * dx + dy * dy);
        float gx = dx / rx, gy = dy / ry;
        float gradient = Mathf.Sqrt(gx * gx + gy * gy);
        return gradient > 0f ? (k - 1f) * k / gradient : -Mathf.Min(rx, ry);
    }

    // Where (x, y) was before a turn of degrees (clockwise on screen) around (cx, cy).
    private static Vector2 Unturn(float x, float y, float cx, float cy, float degrees)
    {
        float a = -degrees * Mathf.Deg2Rad, cos = Mathf.Cos(a), sin = Mathf.Sin(a);
        float dx = x - cx, dy = y - cy;
        return new Vector2(cx + dx * cos - dy * sin, cy + dx * sin + dy * cos);
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

        // Draws a picture stretched into the box at (x, y) of size w x h, turned by angle degrees (clockwise) around
        // the box's middle. A negative width draws it mirrored. tint multiplies its colors (and alpha); mask(u, v), from 0
        // to 1 across the picture, multiplies its alpha.
        internal void Draw(Pixels source, float x, float y, float w, float h, Color? tint = null, float angle = 0f,
            Func<float, float, float>? mask = null)
        {
            Color t = tint ?? Color.white;
            float cx = x + w / 2f, cy = y + h / 2f;
            for (int py = 0; py < Size; py++)
            {
                for (int px = 0; px < Size; px++)
                {
                    Vector2 p = angle != 0f ? Unturn(px + 0.5f, py + 0.5f, cx, cy, angle) : new Vector2(px + 0.5f, py + 0.5f);
                    float u = (p.x - x) / w, v = (p.y - y) / h;
                    if (u < 0f || u > 1f || v < 0f || v > 1f) continue;
                    Color c = source.Sample(u, v);
                    float m = mask?.Invoke(u, v) ?? 1f;
                    if (m <= 0f) continue;
                    Blend(py * Size + px, new Color(c.r * t.r, c.g * t.g, c.b * t.b, c.a * t.a * m));
                }
            }
        }

        // Fades the picture out toward its edges and corners, softly over edge pixels, so it isn't a hard square.
        internal void Fade(float edge)
        {
            for (int py = 0; py < Size; py++)
                for (int px = 0; px < Size; px++)
                {
                    float nx = Mathf.Abs(px + 0.5f - Size / 2f) / (Size / 2f), ny = Mathf.Abs(py + 0.5f - Size / 2f) / (Size / 2f);
                    float s = Mathf.Pow(nx * nx * nx + ny * ny * ny, 1f / 3f);
                    int i = py * Size + px;
                    _pixels[i].a *= Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - s) * (Size / 2f) / edge));
                }
        }

        // Fades the picture out over edge pixels toward its sides and bottom (and its top, when top is set), so its
        // edges aren't a harsh line but it stays square, its faded corners rounded by corner pixels.
        internal void FadeEdges(float edge, bool top, float corner = 0f)
        {
            for (int py = 0; py < Size; py++)
                for (int px = 0; px < Size; px++)
                {
                    float x = px + 0.5f, y = py + 0.5f;
                    float d = Mathf.Min(Mathf.Min(x, Size - x), Size - y);
                    if (top) d = Mathf.Min(d, y);
                    // In a rounded corner: how far inside its arc.
                    float cx = Mathf.Clamp(x, corner, Size - corner);
                    float cy = top ? Mathf.Clamp(y, corner, Size - corner) : Mathf.Min(y, Size - corner);
                    if (corner > 0f && cx != x && cy != y)
                        d = Mathf.Min(d, corner - Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)));
                    _pixels[py * Size + px].a *= Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(d / edge));
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

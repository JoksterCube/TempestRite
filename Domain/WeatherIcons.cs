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
// - Equilibrium: a balance scale, level, between a cool and a warm glow.
// - Sun-Fall: the sun sinking at the end of its dotted golden path across the sky, the moon rising at the other end.
// - Nature's Rebound: two leafy vines chasing each other round a circle, a sprout rising in the middle.
// - True Sight: an eye glowing, the Mistlands mist pushed back around it. Sootfall: the red Ashlands sun breaking
//   through as the smoke parts, ash falling away.
// - Fair-Wind: a ship's striped sail turned on its mast, catching gusts slanting in from the side. Crypt Delver: the wooden chest in a crypt's stone doorway, lit green,
//   with gold spilling out. Verdant: a glowing seedling with sparkles.
// - Abyssal Surge: a sea serpent rearing out of the night sea over a school of fish, the sea fading out slightly at the
//   icon's sides and bottom.
// - Domain: Packward: wolf tracks leading away from a warded fire. Carrion: boar tracks leading in to rotting meat in
//   a green ring, its stench rising. Vat-Boil: the fermenter over a log fire, foam boiling
//   over. Pitch-Bellows: tar-black bellows blowing embers into a stone furnace's glowing mouth, flames leaping from
//   its chimney. Kindred: a big rose heart, a small one rising beside it. Harrow-Spur: a wraith called up from a glowing summoning ring, arms raised, souls rising beside it. Amber-Mend: a cracked wall sealed with glowing amber, the hammer striking it. Sovereign Ward: a longhouse
//   under a crowned golden dome, a fireball shattering on it. Hearth-Haul: a bulging sack in the hearth's glow, feathers
//   lifting off it. Plume-Ward: a heart falling onto a glowing feather. Sentry-Sight: a ballista bolt flying
//   at a red-eyed foe in a steel-blue ring of sight. Taproot-Surge: an ancient root, a tap in it pouring glowing sap
//   into a jar. Pitch-Seal: a sword, a coin and a bone stuck fast in a pool of black
//   pitch, pitch creeping up the blade. Primal-Hearth: an
//   eternal pyre burning violet, wisps drawn in. Hearth-Kindle: a campfire under a crescent moon. Grounding: an iron
//   stake draining the green rings into the earth.
// - Blessings: Glacial Satiety: meat frozen in a block of ice. Vein-Shatter: a boulder split open by a
//   pickaxe, gold veins glowing through it, nuggets popping out. Carapace Temper: a sword on an anvil in a glowing amber shell. Grave-Bind: a chained tombstone under a beam of light. Soul-Tether: a graveyard at night
//   under the moon, one more tombstone flying in to join it.
//   Fortune's Boon: a four-leaf clover in a golden glow, lucky "+" bursting out. Luminary: a lantern shining. Abyssal Draw: a whirlpool, glowing motes
//   streaming in along its arms. Hush-Felling: an axe buried in a stump, the sound of the blow fading, struck through.
//   Scald-Drift: a shield, half boiling sea and half lava, its iron rim frosted blue. Tether-Lock: a barbed harpoon on a taut line,
//   gold bindings glowing. Strider-Girth: a riding saddle, girth and stirrup, golden speed streaks. Second-Kindle: an
//   ember heart flaring into flame in a bursting ring of fire. Morgen-Pouch: a leather pouch, its mouth glowing violet,
//   wisps curling in, a paw print and a saddle drifting in. Tendril-Draw: a spear drawn back butt first by two
//   glowing roots coiled round its shaft, streaks trailing off its head.
// - Kinship: Skål: two tankards clinking. Sky-Call: lightning striking a runestone lit with the rune Jera.
//   Anvil-Spark: a hammer striking an anvil, sparks bursting from it. Eldhrímnir: the gods' cauldron over a fire, a boar's
//   haunch in its glowing stew, steam curling up. Rally-Horn: a war horn banded in gold, its call rolling out in
//   waves.
// - Reckoning: Crimson: a blood moon over a horde with red eyes. Ragnarök: Surtr's flaming sword under a black sun
//   Hoard-Bane: a horned helm with burning eyes looming as lightning strikes Hildir's chest (all three fading out toward
//   their edges).
//   Ley-Survey: glowing ley lines running out from a standing stone to a dashed red ring on the ground, the sea inside
//   it washed red, a red beam rising over one spot.
//   Sanctuary: a shield shattering a raid's red ring. Unmaking: a sword breaking apart
//   over an iron bar. Elder-Sprout: a sapling glowing green, rising out of the rings of an old cut stump.
// - The Bounds ability: a longhouse on a grassy mound inside a ring of the ward's glowing dashes.
// - The Blood Grounds status: an altar stone burning with a red rune on blood-dark ground, in a ring of red dashes, red
//   eyes watching from inside.
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
    // A rune stone on a mossy mound, a lightning bolt cut in it and glowing storm blue, sparks crackling off its edges:
    // every rite is called down by the obliterator's lightning. The mod's Thunderstore icon carries the same stone.

    private static Sprite? _flap;

    internal static Sprite Flap() => _flap ? _flap! : _flap = FlapPicture().ToSprite("TempestRite_Flap");

    // The picture's 64 x 64 colors, row 0 at the top (previews).
    internal static Color[] RenderFlap() => FlapPicture().Colors;

    private static readonly Vector2[] RuneStone =
    [
        new(19f, 57f), new(18f, 29f), new(20f, 15f), new(24f, 7f), new(29f, 4f), new(33f, 6f), new(38f, 9f),
        new(43f, 15f), new(46f, 26f), new(46.5f, 44f), new(45f, 57f)
    ];
    private static readonly Vector2[] RuneBolt = [new(36f, 15f), new(27f, 33f), new(36f, 33f), new(28f, 51f)];
    private static readonly Vector2[] SparkRight = [new(44.5f, 15f), new(49f, 12f), new(48f, 8.5f), new(52.5f, 5f)];
    private static readonly Vector2[] SparkLeft = [new(19.5f, 25f), new(15f, 22.5f), new(16.5f, 19f), new(11.5f, 16f)];

    private static float Zigzag(float x, float y, Vector2[] points, float r)
    {
        float d = float.MaxValue;
        for (int i = 1; i < points.Length; i++)
            d = Mathf.Min(d, Capsule(x, y, points[i - 1].x, points[i - 1].y, points[i].x, points[i].y, r));
        return d;
    }

    private static Canvas FlapPicture()
    {
        Canvas canvas = new();
        Color storm = new(0.35f, 0.72f, 1f);
        Glow(canvas, 32f, 30f, 31f, storm, 0.5f);

        // The mound, ending softly inside the icon.
        canvas.Paint((x, y) =>
        {
            float d = Ellipse(x, y, 32f, 58f, 25f, 6.5f);
            float light = Mathf.Lerp(1.05f, 0.6f, Mathf.Clamp01((y - 52f) / 10f));
            return new Color(0.3f * light, 0.4f * light, 0.19f * light, Mathf.Clamp01(-d / 3f) * 0.95f);
        });

        // The stone: grey, lit from the upper left, darker toward its edges and foot, a cool tint from the rune.
        Shape(canvas, (x, y) => Polygon(x, y, RuneStone), (x, y, depth) =>
        {
            float light = Mathf.Lerp(0.72f, 0.38f, Mathf.Clamp01((y - 8f) / 50f)) * Mathf.Lerp(1.12f, 0.8f, Mathf.Clamp01((x - 18f) / 28f));
            light *= 0.72f + 0.28f * Mathf.Clamp01(depth / 2.5f);
            return new Color(light * 0.97f, light, light * 1.04f, 1f);
        });

        // The rune: cut dark into the stone, then glowing from inside.
        canvas.Paint((x, y) => new Color(0.08f, 0.08f, 0.1f, Mathf.Clamp01(0.5f - Zigzag(x, y, RuneBolt, 2.2f)) * 0.85f));
        GlowLine(canvas, (x, y) => Zigzag(x, y, RuneBolt, 1.3f), storm, new Color(0.88f, 0.97f, 1f), 6f, 0.85f);

        // Sparks leaping off the stone.
        GlowLine(canvas, (x, y) => Zigzag(x, y, SparkRight, 0.55f), storm, new Color(0.85f, 0.96f, 1f), 3.5f, 0.7f);
        GlowLine(canvas, (x, y) => Zigzag(x, y, SparkLeft, 0.55f), storm, new Color(0.85f, 0.96f, 1f), 3.5f, 0.7f);
        Sparkle(canvas, 51f, 22f, 2f, storm);
        Sparkle(canvas, 13f, 33f, 1.6f, storm);
        return canvas;
    }
    // ---- The Bounds ability (see DomainBorder) ----
    // A longhouse on a grassy mound inside a ring of the ward's glowing dashes: the edge of the Domain rites' area.

    private static Sprite? _bounds;

    internal static Sprite Bounds() => _bounds ? _bounds! : _bounds = BoundsPicture().ToSprite("TempestRite_Bounds");

    // The picture's 64 x 64 colors, row 0 at the top (previews).
    internal static Color[] RenderBounds() => BoundsPicture().Colors;

    private const float BoundsX = 32f, BoundsY = 45f, BoundsRx = 28f, BoundsRy = 10f;
    private const int BoundsDashes = 22;

    private static Canvas BoundsPicture()
    {
        Canvas canvas = new();
        Color warm = new(1f, 0.6f, 0.2f);
        Glow(canvas, 32f, 40f, 31f, warm, 0.4f);

        // The mound, ending softly inside the icon, lit from above.
        canvas.Paint((x, y) =>
        {
            float d = Ellipse(x, y, 32f, 48f, 31f, 13f);
            float alpha = Mathf.Clamp01(-d / 4f);
            float light = Mathf.Lerp(1.05f, 0.55f, Mathf.Clamp01((y - 37f) / 22f));
            return new Color(0.33f * light, 0.42f * light, 0.2f * light, alpha * 0.95f);
        });

        // The far half of the ring, behind the house.
        BoundsRing(canvas, far: true);

        // The longhouse, its door lit from the hearth inside.
        const float scale = 64f / 34f;
        Shape(canvas, (x, y) => LonghouseShape((x - 15f) * scale, (y - 12f) * scale) / scale, (x, y, depth) =>
        {
            bool roof = y < 33.3f;
            float light = Mathf.Lerp(1.15f, 0.7f, Mathf.Clamp01((y - 14f) / 29f)) * (0.8f + 0.2f * Mathf.Clamp01(depth / 1.2f));
            return roof
                ? new Color(0.62f * light, 0.44f * light, 0.24f * light, 1f)
                : new Color(0.5f * light, 0.32f * light, 0.18f * light, 1f);
        });
        canvas.Paint((x, y) =>
        {
            float door = Box(x, y, 29.4f, 35.4f, 34.6f, 43f, 1.4f);
            return new Color(1f, 0.72f, 0.3f, Mathf.Clamp01(0.5f - door) * 0.95f);
        });
        Glow(canvas, 32f, 41f, 6f, warm, 0.5f);

        // The near half, in front of it.
        BoundsRing(canvas, far: false);
        return canvas;
    }

    // ---- The Blood Grounds status (see ArenaPreview) ----
    // Blood-dark ground in a ring of red dashes, an altar stone in its middle burning with a red rune, red eyes watching
    // from inside: where a Reckoning rite would be fought, and what has to be cleared first. Dark, fading out to its edges.

    private static Sprite? _bloodGrounds;

    internal static Sprite BloodGrounds() =>
        _bloodGrounds ? _bloodGrounds! : _bloodGrounds = BloodGroundsPicture().ToSprite("TempestRite_BloodGrounds");

    // The picture's 64 x 64 colors, row 0 at the top (previews).
    internal static Color[] RenderBloodGrounds() => BloodGroundsPicture().Colors;

    private static Canvas BloodGroundsPicture()
    {
        Canvas canvas = new();
        Color blood = new(1f, 0.12f, 0.08f);
        // A dark red sky, deepest at the top.
        canvas.Paint((x, y) =>
        {
            float t = Mathf.Clamp01(y / 60f);
            return new Color(Mathf.Lerp(0.06f, 0.32f, t), Mathf.Lerp(0.01f, 0.03f, t), Mathf.Lerp(0.02f, 0.03f, t), 1f);
        });
        Glow(canvas, 32f, 44f, 30f, blood, 0.35f);

        // The ground: trampled, blood-dark, lit red from the ring.
        canvas.Paint((x, y) =>
        {
            float d = Ellipse(x, y, BoundsX, 47f, 31f, 13f);
            float alpha = Mathf.Clamp01(-d / 4f);
            float light = Mathf.Lerp(1f, 0.55f, Mathf.Clamp01((y - 37f) / 22f));
            float stain = Mathf.Clamp01(0.5f - Ellipse(x, y, 24f, 49f, 7f, 2.2f)) + Mathf.Clamp01(0.5f - Ellipse(x, y, 42f, 51f, 5f, 1.6f));
            Color dirt = new(0.2f * light, 0.09f * light, 0.06f * light, alpha);
            return Color.Lerp(dirt, new Color(0.42f, 0.04f, 0.03f, alpha), Mathf.Clamp01(stain) * 0.7f);
        });

        Color halo = new(1f, 0.08f, 0.04f), core = new(1f, 0.3f, 0.22f);
        BoundsRing(canvas, far: true, halo, core);

        // Red eyes watching from inside the ring.
        foreach ((float ex, float ey) in new[] { (17f, 43.5f), (47f, 43f) })
            canvas.Paint((x, y) =>
            {
                float d = Mathf.Min(Circle(x, y, ex - 1.4f, ey, 0.7f), Circle(x, y, ex + 1.4f, ey, 0.7f));
                float glow = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Max(d, 0f) / 2.5f), 2f) * 0.8f;
                return Canvas.Over(new Color(1f, 0.1f, 0.05f, glow), new Color(1f, 0.5f, 0.35f, Mathf.Clamp01(0.5f - d)));
            });

        // The altar stone, standing in the middle, a little rounded at the top.
        Shape(canvas, (x, y) => Mathf.Min(
                Polygon(x, y, [new(25.5f, 46f), new(26.5f, 22f), new(29f, 17.5f), new(35f, 17.5f), new(37.5f, 22f), new(38.5f, 46f)]),
                Box(x, y, 22f, 43f, 42f, 48f, 1.2f)),
            (x, y, depth) =>
            {
                float light = Mathf.Lerp(1.1f, 0.55f, Mathf.Clamp01((x - 24f) / 16f)) * (0.75f + 0.25f * Mathf.Clamp01(depth / 1.5f));
                return new Color(0.3f * light, 0.27f * light, 0.27f * light, 1f);
            });
        // Its rune (Thurisaz, the thorn) burning red.
        canvas.Paint((x, y) =>
        {
            float d = Mathf.Min(Capsule(x, y, 29.5f, 23f, 29.5f, 38f, 0.8f),
                Polyline(x, y, [new(29.5f, 26.5f), new(35f, 30.5f), new(29.5f, 34.5f)], 0.8f));
            float glow = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Max(d, 0f) / 4f), 2f) * 0.75f;
            return Canvas.Over(new Color(1f, 0.12f, 0.06f, glow), new Color(1f, 0.55f, 0.4f, Mathf.Clamp01(0.5f - d)));
        });

        BoundsRing(canvas, far: false, halo, core);
        canvas.Fade(10f);
        return canvas;
    }

    // Half the ring of dashes lying on the ground, the far half dimmer; the ward's warm glow unless told otherwise.
    private static void BoundsRing(Canvas canvas, bool far, Color? haloColor = null, Color? coreColor = null) =>
        canvas.Paint((x, y) =>
        {
            if (far != y < BoundsY) return Color.clear;
            float dx = (x - BoundsX) / BoundsRx, dy = (y - BoundsY) / BoundsRy;
            float angle = Mathf.Atan2(dy, dx);
            float t = angle / (2f * Mathf.PI) * BoundsDashes + 0.25f;
            float along = Mathf.Abs(t - Mathf.Round(t));
            float speed = Mathf.Sqrt(BoundsRx * BoundsRx * dy * dy + BoundsRy * BoundsRy * dx * dx)
                / Mathf.Max(0.001f, Mathf.Sqrt(dx * dx + dy * dy));
            float arc = 2f * Mathf.PI * speed / BoundsDashes;
            float across = Mathf.Abs(Ellipse(x, y, BoundsX, BoundsY, BoundsRx, BoundsRy)) - (far ? 0.75f : 1f);
            float d = Mathf.Max(across, (along - 0.27f) * arc);
            float strength = far ? 0.55f : 1f;
            float halo = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Max(d, 0f) / 3f), 2f) * 0.6f * strength;
            Color h = haloColor ?? new Color(1f, 0.55f, 0.15f), c = coreColor ?? new Color(1f, 0.85f, 0.5f);
            return Canvas.Over(new Color(h.r, h.g, h.b, halo), new Color(c.r, c.g, c.b, Mathf.Clamp01(0.5f - d) * strength));
        });

    // ---- The active rites page's button ----
    // An hourglass in a wooden frame, its sand glowing amber as it runs: the time the rites going on have left.

    private static Sprite? _hourglass;

    internal static Sprite Hourglass() => _hourglass ? _hourglass! : _hourglass = HourglassPicture().ToSprite("TempestRite_Hourglass");

    // The picture's 64 x 64 colors, row 0 at the top (previews).
    internal static Color[] RenderHourglass() => HourglassPicture().Colors;

    // The glass: two bulbs meeting at a narrow neck at y = 32, as a distance from its outline.
    private static float Glass(float x, float y)
    {
        float t = Mathf.Clamp01(Mathf.Abs(y - 32f) / 19f);
        float half = 1.8f + 11f * Mathf.Sin(t * Mathf.PI * 0.5f) * (1f - 0.35f * t * t * t);
        return Mathf.Max(Mathf.Abs(x - 32f) - half, Mathf.Abs(y - 32f) - 19.5f);
    }

    private static Canvas HourglassPicture()
    {
        Canvas canvas = new();
        Glow(canvas, 32f, 40f, 26f, new Color(1f, 0.55f, 0.15f), 0.5f);

        // The frame: a board at the top and bottom, two posts between them, with a soft shadow.
        Func<float, float, float> boards = (x, y) => Mathf.Min(Box(x, y, 12f, 6f, 52f, 13f, 2.5f), Box(x, y, 12f, 51f, 52f, 58f, 2.5f));
        Func<float, float, float> posts = (x, y) => Mathf.Min(Capsule(x, y, 16f, 12f, 16f, 52f, 1.8f), Capsule(x, y, 48f, 12f, 48f, 52f, 1.8f));
        float[] alpha = new float[Size * Size];
        for (int py = 0; py < Size; py++)
            for (int px = 0; px < Size; px++)
            {
                float x = px + 0.5f, y = py + 0.5f;
                alpha[py * Size + px] = Mathf.Clamp01(0.5f - Mathf.Min(Mathf.Min(boards(x, y), posts(x, y)), Glass(x, y)));
            }
        float[] shadow = Canvas.Blur(alpha, 2);
        for (int i = Size; i < shadow.Length; i++) canvas.Blend(i, new Color(0f, 0f, 0f, shadow[i - Size] * 0.55f));
        canvas.Paint((x, y) => new Color(0.32f, 0.19f, 0.1f, Mathf.Clamp01(0.5f - posts(x, y))));

        // The glass, faintly blue, brighter at its rim.
        canvas.Paint((x, y) =>
        {
            float d = Glass(x, y);
            float rim = Mathf.Clamp01(1f - Mathf.Abs(d + 0.8f) / 1.2f);
            return new Color(0.8f, 0.9f, 1f, Mathf.Clamp01(0.5f - d) * (0.22f + 0.5f * rim));
        });

        // The sand: the lower part of the top bulb, a thin stream through the neck, a mound in the bottom bulb; it
        // glows amber.
        canvas.Paint((x, y) =>
        {
            float glass = Glass(x, y) + 1.2f;
            float top = Mathf.Max(glass, 22f - y);
            float mound = Mathf.Max(glass, 41f + 0.09f * (x - 32f) * (x - 32f) - y);
            float stream = Capsule(x, y, 32f, 30f, 32f, 44f, 0.6f);
            float d = Mathf.Min(Mathf.Min(top, mound), stream);
            float glow = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Max(d, 0f) / 4f), 2f) * 0.55f;
            float depth = Mathf.Clamp01(-d / 4f);
            Color sand = new(1f, Mathf.Lerp(0.6f, 0.8f, depth), Mathf.Lerp(0.18f, 0.4f, depth), Mathf.Clamp01(0.5f - d));
            return Canvas.Over(new Color(1f, 0.5f, 0.12f, glow), sand);
        });

        // The boards, lit from the sand's glow, on top of the glass ends.
        canvas.Paint((x, y) =>
        {
            float d = boards(x, y);
            float light = 0.8f + 0.2f * Mathf.Clamp01(-d / 2f);
            return new Color(0.5f * light, 0.29f * light, 0.15f * light, Mathf.Clamp01(0.5f - d));
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
            case WeatherKind.HoardBane:
                HoardBanePicture(canvas);
                break;
            case WeatherKind.FairWind:
                FairWindPicture(canvas);
                break;
            case WeatherKind.HushFelling:
                HushFellingPicture(canvas);
                break;
            case WeatherKind.DraughtFlush:
                DraughtFlushPicture(canvas);
                break;
            case WeatherKind.SoulTether:
                SoulTetherPicture(canvas);
                break;
            case WeatherKind.TundraShroud:
                TundraShroudPicture(canvas);
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
            case WeatherKind.Kindred:
                KindredPicture(canvas);
                break;
            case WeatherKind.HarrowSpur:
                HarrowSpurPicture(canvas);
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
            case WeatherKind.HearthHaul:
                HearthHaulPicture(canvas);
                break;
            case WeatherKind.AetherClear:
                AetherClearPicture(canvas);
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
            case WeatherKind.Eldhrimnir:
                EldhrimnirPicture(canvas);
                break;
            case WeatherKind.RallyHorn:
                RallyHornPicture(canvas);
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
            case WeatherKind.SunFall:
                SunFallPicture(canvas);
                break;
            case WeatherKind.NaturesRebound:
                NaturesReboundPicture(canvas);
                break;
            case WeatherKind.Unmaking:
                UnmakingPicture(canvas);
                break;
            case WeatherKind.LeySurvey:
                LeySurveyPicture(canvas);
                break;
            case WeatherKind.ElderSprout:
                ElderSproutPicture(canvas);
                break;
            case WeatherKind.PlumeWard:
                PlumeWardPicture(canvas);
                break;
            case WeatherKind.SentrySight:
                SentrySightPicture(canvas);
                break;
            case WeatherKind.TaprootSurge:
                TaprootSurgePicture(canvas);
                break;
            case WeatherKind.MantleAnchor:
                PitchSealPicture(canvas);
                break;
            case WeatherKind.PrimalHearth:
                PrimalHearthPicture(canvas);
                break;
            case WeatherKind.HearthKindle:
                HearthKindlePicture(canvas);
                break;
            case WeatherKind.ScaldDrift:
                ScaldDriftPicture(canvas);
                break;
            case WeatherKind.TetherLock:
                TetherLockPicture(canvas);
                break;
            case WeatherKind.StriderGirth:
                StriderGirthPicture(canvas);
                break;
            case WeatherKind.SecondKindle:
                SecondKindlePicture(canvas);
                break;
            case WeatherKind.MorgenPouch:
                MorgenPouchPicture(canvas);
                break;
            case WeatherKind.TendrilDraw:
                TendrilDrawPicture(canvas);
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

    // Vein-Shatter: a boulder split open by a pickaxe's bite, gold veins running through it and glowing in the crack,
    // nuggets popping out.
    private static readonly Vector2[] VeinRock =
    [
        new(4f, 57f), new(5f, 47f), new(11f, 37f), new(20f, 31f), new(31f, 28.5f), new(41f, 30f), new(49f, 36f), new(55f, 46f),
        new(56f, 55f), new(51f, 60f), new(9f, 61f)
    ];
    private static readonly Vector2[] VeinCrack =
        [new(29f, 26f), new(27.5f, 33f), new(31f, 39f), new(27f, 46f), new(30f, 53f), new(27.5f, 62f)];
    private static readonly Vector2[][] VeinLodes =
    [
        [new(8f, 52f), new(15f, 49f), new(24f, 50f)],
        [new(14f, 43f), new(20f, 44.5f), new(25f, 42f)],
        [new(34f, 46f), new(41f, 48.5f), new(51f, 47f)],
        [new(36f, 55f), new(44f, 54f), new(50f, 56.5f)]
    ];

    // The edge between the boulder's top face and its front, at x.
    private static float VeinFaceEdge(float x) => x < 30f ? Mathf.Lerp(46f, 38f, Mathf.Clamp01((x - 6f) / 24f))
        : Mathf.Lerp(38f, 44f, Mathf.Clamp01((x - 30f) / 24f));

    // Where the crack runs at height y.
    private static float VeinCrackX(float y)
    {
        for (int k = 1; k < VeinCrack.Length; k++)
            if (y <= VeinCrack[k].y || k == VeinCrack.Length - 1)
            {
                Vector2 a = VeinCrack[k - 1], b = VeinCrack[k];
                return Mathf.Lerp(a.x, b.x, Mathf.Clamp01((y - a.y) / (b.y - a.y)));
            }
        return VeinCrack[0].x;
    }

    private static void VeinShatterPicture(Canvas canvas)
    {
        Color gold = new(1f, 0.68f, 0.2f);
        Color pale = new(1f, 0.92f, 0.62f);
        Glow(canvas, 29f, 34f, 30f, gold, 0.5f);
        // The two halves, the left lit, the right turned away, parted a little at the crack.
        foreach (int side in new[] { -1, 1 })
        {
            float shift = side * 1.3f;
            Shape(canvas, (x, y) => Mathf.Max(Polygon(x - shift, y, VeinRock), side * (VeinCrackX(y) - (x - shift)) + 0.9f) - 0.5f,
                (x, y, depth) =>
                {
                    // A flat top face, lit, and a front face falling away below its edge.
                    float edge = VeinFaceEdge(x - shift) - y;
                    float light = edge > 0f
                        ? Mathf.Lerp(0.66f, 0.54f, Mathf.Clamp01(edge / 8f))
                        : Mathf.Lerp(0.46f, 0.3f, Mathf.Clamp01(-edge / 18f));
                    light *= side < 0 ? 1f : 0.88f;
                    light *= 0.8f + 0.2f * Mathf.Clamp01(depth / 2f);
                    light += Mathf.Clamp01(1f - Mathf.Abs(edge) / 1.2f) * 0.1f;
                    // The rock warms where the crack glows.
                    float heat = Mathf.Clamp01(1f - Mathf.Abs(x - shift - VeinCrackX(y)) / 6f) * 0.45f;
                    return Color.Lerp(new Color(light * 1.02f, light, light * 0.94f, 1f), new Color(1f, 0.6f, 0.2f, 1f), heat);
                });
            // Ore running through the half.
            foreach (Vector2[] lode in VeinLodes)
                if ((lode[0].x < VeinCrackX(lode[0].y) ? -1 : 1) == side)
                    GlowLine(canvas, (x, y) => Polyline(x - shift, y, lode, 0.6f), gold, new Color(1f, 0.78f, 0.35f), 2.5f, 0.45f);
        }
        // The light pouring out of the crack.
        GlowLine(canvas, (x, y) => Polyline(x, y, VeinCrack, 0.8f), gold, pale, 5f, 0.9f);
        // Nuggets popping out of it.
        foreach ((float x, float y, float r) in new[] { (17f, 22f, 2.4f), (37f, 16f, 1.8f), (10f, 30f, 1.5f), (22f, 13f, 1.4f) })
            canvas.Paint((px, py) =>
            {
                float d = (Mathf.Abs(px - x) + Mathf.Abs(py - y)) * 0.8f - r;
                float glow = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Max(d, 0f) / 3f), 2f) * 0.6f;
                float shine = Mathf.Clamp01((y - py + x - px) / (r * 1.5f) + 0.5f);
                return Canvas.Over(new Color(gold.r, gold.g, gold.b, glow),
                    new Color(Mathf.Lerp(0.85f, 1f, shine), Mathf.Lerp(0.55f, 0.9f, shine), Mathf.Lerp(0.15f, 0.55f, shine), Mathf.Clamp01(0.5f - d)));
            });
        // The pickaxe bitten into the top of the crack, its handle off to the right.
        if (GameArt.Get("pickaxe_bronze") is { } pick) canvas.Draw(pick, 70f, -4f, -56f, 56f, new Color(1.25f, 1.18f, 1.1f), -22f);
        Sparkle(canvas, 29f, 27f, 5f, gold);
        Sparkle(canvas, 50f, 28f, 3f, gold);
        Sparkle(canvas, 7f, 21f, 2.5f, gold);
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

    // Hush-Felling: an axe buried in a stump under a pale moonlit hush, the sound of the blow rising from where the
    // blade bit in, fading as it spreads, struck through.
    // The new picture sets the stump and axe this far right, leaving room for the sound on the left.
    private const float HushShift = 5f;

    private static void HushFellingPicture(Canvas canvas)
    {
        Color hush = new(0.72f, 0.85f, 1f);
        Glow(canvas, 32f + HushShift, 38f, 31f, hush, 0.5f);

        // The sound of the blow: arcs spreading out to the left from the bite, above the stump, each fainter than the last.
        const float cx = 28f + HushShift, cy = 33f;
        canvas.Paint((x, y) =>
        {
            float angle = Mathf.Atan2(y - cy, x - cx) * Mathf.Rad2Deg;
            if (angle < 0f) angle += 360f;
            float span = Mathf.Clamp01((40f - Mathf.Abs(angle - 205f)) / 6f);
            if (span <= 0f) return Color.clear;
            float a = 0f;
            for (int k = 0; k < 3; k++)
            {
                float d = Mathf.Abs(Circle(x, y, cx, cy, 9f + k * 6f)) - (1.1f - k * 0.15f);
                a = Mathf.Max(a, Mathf.Clamp01(0.5f - d) * (0.85f - k * 0.22f));
            }
            return new Color(hush.r, hush.g, hush.b, a * span);
        });
        // Struck through: one stroke slanting across all three arcs.
        GlowLine(canvas, (x, y) => Capsule(x, y, 8f, 32f, 30f, 19f, 1f), hush, new Color(0.92f, 0.96f, 1f), 3f, 0.6f);

        HushStumpAndAxe(canvas, HushShift);
        Sparkle(canvas, 57f, 24f, 3f, hush);
        Sparkle(canvas, 9f, 12f, 2.5f, hush);
    }

    // The Hush-Felling icon as it was first drawn: the struck-through sound arcs off to the left side. Kept to switch
    // back to.
    private static void HushFellingPictureOriginal(Canvas canvas)
    {
        Color hush = new(0.72f, 0.85f, 1f);
        Glow(canvas, 32f, 38f, 31f, hush, 0.5f);

        // The sound of the blow: arcs to the left, fading, struck through.
        canvas.Paint((x, y) =>
        {
            float a = 0f;
            for (int k = 0; k < 3; k++)
            {
                float r = 9f + k * 6f;
                float d = Mathf.Abs(Circle(x, y, 30f, 34f, r)) - 0.9f;
                // Only the left side of each ring, toward the top.
                bool side = x < 30f - r * 0.35f && y < 46f && y > 18f;
                if (side) a = Mathf.Max(a, Mathf.Clamp01(0.5f - d) * (0.75f - k * 0.2f));
            }
            return new Color(hush.r, hush.g, hush.b, a);
        });
        GlowLine(canvas, (x, y) => Capsule(x, y, 4f, 44f, 22f, 16f, 1f), hush, new Color(0.92f, 0.96f, 1f), 3f, 0.6f);

        HushStumpAndAxe(canvas);
        Sparkle(canvas, 52f, 26f, 3f, hush);
        Sparkle(canvas, 10f, 56f, 2.5f, hush);
    }


    // The stump with the axe bitten into its cut, shift pixels to the right.
    private static void HushStumpAndAxe(Canvas canvas, float shift = 0f)
    {
        // The stump: bark with grooves, the cut top pale with rings.
        Shape(canvas, (x, y) => { x -= shift; return Mathf.Min(Box(x, y, 19f, 38f, 49f, 60f, 2f), Ellipse(x, y, 34f, 60f, 18f, 3.5f)); }, (x, y, depth) =>
        {
            x -= shift;
            float light = Mathf.Lerp(0.55f, 0.3f, Mathf.Clamp01((x - 19f) / 30f));
            float groove = Mathf.Abs(Mathf.Repeat(x + Mathf.Sin(y * 0.4f) * 1.2f, 5f) - 2.5f) < 0.6f ? 0.7f : 1f;
            return new Color(light * 0.8f * groove, light * 0.55f * groove, light * 0.32f * groove, 1f);
        });
        Shape(canvas, (x, y) => { x -= shift; return Ellipse(x, y, 34f, 38f, 15f, 5f); }, (x, y, depth) =>
        {
            x -= shift;
            float r = Mathf.Sqrt((x - 34f) * (x - 34f) / 225f + (y - 38f) * (y - 38f) / 25f);
            float ring = Mathf.Abs(Mathf.Repeat(r * 5f, 1f) - 0.5f) < 0.12f ? 0.82f : 1f;
            return new Color(0.93f * ring, 0.78f * ring, 0.55f * ring, 1f);
        }, 0f);

        // The axe: its blade bitten into the cut, the handle rising to the top right.
        Shape(canvas, (x, y) => { x -= shift; return Capsule(x, y, 32f, 34f, 52f, 5f, 1.7f); }, (x, y, depth) =>
        {
            x -= shift;
            float t = Mathf.Clamp01(depth / 1.7f);
            return new Color(Mathf.Lerp(0.45f, 0.7f, t), Mathf.Lerp(0.28f, 0.45f, t), Mathf.Lerp(0.14f, 0.24f, t), 1f);
        });
        // Only the part above the cut shows: the rest is in the wood.
        Shape(canvas, (x, y) => { x -= shift; return Mathf.Max(Polygon(x, y, HushAxeBlade), y - 38.5f); }, (x, y, depth) =>
        {
            x -= shift;
            float t = Mathf.Clamp01((y - 28f) / 11f);
            Color steel = Color.Lerp(new Color(0.95f, 0.97f, 1f), new Color(0.5f, 0.55f, 0.62f), t);
            float edge = Mathf.Lerp(0.85f, 1f, Mathf.Clamp01(depth / 1.5f));
            return new Color(steel.r * edge, steel.g * edge, steel.b * edge, 1f);
        });
    }

    private static readonly Vector2[] HushAxeBlade = [new(27f, 33f), new(33f, 27.5f), new(44f, 37f), new(42f, 43f), new(31f, 42f)];

    // Draught-Flush: health, stamina and eitr meads fanned out, a swirl of royal-jelly gold looping around them and back
    // on itself (the next draught comes round sooner), arrowhead at its end.
    private static void DraughtFlushPicture(Canvas canvas)
    {
        Color jelly = new(1f, 0.82f, 0.35f);
        Glow(canvas, 32f, 34f, 30f, jelly, 0.55f);

        // The swirl: most of a ring, thickest at its head, fading toward its tail.
        const float cx = 32f, cy = 34f, r = 25f;
        canvas.Paint((x, y) =>
        {
            float angle = Mathf.Atan2(y - cy, x - cx) * Mathf.Rad2Deg;
            // 0 at the tail (bottom left), up to 1 at the head (bottom right), clockwise on screen.
            float t = Mathf.Repeat(angle - 120f, 360f) / 300f;
            if (t > 1f) return Color.clear;
            float width = Mathf.Lerp(0.6f, 2.2f, t);
            float d = Mathf.Abs(Circle(x, y, cx, cy, r)) - width;
            float a = Mathf.Clamp01(0.5f - d) * Mathf.Lerp(0.25f, 1f, t);
            return new Color(jelly.r, jelly.g, jelly.b, a);
        });
        // Its head: an arrowhead pointing on round the ring.
        float head = (120f + 300f) * Mathf.Deg2Rad;
        Vector2 tip = new(cx + Mathf.Cos(head) * r, cy + Mathf.Sin(head) * r);
        Vector2 along = new(-Mathf.Sin(head), Mathf.Cos(head));
        Vector2 outward = new(Mathf.Cos(head), Mathf.Sin(head));
        Vector2[] arrow = [tip + along * 6f, tip - along * 1.5f + outward * 5.5f, tip - along * 1.5f - outward * 5.5f];
        canvas.Paint((x, y) => new Color(1f, 0.92f, 0.6f, Mathf.Clamp01(0.5f - Polygon(x, y, arrow))));

        // The meads: stamina and eitr leaning out behind, health in front. (Named one by one: the README's icon previews
        // find the game art to fetch by these names.)
        DraughtMead(canvas, GameArt.ItemIconPixels("MeadStaminaMinor"), 5f, 15f, 30f, 18f, new Color(1f, 0.8f, 0.2f));
        DraughtMead(canvas, GameArt.ItemIconPixels("MeadEitrMinor"), 33f, 17f, 26f, -18f, new Color(0.7f, 0.45f, 1f));
        DraughtMead(canvas, GameArt.ItemIconPixels("MeadHealthMajor"), 16f, 20f, 32f, 0f, new Color(1f, 0.3f, 0.25f));

        Sparkle(canvas, 54f, 12f, 3.5f, jelly);
        Sparkle(canvas, 9f, 52f, 2.5f, jelly);
    }

    private static void DraughtMead(Canvas canvas, Pixels? mead, float x, float y, float size, float angle, Color fallback)
    {
        if (mead != null) canvas.Draw(mead, x, y, size, size, null, angle);
        else Glow(canvas, x + size / 2f, y + size * 0.55f, size * 0.28f, fallback, 0.9f);
    }

    // Harrow-Spur: a wraith called up out of a glowing summoning ring, arms raised, its body twisting down into a tail
    // that pours out of the ring, eyes glowing in hollow sockets; souls rise like pale flames beside it, to follow.
    private static void HarrowSpurPicture(Canvas canvas)
    {
        Color ecto = new(0.55f, 1f, 0.85f);
        Color pale = new(0.88f, 1f, 0.96f);
        Glow(canvas, 32f, 38f, 32f, ecto, 0.5f);

        // The summoning ring on the ground, glowing, faintly filled, brightest where the tail pours out.
        canvas.Paint((x, y) =>
        {
            float d = Ellipse(x, y, 32f, 54f, 23f, 6f);
            float ring = Mathf.Abs(d) - 0.9f;
            float halo = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Max(ring, 0f) / 4f), 2f) * 0.6f;
            float core = d < 0f ? 0.18f + 0.4f * Mathf.Pow(Mathf.Clamp01(1f - Circle(x, y, 32f, 54f, 0f) / 12f), 2f) : 0f;
            Color fill = new(ecto.r, ecto.g, ecto.b, core);
            return Canvas.Over(Canvas.Over(fill, new Color(ecto.r, ecto.g, ecto.b, halo)), new Color(pale.r, pale.g, pale.b, Mathf.Clamp01(0.5f - ring)));
        });

        // Souls rising beside it: pale flames, their tails fading below them.
        foreach ((float sx, float sy, float size) in new[] { (11f, 30f, 3.6f), (53f, 26f, 4f), (51f, 44f, 2.8f) })
        {
            Glow(canvas, sx, sy, size * 2.6f, ecto, 0.55f);
            Shape(canvas, (x, y) => SoulFlame(x, y, sx, sy, size), (x, y, depth) =>
            {
                float t = Mathf.Clamp01((y - (sy - size)) / (size * 4f));
                Color c = Color.Lerp(new Color(0.97f, 1f, 0.99f), ecto, t);
                return new Color(c.r, c.g, c.b, Mathf.Lerp(1f, 0.15f, t));
            }, 0f);
        }

        // The wraith: pale at the head, fading as it twists down into the ring.
        Shape(canvas, HarrowWraith, (x, y, depth) =>
        {
            float t = Mathf.Clamp01((y - 9f) / 45f);
            Color body = Color.Lerp(new Color(0.96f, 1f, 0.98f), new Color(0.4f, 0.92f, 0.78f), t);
            float rim = Mathf.Lerp(0.78f, 1f, Mathf.Clamp01(depth / 2.2f));
            return new Color(body.r * rim, body.g * rim, body.b * rim, Mathf.Lerp(0.95f, 0.25f, t * t));
        }, 0f);
        // Hollow sockets with a glint of light deep in each, and a long wailing mouth.
        canvas.Paint((x, y) =>
        {
            float sockets = Mathf.Min(Ellipse(x, y, 28.5f, 18.5f, 2.3f, 3f), Ellipse(x, y, 35.5f, 18.5f, 2.3f, 3f));
            float mouth = Ellipse(x, y, 32f, 26f, 1.6f, 3f);
            float glint = Mathf.Min(Circle(x, y, 28.5f, 19.2f, 0.9f), Circle(x, y, 35.5f, 19.2f, 0.9f));
            Color dark = new(0.04f, 0.13f, 0.17f, Mathf.Clamp01(0.5f - Mathf.Min(sockets, mouth)) * 0.92f);
            return Canvas.Over(dark, new Color(0.6f, 1f, 0.9f, Mathf.Clamp01(0.5f - glint)));
        });
        Sparkle(canvas, 21f, 6f, 2.3f, ecto);
        Sparkle(canvas, 58f, 55f, 2.3f, ecto);
    }

    // The wraith's outline: head, raised arms, and a body narrowing as it sways down into a tail.
    private static float HarrowWraith(float x, float y)
    {
        float head = Circle(x, y, 32f, 18f, 8.5f);
        float arms = Mathf.Min(Capsule(x, y, 25f, 27f, 15f, 15f, 2.6f), Capsule(x, y, 39f, 27f, 49f, 15f, 2.6f));
        float hands = Mathf.Min(Circle(x, y, 14.5f, 13.5f, 2.4f), Circle(x, y, 49.5f, 13.5f, 2.4f));
        // Below the head the body narrows from 9 to 1 wide, its middle swaying side to side.
        float t = Mathf.Clamp01((y - 22f) / 31f);
        float middle = 32f + Mathf.Sin(t * Mathf.PI * 1.4f) * 4f * t;
        float half = Mathf.Lerp(9f, 1f, t);
        float body = Mathf.Max(Mathf.Max(Mathf.Abs(x - middle) - half, 22f - y), y - 54f);
        return Mathf.Min(Mathf.Min(head, body), Mathf.Min(arms, hands));
    }

    // A pale flame standing on its point at the bottom: a round head at (cx, cy), drawn out into a tail below it.
    private static float SoulFlame(float x, float y, float cx, float cy, float size)
    {
        float head = Circle(x, y, cx, cy, size);
        float sway = Mathf.Sin((y - cy) * 0.6f) * size * 0.3f;
        float t = Mathf.Clamp01((y - cy) / (size * 3.4f));
        float tail = Mathf.Max(Mathf.Max(Mathf.Abs(x - cx - sway) - size * (1f - t), cy - y), y - cy - size * 3.4f);
        return Mathf.Min(head, tail);
    }

    // Kindred: a big heart and a small one rising beside it (a mother and her young), aglow in rose pink, with sparkles.
    private static void KindredPicture(Canvas canvas)
    {
        Color rose = new(1f, 0.45f, 0.62f);
        Glow(canvas, 30f, 34f, 31f, rose, 0.55f);
        HeartShape(canvas, 27f, 37f, 17f);
        HeartShape(canvas, 48f, 16f, 8.5f);
        Sparkle(canvas, 11f, 13f, 3.5f, rose);
        Sparkle(canvas, 55f, 42f, 3f, rose);
        Sparkle(canvas, 9f, 52f, 2.5f, rose);
    }

    // A glossy red heart, point down, size its half width: lighter at the top left, with a gleam.
    private static void HeartShape(Canvas canvas, float cx, float cy, float size) =>
        Shape(canvas, (x, y) => Heart(x, y, cx, cy, size), (x, y, depth) =>
        {
            float t = Mathf.Clamp01((x - cx + y - cy) / (2.4f * size) + 0.5f);
            Color color = Color.Lerp(new Color(1f, 0.6f, 0.7f), new Color(0.7f, 0.08f, 0.24f), t);
            float edge = Mathf.Lerp(0.8f, 1f, Mathf.Clamp01(depth / (0.25f * size)));
            color = new Color(color.r * edge, color.g * edge, color.b * edge, 1f);
            float gleam = Circle(x, y, cx - 0.48f * size, cy - 0.36f * size, 0.17f * size);
            return Canvas.Over(color, new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - gleam) * 0.75f));
        });

    // Distance to a heart: two round lobes over a point.
    private static float Heart(float x, float y, float cx, float cy, float size)
    {
        float lobes = Mathf.Min(Circle(x, y, cx - 0.5f * size, cy - 0.3f * size, 0.56f * size),
            Circle(x, y, cx + 0.5f * size, cy - 0.3f * size, 0.56f * size));
        float point = Polygon(x, y, [new(cx - 1.02f * size, cy - 0.2f * size), new(cx + 1.02f * size, cy - 0.2f * size), new(cx, cy + 1.05f * size)]);
        return Mathf.Min(lobes, point);
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

    // Hearth-Haul: a laden leather sack, tied with a golden cord, in the warm glow of a hearth, light as the feathers
    // drifting up off it.
    private static readonly Vector2[] SackNeck = [new(20.5f, 33f), new(25f, 21f), new(35f, 21f), new(39.5f, 33f)];
    private static readonly Vector2[] SackFrill =
        [new(25f, 22f), new(21.5f, 16.5f), new(25.5f, 16f), new(28f, 13.5f), new(31f, 15.5f), new(34.5f, 13.5f), new(38.5f, 16.5f), new(35f, 22f)];

    private static void HearthHaulPicture(Canvas canvas)
    {
        Color hearth = new(1f, 0.55f, 0.18f);
        Color tan = new(0.9f, 0.68f, 0.45f);
        Glow(canvas, 30f, 46f, 32f, hearth, 0.6f);

        // The hearth's embers glowing under it.
        canvas.Paint((x, y) =>
        {
            float bed = Ellipse(x, y, 30f, 60f, 24f, 4.5f);
            float flicker = 0.75f + 0.25f * Mathf.Sin(x * 0.9f) * Mathf.Sin(x * 0.37f + 1f);
            Color fire = Color.Lerp(new Color(0.85f, 0.2f, 0.05f), new Color(1f, 0.8f, 0.35f), Mathf.Clamp01(-bed / 3f) * flicker);
            fire.a = Mathf.Clamp01(0.5f - bed) * 0.95f;
            return fire;
        });

        static float Sack(float x, float y) => Mathf.Min(Ellipse(x, y, 30f, 43f, 19f, 15.5f), Polygon(x, y, SackNeck));
        Shape(canvas, Sack, (x, y, depth) =>
        {
            // Lit from the upper left and warmed by the fire below; a stitched seam down its belly.
            float light = Mathf.Lerp(1.15f, 0.62f, Mathf.Clamp01((x - 14f + (y - 24f) * 0.6f) / 40f));
            light *= 0.78f + 0.22f * Mathf.Clamp01(depth / 3f);
            float warm = Mathf.Clamp01((y - 46f) / 12f) * 0.35f;
            float seam = Mathf.Abs(x - 31f - (y - 43f) * 0.12f) < 0.6f && y > 31f && Mathf.Repeat(y, 3f) < 1.8f ? 0.65f : 1f;
            float fold = 1f - 0.12f * Mathf.Clamp01(1f - Mathf.Abs(Ellipse(x, y, 24f, 38f, 6f, 9f)) / 1.2f);
            Color leather = new(0.55f * light * seam * fold, 0.36f * light * seam * fold, 0.2f * light * seam * fold, 1f);
            return Color.Lerp(leather, new Color(0.95f, 0.5f, 0.2f, 1f), warm);
        });
        Shape(canvas, (x, y) => Polygon(x, y, SackFrill), (x, y, depth) =>
        {
            float light = Mathf.Lerp(1.05f, 0.7f, Mathf.Clamp01((x - 21f) / 18f)) * (0.8f + 0.2f * Mathf.Clamp01(depth / 1.5f));
            return new Color(0.5f * light, 0.33f * light, 0.18f * light, 1f);
        }, 0.4f);
        // The cord, tied in a knot with its ends hanging.
        Shape(canvas, (x, y) => Mathf.Min(Mathf.Min(Capsule(x, y, 24f, 22f, 36f, 22f, 1.6f), Circle(x, y, 30f, 22.5f, 2.4f)),
                Mathf.Min(Capsule(x, y, 30f, 23f, 27f, 30f, 0.9f), Capsule(x, y, 30.5f, 23f, 33f, 29f, 0.9f))),
            (x, y, depth) =>
            {
                float light = 0.8f + 0.35f * Mathf.Clamp01(depth / 1.2f);
                return new Color(1f * light, 0.78f * light, 0.3f * light, 1f);
            }, 0.4f);

        // Feathers drifting up off it.
        Feather(canvas, 51f, 22f, 17f, 0.55f, tan);
        Feather(canvas, 11f, 24f, 13f, -0.45f, tan);
        Feather(canvas, 44f, 7f, 10f, 0.9f, tan);
        Sparkle(canvas, 57f, 38f, 3f, hearth);
        Sparkle(canvas, 8f, 44f, 2.5f, hearth);
    }

    // A pale feather centered at (cx, cy), length long, turned angle radians from upright: a quill and a soft vane
    // with faint barbs, glowing a little in the given color.
    private static void Feather(Canvas canvas, float cx, float cy, float length, float angle, Color glow, float barbs = 3.2f)
    {
        float c = Mathf.Cos(angle), s = Mathf.Sin(angle), half = length / 2f;
        Glow(canvas, cx, cy, length * 0.9f, glow, 0.35f);
        canvas.Paint((x, y) =>
        {
            float dx = x - cx, dy = y - cy;
            // Along the feather toward its tip (screen y points down), and across it.
            float u = dx * s - dy * c, v = dx * c + dy * s;
            float t = Mathf.Clamp01((u + half * 0.7f) / (half * 1.7f));
            float width = length * 0.17f * Mathf.Sin(t * Mathf.PI);
            float vane = Mathf.Max(Mathf.Abs(v - length * 0.03f * Mathf.Sin(t * Mathf.PI)) - width, Mathf.Abs(u - half * 0.15f) - half * 0.85f);
            float quill = Mathf.Max(Mathf.Abs(v) - 0.45f, Mathf.Abs(u) - half);
            float alpha = Mathf.Max(Mathf.Clamp01(0.5f - vane) * 0.9f, Mathf.Clamp01(0.5f - quill));
            if (alpha <= 0f) return Color.clear;
            float grain = 0.88f + 0.12f * Mathf.Sin((u - Mathf.Abs(v) * 0.9f) * barbs);
            float shade = quill < 0f ? 1f : grain * (1f - 0.22f * Mathf.Clamp01(Mathf.Abs(v) / Mathf.Max(0.5f, width)));
            return new Color(shade, shade * 0.97f, shade * 0.9f, alpha);
        });
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

    // Hoard-Bane: a horned helm looms out of a black storm, its slit eyes burning, as lightning strikes Hildir's chest
    // on scorched ground.
    private static void HoardBanePicture(Canvas canvas)
    {
        // The storm: black at the edges, a sickly brass glow behind the helm.
        canvas.Paint((x, y) =>
        {
            float r = Mathf.Sqrt((x - 32f) * (x - 32f) + (y - 20f) * (y - 20f)) / 40f;
            Color near = new(0.45f, 0.3f, 0.07f), mid = new(0.16f, 0.07f, 0.03f), far = new(0.02f, 0.01f, 0.01f);
            Color sky = r < 0.5f ? Color.Lerp(near, mid, r / 0.5f) : Color.Lerp(mid, far, Mathf.Clamp01((r - 0.5f) / 0.5f));
            sky.a = 1f;
            return sky;
        });
        // The helm: a dome, a narrowing face and two great horns, edged with the storm's light.
        canvas.Paint((x, y) =>
        {
            float d = Mathf.Min(Mathf.Min(Circle(x, y, 32f, 21f, 13f), Polygon(x, y, HelmFace)),
                Mathf.Min(Polygon(x, y, HelmHornLeft), Polygon(x, y, Mirror(HelmHornLeft))));
            float rim = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(d) / 1.4f), 2f) * 0.35f;
            Color helm = new(0.03f, 0.02f, 0.02f, Mathf.Clamp01(0.5f - d));
            return Canvas.Over(helm, new Color(0.95f, 0.7f, 0.3f, rim * Mathf.Clamp01(0.5f - d + 1.5f)));
        });
        // The nose guard, a shade lighter.
        canvas.Paint((x, y) => new Color(0.09f, 0.06f, 0.04f, Mathf.Clamp01(0.5f - Polygon(x, y, HelmNose))));
        // The eyes: slanted slits, burning.
        canvas.Paint((x, y) =>
        {
            float d = Mathf.Min(Polygon(x, y, HelmEye), Polygon(x, y, Mirror(HelmEye)));
            float glow = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Max(d, 0f) / 6f), 2f) * 0.95f;
            return Canvas.Over(new Color(1f, 0.25f, 0.05f, glow), new Color(1f, 0.85f, 0.4f, Mathf.Clamp01(0.5f - d)));
        });
        // The lightning, striking down onto the chest.
        canvas.Paint((x, y) =>
        {
            float d = Polygon(x, y, HoardBolt);
            float glow = Mathf.Clamp01(1f - Mathf.Max(d, 0f) / 5f) * 0.65f;
            return Canvas.Over(new Color(1f, 0.75f, 0.3f, glow), new Color(1f, 0.97f, 0.85f, Mathf.Clamp01(0.5f - d)));
        });
        Glow(canvas, 32f, 50f, 15f, new Color(1f, 0.35f, 0.08f), 0.7f);
        if (GameArt.ItemIconPixels("chest_hildir1") is { } chest) canvas.Draw(chest, 19f, 36f, 26f, 26f, new Color(1f, 0.62f, 0.38f));
        else Glow(canvas, 32f, 50f, 9f, new Color(0.75f, 0.55f, 0.2f), 0.9f);
        // Scorched ground, embers glowing in its cracks.
        canvas.Paint((x, y) =>
        {
            float ground = Polygon(x, y, RagnarokGround);
            float cracks = Mathf.Min(Polyline(x, y, [new(3f, 63f), new(9f, 58f), new(16f, 60f)], 0.6f),
                Polyline(x, y, [new(62f, 62f), new(55f, 58f), new(48f, 60f)], 0.6f));
            Color earth = new(0.05f, 0.02f, 0.01f, Mathf.Clamp01(0.5f - ground) * 0.9f);
            float ember = Mathf.Clamp01(0.5f - cracks) * Mathf.Clamp01(-ground);
            return Canvas.Over(earth, new Color(1f, 0.45f, 0.1f, ember));
        });
        Embers(canvas);
        canvas.Fade(12f);
    }

    private static Vector2[] Mirror(Vector2[] points)
    {
        Vector2[] mirrored = new Vector2[points.Length];
        for (int i = 0; i < points.Length; i++) mirrored[i] = new Vector2(64f - points[i].x, points[i].y);
        return mirrored;
    }

    private static readonly Vector2[] HelmFace =
        [new(19f, 20f), new(45f, 20f), new(44f, 31f), new(38f, 38f), new(32f, 40f), new(26f, 38f), new(20f, 31f)];

    private static readonly Vector2[] HelmHornLeft =
        [new(21f, 18f), new(15f, 15f), new(11f, 10f), new(10f, 3f), new(14f, 8f), new(19f, 11f), new(24f, 12f)];

    private static readonly Vector2[] HelmNose = [new(30.8f, 22f), new(33.2f, 22f), new(33f, 35f), new(32f, 37f), new(31f, 35f)];

    private static readonly Vector2[] HelmEye = [new(22f, 24f), new(29.5f, 26.5f), new(28.5f, 28.5f), new(23f, 26.5f)];

    private static readonly Vector2[] HoardBolt =
    [
        new(47f, 0f), new(40f, 16f), new(44f, 16f), new(36f, 31f), new(40f, 31f), new(32f, 44f), new(35f, 33f), new(31f, 33f),
        new(38f, 19f), new(34f, 19f), new(42f, 0f)
    ];

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

    // Grave-Bind: a tombstone in chains, the rune Eihwaz (the yew, the graveyard tree) glowing on it, a beam of light
    // rising behind it.
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
        GlowLine(canvas, (x, y) => Strokes(x, y, 1.1f, new Vector4(32f, 25f, 32f, 42f), new Vector4(32f, 25f, 37f, 30f),
            new Vector4(32f, 42f, 27f, 37f)), new Color(0.4f, 0.85f, 1f), new Color(0.85f, 1f, 1f), 4f, 0.8f);
    }

    // Tundra Shroud: a hooded fur cloak, thick white fur round its face, warmth glowing amber inside the hood; fat
    // raindrops bounce off its crown under a crescent moon (neither rain nor nightfall gets through).
    private static void TundraShroudPicture(Canvas canvas)
    {
        Color warm = new(1f, 0.6f, 0.22f);
        Color rain = new(0.35f, 0.7f, 1f);
        Color cream = new(1f, 0.93f, 0.75f);

        Glow(canvas, 32f, 30f, 34f, new Color(0.35f, 0.55f, 1f), 0.45f);

        // The crescent moon.
        Glow(canvas, 56f, 8f, 9f, new Color(0.75f, 0.88f, 1f), 0.5f);
        canvas.Paint((x, y) => new Color(0.94f, 0.97f, 1f,
            Mathf.Clamp01(0.5f - Mathf.Max(Circle(x, y, 56f, 8f, 5f), -Circle(x, y, 58.8f, 5.8f, 4.2f)))));

        // The cloak: a rounded hood flowing out into a wide hem, brown fur lit from the upper left, in rough strands.
        Vector2[] body = [new(21f, 26f), new(43f, 26f), new(50f, 37f), new(56f, 63f), new(8f, 63f), new(14f, 37f)];
        float Cloak(float x, float y) => Mathf.Min(Circle(x, y, 32f, 25f, 13.5f), Polygon(x, y, body));
        Shape(canvas, Cloak, (x, y, depth) =>
        {
            float light = Mathf.Lerp(1.25f, 0.6f, Mathf.Clamp01((x - 14f + y - 12f) / 80f));
            float strands = 0.9f + 0.1f * Mathf.Sin(x * 1.7f + Mathf.Sin(y * 0.35f) * 2f);
            float edge = Mathf.Lerp(1.3f, 1f, Mathf.Clamp01(depth / 2.5f));
            float k = light * strands * edge;
            return new Color(0.52f * k, 0.36f * k, 0.24f * k, 1f);
        });

        // The front of the cloak, a white fur band running down from the face.
        canvas.Paint((x, y) =>
        {
            if (y < 34f) return Color.clear;
            float fluff = 2.2f + 0.6f * Mathf.Sin(y * 1.9f);
            float d = Mathf.Max(Mathf.Abs(x - 32f) - fluff, Mathf.Abs(y - 49f) - 15f);
            float shade = Mathf.Lerp(1f, 0.75f, Mathf.Clamp01((y - 34f) / 30f));
            return new Color(0.95f * shade, 0.95f * shade, 0.97f * shade, Mathf.Clamp01(0.5f - d));
        });

        // Inside the hood: dark, glowing warm.
        canvas.Paint((x, y) =>
        {
            float d = Ellipse(x, y, 32f, 28f, 7.5f, 9.5f);
            if (d > 0.5f) return Color.clear;
            float r = Mathf.Clamp01(Circle(x, y, 32f, 30f, 0f) / 9f);
            Color inside = Color.Lerp(new Color(1f, 0.78f, 0.4f), new Color(0.2f, 0.08f, 0.04f), Mathf.Pow(r, 0.8f));
            return new Color(inside.r, inside.g, inside.b, Mathf.Clamp01(0.5f - d));
        });
        Glow(canvas, 32f, 30f, 12f, warm, 0.45f);

        // The fur trim round the face: thick, fluffy and white.
        canvas.Paint((x, y) =>
        {
            float angle = Mathf.Atan2(y - 28f, x - 32f);
            float fluff = 0.7f * Mathf.Sin(angle * 13f) + 0.35f * Mathf.Sin(angle * 29f + 1f);
            float d = Mathf.Abs(Ellipse(x, y, 32f, 28f, 10f, 12f)) - (2.6f + fluff);
            float shade = Mathf.Lerp(1f, 0.78f, Mathf.Clamp01((y - 16f) / 26f));
            return new Color(0.97f * shade, 0.97f * shade, 1f * shade, Mathf.Clamp01(0.5f - d));
        });

        // Raindrops bouncing off its crown: one falling onto it, a splash, and two thrown off to either side.
        Raindrop(canvas, 13f, 11f, 5f, -40f, rain);
        Raindrop(canvas, 46f, 12f, 4.4f, 40f, rain);
        Raindrop(canvas, 32f, 3f, 3.6f, 0f, rain);
        canvas.Paint((x, y) =>
        {
            float d = Mathf.Min(Mathf.Min(Circle(x, y, 26.5f, 9.5f, 1.2f), Circle(x, y, 37.5f, 9.5f, 1.2f)),
                Mathf.Min(Circle(x, y, 22f, 13f, 0.9f), Circle(x, y, 42f, 13f, 0.9f)));
            return new Color(rain.r, rain.g, rain.b, Mathf.Clamp01(0.5f - d));
        });

        Sparkle(canvas, 7f, 40f, 3f, cream);
        Sparkle(canvas, 58f, 34f, 2.5f, cream);
    }

    // A raindrop, rounded at the bottom and pointed at the top, leaning by angle (degrees, clockwise).
    private static void Raindrop(Canvas canvas, float cx, float cy, float size, float angle, Color color)
    {
        float a = angle * Mathf.Deg2Rad, cos = Mathf.Cos(a), sin = Mathf.Sin(a);
        canvas.Paint((x, y) =>
        {
            float dx = x - cx, dy = y - cy;
            float u = dx * cos + dy * sin, v = -dx * sin + dy * cos;
            float d = Mathf.Min(Circle(u, v, 0f, size * 0.35f, size * 0.62f),
                Capsule(u, v, 0f, -size * 0.9f, 0f, size * 0.3f, 0.15f + Mathf.Clamp01((v + size * 0.9f) / (size * 1.2f)) * size * 0.5f));
            float shine = Circle(u, v, -size * 0.2f, size * 0.25f, size * 0.17f);
            Color c = Canvas.Over(new Color(color.r, color.g, color.b, Mathf.Clamp01(0.5f - d)),
                new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - shine) * 0.85f * Mathf.Clamp01(0.5f - d)));
            return c;
        });
    }

    // Soul-Tether: a graveyard gathered at night: two rows of tombstones with glowing crosses on a mound of earth, mist
    // drifting over their feet under a pale moon, and one more tombstone flying in to join them on a ghostly trail.
    private static void SoulTetherPicture(Canvas canvas)
    {
        Color spirit = new(0.5f, 0.9f, 1f);
        Color pale = new(0.85f, 1f, 1f);
        Glow(canvas, 32f, 42f, 32f, new Color(0.4f, 0.75f, 1f), 0.45f);

        // The moon.
        Glow(canvas, 52f, 9f, 11f, pale, 0.45f);
        canvas.Paint((x, y) => new Color(0.92f, 0.97f, 1f, Mathf.Clamp01(0.5f - Circle(x, y, 52f, 9f, 4.5f)) * 0.95f));

        // The mound of earth they stand on.
        canvas.Paint((x, y) =>
        {
            float ex = (x - 32f) / 33f, ey = (y - 64f) / 13f;
            float inside = 1f - Mathf.Sqrt(ex * ex + ey * ey);
            float light = Mathf.Lerp(1.2f, 0.7f, Mathf.Clamp01((y - 51f) / 12f));
            return new Color(0.2f * light, 0.18f * light, 0.15f * light, Mathf.Clamp01(inside * 12f));
        });

        // The back row, farther away: smaller and dimmer.
        Gravestone(canvas, 17f, 33f, 6f, -8f, spirit, 0.7f, 0.25f);
        Gravestone(canvas, 32f, 29f, 6.5f, 4f, spirit, 0.7f, 0.25f);
        Gravestone(canvas, 47f, 33f, 6f, 10f, spirit, 0.7f, 0.25f);
        // The front row.
        Gravestone(canvas, 10f, 45f, 8f, -12f, spirit, 1f, 0.4f);
        Gravestone(canvas, 32f, 44f, 9.5f, 0f, spirit, 1f, 0.45f);
        Gravestone(canvas, 54f, 45f, 8f, 13f, spirit, 1f, 0.4f);

        // Mist drifting low over their feet.
        canvas.Paint((x, y) =>
        {
            float top = 52f + Mathf.Sin(x * 0.22f) * 1.5f + Mathf.Sin(x * 0.07f + 1f) * 1.5f;
            float a = Mathf.Clamp01((y - top) / 6f) * Mathf.Clamp01((64f - y) / 5f);
            return new Color(pale.r, pale.g, pale.b, a * 0.3f);
        });

        // One more, called in from far away.
        SoulTrail(canvas, new Vector2(-2f, 24f), new Vector2(1f, 4f), new Vector2(14f, 7f), spirit, pale);
        Gravestone(canvas, 16f, 9f, 5.5f, 30f, spirit, 1f, 0.55f);

        Sparkle(canvas, 36f, 7f, 2.5f, spirit);
        Sparkle(canvas, 62f, 22f, 2f, spirit);
    }

    // A trail of ghostly light along a curve from start (faint) through bend to end (bright), thicker toward its end.
    private static void SoulTrail(Canvas canvas, Vector2 start, Vector2 bend, Vector2 end, Color glow, Color core)
    {
        const int steps = 20;
        Vector2[] points = new Vector2[steps + 1];
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            points[i] = Vector2.Lerp(Vector2.Lerp(start, bend, t), Vector2.Lerp(bend, end, t), t);
        }
        canvas.Paint((x, y) =>
        {
            float best = float.MaxValue, along = 0f;
            for (int i = 0; i < steps; i++)
            {
                float d = Capsule(x, y, points[i].x, points[i].y, points[i + 1].x, points[i + 1].y, 0f);
                if (d < best)
                {
                    best = d;
                    along = (i + 0.5f) / steps;
                }
            }
            float width = Mathf.Lerp(0.3f, 2.4f, along);
            float halo = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Max(best - width, 0f) / 4.5f), 2f) * 0.7f * along;
            Color line = new(core.r, core.g, core.b, Mathf.Clamp01(width + 0.5f - best) * Mathf.Lerp(0.15f, 0.9f, along));
            return Canvas.Over(new Color(glow.r, glow.g, glow.b, halo), line);
        });
    }

    // A small grey tombstone with a glowing carved cross, turned by angle degrees, in a soft glow, as Grave-Bind's.
    // shade darkens the stone (farther ones), glowStrength is how bright the glow around it is.
    private static void Gravestone(Canvas canvas, float cx, float cy, float size, float angle, Color glow, float shade = 1f,
        float glowStrength = 0.55f)
    {
        Glow(canvas, cx, cy, size * 2.2f, glow, glowStrength);
        float sin = Mathf.Sin(-angle * Mathf.Deg2Rad), cos = Mathf.Cos(-angle * Mathf.Deg2Rad);
        // The point turned back, so the stone is drawn upright around (0, 0).
        Vector2 Upright(float x, float y) => new((x - cx) * cos - (y - cy) * sin, (x - cx) * sin + (y - cy) * cos);
        float half = size * 0.75f;
        Shape(canvas, (x, y) =>
        {
            Vector2 p = Upright(x, y);
            return Mathf.Min(Box(p.x, p.y, -half, -size * 0.3f, half, size, size * 0.12f), Circle(p.x, p.y, 0f, -size * 0.3f, half));
        }, (x, y, depth) =>
        {
            Vector2 p = Upright(x, y);
            float light = Mathf.Lerp(1.1f, 0.7f, Mathf.Clamp01((p.y + size) / (size * 2f))) * (0.75f + 0.25f * Mathf.Clamp01(depth / 1.5f));
            return new Color(0.56f * light * shade, 0.58f * light * shade, 0.62f * light * shade, 1f);
        }, 0.5f);
        canvas.Paint((x, y) =>
        {
            Vector2 p = Upright(x, y);
            float r = Mathf.Max(0.45f, size * 0.09f);
            float d = Strokes(p.x, p.y, r, new Vector4(0f, -size * 0.55f, 0f, size * 0.6f), new Vector4(-size * 0.4f, -size * 0.15f, size * 0.4f, -size * 0.15f));
            return new Color(0.4f, 0.85f, 1f, Mathf.Clamp01(0.5f - d) * 0.9f * Mathf.Lerp(0.6f, 1f, shade));
        });
    }

    // Fortune's Boon: a four-leaf clover (luck) in a golden glow, the orange "+" of lucky crafts and harvests bursting
    // out beside it.
    private static void FortunesBoonPicture(Canvas canvas)
    {
        Color gold = new(1f, 0.72f, 0.18f);
        const float cx = 28f, cy = 32f, leaf = 9.5f, tilt = 20f;
        Glow(canvas, cx, cy, 31f, new Color(1f, 0.75f, 0.25f), 0.55f);
        Glow(canvas, cx, cy, 18f, new Color(0.6f, 1f, 0.4f), 0.35f);
        // The stem, curving down from the clover's heart.
        Vector2[] stem = [new(cx, cy), new(cx + 3f, cy + 9f), new(cx + 7f, cy + 17f), new(cx + 13f, cy + 25f)];
        Shape(canvas, (x, y) => Polyline(x, y, stem, 1.6f), (x, y, depth) =>
            Color.Lerp(new Color(0.12f, 0.36f, 0.1f, 1f), new Color(0.4f, 0.72f, 0.25f, 1f), Mathf.Clamp01(depth / 1.2f)));
        for (int i = 0; i < 4; i++) CloverLeaf(canvas, cx, cy, leaf, tilt + i * 90f);
        // A gold gleam where the leaves meet.
        Glow(canvas, cx, cy, 4f, new Color(1f, 0.95f, 0.6f), 0.7f);
        Bonus(canvas, 52f, 12f, 6.5f);
        Bonus(canvas, 58f, 26f, 4.5f);
        Bonus(canvas, 43f, 4.5f, 3.5f);
        Sparkle(canvas, 8f, 10f, 3f, gold);
        Sparkle(canvas, 9f, 52f, 2.5f, gold);
        Sparkle(canvas, 55f, 44f, 3f, gold);
    }

    // One heart-shaped clover leaf, its point at (cx, cy), reaching out turned by angle degrees (0: up): deep green at
    // the point, bright at the rim, a pale vein down its middle and a gleam.
    private static void CloverLeaf(Canvas canvas, float cx, float cy, float size, float angle)
    {
        // The upright heart's point sits at hy + 1.05 size: put it on the clover's heart.
        float hy = cy - 1.05f * size + 0.6f;
        Shape(canvas, (x, y) =>
        {
            Vector2 p = Unturn(x, y, cx, cy, angle);
            return Heart(p.x, p.y, cx, hy, size);
        }, (x, y, depth) =>
        {
            Vector2 p = Unturn(x, y, cx, cy, angle);
            float t = Mathf.Clamp01((cy - p.y) / (1.9f * size));
            Color color = Color.Lerp(new Color(0.1f, 0.42f, 0.12f), new Color(0.42f, 0.86f, 0.3f), t);
            float edge = Mathf.Lerp(0.72f, 1f, Mathf.Clamp01(depth / (0.22f * size)));
            color = new Color(color.r * edge, color.g * edge, color.b * edge, 1f);
            float vein = Capsule(p.x, p.y, cx, cy - 1.5f, cx, hy - 0.1f * size, 0.45f);
            color = Canvas.Over(color, new Color(0.75f, 1f, 0.6f, Mathf.Clamp01(0.5f - vein) * 0.55f));
            float gleam = Circle(p.x, p.y, cx - 0.5f * size, hy - 0.4f * size, 0.16f * size);
            return Canvas.Over(color, new Color(1f, 1f, 0.9f, Mathf.Clamp01(0.5f - gleam) * 0.6f));
        }, 0.7f);
    }

    // The game's bonus "+": thick, in the orange of its lucky craft popups, outlined dark to stand out.
    private static void Bonus(Canvas canvas, float cx, float cy, float size)
    {
        float r = Mathf.Max(1f, size * 0.32f);
        Shape(canvas, (x, y) => Mathf.Min(Capsule(x, y, cx - size, cy, cx + size, cy, r), Capsule(x, y, cx, cy - size, cx, cy + size, r)),
            (x, y, depth) => Color.Lerp(new Color(0.45f, 0.18f, 0.02f, 1f), Color.Lerp(new Color(1f, 0.63f, 0.24f, 1f), new Color(1f, 0.9f, 0.6f, 1f),
                Mathf.Clamp01((cy - y) / size * 0.5f + 0.3f)), Mathf.Clamp01(depth / 0.9f)), 0.8f);
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

    // Eldhrímnir: the cauldron the boar Saehrímnir is cooked in each night, as fresh again by morning: a round iron pot on
    // short legs over a log fire, a haunch standing in its glowing stew, steam curling up from it.
    private static void EldhrimnirPicture(Canvas canvas)
    {
        Color gold = new(1f, 0.72f, 0.3f);
        Color steam = new(1f, 0.95f, 0.85f);
        Glow(canvas, 32f, 58f, 26f, new Color(1f, 0.45f, 0.1f), 0.55f);
        Glow(canvas, 32f, 24f, 24f, gold, 0.5f);
        // Steam: three wisps curling up from the stew, fading as they rise.
        foreach ((float x0, float phase, float top) in new[] { (22f, 0f, 7f), (32f, 2f, 3f), (42f, 4f, 8f) })
            canvas.Paint((x, y) =>
            {
                if (y < top || y > 23f) return Color.clear;
                float rise = Mathf.Clamp01((23f - y) / (23f - top));
                float cx = x0 + Mathf.Sin(y * 0.42f + phase) * (1f + 2f * rise);
                float d = Mathf.Abs(x - cx) - (1.1f + 0.6f * rise);
                return new Color(steam.r, steam.g, steam.b, Mathf.Clamp01(0.5f - d) * Mathf.Lerp(0.75f, 0f, rise));
            });
        // The handles, iron rings on either side under the rim.
        Shape(canvas, (x, y) => Mathf.Min(Mathf.Abs(Circle(x, y, 11f, 31f, 4f)) - 1.1f, Mathf.Abs(Circle(x, y, 53f, 31f, 4f)) - 1.1f),
            (x, y, depth) => new Color(0.3f, 0.3f, 0.33f, 1f), 0.3f);
        // The pot: a round belly cut flat at its rim, lit from the top left.
        Shape(canvas, (x, y) => Mathf.Max(Ellipse(x, y, 32f, 32f, 20f, 18f), 26f - y), (x, y, depth) =>
        {
            float light = Mathf.Lerp(0.62f, 0.22f, Mathf.Clamp01((x - 18f) / 30f * 0.6f + (y - 28f) / 22f * 0.6f));
            light *= 0.8f + 0.2f * Mathf.Clamp01(depth / 3f);
            // The fire's glow on its bottom.
            float fire = Mathf.Clamp01((y - 40f) / 10f) * 0.5f;
            return Color.Lerp(new Color(light * 0.95f, light, light * 1.1f, 1f), new Color(0.9f, 0.4f, 0.12f, 1f), fire);
        });
        // The rim, and the stew within it, glowing gold.
        Shape(canvas, (x, y) => Ellipse(x, y, 32f, 26f, 21.5f, 4.5f), (x, y, depth) =>
        {
            float light = Mathf.Lerp(0.75f, 0.4f, Mathf.Clamp01((x - 12f) / 40f));
            return new Color(light * 0.95f, light, light * 1.08f, 1f);
        }, 0.3f);
        Shape(canvas, (x, y) => Ellipse(x, y, 32f, 26f, 18f, 3f), (x, y, depth) =>
        {
            float heat = Mathf.Clamp01(depth / 2.5f);
            return Color.Lerp(new Color(0.75f, 0.38f, 0.12f, 1f), new Color(1f, 0.85f, 0.45f, 1f), heat);
        }, 0f);
        // The haunch: meat around a bone standing up out of the stew, the bone's knob at its top.
        Shape(canvas, (x, y) => Mathf.Min(Capsule(x, y, 40f, 13f, 45.5f, 5.5f, 1.4f),
            Mathf.Min(Circle(x, y, 44.5f, 4.5f, 1.8f), Circle(x, y, 47f, 6f, 1.8f))), (x, y, depth) =>
        {
            float light = 0.8f + 0.2f * Mathf.Clamp01(depth / 1.2f);
            return new Color(0.96f * light, 0.92f * light, 0.8f * light, 1f);
        }, 0.3f);
        Shape(canvas, (x, y) => Mathf.Max(Capsule(x, y, 35f, 25f, 40.5f, 14f, 4.2f), y - 26f), (x, y, depth) =>
        {
            float light = Mathf.Lerp(1.1f, 0.65f, Mathf.Clamp01((x - 33f) / 12f)) * (0.8f + 0.2f * Mathf.Clamp01(depth / 2f));
            return new Color(0.68f * light, 0.3f * light, 0.16f * light, 1f);
        }, 0.4f);
        // Its short legs, and two crossed logs under it, glowing at the ends, the flames standing on them.
        Shape(canvas, (x, y) => Mathf.Min(Capsule(x, y, 20f, 45f, 17f, 53f, 1.8f), Capsule(x, y, 44f, 45f, 47f, 53f, 1.8f)),
            (x, y, depth) => new Color(0.24f, 0.24f, 0.27f, 1f), 0.3f);
        Shape(canvas, (x, y) => Mathf.Min(Capsule(x, y, 15f, 61f, 47f, 57.5f, 2f), Capsule(x, y, 17f, 57.5f, 49f, 61f, 2f)), (x, y, depth) =>
        {
            float ember = Mathf.Clamp01(1f - Mathf.Min(Mathf.Abs(x - 15.5f), Mathf.Abs(x - 48.5f)) / 3f);
            float light = 0.7f + 0.3f * Mathf.Clamp01(depth / 1.2f);
            return Color.Lerp(new Color(0.42f * light, 0.27f * light, 0.15f * light, 1f), new Color(1f, 0.55f, 0.15f, 1f), ember * 0.8f);
        }, 0.4f);
        Flame(canvas, 24f, 60f, 6f, 9f, -2f);
        Flame(canvas, 40f, 60f, 6f, 9f, 2f);
        Flame(canvas, 32f, 60f, 8f, 11f);
        Sparkle(canvas, 9f, 13f, 3f, gold);
        Sparkle(canvas, 55f, 18f, 3.5f, gold);
    }

    // Rally-Horn: a war horn curving up from its tip at the lower left to its flared mouth, ivory banded in gold, its call
    // rolling out of the mouth in glowing waves.
    private static readonly Vector2 HornTip = new(7f, 44f);
    private static readonly Vector2 HornBend = new(27f, 47f);
    private static readonly Vector2 HornMouth = new(39f, 27f);
    private const int HornSteps = 20;

    private static Vector2 RallyHornPoint(float t) =>
        (1f - t) * (1f - t) * HornTip + 2f * (1f - t) * t * HornBend + t * t * HornMouth;

    private static float RallyHornRadius(float t) => Mathf.Lerp(1.3f, 7.5f, Mathf.Pow(t, 1.6f));

    // The distance from the horn, and how far along it the nearest part is (0 at the tip, 1 at the mouth).
    private static float RallyHornShape(float x, float y, out float along)
    {
        float d = float.MaxValue;
        along = 0f;
        Vector2 previous = RallyHornPoint(0f);
        for (int k = 1; k <= HornSteps; k++)
        {
            float t = k / (float)HornSteps;
            Vector2 point = RallyHornPoint(t);
            float part = Capsule(x, y, previous.x, previous.y, point.x, point.y, RallyHornRadius(t - 0.5f / HornSteps));
            if (part < d)
            {
                d = part;
                along = t - 0.5f / HornSteps;
            }
            previous = point;
        }
        return d;
    }

    private static void RallyHornPicture(Canvas canvas)
    {
        Color amber = new(1f, 0.66f, 0.22f);
        Color white = new(1f, 0.95f, 0.8f);
        Vector2 mouthDirection = (HornMouth - HornBend).normalized;
        Vector2 across = new(-mouthDirection.y, mouthDirection.x);
        Vector2 origin = HornMouth + mouthDirection * 1f;
        Glow(canvas, 42f, 24f, 30f, amber, 0.5f);
        // The call: three arcs rolling out of the mouth, fainter the farther out.
        foreach ((float radius, float strength) in new[] { (10f, 0.9f), (16f, 0.7f), (22f, 0.5f) })
            canvas.Paint((x, y) =>
            {
                Vector2 offset = new(x - origin.x, y - origin.y);
                float distance = offset.magnitude;
                if (distance < 1f) return Color.clear;
                float spread = Mathf.Clamp01(1f - Vector2.Angle(offset, mouthDirection) / 50f);
                if (spread <= 0f) return Color.clear;
                float d = Mathf.Abs(distance - radius) - 0.9f;
                float halo = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Max(d, 0f) / 3.5f), 2f) * 0.7f;
                Color glow = new(amber.r, amber.g, amber.b, halo * strength * spread);
                return Canvas.Over(glow, new Color(white.r, white.g, white.b, Mathf.Clamp01(0.5f - d) * strength * Mathf.Sqrt(spread)));
            });
        // The horn, cut flat at its mouth: ivory, darker toward its tip, lit from above, with two gold bands.
        Shape(canvas, (x, y) => Mathf.Max(RallyHornShape(x, y, out _), Vector2.Dot(new Vector2(x, y) - HornMouth, mouthDirection)),
            (x, y, depth) =>
            {
                RallyHornShape(x, y, out float along);
                float radius = Mathf.Max(RallyHornRadius(along), 0.5f);
                float round = Mathf.Clamp01(depth / radius);
                float facing = Mathf.Clamp((RallyHornPoint(along).y - y) / radius, -1f, 1f);
                float light = (0.8f + 0.22f * facing) * (0.78f + 0.22f * round) * Mathf.Lerp(0.72f, 1.05f, along);
                bool band = Mathf.Abs(along - 0.45f) < 0.028f || Mathf.Abs(along - 0.72f) < 0.028f;
                return band
                    ? new Color(1f * light, 0.72f * light, 0.25f * light, 1f)
                    : new Color(0.98f * light, 0.92f * light, 0.76f * light, 1f);
            });
        // The gold rim of the mouth, seen a little from the side, and the dark opening inside it.
        Func<float, float, float, float, float> mouth = (x, y, u, v) =>
        {
            Vector2 p = new(x - HornMouth.x, y - HornMouth.y);
            float a = Vector2.Dot(p, across) / u, b = Vector2.Dot(p, mouthDirection) / v;
            return (Mathf.Sqrt(a * a + b * b) - 1f) * Mathf.Min(u, v);
        };
        Shape(canvas, (x, y) => mouth(x, y, 8.3f, 3f), (x, y, depth) =>
        {
            float light = 0.8f + 0.3f * Mathf.Clamp01((HornMouth.y - y + 3f) / 6f);
            return new Color(1f * light, 0.74f * light, 0.28f * light, 1f);
        }, 0.3f);
        Shape(canvas, (x, y) => mouth(x, y, 6.4f, 1.9f), (x, y, depth) => new Color(0.22f, 0.13f, 0.07f, 1f), 0f);
        Sparkle(canvas, 12f, 14f, 3f, amber);
        Sparkle(canvas, 54f, 52f, 3.5f, amber);
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

    // Fair-Wind: a ship's striped sail trimmed to the wind, turned on its mast to catch gusts like the Gale-Breath's
    // sweeping in from the side, never head on.
    private static void FairWindPicture(Canvas canvas)
    {
        Color air = new(0.6f, 0.85f, 1f);
        Color white = new(0.95f, 0.98f, 1f);
        Glow(canvas, 38f, 30f, 30f, new Color(0.5f, 0.75f, 1f), 0.4f);
        // The hull, its prow raised at the front (right), lit along the gunwale.
        Shape(canvas, (x, y) => Polygon(x, y, FairWindHull), (x, y, depth) =>
        {
            float light = Mathf.Lerp(1f, 0.6f, Mathf.Clamp01((y - 49f) / 9f)) * (0.8f + 0.2f * Mathf.Clamp01(depth / 1.5f));
            return new Color(0.5f * light, 0.32f * light, 0.18f * light, 1f);
        });
        // The mast and the yard, swung round on it: its far end (right) higher.
        Shape(canvas, (x, y) => Mathf.Min(Capsule(x, y, 40f, 5f, 40f, 51f, 1.4f), Capsule(x, y, 26f, 13.5f, 54f, 6.5f, 1.3f)), (x, y, depth) =>
        {
            float light = 0.75f + 0.25f * Mathf.Clamp01(depth / 1f);
            return new Color(0.45f * light, 0.3f * light, 0.17f * light, 1f);
        });
        // The sail hanging from the turned yard, seen a little edge-on: narrower, bellied out, stripes bending with it.
        Shape(canvas, TrimmedSail, (x, y, depth) =>
        {
            float u = Mathf.Clamp((x - 40f) / TrimmedSailHalf(y), -1f, 1f);
            float t = (u + 1f) * 2.5f;
            float stripe = Mathf.Repeat(t, 2f);
            float red = Mathf.Clamp01(Mathf.Min(stripe - 1f, 2f - stripe) * 6f + 0.5f);
            Color cloth = Color.Lerp(new Color(0.93f, 0.87f, 0.74f), new Color(0.74f, 0.2f, 0.13f), red);
            // Lit on its near (left) side, where the wind fills it.
            float light = (0.72f + 0.33f * (1f - u * u) + 0.08f * -u) * Mathf.Lerp(1.05f, 0.85f, Mathf.Clamp01((y - 12f) / 36f));
            light *= 0.85f + 0.15f * Mathf.Clamp01(depth / 1.5f);
            return new Color(cloth.r * light, cloth.g * light, cloth.b * light, 1f);
        }, 0.5f);
        // The gusts come in slanting, from above and aside.
        foreach ((float y, float x0, float x1, float r, float width) in new[] { (23f, 3f, 17f, 3f, 1.2f), (36f, 2f, 19f, 3.8f, 1.5f) })
            GlowLine(canvas, (px, py) => Slanted(px, py, x1, y, (sx, sy) => Gust(sx, sy, y, x0, x1, r, width)), air, white, 3.5f, 0.7f);
        Sparkle(canvas, 9f, 49f, 2.5f, air);
        Sparkle(canvas, 59f, 23f, 3f, air);
    }

    // The trimmed sail's top runs along the turned yard.
    private static float TrimmedSailTop(float x) => 13.5f - (x - 26f) * (7f / 28f) + 1f;

    private static float TrimmedSailHalf(float y) => 0.82f * FairWindSailHalf(y);

    private static float TrimmedSail(float x, float y)
    {
        float u = Mathf.Clamp((x - 40f) / 14f, -1.2f, 1.2f);
        float bottom = 45f - (x - 40f) * 0.1f + 3f * (1f - u * u);
        return Mathf.Max(Mathf.Max(TrimmedSailTop(x) - y, y - bottom), Mathf.Abs(x - 40f) - TrimmedSailHalf(y));
    }

    // A shape drawn turned by 12 degrees around (cx, cy), its tail raised: wind slanting down into the sail.
    private static float Slanted(float x, float y, float cx, float cy, Func<float, float, float> shape)
    {
        const float Angle = 12f * Mathf.Deg2Rad;
        float c = Mathf.Cos(Angle), s = Mathf.Sin(Angle);
        float dx = x - cx, dy = y - cy;
        return shape(cx + dx * c + dy * s, cy - dx * s + dy * c);
    }

    // The Fair-Wind icon as it was first drawn (the Tailwind Rite's): the sail square to the ship, the wind straight from
    // behind. Kept to switch back to.
    private static void FairWindPictureOriginal(Canvas canvas)
    {
        Color air = new(0.6f, 0.85f, 1f);
        Color white = new(0.95f, 0.98f, 1f);
        Glow(canvas, 38f, 30f, 30f, new Color(0.5f, 0.75f, 1f), 0.4f);
        // The hull, its prow raised at the front (right), lit along the gunwale.
        Shape(canvas, (x, y) => Polygon(x, y, FairWindHull), (x, y, depth) =>
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
        Shape(canvas, FairWindSail, (x, y, depth) =>
        {
            float u = Mathf.Clamp((x - 40f) / FairWindSailHalf(y), -1f, 1f);
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

    private static readonly Vector2[] FairWindHull =
        [new(9f, 46f), new(14f, 50f), new(54f, 50f), new(59f, 43f), new(61f, 44f), new(57f, 56f), new(50f, 59f), new(20f, 59f), new(12f, 54f)];

    // Half the sail's width at y: it bellies out most halfway down.
    private static float FairWindSailHalf(float y) => 14f + 3f * Mathf.Sin(Mathf.PI * Mathf.Clamp01((y - 11f) / 34f));

    // The sail hanging from the yard, its foot sagging in the middle.
    private static float FairWindSail(float x, float y)
    {
        float u = (x - 40f) / 17f;
        float bottom = 44f + 3f * (1f - u * u);
        return Mathf.Max(Mathf.Max(11f - y, y - bottom), Mathf.Abs(x - 40f) - FairWindSailHalf(y));
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

    // Ley-Survey: the land read for a Reckoning. A dashed red ring lies on the ground, seen from above at a slant; ley lines
    // glow from a standing stone at its middle out to the ring; the sea in one side of it is washed red, and a red beam
    // rises over something in the way.
    private static void LeySurveyPicture(Canvas canvas)
    {
        const float cx = 32f, cy = 40f, rx = 27f, ry = 14f;
        Glow(canvas, cx, cy, 30f, new Color(0.45f, 0.85f, 1f), 0.35f);
        canvas.Paint((x, y) =>
        {
            float dx = (x - cx) / rx, dy = (y - cy) / ry;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float angle = Mathf.Atan2(dy, dx);
            Color color = new(0f, 0f, 0f, 0f);
            // Ground inside the ring, dark and earthy; the sea on its left side, washed red.
            if (r < 1f)
            {
                float sea = Mathf.Clamp01((cx - 6f - x - Mathf.Sin(y * 0.5f) * 2f) / 4f);
                Color ground = new(0.24f, 0.2f, 0.14f, 0.85f * Mathf.Clamp01((1f - r) * 6f));
                Color water = new(0.75f, 0.2f, 0.2f, ground.a);
                color = Color.Lerp(ground, water, sea);
            }
            // Ley lines: six thin glowing spokes out to the ring.
            float spoke = Mathf.Abs(Mathf.Repeat(angle / (Mathf.PI * 2f) * 6f + 0.25f, 1f) - 0.5f) * 2f;
            float line = Mathf.Clamp01(1f - (1f - spoke) * r * 14f) * Mathf.Clamp01((1f - r) * 8f) * Mathf.Clamp01(r * 5f);
            color = Canvas.Over(color, new Color(0.55f, 0.95f, 1f, line * 0.9f));
            // The ring, dashed.
            float d = Mathf.Abs(r - 1f) * ry - 1.2f;
            float dash = Mathf.Repeat(angle / (Mathf.PI * 2f) * 16f, 1f) < 0.62f ? 1f : 0f;
            float glow = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Max(d, 0f) / 3f), 2f) * 0.5f;
            return Canvas.Over(color, new Color(1f, 0.22f, 0.18f, Mathf.Max(Mathf.Clamp01(0.5f - d) * dash, glow)));
        });
        // A red beam rising over something in the way.
        canvas.Paint((x, y) =>
        {
            float across = Mathf.Abs(x - 47f);
            float up = Mathf.Clamp01((44f - y) / 40f);
            float a = Mathf.Clamp01(2.2f - across) * (y <= 44f ? 1f - up * 0.85f : 0f);
            float halo = Mathf.Clamp01(1f - across / 5f) * 0.3f * (y <= 44f ? 1f - up : 0f);
            return new Color(1f, 0.3f, 0.25f, Mathf.Max(a, halo));
        });
        // The standing stone at the middle, its rune lit.
        Shape(canvas, (x, y) => Mathf.Min(Box(x, y, 28f, 22f, 36f, 42f, 2.5f), Circle(x, y, 32f, 23f, 4f)), (x, y, depth) =>
        {
            float light = Mathf.Lerp(1.05f, 0.7f, Mathf.Clamp01((x - 27f) / 10f)) * (0.8f + 0.2f * Mathf.Clamp01(depth / 1.5f));
            return new Color(0.55f * light, 0.57f * light, 0.6f * light, 1f);
        });
        Glow(canvas, 32f, 31f, 6f, new Color(0.55f, 0.95f, 1f), 0.8f);
        canvas.Paint((x, y) =>
        {
            float rune = Mathf.Min(Capsule(x, y, 32f, 26f, 32f, 37f, 0.7f), Mathf.Min(Capsule(x, y, 32f, 29f, 35f, 26f, 0.6f), Capsule(x, y, 32f, 32f, 35f, 29f, 0.6f)));
            return new Color(0.7f, 1f, 1f, Mathf.Clamp01(0.5f - rune));
        });
        Sparkle(canvas, 47f, 6f, 3f, new Color(1f, 0.5f, 0.4f));
    }

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

    // Sun-Fall: the sun sinking at the end of its arc across the sky, the moon rising at the other, their path traced in
    // gold: day and night back on their own course.
    private static void SunFallPicture(Canvas canvas)
    {
        Color gold = new(1f, 0.8f, 0.35f);
        Glow(canvas, 32f, 40f, 34f, new Color(1f, 0.55f, 0.2f), 0.4f);
        // The path: a dotted arc over the horizon.
        canvas.Paint((x, y) =>
        {
            float dx = x - 32f, dy = y - 50f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float angle = Mathf.Atan2(-dy, dx);
            if (angle < 0.25f || angle > Mathf.PI - 0.25f) return Color.clear;
            float dots = Mathf.Clamp01(Mathf.Cos(angle * 22f) * 2f);
            float d = Mathf.Abs(r - 27f) - 0.7f;
            return new Color(gold.r, gold.g, gold.b, Mathf.Clamp01(0.5f - d) * dots * 0.75f);
        });
        // The moon rising on the left.
        canvas.Paint((x, y) =>
        {
            float outer = Circle(x, y, 12f, 34f, 6.5f);
            float bite = Circle(x, y, 15.5f, 31f, 5.5f);
            float d = Mathf.Max(outer, -bite);
            float glow = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Max(outer, 0f) / 6f), 2f) * 0.45f;
            return Canvas.Over(new Color(0.6f, 0.8f, 1f, glow), new Color(0.95f, 0.96f, 0.9f, Mathf.Clamp01(0.5f - d)));
        });
        // The sun sinking on the right, half under the horizon.
        Sun(canvas, 51f, 46f, 0.42f);
        // The horizon: dark land, its top lit gold, fading out to the sides and bottom.
        Shape(canvas, (x, y) => Polygon(x, y, SunFallLand), (x, y, depth) =>
        {
            float rim = Mathf.Clamp01(1f - depth / 1.3f) * Mathf.Clamp01((54f - y) / 4f);
            float light = Mathf.Lerp(1f, 0.6f, Mathf.Clamp01((y - 48f) / 16f));
            return Color.Lerp(new Color(0.2f * light, 0.15f * light, 0.25f * light, 1f), new Color(1f, 0.72f, 0.3f, 1f), rim * 0.7f);
        }, 0f);
        Sparkle(canvas, 26f, 14f, 2f, gold);
        Sparkle(canvas, 40f, 19f, 1.6f, gold);
        canvas.FadeEdges(9f, top: false, corner: 16f);
    }

    private static readonly Vector2[] SunFallLand =
        [new(-6f, 72f), new(-6f, 50f), new(12f, 48f), new(26f, 49.5f), new(40f, 48.5f), new(54f, 50f), new(70f, 48f), new(70f, 72f)];

    // Nature's Rebound: two leafy vines chasing each other round in a circle, a sprout rising in the middle.
    private static void NaturesReboundPicture(Canvas canvas)
    {
        Color violet = new(0.72f, 0.5f, 1f);
        Color vine = new(0.42f, 0.68f, 0.3f);
        Glow(canvas, 32f, 32f, 32f, violet, 0.5f);
        foreach ((float from, float to) in new[] { (30f, 165f), (210f, 345f) })
        {
            GlowLine(canvas, (x, y) => Arc(x, y, 32f, 32f, 22f, from, to, 1.3f), violet, vine, 3.5f, 0.55f);
            // Leaves along it, the last one pointing on round the circle.
            for (float a = from + 25f; a < to - 10f; a += 40f)
            {
                float angle = a;
                Vector2 at = new(32f + 22f * Mathf.Cos(angle * Mathf.Deg2Rad), 32f - 22f * Mathf.Sin(angle * Mathf.Deg2Rad));
                Shape(canvas, (x, y) => Leaf(x, y, at.x, at.y, -angle - 35f, 7f), (x, y, depth) => LeafColor(depth), 0.3f);
            }
            float end = to * Mathf.Deg2Rad;
            Vector2 tip = new(32f + 22f * Mathf.Cos(end), 32f - 22f * Mathf.Sin(end));
            float tipAngle = -(to + 90f);
            Shape(canvas, (x, y) => Leaf(x, y, tip.x, tip.y, tipAngle, 9f), (x, y, depth) => LeafColor(depth), 0.3f);
        }
        // The sprout in the middle.
        Glow(canvas, 32f, 42f, 10f, violet, 0.6f);
        GlowLine(canvas, (x, y) => Polyline(x, y, [new(32f, 44f), new(32.5f, 36f), new(32f, 26f)], 1.2f), violet, vine, 2.5f, 0.4f);
        Shape(canvas, (x, y) => Mathf.Min(Leaf(x, y, 32.3f, 37f, -25f, 11f), Leaf(x, y, 32.3f, 37f, -155f, 11f)), (x, y, depth) => LeafColor(depth), 0.3f);
        Shape(canvas, (x, y) => Mathf.Min(Leaf(x, y, 32.2f, 30f, -45f, 8f), Leaf(x, y, 32.2f, 30f, -135f, 8f)), (x, y, depth) => LeafColor(depth), 0.3f);
        Shape(canvas, (x, y) => Leaf(x, y, 32f, 26.5f, -90f, 6f), (x, y, depth) => LeafColor(depth), 0.3f);
        Sparkle(canvas, 52f, 52f, 2.5f, violet);
        Sparkle(canvas, 12f, 12f, 2.5f, violet);
    }

    private static Color LeafColor(float depth)
    {
        float t = Mathf.Clamp01(depth / 2.2f);
        return new Color(Mathf.Lerp(0.3f, 0.62f, t), Mathf.Lerp(0.6f, 0.95f, t), Mathf.Lerp(0.18f, 0.38f, t), 1f);
    }

    // A leaf growing from (lx, ly), pointing degrees clockwise from the right, pointed at both ends.
    private static float Leaf(float x, float y, float lx, float ly, float degrees, float length)
    {
        Vector2 p = Unturn(x, y, lx, ly, degrees);
        float u = (p.x - lx) / length;
        float half = Mathf.Max(0.15f, Mathf.Sin(Mathf.Clamp01(u) * Mathf.PI)) * length * 0.27f;
        return Ellipse(p.x, p.y, lx + length / 2f, ly, length / 2f, half);
    }

    // An arc of a circle, from one angle to another (degrees, counterclockwise from the right), as a line of radius w.
    private static float Arc(float x, float y, float cx, float cy, float r, float from, float to, float w)
    {
        float angle = Mathf.Atan2(-(y - cy), x - cx) * Mathf.Rad2Deg;
        if (angle < 0f) angle += 360f;
        if (angle >= from && angle <= to) return Mathf.Abs(Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r) - w;
        float a0 = from * Mathf.Deg2Rad, a1 = to * Mathf.Deg2Rad;
        return Mathf.Min(Circle(x, y, cx + r * Mathf.Cos(a0), cy - r * Mathf.Sin(a0), w), Circle(x, y, cx + r * Mathf.Cos(a1), cy - r * Mathf.Sin(a1), w));
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

    // Elder-Sprout: an old cut stump, its roots gripping the ground, a young sapling glowing green rising from its rings,
    // golden motes of the ancient seeds drifting around it.
    private static void ElderSproutPicture(Canvas canvas)
    {
        Color sprout = new(0.45f, 1f, 0.4f);
        Color seed = new(1f, 0.85f, 0.4f);
        Glow(canvas, 32f, 30f, 30f, sprout, 0.55f);

        // The stump: bark with grooves, roots flaring at its foot, the cut top pale with rings.
        Shape(canvas, (x, y) => Mathf.Min(Mathf.Min(Box(x, y, 17f, 44f, 47f, 60f, 2f), Ellipse(x, y, 32f, 60f, 17f, 3f)),
            Mathf.Min(Polygon(x, y, ElderRootLeft), Polygon(x, y, ElderRootRight))), (x, y, depth) =>
        {
            float light = Mathf.Lerp(0.55f, 0.28f, Mathf.Clamp01((x - 12f) / 40f));
            float groove = Mathf.Abs(Mathf.Repeat(x + Mathf.Sin(y * 0.45f) * 1.3f, 5f) - 2.5f) < 0.6f ? 0.7f : 1f;
            return new Color(light * 0.78f * groove, light * 0.55f * groove, light * 0.33f * groove, 1f);
        });
        Shape(canvas, (x, y) => Ellipse(x, y, 32f, 44f, 15f, 5f), (x, y, depth) =>
        {
            float r = Mathf.Sqrt((x - 32f) * (x - 32f) / 225f + (y - 44f) * (y - 44f) / 25f);
            float ring = Mathf.Abs(Mathf.Repeat(r * 5f, 1f) - 0.5f) < 0.12f ? 0.82f : 1f;
            return new Color(0.9f * ring, 0.76f * ring, 0.54f * ring, 1f);
        }, 0f);
        // Where the sapling breaks through: a green glow in the heart of the rings.
        Glow(canvas, 32f, 44f, 8f, sprout, 0.6f);

        // The sapling: a slender stem bending up out of the heart of the stump.
        GlowLine(canvas, (x, y) => Polyline(x, y, ElderStem, 1.2f), sprout, new Color(0.45f, 0.75f, 0.3f), 3f, 0.5f);
        // Its leaves, in pairs up the stem, a bud at the top.
        foreach (Vector3 leaf in ElderLeaves)
            Shape(canvas, (x, y) => ElderLeaf(x, y, leaf), (x, y, depth) =>
            {
                float t = Mathf.Clamp01(depth / 2.2f);
                return new Color(Mathf.Lerp(0.3f, 0.62f, t), Mathf.Lerp(0.6f, 0.95f, t), Mathf.Lerp(0.18f, 0.38f, t), 1f);
            }, 0.35f);

        Sparkle(canvas, 13f, 18f, 3.5f, seed);
        Sparkle(canvas, 52f, 14f, 3f, seed);
        Sparkle(canvas, 54f, 36f, 2.5f, sprout);
        Sparkle(canvas, 10f, 38f, 2.5f, sprout);
    }

    private static readonly Vector2[] ElderStem = [new(32f, 44f), new(33f, 36f), new(31.5f, 27f), new(32.5f, 18f), new(32f, 12f)];
    private static readonly Vector2[] ElderRootLeft = [new(18f, 50f), new(10f, 61f), new(22f, 61f)];
    private static readonly Vector2[] ElderRootRight = [new(46f, 50f), new(54f, 61f), new(42f, 61f)];

    // Each leaf: where it grows from the stem (x, y) and the way it points (z, degrees clockwise from the right).
    private static readonly Vector3[] ElderLeaves =
    [
        new(33f, 35f, -25f), new(33f, 35f, -155f), new(31.8f, 26f, -35f), new(31.8f, 26f, -145f), new(32.4f, 18f, -50f),
        new(32.4f, 18f, -130f), new(32f, 12.5f, -90f)
    ];

    // A leaf: pointed at both ends, smaller toward the top of the stem.
    private static float ElderLeaf(float x, float y, Vector3 leaf)
    {
        float length = Mathf.Lerp(6f, 12f, Mathf.Clamp01((leaf.y - 12f) / 23f));
        Vector2 p = Unturn(x, y, leaf.x, leaf.y, leaf.z);
        float u = (p.x - leaf.x) / length;
        float half = Mathf.Max(0.15f, Mathf.Sin(Mathf.Clamp01(u) * Mathf.PI)) * length * 0.27f;
        return Ellipse(p.x, p.y, leaf.x + length / 2f, leaf.y, length / 2f, half);
    }

    // ---- Pictures ----

    // A shape filled by fill(x, y, depth), depth being how far inside it the pixel is, over a soft shadow a pixel down.
    // Plume-Ward: a heart falling, a great glowing feather spread beneath to catch it.
    private static void PlumeWardPicture(Canvas canvas)
    {
        Color sky = new(0.75f, 0.85f, 1f);
        // The cushion: a soft glow and a ring of down where the feather meets the fall.
        Glow(canvas, 32f, 48f, 26f, sky, 0.6f);
        GlowLine(canvas, (x, y) => Mathf.Abs(Ellipse(x, y, 32f, 51f, 20f, 5.5f)) - 0.5f, sky, new Color(0.9f, 0.95f, 1f, 0.7f), 3f, 0.45f);
        Feather(canvas, 32f, 48f, 40f, 1.3f, sky, 1.3f);
        // A heart falling, the air rushing past above it.
        for (int i = 0; i < 3; i++)
        {
            float sx = 26f + i * 6f, top = 3f + (i == 1 ? 0f : 3f);
            GlowLine(canvas, (x, y) => Capsule(x, y, sx, top, sx, top + 8f, 0.5f), sky, new Color(1f, 1f, 1f, 0.6f), 2.5f, 0.3f);
        }
        Glow(canvas, 32f, 27f, 16f, new Color(1f, 0.45f, 0.5f), 0.4f);
        HeartShape(canvas, 32f, 27f, 9f);
        Sparkle(canvas, 52f, 22f, 2.5f, sky);
        Sparkle(canvas, 11f, 32f, 2f, sky);
        canvas.FadeEdges(5f, false, 6f);
    }


    // Sentry-Sight: a ballista bolt flying up at a horned foe, its red eyes caught in a steel-blue ring of sight.
    private static void SentrySightPicture(Canvas canvas)
    {
        Color steel = new(0.55f, 0.72f, 1f);
        Glow(canvas, 44f, 20f, 22f, steel, 0.55f);
        // The foe: a dark horned head, eyes glowing red.
        Shape(canvas, (x, y) => Mathf.Min(Mathf.Min(Ellipse(x, y, 44f, 21f, 9f, 8f), Capsule(x, y, 37f, 15f, 33f, 6f, 1.6f)),
            Capsule(x, y, 51f, 15f, 55f, 6f, 1.6f)), (x, y, depth) =>
        {
            float light = 0.18f + 0.1f * Mathf.Clamp01(depth / 3f);
            return new Color(light, light * 0.95f, light * 1.1f, 1f);
        });
        Glow(canvas, 40.5f, 21f, 3.5f, new Color(1f, 0.2f, 0.1f), 0.9f);
        Glow(canvas, 47.5f, 21f, 3.5f, new Color(1f, 0.2f, 0.1f), 0.9f);
        canvas.Paint((x, y) =>
        {
            float eyes = Mathf.Min(Circle(x, y, 40.5f, 21f, 1.2f), Circle(x, y, 47.5f, 21f, 1.2f));
            return new Color(1f, 0.55f, 0.3f, Mathf.Clamp01(0.5f - eyes));
        });
        // The ring of sight around it.
        GlowLine(canvas, (x, y) => Mathf.Abs(Circle(x, y, 44f, 20f, 15f)) - 0.6f, steel, new Color(0.85f, 0.92f, 1f), 3f, 0.6f);
        foreach (float a in new[] { 0f, 90f, 180f, 270f })
        {
            float c = Mathf.Cos(a * Mathf.Deg2Rad), s = Mathf.Sin(a * Mathf.Deg2Rad);
            GlowLine(canvas, (x, y) => Capsule(x, y, 44f + c * 12f, 20f + s * 12f, 44f + c * 18f, 20f + s * 18f, 0.7f), steel,
                new Color(0.85f, 0.92f, 1f), 2.5f, 0.5f);
        }
        // The bolt, from the lower left, with a streak behind it.
        GlowLine(canvas, (x, y) => Capsule(x, y, 4f, 60f, 18f, 46f, 0.8f), steel, new Color(0.8f, 0.9f, 1f, 0.6f), 4f, 0.4f);
        Shape(canvas, (x, y) => Capsule(x, y, 12f, 52f, 33f, 31f, 1.4f), (x, y, depth) =>
        {
            float light = 0.75f + 0.3f * Mathf.Clamp01(depth / 1.2f);
            return new Color(0.55f * light, 0.38f * light, 0.22f * light, 1f);
        }, 0.5f);
        Shape(canvas, (x, y) => Polygon(x, y, [new(30f, 29f), new(39f, 25f), new(35f, 34f)]), (x, y, depth) =>
        {
            float light = 0.7f + 0.4f * Mathf.Clamp01(depth / 1.5f);
            return new Color(0.68f * light, 0.74f * light, 0.82f * light, 1f);
        }, 0.5f);
        Shape(canvas, (x, y) => Mathf.Min(Polygon(x, y, [new(12f, 52f), new(7f, 51f), new(15f, 46f)]), Polygon(x, y, [new(12f, 52f), new(13f, 57f), new(18f, 49f)])),
            (x, y, depth) => new Color(0.82f, 0.82f, 0.86f, 1f), 0.4f);
        Sparkle(canvas, 22f, 12f, 2.5f, steel);
    }

    // Taproot-Surge: an ancient root arching out of the earth, a wooden tap driven into it pouring glowing blue sap into
    // a jar that is filling up fast.
    private static void TaprootSurgePicture(Canvas canvas)
    {
        Color sap = new(0.45f, 0.95f, 0.6f);
        Glow(canvas, 32f, 38f, 32f, sap, 0.5f);
        // The earth: a low mound fading out.
        Shape(canvas, (x, y) => Ellipse(x, y, 30f, 63f, 34f, 9f), (x, y, depth) =>
        {
            float light = Mathf.Lerp(0.9f, 0.5f, Mathf.Clamp01((y - 54f) / 10f));
            float grain = 0.9f + 0.1f * Mathf.Sin(x * 2.3f) * Mathf.Sin(y * 3.1f);
            return new Color(0.38f * light * grain, 0.29f * light * grain, 0.2f * light * grain, 1f);
        }, 0.3f);
        // The root, thick and gnarled, with a few rootlets where it meets the earth.
        Shape(canvas, TaprootShape, (x, y, depth) =>
        {
            float light = Mathf.Lerp(1.15f, 0.6f, Mathf.Clamp01((y - 20f) / 36f)) * (0.7f + 0.3f * Mathf.Clamp01(depth / 2.5f));
            float bark = 0.86f + 0.14f * Mathf.Sin(depth * 2.2f);
            return new Color(0.44f * light * bark, 0.33f * light * bark, 0.27f * light * bark, 1f);
        });
        // Sap glowing through the bark's cracks.
        GlowLine(canvas, (x, y) => Mathf.Max(Polyline(x, y, [new(7f, 52f), new(12f, 41f), new(22f, 35.5f), new(31f, 36f), new(35f, 42f)], 0.5f),
            TaprootShape(x, y) > -1f ? 9f : -9f), sap, new Color(0.8f, 1f, 0.82f), 3f, 0.6f);
        // The tap: a wooden spout driven into the root's side.
        Shape(canvas, (x, y) => Capsule(x, y, 33f, 38f, 44f, 41f, 1.8f), (x, y, depth) =>
        {
            float light = 0.75f + 0.35f * Mathf.Clamp01(depth / 1.5f);
            return new Color(0.7f * light, 0.52f * light, 0.3f * light, 1f);
        }, 0.5f);
        // The jar, filling with glowing sap.
        Shape(canvas, (x, y) => Mathf.Min(Box(x, y, 41f, 48f, 57f, 62f, 3f), Box(x, y, 43f, 45f, 55f, 49f, 1f)), (x, y, depth) =>
        {
            float level = 52f + 1f * Mathf.Sin(x * 0.9f);
            Color glass = new(0.55f, 0.62f, 0.68f, 1f);
            Color full = Color.Lerp(new Color(0.3f, 0.8f, 0.45f, 1f), new Color(0.82f, 1f, 0.85f, 1f), Mathf.Clamp01((depth - 1f) / 4f));
            Color c = y > level && depth > 1.2f ? full : glass * (0.7f + 0.3f * Mathf.Clamp01(depth / 1.5f));
            c.a = 1f;
            return c;
        }, 0.5f);
        canvas.Paint((x, y) => new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - Capsule(x, y, 44f, 51f, 44f, 58f, 0.6f)) * 0.55f));
        // The stream pouring from the tap into the jar, and drops splashing up.
        GlowLine(canvas, (x, y) => Polyline(x, y, [new(44.5f, 41.5f), new(47f, 44f), new(48.5f, 51f)], 1.1f), sap, new Color(0.8f, 1f, 0.85f), 4f, 0.8f);
        foreach (Vector3 drop in new[] { new Vector3(53f, 41f, 1.4f), new Vector3(57f, 44f, 1.1f), new Vector3(40f, 44f, 1.1f) })
        {
            Glow(canvas, drop.x, drop.y, drop.z * 4f, sap, 0.6f);
            canvas.Paint((x, y) => new Color(0.88f, 1f, 0.9f, Mathf.Clamp01(0.5f - Circle(x, y, drop.x, drop.y, drop.z))));
        }
        Sparkle(canvas, 50f, 14f, 3f, sap);
        Sparkle(canvas, 9f, 22f, 2f, sap);
        canvas.FadeEdges(5f, false, 6f);
    }

    // The root's arch: up out of the earth on the left, over and back down into it, thinning toward its far end.
    private static float TaprootShape(float x, float y) =>
        Mathf.Min(Mathf.Min(
                Polyline(x, y, [new(4f, 58f), new(7f, 46f), new(13f, 37f), new(23f, 32f), new(31f, 33f), new(36f, 39f), new(39f, 48f), new(43f, 59f)],
                    Mathf.Lerp(8f, 4f, Mathf.Clamp01((x - 2f) / 36f)) + 0.6f * Mathf.Sin(x * 0.5f)),
                Polyline(x, y, [new(40f, 54f), new(44f, 60f), new(48f, 62f)], 1.6f)),
            Mathf.Min(Polyline(x, y, [new(6f, 54f), new(2f, 59f)], 1.6f), Polyline(x, y, [new(36f, 54f), new(31f, 61f)], 1.6f)));

    // Pitch-Seal: a pool of black pitch on a mound, what was dropped stuck fast in it: a sword sunk point first, pitch
    // creeping up its blade and dripping off, a coin and a bone half sunk, bubbles on the surface.
    private static void PitchSealPicture(Canvas canvas)
    {
        Color amber = new(1f, 0.62f, 0.25f);
        Glow(canvas, 32f, 42f, 30f, amber, 0.4f);
        // The mound.
        Shape(canvas, (x, y) => Ellipse(x, y, 32f, 63f, 31f, 10f), (x, y, depth) =>
        {
            float light = Mathf.Lerp(0.95f, 0.55f, Mathf.Clamp01((y - 53f) / 10f));
            float grain = 0.9f + 0.1f * Mathf.Sin(x * 2.3f) * Mathf.Sin(y * 3.1f);
            return new Color(0.45f * light * grain, 0.33f * light * grain, 0.22f * light * grain, 1f);
        }, 0.3f);
        // What sinks in it, drawn first so the pitch covers their lower parts: a coin, a bone and the sword's blade.
        Shape(canvas, (x, y) => Ellipse(x, y, 18f, 45f, 3.6f, 3.4f), (x, y, depth) =>
            new Color(1f, 0.78f, 0.25f, 1f) * (0.7f + 0.35f * Mathf.Clamp01(depth / 1.5f)), 0.4f);
        Shape(canvas, (x, y) => Mathf.Min(Capsule(x, y, 45f, 47f, 52f, 43.5f, 1.2f), Mathf.Min(Circle(x, y, 53f, 42.6f, 1.8f), Circle(x, y, 52.8f, 45f, 1.6f))),
            (x, y, depth) => new Color(0.92f, 0.88f, 0.78f, 1f), 0.4f);
        Vector2 point = new(33f, 56f), hilt = new(41f, 22f);
        Vector2 along = (hilt - point).normalized;
        Vector2 across = new(-along.y, along.x);
        Shape(canvas, (x, y) => Capsule(x, y, point.x, point.y, hilt.x, hilt.y, 1.7f), (x, y, depth) =>
        {
            float light = 0.72f + 0.35f * Mathf.Clamp01(depth / 1.2f);
            return new Color(0.66f * light, 0.7f * light, 0.76f * light, 1f);
        });
        Vector2 g0 = hilt - across * 5f, g1 = hilt + across * 5f, grip = hilt + along * 7f;
        Shape(canvas, (x, y) => Capsule(x, y, g0.x, g0.y, g1.x, g1.y, 1.3f), (x, y, depth) => new Color(0.4f, 0.3f, 0.2f, 1f), 0.4f);
        Shape(canvas, (x, y) => Capsule(x, y, hilt.x, hilt.y, grip.x, grip.y, 1.2f), (x, y, depth) => new Color(0.42f, 0.27f, 0.16f, 1f), 0.4f);
        Shape(canvas, (x, y) => Circle(x, y, grip.x + along.x * 1.4f, grip.y + along.y * 1.4f, 1.9f), (x, y, depth) =>
            new Color(0.8f, 0.6f, 0.3f, 1f) * (0.75f + 0.3f * Mathf.Clamp01(depth / 1f)), 0.4f);

        // The pitch: the pool, a sheath of it creeping up the blade, and a drip hanging off the blade.
        static float Pool(float x, float y)
        {
            float angle = Mathf.Atan2(y - 52f, x - 32f);
            return Ellipse(x, y, 32f, 52f, 25f, 7.5f) - 0.7f * Mathf.Sin(angle * 7f + 1f);
        }
        float Sheath(float x, float y) => Mathf.Max(Capsule(x, y, point.x, point.y, hilt.x, hilt.y, 2.6f), 34f + 1.6f * Mathf.Sin(x * 1.7f) - y);
        Vector2 dripTop = point + along * 18f + across * 2.2f;
        float Drip(float x, float y) => Mathf.Min(Capsule(x, y, dripTop.x, dripTop.y, dripTop.x + 0.6f, dripTop.y + 5f, 0.8f),
            Circle(x, y, dripTop.x + 0.6f, dripTop.y + 5.6f, 1.4f));
        float Pitch(float x, float y) => Mathf.Min(Pool(x, y), Mathf.Min(Sheath(x, y), Drip(x, y)));
        canvas.Paint((x, y) =>
        {
            float d = Pitch(x, y);
            if (d > 1f) return Color.clear;
            float light = Mathf.Lerp(1.3f, 0.75f, Mathf.Clamp01((x - 14f + (y - 40f) * 0.6f) / 36f));
            float rim = Mathf.Clamp01(1f + d / 1.6f);
            Color pitch = Color.Lerp(new Color(0.08f, 0.06f, 0.05f), new Color(0.32f, 0.2f, 0.1f), rim * 0.8f) * light;
            pitch.a = Mathf.Clamp01(0.5f - d);
            // A soft sheen on the pool and down the sheath.
            float poolSheen = Mathf.Clamp01(1f - Mathf.Abs(Ellipse(x, y, 30f, 51.5f, 18f, 4.5f)) / 1.2f) * Mathf.Clamp01((51f - y) / 2f);
            float sheathSheen = Mathf.Clamp01(1f - Mathf.Abs(Vector2.Dot(new Vector2(x, y) - point, across) + 1.2f) / 0.7f) * Mathf.Clamp01(-Sheath(x, y)) * Mathf.Clamp01(Pool(x, y));
            return Canvas.Over(pitch, new Color(0.95f, 0.9f, 0.8f, Mathf.Max(poolSheen, sheathSheen) * 0.5f * pitch.a));
        });
        // Bubbles on the pool.
        foreach (Vector3 bubble in new[] { new Vector3(24f, 53f, 1.3f), new Vector3(41f, 55f, 1f), new Vector3(28f, 56.5f, 0.8f) })
            canvas.Paint((x, y) => new Color(0.75f, 0.65f, 0.5f, Mathf.Clamp01(0.6f - Mathf.Abs(Circle(x, y, bubble.x, bubble.y, bubble.z)) * 1.5f) * 0.7f));
        Sparkle(canvas, 52f, 24f, 2.5f, amber);
        Sparkle(canvas, 12f, 36f, 2f, amber);
        canvas.FadeEdges(4f, false, 4f);
    }

    // Primal-Hearth: an eternal pyre burning violet in its stone bowl, wisps and embers drawn spiralling in to it.
    private static void PrimalHearthPicture(Canvas canvas)
    {
        Color violet = new(0.75f, 0.4f, 1f);
        Glow(canvas, 32f, 34f, 32f, violet, 0.6f);
        // Wisps and embers circling in.
        GlowLine(canvas, (x, y) => Arc(x, y, 32f, 32f, 22f, 20f, 160f, 0.5f), violet, new Color(0.9f, 0.75f, 1f, 0.5f), 3f, 0.3f);
        foreach (Vector3 wisp in new[] { new Vector3(10f, 22f, 3f), new Vector3(54f, 18f, 2.6f), new Vector3(16f, 8f, 2.2f), new Vector3(48f, 6f, 2f) })
        {
            Glow(canvas, wisp.x, wisp.y, wisp.z * 3f, new Color(0.7f, 0.95f, 1f), 0.7f);
            canvas.Paint((x, y) => new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - Circle(x, y, wisp.x, wisp.y, wisp.z * 0.55f))));
        }
        // The flames: violet at their edges, pale pink at heart.
        Vector2[] outer = [new(20f, 44f), new(22f, 30f), new(27f, 22f), new(30f, 10f), new(34f, 20f), new(38f, 15f), new(41f, 28f), new(44f, 44f)];
        canvas.Paint((x, y) =>
        {
            float shape = Mathf.Min(Polygon(x, y, outer), Circle(x, y, 32f, 41f, 11f));
            float heart = Mathf.Min(Circle(x, y, 32f, 40f, 6f), Capsule(x, y, 32f, 40f, 31f, 26f, 2.5f));
            float edge = Mathf.Clamp01(-shape / 5f);
            Color flame = new(Mathf.Lerp(0.55f, 0.85f, edge), Mathf.Lerp(0.15f, 0.45f, edge), 1f, Mathf.Clamp01(0.5f - shape));
            return Canvas.Over(flame, new Color(1f, 0.85f, 1f, Mathf.Clamp01(0.5f - heart)));
        });
        // The stone bowl.
        Shape(canvas, (x, y) => Mathf.Max(Ellipse(x, y, 32f, 47f, 18f, 9f), 45f - y), (x, y, depth) =>
        {
            float light = Mathf.Lerp(1.05f, 0.55f, Mathf.Clamp01((x - 14f) / 36f)) * (0.75f + 0.25f * Mathf.Clamp01(depth / 2f));
            float violetLit = Mathf.Clamp01((50f - y) / 6f) * 0.3f;
            return Color.Lerp(new Color(0.45f * light, 0.42f * light, 0.44f * light, 1f), new Color(0.7f, 0.45f, 0.95f, 1f), violetLit);
        });
        Shape(canvas, (x, y) => Box(x, y, 26f, 54f, 38f, 62f, 1.5f), (x, y, depth) => new Color(0.32f, 0.3f, 0.32f, 1f), 0.4f);
        Sparkle(canvas, 52f, 34f, 2.5f, violet);
        canvas.FadeEdges(4f, false, 5f);
    }

    // Hearth-Kindle: a campfire burning warm under a crescent moon, rest coming as stars glint above.
    private static void HearthKindlePicture(Canvas canvas)
    {
        Color warm = new(1f, 0.7f, 0.35f);
        Glow(canvas, 32f, 46f, 30f, warm, 0.7f);
        Glow(canvas, 46f, 14f, 14f, new Color(0.85f, 0.88f, 1f), 0.45f);
        // The moon.
        Shape(canvas, (x, y) => Mathf.Max(Circle(x, y, 46f, 14f, 9f), -Circle(x, y, 50f, 11f, 7.5f)), (x, y, depth) =>
        {
            float light = 0.85f + 0.15f * Mathf.Clamp01(depth / 2f);
            return new Color(1f * light, 0.95f * light, 0.8f * light, 1f);
        }, 0.2f);
        Sparkle(canvas, 22f, 9f, 2.5f, new Color(0.9f, 0.9f, 1f));
        Sparkle(canvas, 12f, 20f, 1.8f, new Color(0.9f, 0.9f, 1f));
        // Stones ringing the fire, the logs crossed in it.
        foreach (Vector2 stone in new[] { new Vector2(12f, 57f), new Vector2(21f, 60f), new Vector2(32f, 61f), new Vector2(43f, 60f), new Vector2(52f, 57f) })
            Shape(canvas, (x, y) => Ellipse(x, y, stone.x, stone.y, 5f, 3.2f), (x, y, depth) =>
            {
                float light = 0.55f + 0.35f * Mathf.Clamp01(depth / 2f);
                return new Color(0.5f * light + 0.15f, 0.46f * light + 0.08f, 0.42f * light, 1f);
            }, 0.3f);
        Shape(canvas, (x, y) => Mathf.Min(Capsule(x, y, 16f, 55f, 44f, 47f, 2.6f), Capsule(x, y, 48f, 55f, 20f, 47f, 2.6f)), (x, y, depth) =>
        {
            float light = 0.7f + 0.3f * Mathf.Clamp01(depth / 2f);
            return new Color(0.45f * light, 0.28f * light, 0.15f * light, 1f);
        }, 0.4f);
        Flame(canvas, 32f, 50f, 18f, 28f, -1.5f);
        Flame(canvas, 25f, 50f, 9f, 14f, -2f);
        Flame(canvas, 39f, 50f, 9f, 16f, 2f);
        canvas.FadeEdges(4f, false, 5f);
    }

    private static readonly Color ScaldCool = new(0.45f, 0.8f, 1f);

    // Black basalt crust with glowing lava in its cracks.
    private static Color LavaCrust(float x, float y, float shade = 1f)
    {
        float crack = Mathf.Abs(Mathf.Sin(x * 0.45f + 2.2f * Mathf.Sin(y * 0.35f)) * Mathf.Sin(y * 0.62f + x * 0.21f));
        float hot = Mathf.Clamp01(1f - crack / 0.2f);
        Color rock = new(0.15f * shade, 0.11f * shade, 0.1f * shade, 1f);
        return Color.Lerp(rock, new Color(1f, 0.5f + 0.25f * hot, 0.12f, 1f), hot);
    }

    // The Ashlands' boiling sea: red, glowing orange on its swells, darker deeper down (depth 0..1).
    private static Color BoilingSea(float x, float y, float depth)
    {
        float swell = 0.5f + 0.5f * Mathf.Sin(x * 0.55f + y * 0.7f);
        Color sea = Color.Lerp(new Color(0.85f, 0.24f, 0.07f), new Color(0.35f, 0.06f, 0.04f), depth);
        return Color.Lerp(sea, new Color(1f, 0.62f, 0.2f), swell * (1f - depth) * 0.4f);
    }

    // A wisp of steam rising at sx between top and bottom.
    private static void SteamWisp(Canvas canvas, float sx, float top, float bottom, float strength = 0.3f) =>
        GlowLine(canvas, (x, y) => Mathf.Abs(x - sx - 2.2f * Mathf.Sin(y * 0.5f + sx)) - 0.7f + Mathf.Max(0f, Mathf.Max(y - bottom, top - y)),
            new Color(1f, 0.88f, 0.8f), new Color(1f, 0.94f, 0.88f, strength), 2.5f, strength * 0.8f);

    // Scald-Drift: a round shield, half boiling sea and half lava, its iron rim frosted cool blue, steam curling off the top.
    private static void ScaldDriftPicture(Canvas canvas)
    {
        const float cx = 32f, cy = 35f, r = 21f;
        Glow(canvas, cx, cy, 31f, ScaldCool, 0.45f);
        Shape(canvas, (x, y) => Circle(x, y, cx, cy, r), (x, y, depth) =>
        {
            float d = Circle(x, y, cx, cy, 0f);
            if (d > r - 3.5f)
            {
                // The iron rim, frosted blue, with rivets.
                float angle = Mathf.Atan2(y - cy, x - cx);
                float rivet = Mathf.Abs(Mathf.Repeat(angle * 8f / Mathf.PI + 0.5f, 1f) - 0.5f);
                float light = 0.8f + 0.3f * Mathf.Clamp01((cy - y) / r);
                Color rim = new(0.55f * light, 0.68f * light, 0.8f * light, 1f);
                if (rivet < 0.12f && Mathf.Abs(d - (r - 1.7f)) < 0.9f) rim = new Color(0.9f, 0.97f, 1f, 1f);
                return rim;
            }
            float shore = cx + 1.5f * Mathf.Sin(y * 0.55f);
            Color c = x < shore ? BoilingSea(x, y, Mathf.Clamp01((y - cy + r) / (2f * r)) * 0.6f) : LavaCrust(x, y);
            // Wave crests on the sea half.
            if (x < shore && Mathf.Abs(Mathf.Repeat(y + 2f * Mathf.Sin(x * 0.45f), 6f) - 3f) < 0.5f) c = Color.Lerp(c, new Color(1f, 0.75f, 0.4f), 0.6f);
            return Color.Lerp(c, new Color(0.05f, 0.03f, 0.03f, 1f), Mathf.Clamp01(1f - Mathf.Abs(x - shore) / 1.2f) * 0.6f);
        }, 0.6f);
        // The iron boss.
        Shape(canvas, (x, y) => Circle(x, y, cx, cy, 5f), (x, y, depth) =>
        {
            float light = 0.65f + 0.5f * Mathf.Clamp01(1f - Circle(x, y, cx - 2f, cy - 2f, 0f) / 6f);
            return new Color(0.55f * light, 0.6f * light, 0.68f * light, 1f);
        }, 0.6f);
        GlowLine(canvas, (x, y) => Mathf.Abs(Circle(x, y, cx, cy, r)) - 0.3f, ScaldCool, new Color(0.85f, 0.95f, 1f, 0.7f), 3f, 0.6f);
        SteamWisp(canvas, 24f, 2f, 12f, 0.35f);
        SteamWisp(canvas, 40f, 4f, 12f, 0.35f);
        Sparkle(canvas, 52f, 12f, 3f, ScaldCool);
        canvas.FadeEdges(4f, false, 5f);
    }

    // Tether-Lock: a barbed harpoon flying out, its line pulled taut, gold sinew bindings glowing on the shaft.
    private static void TetherLockPicture(Canvas canvas)
    {
        Color gold = new(1f, 0.78f, 0.35f);
        Glow(canvas, 34f, 30f, 30f, gold, 0.45f);
        // The line: from a coil at the lower left out to the harpoon's butt, straight and taut.
        canvas.Paint((x, y) =>
        {
            float line = Capsule(x, y, 4f, 61f, 27f, 38f, 0.8f);
            float fade = Mathf.Clamp01((x - 2f) / 14f);
            float halo = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Max(line, 0f) / 3f), 2f) * 0.5f;
            return Canvas.Over(new Color(gold.r, gold.g, gold.b, halo * fade), new Color(0.9f, 0.8f, 0.6f, Mathf.Clamp01(0.5f - line) * fade));
        });
        // The shaft.
        Shape(canvas, (x, y) => Capsule(x, y, 26f, 39f, 46f, 18f, 1.8f), (x, y, depth) =>
        {
            float light = 0.75f + 0.3f * Mathf.Clamp01(depth / 1.5f);
            return new Color(0.55f * light, 0.4f * light, 0.25f * light, 1f);
        });
        // Bindings.
        foreach (float t in new[] { 0.25f, 0.45f })
        {
            float bx = Mathf.Lerp(26f, 46f, t), by = Mathf.Lerp(39f, 18f, t);
            GlowLine(canvas, (x, y) => Capsule(x, y, bx - 1.8f, by - 1.6f, bx + 1.8f, by + 1.6f, 1f), gold, new Color(1f, 0.85f, 0.45f), 3f, 0.7f);
        }
        // The barbed iron head.
        Shape(canvas, (x, y) => Mathf.Min(Polygon(x, y, [new(43f, 17f), new(58f, 5f), new(48f, 22f)]),
            Mathf.Min(Polygon(x, y, [new(46f, 14f), new(43f, 7f), new(49f, 12f)]), Polygon(x, y, [new(50f, 19f), new(57f, 20f), new(52f, 15f)]))),
            (x, y, depth) =>
            {
                float light = 0.7f + 0.45f * Mathf.Clamp01(depth / 1.5f);
                return new Color(0.66f * light, 0.7f * light, 0.76f * light, 1f);
            });
        Sparkle(canvas, 57f, 5f, 3f, gold);
        Sparkle(canvas, 8f, 30f, 2f, gold);
    }

    // Tendril-Draw: a spear flying back butt first toward the lower left, two dark roots reaching in from there coiled
    // round its shaft in a sickly green glow, streaks trailing off its head where it struck.
    private static void TendrilDrawPicture(Canvas canvas)
    {
        Color glow = new(0.55f, 1f, 0.45f);
        Vector2 butt = new(9f, 55f), head = new(46f, 18f);
        Vector2 along = (head - butt).normalized;
        Vector2 across = new(-along.y, along.x);
        float length = Vector2.Distance(butt, head);
        Glow(canvas, 30f, 34f, 30f, glow, 0.4f);
        // Streaks off the head: it is flying back.
        foreach (float off in new[] { -5f, 0f, 5f })
        {
            Vector2 a = head + along * (6f + Mathf.Abs(off)) + across * off, b = a + along * 10f;
            GlowLine(canvas, (x, y) => Capsule(x, y, a.x, a.y, b.x, b.y, 0.5f), glow, new Color(0.85f, 1f, 0.8f, 0.6f), 2.5f, 0.35f);
        }
        // The shaft.
        Shape(canvas, (x, y) => Capsule(x, y, butt.x, butt.y, head.x, head.y, 1.6f), (x, y, depth) =>
        {
            float light = 0.75f + 0.3f * Mathf.Clamp01(depth / 1.2f);
            return new Color(0.58f * light, 0.42f * light, 0.26f * light, 1f);
        });
        // The leaf-shaped iron head.
        Vector2 tip = head + along * 14f, mid = head + along * 5f;
        Shape(canvas, (x, y) => Polygon(x, y, [head - along * 1f, mid + across * 3.6f, tip, mid - across * 3.6f]), (x, y, depth) =>
        {
            float light = 0.7f + 0.45f * Mathf.Clamp01(depth / 1.5f);
            return new Color(0.66f * light, 0.7f * light, 0.76f * light, 1f);
        });
        // Two roots, coming in from the lower left and winding round the shaft, thinning toward its head.
        foreach (float phase in new[] { 0f, Mathf.PI })
        {
            float Root(float x, float y)
            {
                Vector2 p = new Vector2(x, y) - butt;
                float t = Vector2.Dot(p, along);
                if (t < -14f || t > length * 0.72f) return 9f;
                float share = Mathf.Clamp01((t + 14f) / (length * 0.72f + 14f));
                float wave = Mathf.Sin(t * 0.42f + phase) * Mathf.Lerp(4.5f, 2.2f, share);
                return Mathf.Abs(Vector2.Dot(p, across) - wave) - Mathf.Lerp(1.5f, 0.4f, share);
            }
            GlowLine(canvas, Root, glow, new Color(0.24f, 0.2f, 0.14f), 3.5f, 0.55f);
            // The near turns in front of the shaft get a green sheen.
            canvas.Paint((x, y) =>
            {
                float t = Vector2.Dot(new Vector2(x, y) - butt, along);
                bool near = Mathf.Cos(t * 0.42f + phase) > 0f;
                return near ? new Color(glow.r, glow.g, glow.b, Mathf.Clamp01(0.5f - Root(x, y) - 0.6f) * 0.6f) : Color.clear;
            });
        }
        Sparkle(canvas, 58f, 8f, 3f, glow);
        Sparkle(canvas, 22f, 30f, 2f, glow);
        canvas.FadeEdges(4f, false, 4f);
    }

    // Strider-Girth: a riding saddle on its blanket, girth strap and stirrup hanging, golden streaks of untiring stamina
    // rushing past it.
    private static void StriderGirthPicture(Canvas canvas)
    {
        Color stamina = new(1f, 0.82f, 0.3f);
        Glow(canvas, 32f, 32f, 30f, stamina, 0.45f);
        // Speed streaks rushing past behind it.
        foreach (Vector3 streak in new[] { new Vector3(16f, 2f, 24f), new Vector3(30f, 6f, 30f), new Vector3(42f, 10f, 26f) })
        {
            float sy = streak.x, x0 = streak.y, x1 = streak.z;
            GlowLine(canvas, (x, y) => Capsule(x, y, x0, sy, x1, sy, 0.7f) + Mathf.Max(0f, (x1 - x) * 0.03f), stamina,
                new Color(1f, 0.95f, 0.75f, 0.8f), 3f, 0.45f);
        }
        // The girth strap, buckled, and the stirrup on its leather.
        Shape(canvas, (x, y) => Box(x, y, 23f, 36f, 28f, 60f, 1f), (x, y, depth) =>
        {
            float light = Mathf.Lerp(1f, 0.6f, Mathf.Clamp01((y - 36f) / 24f));
            return new Color(0.42f * light, 0.26f * light, 0.15f * light, 1f);
        }, 0.4f);
        GlowLine(canvas, (x, y) => Mathf.Abs(Box(x, y, 21.5f, 49f, 29.5f, 54f, 0.8f)) - 0.6f, stamina, new Color(1f, 0.8f, 0.35f), 2f, 0.4f);
        Shape(canvas, (x, y) => Capsule(x, y, 39f, 36f, 41f, 48f, 1f), (x, y, depth) => new Color(0.36f, 0.22f, 0.13f, 1f), 0.3f);
        Shape(canvas, (x, y) => Mathf.Min(Mathf.Abs(Ellipse(x, y, 41.5f, 52f, 4.2f, 4f)) - 0.9f, Capsule(x, y, 37.5f, 55.5f, 45.5f, 55.5f, 1f)),
            (x, y, depth) => new Color(0.62f, 0.64f, 0.68f, 1f), 0.4f);
        // The blanket under it: woven red with a pale trim.
        Shape(canvas, (x, y) => Box(x, y, 12f, 30f, 52f, 41f, 3f), (x, y, depth) =>
        {
            float stripe = Mathf.Abs(y - 38.5f) < 1f ? 1f : 0f;
            float weave = 0.92f + 0.08f * Mathf.Sin(x * 2.4f) * Mathf.Sin(y * 2.4f);
            Color cloth = Color.Lerp(new Color(0.62f, 0.18f, 0.13f), new Color(0.92f, 0.82f, 0.6f), stripe) * weave;
            cloth.a = 1f;
            return cloth;
        }, 0.5f);
        // The saddle: a deep seat between a high cantle and pommel, lit from above.
        Shape(canvas, (x, y) => Polygon(x, y, StriderSaddle), (x, y, depth) =>
        {
            float light = Mathf.Lerp(1.2f, 0.6f, Mathf.Clamp01((y - 14f) / 20f)) * (0.75f + 0.25f * Mathf.Clamp01(depth / 2f));
            return new Color(0.6f * light, 0.37f * light, 0.2f * light, 1f);
        }, 0.6f);
        // A stitched seam and the shine on the seat.
        canvas.Paint((x, y) =>
        {
            float seam = Mathf.Max(Mathf.Abs(y - 30f) - 0.4f, Mathf.Abs(x - 32f) - 15f);
            float dash = Mathf.Sin(x * 1.6f) > 0f ? 1f : 0f;
            return new Color(0.95f, 0.85f, 0.6f, Mathf.Clamp01(0.5f - seam) * dash * 0.7f);
        });
        canvas.Paint((x, y) => new Color(1f, 0.92f, 0.75f, Mathf.Clamp01(0.5f - Capsule(x, y, 24f, 26.5f, 36f, 26.5f, 0.7f)) * 0.5f));
        Sparkle(canvas, 52f, 8f, 3f, stamina);
        Sparkle(canvas, 54f, 50f, 2f, stamina);
        canvas.FadeEdges(4f, false, 4f);
    }

    // The Strider-Girth saddle (or the blanket under it) shrunk by scale and turned by degrees, centred on (cx, cy).
    private static float MiniSaddle(float x, float y, float cx, float cy, float scale, float degrees, bool blanket)
    {
        Vector2 p = Unturn(x, y, cx, cy, degrees);
        float sx = 32f + (p.x - cx) / scale, sy = 28f + (p.y - cy) / scale;
        return (blanket ? Box(sx, sy, 12f, 30f, 52f, 41f, 3f) : Polygon(sx, sy, StriderSaddle)) * scale;
    }

    private static readonly Vector2[] StriderSaddle =
    [
        new(8f, 22f), new(10f, 15f), new(14f, 14f), new(18f, 20f), new(24f, 25f), new(32f, 26f), new(40f, 24f), new(45f, 17f),
        new(47f, 12f), new(51f, 11f), new(54f, 15f), new(52f, 24f), new(47f, 32f), new(17f, 32f), new(11f, 28f)
    ];

    // Second-Kindle: a dying ember of a heart flaring back to life, flames bursting out of it and a ring of fire throwing
    // back whatever is near.
    private static void SecondKindlePicture(Canvas canvas)
    {
        Color fire = new(1f, 0.45f, 0.12f);
        Glow(canvas, 32f, 36f, 32f, fire, 0.75f);
        // The burst: a ring of fire spreading out, short streaks flying off it.
        GlowLine(canvas, (x, y) => Mathf.Abs(Circle(x, y, 32f, 36f, 24f)) - 0.8f, fire, new Color(1f, 0.75f, 0.35f), 4f, 0.6f);
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4f + 0.3f, c = Mathf.Cos(a), s = Mathf.Sin(a);
            GlowLine(canvas, (x, y) => Capsule(x, y, 32f + c * 26f, 36f + s * 26f, 32f + c * 31f, 36f + s * 31f, 0.7f), fire,
                new Color(1f, 0.85f, 0.5f), 2.5f, 0.6f);
        }
        // Flames leaping up from it.
        Flame(canvas, 32f, 34f, 16f, 30f, 0f);
        Flame(canvas, 23f, 38f, 9f, 18f, -3f);
        Flame(canvas, 41f, 38f, 9f, 18f, 3f);
        // The ember heart: charred dark at its rim, glowing white-hot through its cracks.
        Shape(canvas, (x, y) => Heart(x, y, 32f, 40f, 10f), (x, y, depth) =>
        {
            float hot = Mathf.Clamp01(depth / 6f);
            Color ember = Color.Lerp(new Color(0.3f, 0.08f, 0.04f), new Color(1f, 0.55f, 0.15f), hot);
            float crack = Mathf.Min(Mathf.Abs(x - 32f - Mathf.Sin(y * 0.8f) * 2f), Mathf.Abs(y - 38f - (x - 32f) * 0.4f));
            return Color.Lerp(ember, new Color(1f, 0.95f, 0.7f, 1f), Mathf.Clamp01(1f - crack) * Mathf.Clamp01(depth / 2f));
        });
        Sparkle(canvas, 50f, 14f, 2.5f, fire);
        Sparkle(canvas, 13f, 18f, 2f, fire);
    }

    // Morgen-Pouch: a leather pouch, soft violet light rising from its mouth and wisps curling in; a pet's paw print and
    // a mount's saddle drift down into it.
    private static void MorgenPouchPicture(Canvas canvas)
    {
        Color ether = new(0.7f, 0.5f, 1f);
        Glow(canvas, 32f, 26f, 30f, ether, 0.55f);
        // Soft light rising from the pouch's mouth, widening and fading upward.
        canvas.Paint((x, y) =>
        {
            if (y > 40f) return Color.clear;
            float half = 3f + (40f - y) * 0.5f;
            float a = Mathf.Clamp01(1f - Mathf.Abs(x - 32f) / half) * Mathf.Clamp01((y - 2f) / 30f);
            return new Color(0.82f, 0.68f, 1f, a * a * 0.45f);
        });
        // Two wisps curling in from either side, brightest where they go in.
        SoulTrail(canvas, new Vector2(5f, 8f), new Vector2(4f, 36f), new Vector2(29f, 38f), ether, new Color(0.92f, 0.86f, 1f));
        SoulTrail(canvas, new Vector2(59f, 6f), new Vector2(61f, 36f), new Vector2(35f, 38f), ether, new Color(0.92f, 0.86f, 1f));
        foreach (Vector3 mote in new[] { new Vector3(26f, 30f, 1f), new Vector3(37f, 27f, 1.2f), new Vector3(31f, 12f, 0.9f) })
        {
            Glow(canvas, mote.x, mote.y, mote.z * 4f, ether, 0.6f);
            canvas.Paint((x, y) => new Color(1f, 0.95f, 1f, Mathf.Clamp01(0.5f - Circle(x, y, mote.x, mote.y, mote.z))));
        }
        // What it carries in: a pet's paw print and a mount's saddle tumbling down.
        Shape(canvas, (x, y) => Paw(x, y, 17f, 23f, -110f, 2.6f), (x, y, depth) =>
        {
            float light = 0.8f + 0.25f * Mathf.Clamp01(depth / 1.5f);
            return new Color(0.92f * light, 0.78f * light, 0.6f * light, 1f);
        }, 0.6f);
        Shape(canvas, (x, y) => MiniSaddle(x, y, 44f, 20f, 0.48f, 15f, true), (x, y, depth) => new Color(0.62f, 0.18f, 0.13f, 1f), 0.5f);
        Shape(canvas, (x, y) => MiniSaddle(x, y, 44f, 20f, 0.48f, 15f, false), (x, y, depth) =>
        {
            float light = 0.75f + 0.4f * Mathf.Clamp01(depth / 1.2f);
            return new Color(0.62f * light, 0.4f * light, 0.22f * light, 1f);
        }, 0.5f);
        // The pouch: a round leather bag gathered at the neck.
        Shape(canvas, (x, y) =>
        {
            float body = Ellipse(x, y, 32f, 51f, 14f, 10.5f);
            float neck = Box(x, y, 26.5f, 39.5f, 37.5f, 46f, 2.5f);
            return Mathf.Min(body, neck);
        }, (x, y, depth) =>
        {
            float light = Mathf.Clamp01(depth / 7f) * 0.6f + Mathf.Clamp01((40f - x) / 20f) * 0.25f + Mathf.Clamp01((52f - y) / 14f) * 0.15f;
            Color leather = Color.Lerp(new Color(0.24f, 0.14f, 0.08f), new Color(0.6f, 0.4f, 0.24f), light);
            // A few stitched creases.
            float crease = Mathf.Min(Mathf.Abs(x - 32f - (y - 47f) * 0.25f), Mathf.Abs(x - 25f + (y - 47f) * 0.35f));
            return Color.Lerp(leather, leather * 0.7f, Mathf.Clamp01(1f - crease) * Mathf.Clamp01((y - 46f) / 3f) * 0.6f);
        });
        // The drawstring, tied off at the side.
        GlowLine(canvas, (x, y) => Mathf.Min(Capsule(x, y, 26f, 44f, 38f, 44f, 0.8f), Capsule(x, y, 38f, 44f, 42f, 47f, 0.6f)),
            new Color(0.9f, 0.7f, 0.3f), new Color(0.95f, 0.8f, 0.45f), 1.5f, 0.3f);
        // The mouth, glowing where the swirl goes in.
        Glow(canvas, 32f, 39f, 10f, ether, 0.8f);
        canvas.Paint((x, y) => new Color(0.95f, 0.88f, 1f, Mathf.Clamp01(0.5f - Ellipse(x, y, 32f, 39.5f, 5.5f, 1.6f))));
        Sparkle(canvas, 53f, 13f, 2.5f, ether);
        Sparkle(canvas, 11f, 34f, 2f, ether);
    }

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

    // Aether-Clear: the Wisplight in a great bubble of its light, the Mistlands mist billowing up around it and held off,
    // little wisps drifting inside.
    private static void AetherClearPicture(Canvas canvas)
    {
        Color wisp = new(0.6f, 0.92f, 1f);
        Color mistTop = new(0.76f, 0.7f, 0.9f), mistUnder = new(0.42f, 0.36f, 0.58f);
        Pixels? smoke = GameArt.Get("Smoked");
        const float cx = 32f, cy = 30f, radius = 21f;
        // The mist: billows packed around the bubble's lower half and sides, cut off where it holds them back.
        // Kept inside the icon, so each billow shows its round, soft edge rather than being cut off by the frame.
        Vector3[] puffs = [new(9f, 31f, 4.5f), new(8f, 42f, 6.5f), new(14f, 52f, 7f), new(25f, 56f, 6.5f), new(39f, 56f, 6.5f), new(50f, 52f, 7f),
            new(56f, 42f, 6.5f), new(55f, 31f, 4.5f)];
        canvas.Paint((x, y) =>
        {
            float d = float.MaxValue;
            foreach (Vector3 p in puffs) d = Mathf.Min(d, Circle(x, y, p.x, p.y, p.z));
            float bubble = Circle(x, y, cx, cy, radius);
            // Soft billow edges, fading out over a few pixels.
            float alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(0.6f - d / 5f)) * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((bubble - 0.5f) / 4f)) * 0.95f;
            if (alpha <= 0f) return Color.clear;
            float k = SmokeShade(smoke, x / 64f, y / 64f);
            Color c = Color.Lerp(mistTop, mistUnder, Mathf.Clamp01((y - 28f) / 34f));
            // Lit by the bubble along its edge.
            c = Color.Lerp(c, wisp, Mathf.Clamp01(1f - (bubble - 0.5f) / 5f) * 0.5f);
            return new Color(Mathf.Clamp01(c.r * k), Mathf.Clamp01(c.g * k), Mathf.Clamp01(c.b * k), alpha);
        });
        // The bubble: a faint glow inside, a soft rim brighter on its upper left, and a gleam there.
        canvas.Paint((x, y) =>
        {
            float d = Circle(x, y, cx, cy, radius);
            float lit = 0.55f + 0.45f * Mathf.Clamp01(((cx - x) + (cy - y)) / (radius * 1.4f) + 0.5f);
            float rim = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(d) / 2.8f), 2.5f) * 0.8f * lit;
            float fill = Mathf.Clamp01(-d / 4f) * 0.22f;
            float gx = x - (cx - 9f), gy = y - (cy - 11f);
            float gleam = Mathf.Clamp01(1f - Mathf.Sqrt(gx * gx * 0.6f + gy * gy * 1.6f) / 3.5f) * 0.6f;
            return new Color(Mathf.Lerp(wisp.r, 1f, gleam), Mathf.Lerp(wisp.g, 1f, gleam), 1f, Mathf.Clamp01(Mathf.Max(Mathf.Max(rim, fill), gleam)));
        });
        // The Wisplight itself, glowing at the heart of it; a bright wisp if its picture can't be found.
        Glow(canvas, cx, cy + 1f, 17f, wisp, 0.8f);
        if (GameArt.ItemIconPixels("Demister") is { } light) canvas.Draw(light, cx - 14f, cy - 13f, 28f, 28f, new Color(1.1f, 1.15f, 1.2f));
        else
            canvas.Paint((x, y) =>
            {
                float orb = Circle(x, y, cx, cy, 8f);
                float core = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Max(Circle(x, y, cx - 1f, cy - 1f, 2.5f), 0f) / 5f), 2f);
                Color c = Color.Lerp(wisp, Color.white, core);
                return new Color(c.r, c.g, c.b, Mathf.Clamp01(Mathf.Clamp01(0.5f - orb / 1.5f) * 0.9f + core));
            });
        // Little wisps drifting about inside the bubble.
        foreach ((float wx, float wy, float r) in new[] { (17f, 36f, 1.4f), (46f, 22f, 1.3f), (43f, 42f, 1.2f) })
        {
            Glow(canvas, wx, wy, r * 3.5f, wisp, 0.6f);
            canvas.Paint((x, y) => new Color(0.92f, 1f, 1f, Mathf.Clamp01(0.5f - Circle(x, y, wx, wy, r))));
        }
        Sparkle(canvas, 56f, 8f, 2.6f, wisp);
        Sparkle(canvas, 8f, 10f, 2f, wisp);
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

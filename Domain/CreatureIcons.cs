using System;
using UnityEngine;
using UnityEngine.UI;

namespace JoksterCube.TempestRite.Domain;

// Icons over creatures the Packward and Carrion Rites steer, where the game shows its own alert icons (its yellow
// "aware" waves and red "!"), drawn in the same style as the yellow one (thin white strokes around a dot, tinted):
// - Drawn (Carrion), green: the waves turned around, curving in toward the dot.
// - Scared (Packward), blue: chevrons pointing away from the dot.
internal static class CreatureIcons
{
    // The game's "aware" sprite is 192 x 139, a dot in the middle and two arcs on each side.
    internal const int Width = 192;
    internal const int Height = 139;
    private const float CenterX = 96f;
    private const float CenterY = 69.5f;
    private const float Dot = 21.5f;
    private const float Stroke = 3.6f;
    // Shown at this share of the game's icon size: the shapes fill the sprite more than the game's do.
    internal const float Scale = 0.6f;

    internal const string DrawnName = "TempestRite_Drawn";
    internal const string ScaredName = "TempestRite_Scared";
    internal static readonly Color DrawnColor = new(0.35f, 1f, 0.3f, 1f);
    internal static readonly Color ScaredColor = new(0.3f, 0.7f, 1f, 1f);

    private static Sprite? _drawn;
    private static Sprite? _scared;

    internal static void ClearSession() { }

    internal static Sprite Drawn() => _drawn ? _drawn! : _drawn = ToSprite(RenderDrawn(), DrawnName);

    internal static Sprite Scared() => _scared ? _scared! : _scared = ToSprite(RenderScared(), ScaredName);

    // White with alpha, row 0 at the bottom (as textures are); the shapes are the same upside down.
    internal static Color[] RenderDrawn() => Render(DrawnShape);

    internal static Color[] RenderScared() => Render(ScaredShape);

    private static Color[] Render(Func<float, float, float> shape)
    {
        Color[] pixels = new Color[Width * Height];
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                pixels[y * Width + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - shape(x + 0.5f, y + 0.5f)));
        return pixels;
    }

    // Arcs bowing toward the dot: parts of big circles centered out to each side.
    private static float DrawnShape(float x, float y)
    {
        float d = Circle(x, y, CenterX, CenterY, Dot);
        float side = Mathf.Abs(x - CenterX);
        d = Mathf.Min(d, Arc(side, y - CenterY, 44f, 90f, 30f));
        d = Mathf.Min(d, Arc(side, y - CenterY, 78f, 90f, 46f));
        return d;
    }

    // An arc whose middle is apex from the dot, bowing toward it, of the given radius, reaching reach up and down.
    private static float Arc(float side, float dy, float apex, float radius, float reach)
    {
        // The circle's center lies beyond the apex, away from the dot.
        float cx = apex + radius;
        float angle = Mathf.Asin(Mathf.Clamp(reach / radius, -1f, 1f));
        float a = Mathf.Atan2(dy, cx - side);
        if (Mathf.Abs(a) <= angle)
            return Mathf.Abs(Mathf.Sqrt((side - cx) * (side - cx) + dy * dy) - radius) - Stroke;
        // Past the ends: round caps.
        float ex = cx - radius * Mathf.Cos(angle), ey = radius * Mathf.Sin(angle) * Mathf.Sign(a);
        return Mathf.Sqrt((side - ex) * (side - ex) + (dy - ey) * (dy - ey)) - Stroke;
    }

    // Two chevrons on each side, their points away from the dot.
    private static float ScaredShape(float x, float y)
    {
        float d = Circle(x, y, CenterX, CenterY, Dot);
        float side = Mathf.Abs(x - CenterX);
        d = Mathf.Min(d, Chevron(side, y - CenterY, 58f, 12f, 24f));
        d = Mathf.Min(d, Chevron(side, y - CenterY, 86f, 18f, 40f));
        return d;
    }

    // A ">" (for the right side; mirrored for the left), its point tip from the dot, depth deep and reaching reach.
    private static float Chevron(float side, float dy, float tip, float deep, float reach) =>
        Mathf.Min(Segment(side, dy, tip - deep, reach, tip, 0f), Segment(side, dy, tip - deep, -reach, tip, 0f)) - Stroke;

    private static float Circle(float x, float y, float cx, float cy, float r) =>
        Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r;

    private static float Segment(float x, float y, float ax, float ay, float bx, float by)
    {
        float dx = bx - ax, dy = by - ay;
        float t = Mathf.Clamp01(((x - ax) * dx + (y - ay) * dy) / (dx * dx + dy * dy));
        float px = ax + t * dx - x, py = ay + t * dy - y;
        return Mathf.Sqrt(px * px + py * py);
    }

    private static Sprite ToSprite(Color[] pixels, string name)
    {
        Texture2D texture = new(Width, Height, TextureFormat.RGBA32, false)
        {
            name = name,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, Width, Height), new Vector2(0.5f, 0.5f), 100f);
        sprite.name = name;
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    // A copy of the hud's "aware" icon (same place, size and look), with this sprite and color, hidden.
    internal static GameObject Make(RectTransform aware, string name, Sprite sprite, Color color)
    {
        GameObject icon = UnityEngine.Object.Instantiate(aware.gameObject, aware.parent);
        icon.name = name;
        icon.SetActive(false);
        Image? image = icon.GetComponent<Image>();
        if (image)
        {
            image!.sprite = sprite;
            image.color = color;
        }
        return icon;
    }
}

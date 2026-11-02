using System;
using JoksterCube.TempestRite.Common;
using UnityEngine;
using UnityEngine.UI;

namespace JoksterCube.TempestRite.Domain;

// How a raised Ragnarök Rite boss looks, in every game: for each star (see RagnarokPower.Stars and
// IncursionArena.BossStars) a little darker and deeper in color, with a dim red glow like the Hoard-Bane
// Rite's elites, and as many stars beside its name on its health bar (see RagnarokPatches.StarsPatch).
internal static class RagnarokLook
{
    private const string Feature = "Ragnarök Rite boss look";
    private const string GlowName = "TempestRite_RagnarokGlow";
    private const string StarName = "TempestRite_RagnarokStar";
    private const float Interval = 0.5f;
    // Each star: more saturated and darker; no more than this many count.
    private static readonly Vector3 TintPerStar = new(0f, 0.05f, -0.04f);
    private const int MostTinted = 7;
    // Stars beside the name: this share of its font size, this far apart.
    private const float StarSize = 0.9f;
    private const float StarGap = 0.15f;

    private static float _next;

    internal static int StarsOf(Character character)
    {
        if (!character || !IncursionArena.IsRagnarokBoss(character)) return 0;
        return Math.Max(0, character.m_nview.GetZDO().GetInt(IncursionArena.BossStars));
    }

    // Every frame: the bosses near are tinted (the same every time, so again does no harm: the game may swap their look).
    internal static void Update()
    {
        if (PatchGuard.IsDisabled(Feature) || IncursionArena.Kind != WeatherKind.Ragnarok || Time.time < _next) return;
        _next = Time.time + Interval;
        try
        {
            foreach (Character character in Character.GetAllCharacters())
            {
                if (!character || character.IsDead()) continue;
                int stars = StarsOf(character);
                if (stars > 0) Decorate(character, stars);
            }
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    private static void Decorate(Character boss, int stars)
    {
        HoardBaneArena.Tint(boss, TintPerStar * Math.Min(stars, MostTinted));
        if (boss.transform.Find(GlowName)) return;
        GameObject glow = new(GlowName);
        glow.transform.SetParent(boss.transform, false);
        glow.transform.localPosition = Vector3.up * Mathf.Max(1f, boss.GetHeight() * 0.6f);
        Light light = glow.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.12f, 0.08f);
        light.range = Mathf.Clamp(boss.GetHeight() * 2.5f, 4f, 12f);
        light.intensity = Mathf.Min(0.8f + 0.2f * stars, 2f);
        light.shadows = LightShadows.None;
    }

    // As many stars as it has, just right of its name on its health bar: copies of the star the game draws over
    // creatures.
    internal static void ShowStars(EnemyHud hud, EnemyHud.HudData data, int stars)
    {
        TMPro.TextMeshProUGUI name = data.m_name;
        if (!name || stars <= 0) return;
        RectTransform? source = StarSource(hud);
        if (!source) return;
        float size = Mathf.Max(8f, name.fontSize * StarSize);
        float width = source!.rect.height > 0f ? size * source.rect.width / source.rect.height : size;
        Bounds text = name.textBounds;
        float left = text.size.x > 0f ? text.max.x : 0f;
        float y = text.size.y > 0f ? text.center.y : 0f;
        for (int i = 0; i < stars; i++)
        {
            string starName = StarName + i;
            Transform? star = name.transform.Find(starName);
            if (!star)
            {
                star = UnityEngine.Object.Instantiate(source.gameObject, name.transform, false).transform;
                star!.name = starName;
                RectTransform made = (RectTransform)star;
                made.anchorMin = made.anchorMax = made.pivot = new Vector2(0.5f, 0.5f);
                foreach (Graphic graphic in star.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            }
            RectTransform rect = (RectTransform)star;
            rect.sizeDelta = new Vector2(width, size);
            rect.localScale = Vector3.one;
            rect.localPosition = new Vector3(left + width * StarGap + width * (0.5f + i * (1f + StarGap)), y, 0f);
            star.gameObject.SetActive(true);
        }
    }

    // The star over two-star creatures in the game's usual health bar.
    private static RectTransform? StarSource(EnemyHud hud)
    {
        if (!hud.m_baseHud) return null;
        Transform? level = hud.m_baseHud.transform.Find("level_2");
        if (!level) return null;
        foreach (Transform child in level!)
            if (child is RectTransform rect && child.GetComponent<Image>()) return rect;
        return level.GetComponent<Image>() ? level as RectTransform : null;
    }
}

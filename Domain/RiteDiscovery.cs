using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Settings;
using static JoksterCube.TempestRite.Settings.Constants;

namespace JoksterCube.TempestRite.Domain;

// "New rite" popups, like the game's "New recipe" ones. Nothing shows until the character has found an obliterator
// (built one, opened its hatch or pulled its lever): then every rite they already know shows at once, and after that
// each rite whose first key item they find. Kept with the character, so it carries over between worlds.
internal static class RiteDiscovery
{
    private const string SaveKey = "TempestRite_ObliteratorFound";

    private static bool HasFound(Player player) => player.m_customData.ContainsKey(SaveKey);

    // The local player built, opened or pulled an obliterator.
    internal static void FoundObliterator()
    {
        Player? player = Player.m_localPlayer;
        if (!player || HasFound(player!)) return;
        player!.m_customData[SaveKey] = "1";
        foreach (WeatherKind kind in ObliteratorKeys.Known()) Announce(kind);
    }

    // Before the game learns an item: the rite it makes known, or None (not the local player, no obliterator found yet,
    // item already known, not a key item, or its rite already known from another of its items).
    internal static WeatherKind BeforeLearning(Player player, ItemDrop.ItemData item)
    {
        if (player != Player.m_localPlayer || !HasFound(player) || player.IsMaterialKnown(item.m_shared.m_name)) return WeatherKind.None;
        WeatherKind kind = ObliteratorKeys.KindOf(item);
        return kind == WeatherKind.None || ObliteratorKeys.IsKnown(kind) ? WeatherKind.None : kind;
    }

    internal static void AfterLearning(WeatherKind kind)
    {
        if (kind != WeatherKind.None && ObliteratorKeys.IsKnown(kind)) Announce(kind);
    }

    private static void Announce(WeatherKind kind)
    {
        if (!MessageHud.instance || !PluginConfig.ShowRiteUnlocks.IsOn()) return;
        MessageHud.instance.QueueUnlockMsg(WeatherIcons.For(kind)!, Messages.NewRite, WeatherKinds.Name(kind));
    }
}

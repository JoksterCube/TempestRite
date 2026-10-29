using System;
using HarmonyLib;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain;
using JoksterCube.TempestRite.Settings;

namespace JoksterCube.TempestRite.Patches;

internal static class ObliteratorPatches
{
    // The local player pulled the lever (and had access). Runs only in the game of the player who pulled it.
    [HarmonyPatch(typeof(Incinerator), nameof(Incinerator.OnIncinerate))]
    private static class LeverPulled
    {
        private const string Feature = "Obliterator lever";

        // A Crimson or Ragnarök Rite going on forbids the rites in it: the lever doesn't move and nothing burns.
        private static bool Prefix(Incinerator __instance, ref bool __result)
        {
            if (PatchGuard.IsDisabled(Feature) || !__instance.m_container) return true;
            try
            {
                string? why = ObliteratorPull.Refused(__instance);
                if (why == null) return true;
                if (Player.m_localPlayer) Player.m_localPlayer.Message(MessageHud.MessageType.Center, why);
                __result = false;
                return false;
            }
            catch (Exception exception)
            {
                PatchGuard.Fail(Feature, exception);
                return true;
            }
        }

        private static void Postfix(Incinerator __instance, bool __result)
        {
            if (!__result || PatchGuard.IsDisabled(Feature)) return;
            try
            {
                RiteDiscovery.FoundObliterator();
                ObliteratorPull.Pulled(__instance);
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    // A key item found for the first time makes its rite known: a "New rite" popup (see RiteDiscovery).
    [HarmonyPatch(typeof(Player), nameof(Player.AddKnownItem))]
    private static class ItemLearned
    {
        private const string Feature = "New rite popup";

        private static void Prefix(Player __instance, ItemDrop.ItemData item, out WeatherKind __state)
        {
            __state = WeatherKind.None;
            if (PatchGuard.IsDisabled(Feature) || item?.m_shared == null) return;
            try { __state = RiteDiscovery.BeforeLearning(__instance, item); }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }

        private static void Postfix(WeatherKind __state)
        {
            if (__state == WeatherKind.None || PatchGuard.IsDisabled(Feature)) return;
            try { RiteDiscovery.AfterLearning(__state); }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    // Building an obliterator finds it (see RiteDiscovery). PlacePiece only runs for the local player.
    [HarmonyPatch(typeof(Player), nameof(Player.PlacePiece))]
    private static class Built
    {
        private const string Feature = "Obliterator found";

        private static void Postfix(Player __instance, Piece piece)
        {
            if (PatchGuard.IsDisabled(Feature) || __instance != Player.m_localPlayer || !piece) return;
            try
            {
                if (piece.GetComponentInChildren<Incinerator>(true)) RiteDiscovery.FoundObliterator();
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    // The obliterator's owner answers the player who pulled the lever: burnt, or not.
    [HarmonyPatch(typeof(Incinerator), nameof(Incinerator.RPC_IncinerateRespons))]
    private static class Answered
    {
        private const string Feature = "Obliterator answer";

        private static void Prefix(Incinerator __instance, int r)
        {
            if (PatchGuard.IsDisabled(Feature)) return;
            try { ObliteratorPull.Answered(__instance, r); }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    // Hover lines are added after other mods' (Priority.Last), so mods that change every line of the hover text,
    // like AzuHoverStats adding the slot count, leave these alone.
    [HarmonyPatch(typeof(Incinerator), nameof(Incinerator.GetLeverHoverText))]
    private static class LeverHover
    {
        private const string Feature = "Obliterator lever hint";

        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Incinerator __instance, ref string __result)
        {
            if (PatchGuard.IsDisabled(Feature) || !PluginConfig.ShowLeverHint.IsOn()) return;
            try
            {
                if (!__instance.m_container || !PrivateArea.CheckAccess(__instance.transform.position, 0f, false)) return;
                __result += ObliteratorHints.Lever(__instance);
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    [HarmonyPatch(typeof(Container), nameof(Container.GetHoverText))]
    private static class HatchHover
    {
        private const string Feature = "Obliterator hatch hint";

        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Container __instance, ref string __result)
        {
            if (PatchGuard.IsDisabled(Feature) || !PluginConfig.ShowHatchHint.IsOn()) return;
            try
            {
                if (!ObliteratorKeys.IsObliteratorHatch(__instance, out _)) return;
                if (__instance.m_checkGuardStone && !PrivateArea.CheckAccess(__instance.transform.position, 0f, false)) return;
                __result += ObliteratorHints.Hatch(__instance);
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    // The rites flap under the open obliterator, and the page it opens.
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Update))]
    private static class Flap
    {
        private const string Feature = "Rites flap";

        private static void Postfix(InventoryGui __instance)
        {
            if (PatchGuard.IsDisabled(Feature)) return;
            try { RitesFlap.Update(__instance); }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    // What the Unmaking Rite gives back, shown under the tooltip of gear it unmakes in the open obliterator.
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Update))]
    private static class UnmakingRow
    {
        private const string Feature = "Unmaking tooltip";

        private static void Postfix(InventoryGui __instance)
        {
            if (PatchGuard.IsDisabled(Feature)) return;
            try { UnmakingTooltip.Update(__instance); }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    // The item that decides the weather gets a frame in the open obliterator.
    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui))]
    private static class GridMarks
    {
        private const string Feature = "Obliterator item highlight";

        private static void Postfix(InventoryGrid __instance)
        {
            if (PatchGuard.IsDisabled(Feature)) return;
            try { ObliteratorGrid.UpdateMarks(__instance); }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    // Weather items' tooltips in the open obliterator say whether they decide. The game sets the tooltip every frame,
    // so the text is set once with the line added rather than set and then changed again.
    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.CreateItemTooltip))]
    private static class ItemTooltip
    {
        private const string Feature = "Obliterator item tooltip";

        private static bool Prefix(InventoryGrid __instance, ItemDrop.ItemData item, UITooltip tooltip)
        {
            if (PatchGuard.IsDisabled(Feature)) return true;
            try
            {
                string line = ObliteratorGrid.TooltipLine(__instance, item);
                if (line.Length == 0) return true;
                tooltip.Set(item.m_shared.m_name, item.GetTooltip() + "\n\n" + line, __instance.m_tooltipAnchor);
                return false;
            }
            catch (Exception exception)
            {
                PatchGuard.Fail(Feature, exception);
                return true;
            }
        }
    }
}

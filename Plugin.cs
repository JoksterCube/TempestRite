using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using JoksterCube.ChestKit;
using JoksterCube.ChestKit.Controls;
using JoksterCube.TempestRite.Common;
using JoksterCube.TempestRite.Domain;
using JoksterCube.TempestRite.Domain.Networking;
using JoksterCube.TempestRite.Settings;
using ServerSync;
using UnityEngine;
using static JoksterCube.TempestRite.Settings.Constants;
using static JoksterCube.TempestRite.Settings.Constants.Plugin;

namespace JoksterCube.TempestRite;

[BepInPlugin(ModGUID, ModName, ModVersion)]
// Loaded after AzuHoverStats when it's installed, to show rite times in its hover text (see AzuHoverStatsPatches).
[BepInDependency("Azumatt.AzuHoverStats", BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BaseUnityPlugin
{
    internal static readonly ManualLogSource ModLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);
    internal static string ConnectionError = string.Empty;

    // ModRequired stays false: the mod checks itself (see RequireMod), so the server's Require Mod On Clients setting can
    // let players without it join. Versions must match exactly.
    // Always locked: synced settings come only from the server's config file, or also from admins in game when the
    // server's Admin Locked setting is on (see EnforceServerConfigPatch).
    internal static readonly ConfigSync ServerConfigSync = new(ModGUID)
    {
        DisplayName = ModName,
        CurrentVersion = ModVersion,
        MinimumRequiredVersion = ModVersion,
        IsLocked = true
    };

    private readonly string _configFileFullPath = Path.Combine(Paths.ConfigPath, ConfigFileName);

    // The config file is checked for changes by a watcher and, as a fallback, this often.
    private const float ConfigCheckInterval = 2f;

    private FileSystemWatcher? _configWatcher;
    private DateTime _lastConfigWrite;
    private long _lastConfigSize = -1;
    private float _nextConfigCheck;

    private readonly Harmony _harmony = new(ModGUID);

    private void Awake()
    {
        PluginConfig.Build(Config, ServerConfigSync);
        // The Rally-Horn Rite's horn, made on a worker thread now so it is ready long before it is first heard. A
        // dedicated server (batch mode) hears nothing and skips it.
        if (!UnityEngine.Application.isBatchMode) Domain.HornSound.Prepare();

        // ChestKit's patches go through this Harmony; this mod's own are found by PatchAll.
        ChestKitCore.Initialize(ModLogger, _harmony);
        // The Rally-Horn Rite's key, also in the game's Settings > Controls.
        ControlsMenu.Add("TempestRite_RallyCall", () => Messages.RallyAcceptName, () => PluginConfig.RallyCallShortcut);
        ControlsMenu.Add("TempestRite_RallyRefuse", () => Messages.RallyRefuseName, () => PluginConfig.RallyRefuseShortcut);
        ControlsMenu.Add("TempestRite_DomainBorder", () => Messages.DomainBorderName, () => PluginConfig.DomainBorderShortcut);
        // The Morgen-Pouch Rite's key, also in the game's Settings > Controls.
        ControlsMenu.Add("TempestRite_MorgenPouch", () => Messages.MorgenPouchAbility, () => PluginConfig.MorgenPouchShortcut);

        var assembly = Assembly.GetExecutingAssembly();
        _harmony.PatchAll(assembly);
        RememberConfigFile();
        SetupWatcher();
    }

    private void Update()
    {
        if (Time.unscaledTime >= _nextConfigCheck)
        {
            _nextConfigCheck = Time.unscaledTime + ConfigCheckInterval;
            ReloadConfigIfChanged();
        }
        foreach ((string name, Action update) in FrameUpdates)
        {
            if (PatchGuard.IsDisabled(name)) continue;
            try { update(); }
            catch (Exception exception) { PatchGuard.Fail(name, exception); }
        }
    }

    // Run every frame, in this order. Each is on its own: one failing is switched off (see PatchGuard) and the rest go on.
    private static readonly (string name, Action update)[] FrameUpdates =
    [
        (nameof(WeatherState), WeatherState.Update),
        (nameof(WeatherSync), WeatherSync.Update),
        (nameof(WeatherStore), WeatherStore.Update),
        (nameof(Blessings), Blessings.Update),
        (nameof(PetPouch), PetPouch.Update),
        (nameof(FortuneLuck), FortuneLuck.Update),
        (nameof(KinshipRites), KinshipRites.Update),
        (nameof(RallyHorn), RallyHorn.Update),
        (nameof(RallyPending), RallyPending.Update),
        (nameof(DomainAnchors), DomainAnchors.Update),
        (nameof(CrimsonRaids), CrimsonRaids.Update),
        (nameof(RagnarokRush), RagnarokRush.Update),
        (nameof(HoardBaneRush), HoardBaneRush.Update),
        (nameof(IncursionWatch), IncursionWatch.Update),
        (nameof(IncursionArena), IncursionArena.Update),
        (nameof(ObliteratorBastion), ObliteratorBastion.Update),
        (nameof(HoardBaneArena), HoardBaneArena.Update),
        (nameof(RagnarokLook), RagnarokLook.Update),
        (nameof(ArenaRescue), ArenaRescue.Update),
        (nameof(RiteSpeedup), RiteSpeedup.Update),
        (nameof(DomainWork), DomainWork.Update),
        (nameof(ElderSprout), ElderSprout.Update),
        (nameof(GearWear), GearWear.Update),
        (nameof(GraveBeacon), GraveBeacon.Update),
        (nameof(BlessingLights), BlessingLights.Update),
        (nameof(ItemDraw), ItemDraw.Update),
        (nameof(TendrilReturn), TendrilReturn.Update),
        (nameof(HushedToil), HushedToil.Update),
        (nameof(RiteEffects), RiteEffects.Update),
        (nameof(DomainVisuals), DomainVisuals.Update),
        (nameof(AetherClearing), AetherClearing.Update),
        (nameof(WeatherStatus), WeatherStatus.Update),
    ];

    private void OnDestroy()
    {
        _harmony.UnpatchSelf();
        _configWatcher?.Dispose();
        SessionState.Clear();
        // No Config.Save() here: BepInEx already saves whenever a setting changes in game, and saving on shutdown
        // would write old values over edits made to the file while the game was running.
    }

    // Edits to the config file apply in game without a restart. Many editors save by writing a new file and renaming
    // it over the old one, which a watcher limited to one file name can miss, so the whole folder is watched and any
    // event only means "check our file". The file is also checked every few seconds in case events don't arrive.
    private void SetupWatcher()
    {
        try
        {
            var watcher = new FileSystemWatcher(Paths.ConfigPath, "*.cfg")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.CreationTime
            };
            watcher.Changed += OnConfigFolderChanged;
            watcher.Created += OnConfigFolderChanged;
            watcher.Renamed += OnConfigFolderChanged;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            _configWatcher = watcher;
            watcher.EnableRaisingEvents = true;
        }
        catch (Exception exception)
        {
            ModLogger.LogWarning($"Config file watcher unavailable, checking {ConfigFileName} every {ConfigCheckInterval} s instead: {exception.Message}");
        }
    }

    private void OnConfigFolderChanged(object sender, FileSystemEventArgs e) => ReloadConfigIfChanged();

    private void RememberConfigFile()
    {
        try
        {
            FileInfo file = new(_configFileFullPath);
            if (!file.Exists) return;
            _lastConfigWrite = file.LastWriteTimeUtc;
            _lastConfigSize = file.Length;
        }
        catch (IOException) { }
    }

    // Reloads once per actual change of the file: watchers often report a save several times.
    private void ReloadConfigIfChanged()
    {
        FileInfo file;
        try
        {
            file = new FileInfo(_configFileFullPath);
            if (!file.Exists || (file.LastWriteTimeUtc == _lastConfigWrite && file.Length == _lastConfigSize)) return;
        }
        catch (IOException) { return; }

        _lastConfigWrite = file.LastWriteTimeUtc;
        _lastConfigSize = file.Length;
        try
        {
            ModLogger.LogDebug(DebugMessages.ReadConfigCalled);
            Config.Reload();
            ModLogger.LogInfo($"{ConfigFileName} changed, settings reloaded.");
        }
        catch
        {
            // Possibly read while still being written: try again on the next check.
            _lastConfigWrite = default;
            _lastConfigSize = -1;
            ModLogger.LogError(DebugMessages.ErrorLoadingConfig);
            ModLogger.LogError(DebugMessages.RequestCheckConfig);
        }
    }
}

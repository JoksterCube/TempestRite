---
name: tempestrite-new-rite
description: Add a new rite to TempestRite (or change an existing rite's cost, category, duration or effect) touching every place a rite lives - enum, rite table, settings section, config migration, translations, compendium, status tooltip, painted icon, category visuals, tests, README row and preview image, changelog and version. Use when the user says "new rite", "new blessing/domain/kinship/reckoning rite", "change X rite cost to", "move X to blessings", or describes a rite with a key item and an effect.
---

# Add or change a TempestRite rite

A rite request usually reads: `<Name> Rite - <N>x <key item> - <category> - <effect>, <duration>`. If the category,
duration or cost is missing, take the closest existing rite as the model and say which you picked. Before
designing anything, run the table (tempestrite-rite-table skill) to see the current rites and to check that the key item isn't taken.

Look up game classes, prefab names and methods with the valheim-game-code skill. Don't guess.

## Other sessions edit this repo too

The user often runs several Claude sessions on TempestRite at once. Right before each of these steps, re-read the
current state: the version and the top CHANGELOG rows, the last `WeatherKind` number.
If the build breaks in files you didn't touch, another session is mid-change. Wait and retry instead of "fixing" its code.

## Checklist (new rite)

Model it on the newest rite of the same category. `grep -rn <ThatRite> --include=*.cs --include=*.json .` lists
everything it touches. Follow it file by file.

1. **`Domain/WeatherKind.cs`**
   - `enum WeatherKind`: append with the next free number. **Never renumber.** The numbers are saved and sent over the network.
   - `Rites` dictionary: name/plain props, `RiteGroup`, `EffectSlot` (null for blessings, kinship and instant rites), `located: true` for Domain and Reckoning area rites.
   - `All` array: insert in its group's position (this is the rites page order).
   - New slot? Add it to `EffectSlot`, `Slots`, and `IsDomainSlot` for Domain rites, so the Grounding Rite ends it.
   - `Flavour` switch, plus `Lasts`/`InstantBlessing`/`Untimed` if it happens at once or has no time limit.
2. **`Settings/Constants.cs`**
   - `RiteSettings` block with the section name `<group number> - <Group> - <Rite name>` (e.g. `4 - Domain - Verdant`, no apostrophes). Nothing gets renumbered: rites sort alphabetically within their group. Groups: 1 Atmospheric, 2 Celestial, 3 Biome Mastery, 4 Domain, 5 Blessings, 6 Kinship, 7 Reckoning.
   - Per-player (unsynced) settings go in `Display`, not the rite's section: `8 - Interface` (hints, messages, shortcuts) or `9 - Effects` (motes, tints, glows, bubbles). Key names there carry the rite's name ("Show Verdant Sparkles").
   - Pass `lasts:`, `amount:`, `minutes:`, `radius:`, `distinctKeys:` (for "3 different X") as needed.
   - Its extra `ConfigInfo` settings. Use **multipliers, not percentages** (2 = twice as fast, 0.8 = 80%), as the user asked for all settings.
   - `Messages` props (`XName`, `XPlain`, `XFlavour`, status tooltip, ended/done messages), `CompendiumTexts.X`, and `Rpcs.X` if the server must message players.
3. **`Settings/PluginConfig.cs`**: `Add(WeatherKind.X, X)` plus the `ConfigOptions.Config(...)` fields. Binding order sets the order inside a section; per-player settings go in the `8 - Interface` / `9 - Effects` blocks.
4. **`Translations/English.json`**: every key the new `Text.Get(...)` calls use. `TranslationTests` fails on missing *and* unused keys, and on `{0}` placeholders that don't format.
5. **The effect** in its own `Domain/<Name>.cs` (and `Patches/<Name>Patches.cs`). It must work online:
   - Decide who runs it: the server (with the mod), the ZDO owner, or each client for itself. Unmodded players must not break it.
   - Effects others should see (particles, emotes, lightning) must be sent to them (routed RPC or ZDO data), not just shown locally.
   - Instant rites: hook in `Domain/Networking/WeatherSync.cs` (see ElderSprout) and register RPCs in `Patches/SessionPatches.cs`.
   - Wrap risky patches with `PatchGuard`, as the existing patches do.
6. **Category extras**
   - **Domain**: a color and particle shape for its obliterator "identity" in `DomainVisuals.cs`, hover text on affected objects ("being sped up by ..."), sparse particles on affected objects (cap the count, keep them small and slow; the user asked for that more than once), an "ended" message in `WeatherState.cs`, and the radius is the shared *Domain Rites Radius*.
   - **Blessings**: a status tooltip in `WeatherStatus.cs`, and `addstatus SE_TempestRite_<Kind>`/`clearstatus` must work (`StatusCommands.cs`).
   - **Kinship**: `KinshipRites.cs` (status name, emote, immunity during the emote, cooldown that persists over death, only one kinship rite at a time).
   - **Reckoning arenas** (Crimson/Ragnarök/Hoard-Bane style): use `IncursionArena` and its guards (sealed bubble, no building, no other rites but Sanctuary, spawners suppressed, obliterator protected). Scale rewards with the players inside at the end (`PartyReward`).
7. **Compendium**: `RiteCompendium.cs` switch entry (uses `CompendiumTexts.X` with config values filled in).
8. **Icon**: a painter in `Domain/WeatherIcons.cs`. Add a `case` in `Picture`, a `XPicture(Canvas)` method, and its line in the header comment. Follow the valheim-mod-art skill: a thematic picture of the *mechanic*, **never the key item** (it can be changed in config), in the same painted style as its neighbours.
9. **Tests**: a block in `Tests/TempestRite.Tests/WeatherTests.cs` like the ElderSprout one (number, group, `Lasts`, `HasSlot`, `Personal`, `ForEveryone`, `Clashes`, `All` contains it). Add logic tests for any maths (rewards, multipliers).
10. **Build and test**: `dotnet build -c Release` (0 warnings: fix CS8604 nullability warnings), then `dotnet test Tests/TempestRite.Tests` (the tests reference `bin/Release`).
11. **README**: a row in its group's rite table (`<img src=".../Docs/Rites/<Name>.png" width="48">`, cost written as `**2x Surtling cores**` (no count for a single item: `**Copper ore**`), name, effect; `<Name>` is the rite's English name without " Rite", letters only: "Fortune's Boon Rite" -> `FortunesBoon`, "Ragnarök Rite" -> `Ragnarok`), and any new settings in the settings table.
12. **Preview image**: `python Tools/Icon/rite_previews.py` (needs the art venv, see valheim-mod-art) renders `Docs/Rites/<Name>.png` from the C# painter, named after the rite as players know it (`IconPreview.FileName`), not its enum member (Equilibrium.png, not Reset.png). A rename of a rite renames its image: update the README links and delete the old file. Remind the user it must be committed and pushed, or the README
    shows a broken image. If the rite has a visible in-world effect (weather, light, arena, map pin, HUD), suggest a
    screenshot of it (see the valheim-mod-readme screenshot review).
13. **Version + changelog** (valheim-mod-release skill): bump the patch version in all four places, add a CHANGELOG row written for players. Adding a rite no longer renumbers sections, so existing settings keep their values.
14. Offer to deploy with the valheim-test-deploy skill.

## Changing an existing rite

- **Cost, duration or any default change**: change the default, and register the old one so configs still on it move to the new default once, while values players changed are kept: `.OldDefault("<this release version>", oldValue)` on a `ConfigInfo`, or `.OldKeyItem(...)` / `.OldEnvironments(...)` on a `RiteSettings` (Settings/OldDefaults.cs). Use the version that ships the change (for a default changed in an older release without this, the current one). Then a changelog row: "**X Rite now costs N Y** (was ...). Settings still on the old default update on their own; changed ones are kept."
- **Category move** (e.g. "move Tailwind to blessings"): change its `RiteGroup`/`EffectSlot`, its place in `All` (also decides which rite keeps a shared key item), the group part of its section name, the README table, the tests and the compendium text.
- **Rename**: display texts only (English.json, README). Keep the enum member and number unless the user asks otherwise.

## Report

Say what the rite does in game terms, its cost/duration/category, which settings it added, and what to test in game. List anything you couldn't verify without playing (visuals, other players seeing it).

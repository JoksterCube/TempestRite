---
name: tempestrite-rite-table
description: Print an up-to-date table of every TempestRite rite (name, category, key items, amount, duration/cooldown, radius, default, effect), read from the source code. Use when the user asks for a list or table of rites, costs, categories, durations or effects, or before balancing or reviewing rites.
---

# TempestRite rite table

Don't build the table from memory or old conversation: costs and durations change often. Run the script:

```bash
python -I .claude/skills/tempestrite-rite-table/rite_table.py          # main table
python -I .claude/skills/tempestrite-rite-table/rite_table.py --why    # adds the long description column
```

It reads `Settings/Constants.cs` (the `RiteSettings` blocks), `Settings/PluginConfig.cs` (`Add(WeatherKind.X, ...)`),
`Domain/WeatherKind.cs` (category, list order, untimed rites) and `Translations/English.json` (display names).

## Reading the columns

- **#**: the rite's place on the rites page (`WeatherKinds.All` order). Config sections are named `<group number> - <Group> - <Rite>` (`7 - Reckoning - Crimson`) and carry no per-rite number.
- **Minutes**: the default duration. For **Kinship** rites it is the wait before a player can receive it again. `at once` = no duration; `until won` = Ragnarök and Hoard-Bane (they end when the fight ends).
- **Radius**: the rite's own radius, or `100 m (Domain)` = the shared *Domain Rites Radius* setting.
- These are **code defaults**. A server's `.cfg` can override them. To see the test server's values use `server.ps1 -Action get-config -Mod TempestRite` from the valheim-test-deploy skill.

## Shaping the answer

- If the user asks for specific columns ("name, key item, quantity, category, effect"), print only those, in their order.
- Key items are prefab names, and many don't match the in-game name (`QueenDrop` = Majestic carapace, `YagluthDrop` = Torn spirit, `MushroomSmokePuff` = Smoke puff). For a player-facing list, take names from the README rite table or the game's localization. Don't guess.
- For reward tables (Crimson/Ragnarök/Hoard-Bane per player count), read `Domain/PartyReward.cs`, `RagnarokLoot.cs`, `HoardBaneLoot.cs` and their tests. Don't invent formulas.
- If the script fails after a refactor (e.g. a renamed file), fix the script rather than writing the table by hand.

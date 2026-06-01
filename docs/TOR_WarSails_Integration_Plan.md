# TOR ⨯ War Sails — Integration Plan

Status: **Draft / scoping**
Target game version: **v1.3.15** (War Sails era — matches current `SubModule.xml` dependency versions)
Owner: TBD

---

## 1. Goal

Integrate The Old Realms (TOR) total conversion with TaleWorlds' **War Sails** naval-warfare
DLC (ships, sea travel on the campaign map, ports, naval battles, marine troops) **without
forcing the existing playerbase to own the DLC**.

## 2. Chosen architecture — a separate optional module

Build a new sibling module, **`TOR_WarSails`**, rather than folding naval code into `TOR_Core`.

```
Native → SandBoxCore → SandBox → StoryMode → CustomBattle
       → TOR_Armory → TOR_Environment → TOR_Core → TOR_WarSails
```

Rationale:

- **DLC stays optional.** Players without War Sails simply don't enable `TOR_WarSails` and
  play TOR exactly as today (zero impact). Owners enable it to turn naval features on.
  Folding a hard `<DependedModule>` for War Sails into `TOR_Core` would lock out everyone
  who doesn't own the DLC.
- **Clean separation.** All War Sails-specific Harmony patches, troops, items, and the
  naval-capacity data fixes live in one place; `TOR_Core` stays DLC-agnostic.
- **Matches existing structure.** TOR is already split into `TOR_Core`, `TOR_Armory`,
  `TOR_Environment` — this follows the same pattern.

Loads last, so it can patch/extend everything beneath it.

## 3. Current state (baseline)

No naval mechanics exist yet. All `naval` / `ship` / `sail` references in the repo are
**lore flavor text** (Imperial Navy, Nordland Second Fleet at Dietershafen, High Elf
Seaguard, Norscan longships, Marienburg/Le Havre ports).

Two places where the War Sails API already *collides* with TOR today:

| # | Location | Issue |
|---|----------|-------|
| 1 | `CSharpSourceCode/HarmonyPatches/CustomResourcePatches.cs:78` | Passes `troopToUpgrade.GetTraitLevel(DefaultTraits.NavalSoldier) != 0` into the party-screen upgrade VM. TOR troops never set the trait → always false. Harmless, but proves the naval API is present. |
| 2 | `CSharpSourceCode/CampaignMechanics/Companions/TORCompanionsCampaignBehavior.cs:119` | `TeleportHeroAction` was abandoned because the vanilla delay calc reads **clan naval-navigation capacity with no null check**, and TOR clans have no naval data → null-ref. A War Sails system actively biting the mod. |

Module load order today (`TOR_Core.csproj` `StartArguments`, `SubModule.xml`
`<DependedModules>`): `Native, SandBoxCore, SandBox, StoryMode, CustomBattle, TOR_Armory,
TOR_Environment` — **War Sails is absent.**

## 4. Workstreams

### 4.1 Module scaffold (low effort)
- [ ] Create `TOR_WarSails/` module folder: `SubModule.xml`, `.csproj`, `SubModule.cs`
      (extends `MBSubModuleBase`).
- [ ] `<DependedModules>`: `TOR_Core` + the War Sails module (confirm exact module ID).
- [ ] Add to local launch `StartArguments` load order, after `TOR_Core`.
- [ ] Decide hard vs. soft dependency (see §5).

### 4.2 Runtime DLC sanity guard (low effort)
- [x] On `OnSubModuleLoad`, check the War Sails module is present (`ModuleHelper`).
- [x] Surface a clear in-game message confirming integration is active, or warning if the
      DLC is missing. (War Sails is a hard `DependedModule`, so absence should be
      unreachable — this is belt-and-suspenders, not feature gating.)

### 4.3 Fix existing collisions (low–medium effort)
- [ ] **Clanless-hero teleport null-ref.** Root cause (confirmed): the vanilla teleport
      **delay calculation dereferences `hero.Clan`** to read naval-navigation capacity with
      no null check. Unhired wanderers have `Clan == null` → null-ref. (`SkillTrainerBehavior.cs:93`
      calls the same `TeleportHeroAction.ApplyDelayedTeleportToParty` safely because those
      heroes are clan members.) Proper fix: a Harmony guard on the vanilla naval-capacity
      method that treats a null clan as "no naval capacity".
      *Blocked in CI/source-only environments:* the exact target method/signature lives in
      `TaleWorlds.CampaignSystem.dll`, which is not present here — needs a War Sails game
      install (or the decompiled signature) to author and compile-verify the patch.
- [ ] Once fixed, revert the workaround in `TORCompanionsCampaignBehavior` (re-enable the
      delayed teleport for wanderer travel simulation).
- [ ] Audit the `NavalSoldier` usage in `CustomResourcePatches` once real naval troops exist.

### 4.3b Bridge module infrastructure (DONE)
- [x] Harmony bootstrap in `TOR_WarSails.SubModule` (`PatchAll` over the module assembly),
      mirroring `TOR_Core`'s pattern, so future patches under a `Patches` namespace apply
      automatically.
- [x] Localization strings (`ModuleData/tor_warsails_strings.xml`) for the module's status
      messages, registered via a `GameText` XmlNode in `SubModule.xml`.

### 4.4 Troops, items, traits (medium effort — mostly content)
- [ ] Naval/marine troop entries; assign `DefaultTraits.NavalSoldier` to appropriate units.
- [ ] Ship items / crew definitions.
- [ ] Wire into faction rosters where lore supports it: Imperial Navy (Nordland Second
      Fleet), High Elf Seaguard marines, Norscan longships, Marienburg buccaneers.

### 4.5 Cultures & settlements (medium effort — data)
- [ ] Port-settlement templates and per-culture naval capability flags
      (`tor_cultures.xml`, settlement data).
- [ ] Mark existing coastal settlements (Dietershafen, Marienburg, Le Havre, Se-Athil, …)
      as ports where appropriate.

### 4.6 Campaign-map naval data (HIGH effort — the real cost)
- [ ] Author **navigable water, port nodes, and sea lanes** into the custom Old World map.
      TOR's coastline is currently decorative (no naval navmesh / port nodes).
- [ ] **Blocker:** requires the game's map/scene assets, which per the README are *not*
      in this repo. Cannot be completed from source alone.

### 4.7 Scenes & prefabs (high effort — content/assets)
- [ ] Naval battle scenes and ship prefabs (do not exist in TOR).

## 5. Decisions

1. **Hard dependency on War Sails (RESOLVED).** The bridge **assumes the DLC is owned** —
   War Sails is a hard `<DependedModule>`, and the project hard-references its assemblies in
   the same style `TOR_Core` references the base game (`<HintPath>` + `<Private>False</Private>`).
   No reflection. Players without the DLC simply don't enable `TOR_WarSails`.
2. **Exact War Sails module ID and assembly names (OPEN).** Currently a placeholder
   (`WarSails`, marked `TODO`) in `SubModule.xml`, `SubModule.cs`, and the `.csproj`. Confirm
   from a War Sails install before the references can compile.
3. **Scope of v1** — ship the C#/data layers (§4.1–4.5) first as a "naval-aware" bridge,
     and treat map/scene work (§4.6–4.7) as a separate later milestone?

## 6. Effort summary

| Workstream | Effort | Notes |
|------------|--------|-------|
| Module scaffold + detection (4.1–4.2) | Low | Self-contained, safe |
| Collision fixes (4.3) | Low–Med | Improves base mod regardless of DLC |
| Troops/items/cultures (4.4–4.5) | Med | Content authoring; lore already supports it |
| Map naval data (4.6) | **High** | Needs map assets not in repo |
| Scenes/prefabs (4.7) | **High** | Needs art/scene assets |

The code and data wiring is a few days. The dominant cost is map + scene/asset content,
much of which cannot be done from this repository alone.

## 7. Proposed first milestone

Scaffold `TOR_WarSails` (4.1), add runtime DLC detection (4.2), and move/fix the two
existing collisions into it (4.3). Self-contained, safe, and improves the base mod even
before any naval content lands.

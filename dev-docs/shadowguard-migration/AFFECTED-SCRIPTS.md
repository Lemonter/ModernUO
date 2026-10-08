# Time of Legends port (Shadowguard + Eodon + Underworld + Vice vs Virtue) — affected/touched scripts log

Running log kept per the user's request, so existing Mahaon scripts that need
adapting to the new environment can be found later. Updated as work proceeds.
Plan: `C:\Users\lenovo150\.claude\plans\cryptic-mapping-dragonfly.md`.

## New files (additive — low risk, nothing existing depends on these yet)

### Prerequisites
- `Projects/UOContent/Items/Functional/BaseDecayingItem.cs` — new base class, ported from ServUO `Scripts/Items/Functional/BaseDecayingItem.cs`
- `Projects/UOContent/Items/Clothing/LeatherTalons.cs` — new gargoyle boot-slot armor base
- `Projects/UOContent/Items/Clothing/GargoyleHalfApron.cs` — new gargoyle waist-slot base
- `Projects/UOContent/Items/Jewels/GargishEarrings.cs` — new gargoyle earring base
- `Projects/UOContent/Items/Weapons/Swords/PaladinSword.cs` — new weapon base

### Shadowguard (`Projects/UOContent/Engines/Shadowguard/`)
- `ShadowguardController.cs` — `ShadowguardController : Item`
- `ShadowguardRegion.cs` — `ShadowguardRegion : BaseRegion`
- `EncounterType.cs` — flags enum
- `ShadowguardInstance.cs`
- `ShadowguardEncounter.cs` (abstract base)
- `Encounters/BarEncounter.cs`, `OrchardEncounter.cs`, `ArmoryEncounter.cs`, `FountainEncounter.cs`, `BelfryEncounter.cs`, `RoofEncounter.cs`
- `Mobiles.cs` (9 creatures), `Bosses.cs` (4 bosses + base), `Items.cs` (17 props), `Artifacts.cs` (27 loot items), `ExitEntry.cs`
- `Addons.cs` — 5 placeholder single-component `BaseAddon` stand-ins (BarAddon, OrchardAddon, ArmoryAddon, BelfryAddon, ShadowguardFountainAddon). ServUO's real multi-component layouts weren't in any fetched source file; swap these for the real decorative structures later if found.
- `ShadowguardGump.cs`
- `ShadowguardPersistence.cs` — new `GenericPersistence` subclass (see decision below)

**Build status: compiles clean (0 errors, 0 warnings) as of this update.** Not yet in-game verified — see plan's Verification section (queue each encounter, kill each boss, save/load restart test).

### Eodon bestiary — DONE, now a real ServUO port (superseded an earlier from-scratch version)

**Correction, 2026-08-25**: the original search of ServUO's tree (which concluded ServUO has
zero Eodon/Underworld/VvV content) used a `grep` pattern (`"path":"..."`, no space) that
didn't match the GitHub API's actual JSON format (`"path": "..."`, with a space) — every
match silently failed. Re-checked with the fixed pattern: **ServUO's `master` branch has
substantial real Eodon content** (`Scripts/Mobiles/Normal/*.cs` per-creature files, a full
Hawkwind/MyrmidexThreat/Valley-of-One quest chain, `EodonTribesman.cs`). This replaced the
31-type from-scratch bestiary written before the mistake was caught — most of it is now a
real port, not original content. Source cached at `scratchpad/servuo-eodon-src/` (session
temp dir). Simplifications common to all the ported types: RunUO/ServUO-era
`SetWeaponAbility`/`SetSpecialAbility`/`SetAreaEffect`/`SetMagicalAbility` (a creature
special-move framework) and `AttacksFocus`/`IsChampionSpawn`/`SetToChampionSpawn`/
`DragonBlood` don't exist in this codebase, so those calls/overrides are dropped everywhere
they appeared — stat/loot/resist values are otherwise verbatim from ServUO. In-game display
names are Russian per [[feedback_russian_language]].

- `Projects/UOContent/Mobiles/Monsters/Reptile/Eodon/Dinosaurs.cs` — **real port**: Anchisaur, Archaeosaurus, Dimetrosaur, Gallusaurus, Najasaurus, Saurosaurus, Allosaurus (`Scripts/Mobiles/Normal/*.cs`), TRex (`Scripts/Quests/Eodon/Valley of One Quest/Creatures.cs`, minus its elaborate MovementPath-based "freeze wall" OnThink spectacle — RunUO-era `IPooledEnumerable` this codebase doesn't have).
- `Projects/UOContent/Mobiles/Monsters/Arachnid/Eodon/Myrmidex.cs` — **real port**: MyrmidexLarvae/Drone/Warrior (`Scripts/Mobiles/Normal/Myrmidex*.cs`). The `Server.Engines.MyrmidexInvasion` event system (alliance-based IsEnemy, region-gated rare drops) doesn't exist here, so IsEnemy falls back to default and the MyrmidexEggsac/MoonstoneCrystalShard drops are dropped (those item types don't exist either).
- `Projects/UOContent/Mobiles/Animals/Eodon/WildBeasts.cs` — **real port**: WildTiger/WildWhiteTiger/WildBlackTiger (`Scripts/Mobiles/Normal/WildTiger.cs`, a `BaseMount` — pelt-carving and PeculiarSeed rare loot dropped, neither the pelt items nor `LootPack.PeculiarSeed1-4` exist here), SilverbackGorilla (`Scripts/Mobiles/Normal/SilverbackGorilla.cs`), GreatApe (`Valley of One Quest/Creatures.cs`, `GreatApe(bool teleports)` overload matches XML's `greatape,true` XmlSpawner constructor-arg syntax), DesertScorpion (`Scripts/Mobiles/Normal/DesertScorpion.cs`). Gorilla/ape OnThink specials (banana-throw, teleport ambush, barrel-throw) dropped — RunUO-era `IPooledEnumerable`/`ColUtility` and a "GreatApeLair" region this codebase doesn't have.
- `Projects/UOContent/Mobiles/Monsters/Elemental/Eodon/VolcanoElemental.cs` — **real port** (`Valley of One Quest/Creatures.cs`).
- `Projects/UOContent/Mobiles/Monsters/Humanoid/Eodon/EodonNatives.cs` — `TribeWarrior`/`TribeShaman`/`TribeChieftan` are now a **real port** of `Scripts/Mobiles/Normal/EodonTribesman.cs` (shared `BaseEodonTribesman` abstract parent, real `EodonTribe` enum with 6 clans incl. Barrab which the earlier version missed; `Masteries`/`MyrmidexInvasionSystem` IsEnemy branch dropped; `SetWearable(item, hue)` → `EquipItem` via a `Wear()` helper since that RunUO API doesn't exist here). Each still keeps a `(string tribe)` overload for Eodon.xml's `tribewarrior,Barrab` XmlSpawner syntax (`TribeChieftan` keeps that exact spelling, not "Chieftain", so `AssemblyHandler.FindTypeByName` still resolves it) plus a parameterless random-tribe overload for XML entries with no clan argument. **`WanderingShaman`, `BarakoHighChief`/`JukariHighChief`/`KurakHighChief`/`SakkhraHighChieftess`/`UraliHighChieftess`, `Hawkwind`, `SirGeoffery` remain original content** — searched ServUO's full tree and found no equivalent for any of them.
- `Projects/UOContent/Items/Decorative/Eodon/EodonProps.cs` — `NestWithEgg`, `CubeEnclosure` remain original content (placeholder statics); ServUO has a same-named `Scripts/Services/CleanUpBritannia/Items/NestWithEggs.cs` but it's a different, unrelated decoration system — not checked in detail, worth a look if revisited.

`Bullfrog` (lowercase in the XML) needed no new class — `AssemblyHandler.FindTypeByName` resolves case-insensitively by default and the existing `BullFrog` class already matches.

**Hit one real bug while rewriting this file**: a duplicate `[SerializationGenerator(0, false)]` attribute accidentally applied twice to `WanderingShaman` made the ModernUO.Serialization.Generator throw `CS8785` ("HintName ... must be unique"), which cascaded into ~150 unrelated CS0535/CS0246 errors across the whole project (every other codegen'd type lost its generated members) — a good pattern to recognize if this class of cascading error shows up again: check for a duplicated `[SerializationGenerator]` attribute before assuming a cache/generator bug.

**Build status: compiles clean (0 errors, 0 warnings).** Not yet in-game verified (spawn a point from `Eodon.xml` and confirm the monsters appear).

### Underworld — base done (JustUO), now expanding with real ServUO content

**Correction, 2026-08-25**: the initial "ServUO has no Underworld content" conclusion was the
same grep-pattern bug described in the Eodon section above — ServUO's `master` branch
actually has a fuller Underworld than JustUO's (Navrey's Lair pillar-puzzle boss encounter,
Maze of Death, Puzzle Room, Experimental Room). The JustUO port below stands as the base
layer (region + 2 regular creatures + basic boss); ServUO's real content is now being layered
on top per the user's "расширить до полной версии ServUO" choice. Source for both cached at
`scratchpad/justuo-src/` and `scratchpad/servuo-underworld-full/` (session temp dir).

- `Projects/UOContent/Regions/UnderworldRegion.cs` — new region type. Inherits `NoTravelSpellsAllowedRegion`/`DungeonRegion` (both already exist in this codebase) for free: no recall/mark/gate/sacred-journey travel and no player housing, so the only new code is the entrance flavor message. Registered in `RegionJsonRegistration.cs`.
- `Distribution/Data/regions.json` — the "Underworld" region entry **already existed** (real OSI coordinates, TerMur Area 898,808→1280,1231) as a plain `DungeonRegion`; changed its `$type` to `UnderworldRegion` to pick up the new behavior. No new region geometry was needed.
- `Projects/UOContent/Mobiles/Monsters/Underworld/Gremlin.cs`, `EnragedEarthElemental.cs` — 2 regular creatures (JustUO source).
- `Projects/UOContent/Mobiles/Monsters/Underworld/Navrey.cs` — hybrid: JustUO's web-throwing combat mechanic (functional, kept) + ServUO real `Navrey.cs`'s `Spawner`/`UsedPillars` fields and `OnDeath → Spawner.OnNavreyKilled()` hook (added, to integrate with the pillar-puzzle below). Loot simplified: ServUO's ~2.5% chance at one of 2 unique TOL artifacts (NightEyes, Tangle1) dropped — neither exists here, and "Tangle" already names an unrelated ML creature (`Mobiles/Monsters/ML/Blighted Grove/Tangle.cs`) — ServUO itself renamed its own to "Tangle1" for the same collision. "Green with Envy" quest-NPC (Vernix) gating on `EyeOfNavrey` dropped too — no such quest chain exists here, so it's a plain rare trophy drop.
- `Projects/UOContent/Items/Underworld/AcidPopper.cs`, `EyeOfNavrey.cs`, `NavreyParalyzingWeb.cs` — supporting items (JustUO source). `NavreyParalyzingWeb`'s DateTime end-time field uses `[SerializableField]` + `[AfterDeserialization]` to resume its self-delete timer, same pattern as `ShadowguardController`.
- `Projects/UOContent/Items/Underworld/NavreysController.cs`, `NavreysPillar.cs` — **real ServUO port** (`Scripts/Services/Dungeons/Underworld/Navrey's Lair/*.cs`): the "three ruins" puzzle — double-click each pillar within its random 3/6/9-second hot window, and once all three are lit at once a 30-second rock-rain barrage does massive damage to Navrey (and anyone standing near her). `Map.GetSpawnPosition` (doesn't exist here, same fix as Shadowguard) replaced with a manual nearby-point search; RunUO-era `HuedEffect`/`Effects.SendPacket` replaced with the existing `Effects.SendMovingEffect` helper; `IPooledEnumerable` replaced with `GetMobilesInRange`. Hit one real generator bug here: `[SerializableFieldChanged]` callback methods must match the exact `(TOld, TNew)` signature the codegen expects, not a parameterless one — worth remembering for future `[SerializableField]`+custom-setter-logic work.

**Update, 2026-08-25 (session 2) — Maze of Death + Puzzle Room ported after all.** The
earlier conclusion above ("no ICircuitTrap framework exists, not just missing types but a
missing UI framework") turned out to be an incomplete check — it verified the framework
wasn't already *implemented* in this codebase, but never checked whether ServUO's own
source for it was small enough to realistically port. It is: `Scripts/Gumps/Traps/
CircuitTrap.cs` (291 lines) + the training-kit file (unused here) total well under 15KB, and
the only real obstacle was that it subclasses ServUO's own `BaseGump` convenience wrapper
(`Refresh()`/`User`/`GetType()`-based close), which doesn't exist here — same mechanical
rewrite against our real `Server.Gumps.Gump` already done for every other TOL gump this
session (ShadowguardGump, VvV gumps, etc).

Ported:
- `Projects/UOContent/Gumps/Traps/CircuitTrap.cs` — `ICircuitTrap` interface + `CircuitCount`
  enum + `CircuitTrapGump : Gump` (the reusable "close the circuit" grid mini-game). Not
  Underworld-specific — lives in `Gumps/`, not `Items/Underworld/`, since anything trapped/
  locked could reuse it later.
- `Projects/UOContent/Items/Underworld/MazePuzzleItem.cs` — the Puzzle Room's circuit-trap
  board, real `ICircuitTrap` implementation; failing it shocks the holder for damage, solving
  it awards a Copper/Gold Puzzle Key.
- `Projects/UOContent/Items/Underworld/MastermindPuzzleItem.cs` — the third Puzzle Room
  board, riding entirely on the existing `PuzzleChest` (`Engines/Khaldun/PuzzleChest.cs`) —
  confirmed the earlier note right, this really was a thin wrapper once attempted. Its
  ServUO reward (an `ExperimentalGem`) is dropped since Experimental Room wasn't ported (see
  below) — completion still pays out through `PuzzleChest`'s own real gold/gem/gear loot.
- `Projects/UOContent/Items/Underworld/{PuzzleBox,PuzzleBook,PuzzleRoomKeys,MagicKey,
  PuzzleRoomTeleporter}.cs` — the supporting Puzzle Room fixtures (get-a-board stones,
  instructions book, Copper/Gold puzzle keys, the 30-minute magic key gating entry,
  MagicKey-gated teleporter). One real bug fix ported along the way: ServUO's own
  `MagicKey.Decay()` checks against `new Rectangle2D(1234, 1234, 10, 10)` — an obvious
  placeholder that doesn't match the real Puzzle Room bounds anywhere in the source —
  corrected to the actual room rectangle.
- `Projects/UOContent/Items/Underworld/{GoldenCompass,RolledMapOfTheUnderworld,
  UnderworldPuzzleBox,UnderworldPuzzleItem,MazeRewards}.cs`,
  `Projects/UOContent/Gumps/{CompassDirectionGump,UnderworldPuzzleGump}.cs`,
  `Projects/UOContent/Regions/MazeOfDeathRegion.cs` — the Maze of Death: a trap corridor
  where only a hidden "safe path" (seeded + randomized at server start, same shape as
  ServUO's own) avoids random damage traps, a compass item that shows the safe direction via
  gump, and an unrelated "shift the colored blocks" Mastermind-style puzzle box
  (`UnderworldPuzzleItem`/`UnderworldPuzzleGump`) handed out by `UnderworldPuzzleBox`. Reward
  table for the block puzzle: 5 of ServUO's 13 reward types (`VoidEssence`,
  `SilverSerpentVenom`, `ScouringToxin`, `ToxicVenomSac`, `LuckyCoin`) don't exist anywhere in
  this codebase or in ServUO's own shared reward files either (dead-end lookups) — replaced
  with the existing Mahaon `EnchantedEssence` Imbuing material rather than inventing new item
  classes for a single reward table; the other 8 real decorative rewards ported unchanged.
- `Projects/UOContent/Items/Underworld/UnderworldPuzzleController.cs` — world-placement
  controller, same "no separate GM command, place fixtures in the constructor" pattern as
  `ShadowguardController` (per the shard owner's earlier explicit preference). Real OSI
  coordinates from `Generate.cs`. Two of `Generate.cs`'s own fixtures (a `MetalDoor2` pair
  sitting right on top of the teleporter pair) were dropped as redundant — they duplicate the
  teleporter's own MagicKey gate, and `Generate.cs` has a copy-paste bug right there
  (`WeakEntityCollection.Add("sa", door)` where it should read `door2`), suggesting dead
  leftover rather than a real second path in.

**Update, 2026-08-25 (session 3) — Experimental Room and FountainOfFortune ported after
all.** Both self-contained, as predicted below; no changes needed to anything else in this
log to add them.
- **Experimental Room** (`Items/Underworld/{ExperimentalGem,ExperimentalRoomController,
  ExperimentalRoomDoor,ExperimentalRoomChest,ExperimentalBook,ExperimentalRoomRewards}.cs`,
  `Regions/ExperimentalRoomRegion.cs`) — real ServUO port
  (`Scripts/Services/Underworld/ExperimentalRoom/*.cs`), the hue-matching puzzle: an
  `ExperimentalGem` cycles through named colors on a timer, and the holder must stand on the
  matching colored floor region before it reaches an "extreme" state or gets kicked out.
  `ExperimentalRoomDoor`/`ExperimentalRoomBlocker` share one source file, ported to one file
  here too. `ExperimentalRoomRewards.cs`'s 8 trivial decoration drops (`Stalagmite`,
  `Flowstone`, `CanvaslessEasel`, `HangingChainmailLegs`, `HangingRingmailTunic`,
  `PluckedChicken`, `ColorfulTapestry`, `TwoStoryBanner`) ported verbatim. Two real upstream
  ServUO bugs found and fixed while porting (see the header comments on
  `ExperimentalGem.cs` and `UnderworldPuzzleController.cs` for the full detail): (1)
  `GetRevertedHue`'s DarkGreen case returned a no-op hue instead of DarkGreen's actual Normal
  state (LightGreen) when standing on its SlowOpposite tile; (2) `Generate.cs`'s "Room 3 to
  4" door pair used `Room.RoomTwo` instead of `Room.RoomThree`, which would have let a player
  skip the third room's puzzle entirely once the door (not just the blocker) is accounted
  for. `ExperimentalRoomController`'s 24-hour retry cooldown is deliberately persisted here
  (`[SerializableField]`) even though the real source never actually saves it (Deserialize
  resets it fresh every load) — reads as an oversight against a shard that restarts far more
  than a live OSI server would, not an intentional design choice.
- **FountainOfFortune** (`Items/Underworld/FountainOfFortune.cs`) — real ServUO port
  (`Scripts/Items/Addons/FountainOfFortune.cs`), a standalone SA-era buff/resurrection
  fountain addon near the Maze of Death, unrelated to either puzzle's mechanics. Two of its 4
  real item rewards (`SolesOfProvidence`, `GemologistsSatchel`) don't exist here — dropped,
  keeping `RelicFragment`/`EnchantedEssence` (the same substitution already used for the Maze
  of Death's own reward table). A third upstream bug fixed: the buff branch's
  `Utility.Random(4)` only ever reached 4 of its 6 `switch` cases — the special-protection
  and balm-boost buffs were dead, unreachable code — changed to `Utility.Random(6)`.
  ModernUO's `ResurrectGump` has no result-callback parameter (unlike ServUO's), so the
  10-minute re-prompt cooldown is set when the gump is *sent* rather than from the player's
  answer — same practical throttling effect, simpler trigger point. `LuckTable`/
  `SpecialProtectionTable`/`BalmBoostTable` are exposed as public static queries
  (`GetLuckBonus`/`UnderProtection`/`HasBalmBoost`) exactly like the original but, matching
  its own "standalone, self-contained" scope, aren't wired into this codebase's loot/combat/
  poison systems — a later pass can hook them in if wanted.

`Generate.cs`'s cannon/cannoneer placement (`CheckCannoneers`) and its "reveal tile" grid
near a different TerMur coordinate range (1182-1192/1120-1134) look unrelated to either
puzzle system — likely leftover world-decoration for something else entirely — and were
left alone rather than ported blind.

Both wired into the existing `UnderworldPuzzleController` (renamed in scope, not in code, to
also cover Experimental Room + the fountain) rather than a second controller — same "one
controller places everything for this themed area" pattern as Shadowguard.

Build verified clean (0 errors, 0 warnings) after this addition, same as every other TOL
piece this session. Not yet verified in-game — same standing caveat as the rest of this log.

**Build status: compiles clean (0 errors, 0 warnings).** Not yet in-game verified.

## Existing files touched (real risk surface — review these when adapting Mahaon scripts)

- **`Data/regions.json` / `RegionJsonRegistration.cs`** — RESOLVED, neither needed touching. `ShadowguardRegion` is fully dynamic/item-tracked (one instance created per `ShadowguardInstance` at runtime, registered via `Region.Find`), never loaded from JSON — confirmed no edits needed here.
- **Command namespace** — RESOLVED, no collision. Only one bespoke command remains: `"ShadowguardCompleteAllRooms"` (GameMaster, debug helper for testing — deferred, per user's "отложим тесты на потом"), renamed from ServUO's generic `CompleteAllRooms` to avoid the collision class seen in the earlier `ImportSpawners` bug (`Engines/Spawners/Commands/ImportSpawnersCommand.cs` vs `Engines/XmlSpawner/SpawnerExporter.cs`). World placement itself no longer has a dedicated command (see below). Grepped the full `Projects/UOContent` tree — the name is unique.
- **`Items/Talismans/BaseTalisman.cs`** — ServUO's `SetProtection`/creature-specific-resist mechanic is scoped wider there (usable from `GargishEarrings` too); ModernUO currently only has it on `BaseTalisman`. Decision: extend the mechanic onto the new `GargishEarrings` base directly (self-contained, doesn't touch `BaseTalisman.cs` itself) rather than generalizing a shared mixin — smaller blast radius, revisit if a future artifact needs the same pattern a third time.

### Pre-existing bugs fixed to unblock the build (unrelated to Shadowguard scope)

These were prior uncommitted Mahaon "custom house" work (written earlier, never build-verified) that broke compilation once touched during this session:
- `Projects/UOContent/Items/Misc/Mahaon/MahaonAreaMarkingTool.cs` — missing `using Server.Network;` for `NetState`, added.
- `Projects/UOContent/Items/Misc/Mahaon/MahaonCityHouseSign.cs` — same fix.

## Design decisions worth remembering later

- **Persistence**: encounter/instance state saved via a new `ShadowguardPersistence : GenericPersistence` (mirrors `Systems/MahaonCities/CityControlSystem.cs`'s pattern), NOT baked as codegen-serialized fields on `ShadowguardController` itself. `ShadowguardEncounter` stays a plain composed class close to ServUO's shape.
- **IChopable → IAxe**: ServUO's `ShadowguardCanal : BaseDecayingItem, IChopable` (`OnChop(Mobile)`) ported to ModernUO's existing `IAxe` (`Axe(Mobile, BaseAxe)`, dispatched from `Engines/Harvest/Core/HarvestTarget.cs:209`) instead of inventing a parallel interface.
- **World placement**: no bespoke admin command — per explicit user direction ("не нужна отдельная команда, пусть будет наравне с другими"), removed the earlier `AddShadowguardController` command entirely. The controller is now placed the same way as any other world Item in this codebase: the standard generic `[add ShadowguardController` (built-in `AddCommand`, driven by the existing `[Constructible]` attribute). Its 4 `MetalDoor`s (lobby, hue 1779), `AnkhWest`, and 4 landmark `Static(19343)` decorations — all OSI-fixed coordinates, ported verbatim from ServUO's `Controller.cs: SetupShadowguard(Mobile from)` — now spawn automatically inside the constructor itself (guarded by `Instance == null` so a second `[add` doesn't duplicate world furniture). Safe because ModernUO codegen deserialization bypasses the constructor entirely (confirmed pattern, see `[AfterDeserialization]` elsewhere in this file), so `PlaceWorldFixtures()` only ever runs on a real new placement, never on world load. No `Region.Find`/`Abyss`-parenting was needed: `ShadowguardRegion` is item-tracked per-instance (via `ShadowguardInstance`), not statically registered in `regions.json`. Walkability depends on TerMur map files being complete (client was mid-redownload earlier this session, unrelated to this port) — not yet verified in-game.

## Vice vs Virtue — DONE (real port from ServUO), the largest of the four systems

58 source files, ~9500 lines — larger than Shadowguard. All new files live under
`Projects/UOContent/Engines/VvV/` (namespace `Server.Engines.VvV`), mirroring ServUO's own
`Scripts/Services/ViceVsVirtue/` folder structure (`Gumps/`, `Items/Rewards/` -> `Rewards/`).
Source cached at `scratchpad/servuo-vvv-src/` (session temp dir).

**Structural change, biggest of the whole port**: ServUO's `ViceVsVirtueSystem : PointsSystem`
and `VvVPlayerEntry : PointsEntry` build on `Server.Engines.Points`, a generic loyalty-points
framework used by several unrelated ServUO systems — it doesn't exist in this codebase at all.
`ViceVsVirtueSystem` is a plain singleton here instead, `VvVPlayerEntry` a plain class, and
state (player entries, guild stats, exempt cities, the live `VvVBattle`) is saved through a new
`VvVPersistence : GenericPersistence` (same pattern as `ShadowguardPersistence`) instead of the
points-system's own save format.

### Core files
- `ViceVsVirtueSystem.cs` — guild/player state, `IsVvV`/`IsEnemy`/`IsAllied` combat-flagging API, skill-loss-on-death, silver-trader spawning. `EventSink.PlayerDeath` doesn't exist here — replaced with the `[OnEvent(nameof(PlayerMobile.PlayerDeathEvent))]` `ModernUO.CodeGeneratedEvents` pattern (see `Engines/CannedEvil/ChampionTitleSystem.cs` for the precedent). `Config.Get` → `ServerConfiguration.GetSetting`. `PVPArenaSystem.IsSameIP` (engine doesn't exist here) dropped from `RestrictSilver`, simplified to an account-only check. Registers a **new** `"VvV"` player command (join/leaderboard) — ServUO's own 58 files never wire up an in-game join entry point (presumably lives in a guildstone file outside that folder, not part of this port), so one was added from scratch.
- `VvVPlayerEntry.cs`, `GuildStats.cs` — plain data classes (obsolete `VvVGuildBattleStats`, ServUO's own comment says "no longer used", not ported).
- `VvVBattle.cs` — the battle state machine (altars, sigil spawn/turn-in, scoring, silver awards). `Region.GetEnumeratedMobiles()` → `Region.GetMobiles()`; `GuardedRegion.Disabled`/`.Disable()` → this codebase's real `GuardsDisabled`/`GuardedRegion.Disable()`; `Map.GetRandomSpawnPoint(Rectangle2D)` (doesn't exist) → manual nearby-point search; `Aggression.CombatHeatDelay` (class doesn't exist) → `HouseRegion.CombatHeatDelay`, the equivalent constant already used for the same mechanic elsewhere in this codebase. Manual `Serialize`/`Deserialize` (not codegen — `VvVBattle` is a `[PropertyObject]`, not an Item/Mobile entity) persisted via `VvVPersistence`.
- `VvVSigil.cs`, `VvVAltar.cs` — sigil steal/return, the 3-altar occupy-and-hold mechanic with fireworks. **`AltarArrow` (a `QuestArrow` pointing players at the lit altar) isn't ported** — this codebase's `QuestArrow(PlayerMobile, Mobile)` only targets Mobile entities, not arbitrary Items like `VvVAltar` (a `BaseAddon`); `VvVBattle.CheckArrow`/`ActivateArrows` are no-ops as a result.
- `VvVCityInfo.cs` — the 8 VvV cities' sigil/altar/priest/trader coordinates. Renamed from ServUO's `CityInfo` to avoid colliding with the existing stock `Server.CityInfo` (unrelated moongate-description class).
- `SilverTrader.cs`, `VvVPriest.cs` — vendor/priest NPCs. SilverTrader's gargoyle-artifact-conversion drag-drop dropped (needs 4 `Gargish*` variant classes that don't exist here).
- `VvVInterfaces.cs` — `IVvVItem`/`IOwnerRestricted`/`IAccountRestricted`/`IRevealableItem`, small interfaces ServUO's items depend on that don't exist in this codebase; defined locally since VvV is the only system using them here.
- `VvVEquipment.cs` — a no-op stub. ServUO's real version normalizes ~20 pre-existing TOL/HS artifact drops (PrimerOnArmsTalisman, ClaininsSpellbook, etc.) to fixed VvV-balanced attribute values; most don't exist in this codebase (see Rewards below) and mutating the ones that do felt out of VvV's own scope.
- `CollectionItem.cs` — minimal standalone reward-store entry type; ServUO's real one comes from the same generic collection framework as `PointsSystem`.

### Rewards (`Rewards/` subfolder)
- `Banners.cs` (16 virtue/vice banners), `Tiles.cs` (8 dungeon-teleport tiles) — near-identical decoration sets, one shared abstract base each. **Tiles' North/East orientation picker dropped**: real `RewardOptionGump.Add(int, int)` only accepts localized cliloc numbers, not arbitrary strings like "Covetous (North)" — tiles always place North-facing now.
- `VvVPotions.cs`, `VvVTrapKit.cs`, `VvVTraps.cs` (5 damage types), `VvVSteeds.cs`, `CannonTurret.cs`, `ManaSpike.cs`, `MiscRewards.cs` (robe/hair-dye/chest/hat/earrings/arms/essence/pardon/morph-earrings/wands) — the VvV-specific reward items themselves, all real ports.
  - `VvVTrap.SetTripwire`: ServUO traces the tripwire with `MovementPath` (Mobile-pathfinding helper) — this codebase's `MovementPath` only has a Mobile-based constructor, no two-Point3D overload — replaced with a direct Bresenham line between the two points.
  - `CannonTurret.CannonBase`: ServUO's is a `DamageableItem` (a destructible-prop framework with its own HP bar) — doesn't exist here, so it's a plain immovable `Item`; the turret can no longer be destroyed by attacking its base, only via `ShotsRemaining` reaching 0.
  - `MorphEarrings`: drops equipment that becomes race-incompatible on wear via `Race.ValidateEquipment(Item)` — that method doesn't exist on this codebase's `Race` class, so incompatible gear is no longer auto-unequipped.
  - `VvVWand1`/`VvVWand2`: `IArcaneEquip` (charge-counter interface) implementation dropped for simplicity — wands work as plain weapons without the arcane-charge display.
- **Not ported — 4 reward item types with no equivalent base class here**: `VvVEpaulette`/`VvVGargishEpaulette` (`Epaulette`/`GargishEpaulette` don't exist), `VvVGargishPlateArms` (`GargishPlateArms` doesn't exist), `VvVGargishStoneChest` (`GargishStoneChest` doesn't exist).
- **`VvVRewards.cs`**: the reward-store catalog. Dropped entries for item types that don't exist here at all: `MaceAndShieldGlasses`, `VesperOrderShield`, `ClaininsSpellbook`, `CrystallineRing`, `WizardsCrystalGlasses`, `PrimerOnArmsTalisman`, `HumanFeyLeggings` (pre-existing TOL/HS artifacts ServUO repurposes as VvV rewards, not VvV-specific types — most *other* artifacts on the list, like `InquisitorsResolution`/`HuntersHeaddress`/`CrimsonCincture`/`HeartOfTheLion`/`RuneBeetleCarapace`/`KasaOfTheRajin`/`OrnamentOfTheMagician`/`TomeOfLostKnowledge`/`Stormgrip`/`RingOfTheVile`/`SpiritOfTheTotem`/`FoldedSteelGlasses`, already existed and are kept).

### Gumps (`Gumps/` subfolder)
All 8 rewritten against this codebase's plain `Gump` (legacy) class instead of ServUO's own
convenience `BaseGump` wrapper (`AddPage`/`User`/`GetTypeID()`/`Refresh()` helpers) — a
completely unrelated, byte-buffer-based class of the same name (`Server.Gumps.BaseGump`)
already exists here for a different gump system. `Quests.BaseQuestGump.C32216` (24-bit RGB →
16-bit gump color converter) doesn't exist — reimplemented as `GumpColor.Convert32To16`.
- `BattleWarningGump.cs` — warns non-VvV players in a battle region; teleports to nearest moongate. `PublicMoongate.Moongates` (static list) doesn't exist — replaced with a `World.Items` scan.
- `ConfirmSignupGump.cs`, `ExemptCityGump.cs` — join-VvV confirmation and admin city-exemption toggle.
- `VvVBattleStatusGump.cs`, `BattleStatsGump.cs` — live battle HUD and end-of-battle summary.
- `LeaderboardGump.cs` — both `ViceVsVirtueLeaderboardGump` (player rankings) and `GuildLeaderboardGump`, since they cross-navigate each other. `PlayerTable` (a `PointsSystem` collection) → `PlayerEntries.Values`.
- `VvVRewardGump.cs` — the silver-trader reward store, rewritten from scratch (not a `BaseRewardGump` subclass — see VvVEquipment.cs note above for why that framework doesn't exist).

**Build status: compiles clean (0 errors, 0 warnings).** Not yet in-game verified — no battle
has actually been triggered, no reward purchased, no sigil captured. `VvVCityInfo.Region`
depends on the 8 VvV city regions (Britain, Jhelom, Minoc, Moonglow, Ocllo, Skara Brae,
Trinsic, Yew) existing by name in `Distribution/Data/regions.json` on `Map.Felucca` — checked,
all 8 are already there, so `VvVBattle.Region` should resolve correctly once tested.

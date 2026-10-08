using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(1, false)]
public partial class BaseTreasureChest : LockableContainer
{
    public enum TreasureLevel
    {
        Level1,
        Level2,
        Level3,
        Level4,
        Level5,
        Level6
    }

    private TimerExecutionToken _resetTimer;

    [Constructible]
    public BaseTreasureChest() : this(0x9AB)
    {
    }

    [Constructible]
    public BaseTreasureChest(int itemID, TreasureLevel level = TreasureLevel.Level2) : base(itemID)
    {
        _level = level;
        _minSpawnTime = TimeSpan.FromMinutes(10);
        _maxSpawnTime = TimeSpan.FromMinutes(60);

        Locked = true;
        Movable = false;

        SetLockLevel();
        GenerateTreasure();
    }

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private TreasureLevel _level;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private TimeSpan _minSpawnTime;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private TimeSpan _maxSpawnTime;

    [CommandProperty(AccessLevel.GameMaster)]
    public override bool Locked
    {
        get => base.Locked;
        set
        {
            if (base.Locked != value)
            {
                base.Locked = value;

                if (!value)
                {
                    StartResetTimer();
                }

                InvalidateProperties();
            }
        }
    }

    public override bool IsDecoContainer => false;

    public override string DefaultName => Locked ? "a locked treasure chest" : "a treasure chest";

    // Mahaon: автолут срабатывает и на сундуках данжей, не только на трупах — только
    // после того как сундук реально открылся (LockableContainer.Open уже проверило
    // CheckLocked до вызова base.Open, так что если мы дошли сюда и Locked уже false,
    // значит открытие удалось).
    public override void Open(Mobile from)
    {
        base.Open(from);

        if (!Locked)
        {
            Systems.MahaonLooting.AutoLootSystem.TryAutoLootChest(from, this);
        }
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (!Locked)
        {
            StartResetTimer();
        }
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        _level = (TreasureLevel)reader.ReadByte();
        _minSpawnTime = TimeSpan.FromMinutes(reader.ReadShort());
        _maxSpawnTime = TimeSpan.FromMinutes(reader.ReadShort());
    }

    protected virtual void SetLockLevel()
    {
        // MaxLockLevel used to sit at the LockableContainer default (100) for every tier
        // regardless of LockLevel — harmless before, since a hard skill-floor gate in
        // LockPick.cs blocked any attempt below RequiredSkill anyway. Now that attempts
        // are allowed from 0 skill (see LockPick.cs/MahaonLockpick.cs), the chance curve
        // itself needs to actually scale with tier difficulty instead of every tier
        // topping out at the same 100 — same +40 spread TreasureMapChest/ParagonChest
        // already use.
        RequiredSkill = _level switch
        {
            TreasureLevel.Level1 => LockLevel = 5,
            TreasureLevel.Level2 => LockLevel = 20,
            TreasureLevel.Level3 => LockLevel = 50,
            TreasureLevel.Level4 => LockLevel = 70,
            TreasureLevel.Level5 => LockLevel = 90,
            TreasureLevel.Level6 => LockLevel = 100,
            _                    => LockLevel = 120
        };

        MaxLockLevel = LockLevel + 40;
    }

    private void StartResetTimer()
    {
        _resetTimer.Cancel();

        var randomDuration = Utility.RandomMinMax(_minSpawnTime.Ticks, _maxSpawnTime.Ticks);
        Timer.StartTimer(TimeSpan.FromTicks(randomDuration), Reset, out _resetTimer);
    }

    protected virtual void GenerateTreasure()
    {
        var spec = LootSpecFor((int)_level + 1);

        DropItem(new Gold(Utility.RandomMinMax(spec.MinGold, spec.MaxGold)));

        AddMahaonDrops(this, (int)_level + 1);
    }

    // Full loot rework per the shard owner's own exact per-tier spec (gold range, reagent/
    // potion/gem type-count and quantity ranges, ammo range, item count) — replaces the
    // older ad-hoc `1 + level/2`-style formulas below with the literal numbers given.
    // Level6 was NOT specified (the shard owner only described 5 tiers) — extrapolated as
    // a modest step up from Level5, flagged here rather than silently guessed: gold
    // 1500-2200, reagents 9-10 types x20-25, potions 5 types x12-15, ammo 40-45, gems 5
    // types x8-10, 6 items, blueprint 20%, soul stone 15% (Giant only).
    private readonly record struct TierLootSpec(
        int MinGold, int MaxGold,
        int MinRegTypes, int MaxRegTypes, int MinRegStack, int MaxRegStack,
        int PotionTypes, int MinPotionQty, int MaxPotionQty,
        int MinAmmo, int MaxAmmo,
        int GemTypes, int MinGemQty, int MaxGemQty,
        int Items,
        double BlueprintChance, double SoulStoneChance,
        SoulStoneSize[] SoulStoneSizes
    );

    private static readonly TierLootSpec[] LootSpecs =
    {
        new(100, 300, 1, 3, 3, 5, 1, 1, 3, 10, 15, 1, 1, 3, 1,
            0.05, 0.03, new[] { SoulStoneSize.Small }),
        new(300, 500, 2, 4, 5, 7, 2, 2, 3, 15, 20, 2, 2, 4, 2,
            0.08, 0.05, new[] { SoulStoneSize.Small, SoulStoneSize.Medium }),
        new(500, 700, 3, 5, 7, 9, 3, 3, 4, 20, 25, 3, 3, 5, 3,
            0.12, 0.08, new[] { SoulStoneSize.Medium, SoulStoneSize.Large }),
        // Tier 4: blueprint/soul-stone chance deliberately UNCHANGED from tier 3 — "такие
        // же шансы... что и раньше" was explicit, not a copy/paste oversight.
        new(700, 900, 4, 6, 9, 12, 4, 4, 5, 25, 30, 4, 4, 6, 4,
            0.12, 0.08, new[] { SoulStoneSize.Large, SoulStoneSize.Giant }),
        new(1000, 1500, 7, 9, 15, 20, 5, 10, 12, 35, 40, 5, 7, 8, 5,
            0.18, 0.12, new[] { SoulStoneSize.Giant }),
        // Level6 — extrapolated, see doc comment above.
        new(1500, 2200, 9, 10, 20, 25, 5, 12, 15, 40, 45, 5, 8, 10, 6,
            0.20, 0.15, new[] { SoulStoneSize.Giant })
    };

    private static TierLootSpec LootSpecFor(int level) => LootSpecs[Math.Clamp(level, 1, 6) - 1];

    // Shared by every BaseTreasureChest-derived chest (including the Chest1-5 spawner
    // aliases in Items/TreasureChests/Chests.cs) so "level" always means the same thing —
    // the blueprint's own tier matches the chest's level, and drop odds climb with it
    // (chosen linearly per the shard owner's own call: "chances, the higher the level, the
    // more chance"). Tune the three multipliers below if the odds feel off — they're the
    // only knobs. Also assigns a metal-tier lock via the same weighting TreasureMapChest
    // already uses for its own level 0-5 (MahaonLockSystem.AssignRandomLock, bias =
    // level/5.0) — a level-5 chest skews toward the rarest lock metals, same as a level-5
    // treasure map.
    public static void AddMahaonDrops(Container chest, int level)
    {
        var tier = System.Math.Clamp(level, 0, 5);
        var spec = LootSpecFor(level);

        Systems.MahaonMetals.MahaonLockSystem.AssignRandomLock(chest, level / 5.0);

        // The one "чертёж" (blueprint) chance the shard owner's spec is actually about —
        // exact per-tier numbers in LootSpecs above, plateaued at tier 4 as specified.
        if (Utility.RandomDouble() < spec.BlueprintChance)
        {
            chest.DropItem(new MahaonMysteryBlueprint(tier));
        }

        // Sakuro/resource-bag blueprints are separate bonus extras layered on top (not
        // what the shard owner's numbers above are about) — left on their original
        // level-gated formula.
        if (level >= 3 && Utility.RandomDouble() < level * 0.05)
        {
            var allTypes = System.Enum.GetValues<SakuroType>();
            chest.DropItem(new SakuroBlueprint(allTypes[Utility.Random(allTypes.Length)]));
        }

        if (level >= 4 && Utility.RandomDouble() < level * 0.03)
        {
            var categories = System.Enum.GetValues<MahaonResourceCategory>();
            chest.DropItem(new MahaonResourceBagBlueprint(categories[Utility.Random(categories.Length)]));
        }

        AddVariedLoot(chest, tier);
    }

    // Reagents/scrolls/potions/gear — same real vanilla loot classes TreasureChestLevel4
    // already uses (Loot.RandomReagent/RandomScroll/RandomPotion/RandomArmorOrShieldOrWeapon),
    // just scaled by Mahaon's 1-6 level instead of vanilla's flat roll counts, so a Chest1
    // stays noticeably thinner than a Chest5/6. Necromancy reagents and occasional soul
    // stones are Mahaon-specific additions on top of the vanilla set.
    //
    // Reagents/potions are picked as DISTINCT types via DropDistinctStacks — was rolling
    // each one independently (Loot.RandomReagent() N times), which regularly picked the
    // same type twice and dropped it as two separate item stacks side by side instead of
    // one real pile ("зачем дублировать?"). Potions also used to come ONLY from
    // Loot.PotionTypes, a hardcoded weakest-tier-only pool (Lesser/base potions) —
    // regardless of chest level, so a level-6 chest gave the exact same quality as a
    // level-1 one; PotionPoolForLevel below pulls from the real Lesser/base/Greater potion
    // classes and widens the pool as level rises.
    // Combined regular + necromancy reagent pool — the shard owner's spec talks about
    // "реагенты" as one concept (a type-count + a stack-size range), not two separate
    // mechanics, so both real vanilla pools are merged into one for the distinct-type roll
    // below instead of drawing from them as two independent steps.
    private static readonly Type[] AllReagentTypes = BuildAllReagentTypes();

    private static Type[] BuildAllReagentTypes()
    {
        var combined = new Type[Loot.RegTypes.Length + Loot.NecroRegTypes.Length];
        Loot.RegTypes.CopyTo(combined, 0);
        Loot.NecroRegTypes.CopyTo(combined, Loot.RegTypes.Length);
        return combined;
    }

    private static readonly Type[] GemTypes =
    {
        typeof(Amber), typeof(Amethyst), typeof(Citrine), typeof(Diamond), typeof(Emerald),
        typeof(Ruby), typeof(Sapphire), typeof(StarSapphire), typeof(Tourmaline)
    };

    private static void AddVariedLoot(Container chest, int tier)
    {
        var level = tier + 1; // 1-6, matches the chest's own TreasureLevel numbering
        var spec = LootSpecFor(level);

        // Реагенты — N distinct types (spec.MinRegTypes..MaxRegTypes), each a real stack
        // in spec.MinRegStack..MaxRegStack.
        DropDistinctStacks(
            chest, AllReagentTypes, Utility.RandomMinMax(spec.MinRegTypes, spec.MaxRegTypes),
            spec.MinRegStack, spec.MaxRegStack
        );

        // Немного реагентов зачарования — MagicalResidue common at any level, RelicFragment
        // (real-drop rule elsewhere is "only from strong monsters") only from mid-tier up,
        // both kept deliberately small (shard owner: "немного"). Unrelated to the main
        // reagent spec above — a separate bonus layer, left as-is.
        if (Utility.RandomDouble() < level * 0.08)
        {
            chest.DropItem(new MagicalResidue(Utility.RandomMinMax(1, 2 + level / 2)));
        }

        if (level >= 3 && Utility.RandomDouble() < level * 0.04)
        {
            chest.DropItem(new RelicFragment(Utility.RandomMinMax(1, 1 + level / 3)));
        }

        var maxScrollIndex = Math.Min(63, level * 8 - 1);

        for (var i = Utility.Random(level); i > 0; i--)
        {
            var scroll = Loot.RandomScroll(0, maxScrollIndex, SpellbookType.Regular);
            scroll.Amount = Utility.RandomMinMax(1, 3);
            chest.DropItem(scroll);
        }

        // Бутылки (зелья) — N distinct types, each spec.MinPotionQty..MaxPotionQty.
        DropDistinctStacks(chest, PotionPoolForLevel(level), spec.PotionTypes, spec.MinPotionQty, spec.MaxPotionQty);

        // Драгоценные камни — N distinct types, each spec.MinGemQty..MaxGemQty. New —
        // there was no gem drop at all before this.
        DropDistinctStacks(chest, GemTypes, spec.GemTypes, spec.MinGemQty, spec.MaxGemQty);

        // Вещи из металла или дерева — exactly spec.Items pieces (was a random 1..1+level/2
        // count before; the shard owner gave an exact count per tier instead).
        for (var i = 0; i < spec.Items; i++)
        {
            var item = Loot.RandomArmorOrShieldOrWeaponOrJewelry();

            switch (item)
            {
                case BaseWeapon weapon:
                    weapon.DamageLevel = (WeaponDamageLevel)Utility.Random(1 + tier);
                    weapon.AccuracyLevel = (WeaponAccuracyLevel)Utility.Random(1 + tier);
                    weapon.DurabilityLevel = (WeaponDurabilityLevel)Utility.Random(1 + tier);
                    break;

                case BaseArmor armor:
                    armor.ProtectionLevel = (ArmorProtectionLevel)Utility.Random(1 + tier);
                    armor.Durability = (ArmorDurabilityLevel)Utility.Random(1 + tier);
                    break;
            }

            // A found piece is occasionally already reforged (weapon/armor/jewelry alike —
            // "украшения тоже должны быть из разных металлов") — flavor tie-in to the
            // shard's own metal system, more likely the deeper the chest. Reforge target
            // matches this SAME chest's own level, per "вещи ... под N-й тир сундуков".
            if (Utility.RandomDouble() < level * 0.06)
            {
                ReforgeRandomly(item, level);
            }
            else if (Utility.RandomDouble() < level * 0.05)
            {
                // Occasionally instead a Lower Reagent Cost roll — separate from the metal
                // roll (an item doesn't need to be reforged to also carry a real AOS
                // attribute), scales with level like everything else here.
                ApplyLowerRegCost(item, level);
            }

            chest.DropItem(item);
        }

        // Стрелы/болты — spec.MinAmmo..MaxAmmo, both ammo types since "различных
        // материалов" for consumable ammo mostly just means mixing arrow vs bolt rather
        // than reforging a throwaway stack into one of 24 metals.
        chest.DropItem(new Arrow(Utility.RandomMinMax(spec.MinAmmo, spec.MaxAmmo)));
        chest.DropItem(new Bolt(Utility.RandomMinMax(spec.MinAmmo, spec.MaxAmmo)));

        // Камень души — chance and allowed size range both come from the per-tier spec
        // (small at tier 1, climbing to giant-only at tier 5+).
        if (Utility.RandomDouble() < spec.SoulStoneChance)
        {
            var size = spec.SoulStoneSizes[Utility.Random(spec.SoulStoneSizes.Length)];

            var color = Utility.RandomList(
                SoulStoneColor.Black, SoulStoneColor.Red, SoulStoneColor.Blue, SoulStoneColor.Green, SoulStoneColor.Gold
            );

            chest.DropItem(new MahaonSoulStone(size, color));
        }
    }

    // Picks `count` DISTINCT types from `pool` (never the same type twice in one call) and
    // drops one real stack per type with a random amount in [minAmount, maxAmount] — the
    // shared fix for "why does the same reagent/potion show up as two separate piles?".
    // internal (not private) — TreasureMapChest reuses this for its own potion drop, see there.
    internal static void DropDistinctStacks(Container chest, Type[] pool, int count, int minAmount, int maxAmount)
    {
        count = Math.Min(count, pool.Length);

        if (count <= 0)
        {
            return;
        }

        var shuffled = (Type[])pool.Clone();

        for (var i = 0; i < count; i++)
        {
            var j = i + Utility.Random(shuffled.Length - i);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        for (var i = 0; i < count; i++)
        {
            // Mahaon: raw Activator.CreateInstance(Type) only matches a REAL zero-parameter
            // constructor — reagent classes like Garlic/SulfurousAsh/Bloodmoss/Ginseng/
            // BlackPearl/MandrakeRoot/Nightshade/SpidersSilk only have `(int amount = 1)`
            // (a default value, not a true parameterless overload), so this crashed with
            // MissingMethodException on every chest spawn that rolled one of them (seen live
            // in Distribution logs). Loot.Construct already exists for exactly this — same
            // helper every other Mahaon/vanilla loot table uses — and its CreateInstance<T>
            // extension resolves default-valued constructor params correctly.
            var item = Loot.Construct(shuffled[i]);
            if (item == null)
            {
                continue;
            }

            item.Amount = Utility.RandomMinMax(minAmount, maxAmount);
            chest.DropItem(item);
        }
    }

    // Loot.PotionTypes is a fixed weakest-tier-only pool (Lesser/base potions), used
    // regardless of chest level — "кладутся только слабые зелья, больше разнообразия,
    // не жлобись". Real tiered classes exist in this codebase; widen the pool as level
    // rises instead of always drawing from the same 6 weak types.
    private static readonly Type[] WeakPotions =
    {
        typeof(AgilityPotion), typeof(StrengthPotion), typeof(RefreshPotion),
        typeof(LesserCurePotion), typeof(LesserHealPotion), typeof(LesserPoisonPotion), typeof(LesserExplosionPotion)
    };

    private static readonly Type[] MidPotions =
    {
        typeof(CurePotion), typeof(HealPotion), typeof(ExplosionPotion), typeof(PoisonPotion),
        typeof(NightSightPotion), typeof(InvisibilityPotion)
    };

    private static readonly Type[] StrongPotions =
    {
        typeof(GreaterCurePotion), typeof(GreaterHealPotion), typeof(GreaterAgilityPotion),
        typeof(GreaterStrengthPotion), typeof(GreaterExplosionPotion), typeof(GreaterPoisonPotion),
        typeof(DeadlyPoisonPotion), typeof(TotalRefreshPotion)
    };

    // internal (not private) — TreasureMapChest reuses this for its own potion drop, see there.
    internal static Type[] PotionPoolForLevel(int level)
    {
        if (level <= 2)
        {
            return WeakPotions;
        }

        if (level <= 4)
        {
            var combined = new Type[WeakPotions.Length + MidPotions.Length];
            WeakPotions.CopyTo(combined, 0);
            MidPotions.CopyTo(combined, WeakPotions.Length);
            return combined;
        }

        var pool = new Type[MidPotions.Length + StrongPotions.Length];
        MidPotions.CopyTo(pool, 0);
        StrongPotions.CopyTo(pool, MidPotions.Length);
        return pool;
    }

    // 24 metals split into 6 even groups of 4, one group per chest level (1-6) — a hard
    // partition, not just a soft weight shift, per the shard owner's explicit ask: Iron
    // (the very first, most common metal) should never be reforged into gear found in a
    // high-level chest. Metal ordinals are declared in exact rarity order (see
    // MahaonMetal.cs), so a straight slice by level is enough.
    private static Systems.MahaonMetals.MahaonMetal RandomMetalForLevel(int level)
    {
        var groupIndex = Math.Clamp(level - 1, 0, 5);
        var metalIndex = groupIndex * 4 + Utility.Random(4);
        return (Systems.MahaonMetals.MahaonMetal)metalIndex;
    }

    // 10 woods in rarity order — the three Mahaon ones sit between Bloodwood and Frostwood
    // by rarity even though their enum values were appended after it (see the comment on
    // CraftResource.BananaWood in ResourceInfo.cs for why the enum couldn't be reordered).
    private static readonly CraftResource[] WoodsByRarity =
    {
        CraftResource.RegularWood, CraftResource.OakWood, CraftResource.AshWood,
        CraftResource.YewWood, CraftResource.Heartwood, CraftResource.Bloodwood,
        CraftResource.BananaWood, CraftResource.CoconutWood, CraftResource.PalmWood,
        CraftResource.Frostwood
    };

    // Same "deeper chest = rarer material" idea as RandomMetalForLevel, just not an even
    // 4-per-level split since there are 10 woods rather than 24 metals.
    private static CraftResource RandomWoodForLevel(int level)
    {
        var span = WoodsByRarity.Length / 6.0;
        var start = (int)((Math.Clamp(level, 1, 6) - 1) * span);
        var count = Math.Min(Math.Max(2, (int)Math.Ceiling(span)), WoodsByRarity.Length - start);

        return WoodsByRarity[start + Utility.Random(count)];
    }

    /// <summary>Whether a Mahaon metal can sensibly be forged into this piece at all. A bow
    /// is wood and leather armor is hide — reforging those into steel is what produced
    /// metal-hued "Металл: Валорит" bows in chest loot.</summary>
    private static bool IsMetalItem(Item item) =>
        item switch
        {
            BaseRanged                                                                    => false,
            BaseArmor armor => armor.MaterialType is ArmorMaterialType.Ringmail
                or ArmorMaterialType.Chainmail
                or ArmorMaterialType.Plate,
            _                                                                             => true
        };

    private static void ReforgeRandomly(Item item, int level)
    {
        // Bows/crossbows get a wood of the matching tier instead of a metal — a real
        // vanilla CraftResource, so the setter also recolors it and the material line
        // shows up through BaseWeapon.GetProperties' own CraftResource fallback. (The
        // per-wood stat bonuses in BaseRanged.ApplyWoodBonuses only run OnCraft, so a
        // looted bow gets the material and color but not those bonuses — same as every
        // other looted piece, which doesn't run OnCraft either.)
        if (item is BaseRanged bow)
        {
            bow.Resource = RandomWoodForLevel(level);

            return;
        }

        if (!IsMetalItem(item))
        {
            return;
        }

        var metal = RandomMetalForLevel(level);

        Systems.MahaonMetals.MahaonMetalTracker.SetMetal(item, metal);
        item.Hue = Systems.MahaonMetals.MahaonMetalTable.Get(metal).Hue;
    }

    // "Вещи с лоуреагент кост также могут попадаться" — a real AOS attribute, applied
    // directly rather than invented; scales modestly with level, capped well under the
    // AOS 100% ceiling.
    private static void ApplyLowerRegCost(Item item, int level)
    {
        var value = Math.Min(100, 5 + level * 5 + Utility.Random(10));

        switch (item)
        {
            case BaseWeapon weapon:
                weapon.Attributes.LowerRegCost = value;
                break;
            case BaseArmor armor:
                armor.Attributes.LowerRegCost = value;
                break;
            case BaseJewel jewel:
                jewel.Attributes.LowerRegCost = value;
                break;
        }
    }

    public void ClearContents()
    {
        for (var i = Items.Count - 1; i >= 0; --i)
        {
            if (i < Items.Count)
            {
                Items[i].Delete();
            }
        }
    }

    public void Reset()
    {
        _resetTimer.Cancel();
        Locked = true;
        ClearContents();
        GenerateTreasure();
    }
}

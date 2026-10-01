using System;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>The Prism of Light peerless. Ported from ServUO
/// (Scripts/Mobiles/Bosses/ShimmeringEffusion.cs).
///
/// Four of its drops — ShimmeringEffusionStatuette, CorporealBrumeStatuette,
/// MantraEffervescenceStatuette, FetidEssenceStatuette — plus FerretImprisonedInCrystal and
/// CrystallineRing have no class in this codebase and are left out; CapturedEssence and
/// ShimmeringCrystals, the two that matter for imbuing, are here. The parrot is absent for the
/// usual reason: LootPack.Parrot is commented out upstream.
///
/// ServUO reaches for LootPack.LootItem&lt;T&gt;() and LootPack.RandomLootItem(), neither of
/// which exists here — the guaranteed drops go through OnDeath instead, which is what the rest
/// of this codebase does (see the Ter Mur creatures).</summary>
[SerializationGenerator(0, false)]
public partial class ShimmeringEffusion : BasePeerless
{
    [Constructible]
    public ShimmeringEffusion() : base(AIType.AI_Mage, FightMode.Closest, 10, 1)
    {
        ActiveSpeed = 0.2;
        PassiveSpeed = 0.4;

        Name = "a shimmering effusion";
        Body = 0x105;

        SetStr(500, 550);
        SetDex(350, 400);
        SetInt(1500, 1600);

        SetHits(20000);

        SetDamage(27, 31);

        SetDamageType(ResistanceType.Physical, 20);
        SetDamageType(ResistanceType.Fire, 20);
        SetDamageType(ResistanceType.Cold, 20);
        SetDamageType(ResistanceType.Poison, 20);
        SetDamageType(ResistanceType.Energy, 20);

        SetResistance(ResistanceType.Physical, 60, 80);
        SetResistance(ResistanceType.Fire, 60, 80);
        SetResistance(ResistanceType.Cold, 60, 80);
        SetResistance(ResistanceType.Poison, 60, 80);
        SetResistance(ResistanceType.Energy, 60, 80);

        SetSkill(SkillName.Wrestling, 100.0, 105.0);
        SetSkill(SkillName.Tactics, 100.0, 105.0);
        SetSkill(SkillName.MagicResist, 150.0);
        SetSkill(SkillName.Magery, 150.0);
        SetSkill(SkillName.EvalInt, 150.0);
        SetSkill(SkillName.Meditation, 120.0);
        SetSkill(SkillName.Spellweaving, 120.0);

        Fame = 30000;
        Karma = -30000;
    }

    public override string CorpseName => "a shimmering effusion corpse";

    public override int GetIdleSound() => 0x1BF;
    public override int GetAttackSound() => 0x1C0;
    public override int GetHurtSound() => 0x1C1;
    public override int GetDeathSound() => 0x1C2;

    public override bool AutoDispel => true;
    public override int TreasureMapLevel => 5;

    public override bool HasFireRing => true;
    public override double FireRingChance => 0.1;

    public override bool CanSpawnHelpers => true;
    public override int MaxHelpersWaves => 4;
    public override double SpawnHelpersChance => 0.1;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.SuperBoss, 8);
        AddLoot(LootPack.HighScrolls, 3);
        AddLoot(LootPack.MedScrolls, 3);
        AddLoot(LootPack.ArcanistScrolls, Utility.RandomMinMax(1, 6));
        AddLoot(LootPack.PeerlessResource, 8);
        AddLoot(LootPack.Talisman, 5);
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        c.DropItem(new CapturedEssence());
        c.DropItem(new ShimmeringCrystals());
    }

    /// <summary>One helper per fighter, up to five, drawn from the prism's own residents.</summary>
    public override void SpawnHelpers()
    {
        var count = Math.Clamp(Altar?.Fighters.Count ?? 1, 1, 5);

        for (var i = 0; i < count; i++)
        {
            BaseCreature helper = Utility.Random(3) switch
            {
                0 => new MantraEffervescence(),
                1 => new CorporealBrume(),
                _ => new FetidEssence()
            };

            SpawnHelper(helper, 5);
        }
    }
}

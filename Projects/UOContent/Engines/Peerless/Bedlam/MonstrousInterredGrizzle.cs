using System;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>The Bedlam peerless. Ported from ServUO
/// (Scripts/Mobiles/Bosses/MonstrousInterredGrizzle.cs).
///
/// Not the same creature as the InterredGrizzle already in this codebase — that one is an
/// ordinary Bedlam resident; this is the boss.
///
/// Its acid runs through this codebase's own SpillAcid / NewHarmfulItem hooks on BaseCreature,
/// which is exactly the shape ServUO uses. The parrot drop is absent for the usual reason —
/// LootPack.Parrot is commented out upstream.</summary>
[SerializationGenerator(0, false)]
public partial class MonstrousInterredGrizzle : BasePeerless
{
    [Constructible]
    public MonstrousInterredGrizzle() : base(AIType.AI_Mage, FightMode.Closest, 10, 1)
    {
        ActiveSpeed = 0.2;
        PassiveSpeed = 0.4;

        Name = "a monstrous interred grizzle";
        Body = 0x103;
        BaseSoundID = 589;

        SetStr(1198, 1207);
        SetDex(127, 135);
        SetInt(595, 646);

        SetHits(50000);

        SetDamage(27, 31);

        SetDamageType(ResistanceType.Physical, 60);
        SetDamageType(ResistanceType.Fire, 20);
        SetDamageType(ResistanceType.Energy, 20);

        SetResistance(ResistanceType.Physical, 48, 52);
        SetResistance(ResistanceType.Fire, 77, 82);
        SetResistance(ResistanceType.Cold, 56, 61);
        SetResistance(ResistanceType.Poison, 32, 40);
        SetResistance(ResistanceType.Energy, 69, 71);

        SetSkill(SkillName.Wrestling, 112.6, 116.9);
        SetSkill(SkillName.Tactics, 118.5, 119.2);
        SetSkill(SkillName.MagicResist, 120.0);
        SetSkill(SkillName.Anatomy, 111.0, 111.7);
        SetSkill(SkillName.Magery, 100.0);
        SetSkill(SkillName.EvalInt, 100.0);
        SetSkill(SkillName.Meditation, 100.0);
        SetSkill(SkillName.Spellweaving, 100.0);

        Fame = 24000;
        Karma = -24000;
    }

    public override string CorpseName => "a monstrous interred grizzle corpse";

    public override int GetDeathSound() => 0x57F;
    public override int GetAttackSound() => 0x580;
    public override int GetIdleSound() => 0x581;
    public override int GetAngerSound() => 0x582;
    public override int GetHurtSound() => 0x583;

    public override bool GivesMLMinorArtifact => true;
    public override int TreasureMapLevel => 5;

    public override Item NewHarmfulItem() => new InfernalOoze(this, Utility.RandomBool());

    public override void GenerateLoot()
    {
        AddLoot(LootPack.SuperBoss, 8);
        AddLoot(LootPack.ArcanistScrolls, Utility.RandomMinMax(1, 6));
        AddLoot(LootPack.PeerlessResource, 8);
        AddLoot(LootPack.Talisman, 5);
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        c.DropItem(new GrizzledBones());

        if (Utility.RandomDouble() < 0.05)
        {
            c.DropItem(new GrizzledMareStatuette());
        }
    }

    public override void OnDamage(int amount, Mobile from, bool willKill)
    {
        if (Utility.RandomDouble() < 0.06)
        {
            SpillAcid(null, Utility.RandomMinMax(1, 3));
        }

        base.OnDamage(amount, from, willKill);
    }
}

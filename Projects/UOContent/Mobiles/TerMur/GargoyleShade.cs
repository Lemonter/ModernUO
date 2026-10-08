using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/GargoyleShade.cs). LootPack.MageryRegs
/// doesn't exist here (verified) — dropped, kept LootPack.Meager.</summary>
[SerializationGenerator(0, false)]
[CorpseName("a ghostly corpse")]
public partial class GargoyleShade : BaseCreature
{
    [Constructible]
    public GargoyleShade() : base(AIType.AI_Mage, FightMode.Closest, 10, 1)
    {
        Body = 0x4;
        Hue = 16385;
        BaseSoundID = 0x482;

        SetStr(76, 78);
        SetDex(76, 81);
        SetInt(36, 48);

        SetHits(60, 64);
        SetStam(80, 81);
        SetMana(75, 78);

        SetDamage(7, 14);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 35, 60);
        SetResistance(ResistanceType.Fire, 40, 50);
        SetResistance(ResistanceType.Cold, 10, 20);
        SetResistance(ResistanceType.Poison, 20, 35);

        SetSkill(SkillName.EvalInt, 57.1, 65.5);
        SetSkill(SkillName.Magery, 60.6, 70.1);
        SetSkill(SkillName.MagicResist, 52.6, 70.0);
        SetSkill(SkillName.Tactics, 52.7, 60.0);
        SetSkill(SkillName.Wrestling, 47.7, 55.0);
        SetSkill(SkillName.DetectHidden, 30.0, 40.0);

        Fame = 4000;
        Karma = -4000;
    }

    public override string DefaultName => "тень гаргульи";

    public override bool BleedImmune => true;
    public override Poison PoisonImmune => Poison.Lethal;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Meager);
    }
}

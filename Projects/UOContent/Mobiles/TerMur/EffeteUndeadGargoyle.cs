using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/EffeteUndeadGargoyle.cs).</summary>
[SerializationGenerator(0, false)]
[CorpseName("an effete undead gargoyle corpse")]
public partial class EffeteUndeadGargoyle : BaseCreature
{
    [Constructible]
    public EffeteUndeadGargoyle() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Body = 722;
        BaseSoundID = 372;

        SetStr(60, 65);
        SetDex(60, 65);
        SetInt(30, 35);

        SetHits(65, 70);

        SetDamage(3, 7);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 20);
        SetResistance(ResistanceType.Fire, 5, 10);
        SetResistance(ResistanceType.Cold, 25, 30);
        SetResistance(ResistanceType.Poison, 25);
        SetResistance(ResistanceType.Energy, 14, 15);

        SetSkill(SkillName.MagicResist, 50.0, 55.0);
        SetSkill(SkillName.Tactics, 50.0);
        SetSkill(SkillName.Wrestling, 50.0);

        Fame = 3500;
        Karma = -3500;
    }

    public override string DefaultName => "дряхлая гаргулья-нежить";

    public override int TreasureMapLevel => 1;
    public override int Meat => 1;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Meager);
    }
}

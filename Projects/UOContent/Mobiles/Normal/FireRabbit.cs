using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/FireRabbit.cs). The 50% AnimalPheromone
/// drop is dropped — that pet-training item does not exist here.</summary>
[SerializationGenerator(0, false)]
public partial class FireRabbit : VorpalBunny
{
    [Constructible]
    public FireRabbit()
    {
        Hue = 0x550;

        SetStr(130);
        SetDex(4500);
        SetInt(2500);

        SetHits(2500);
        SetStam(1500);
        SetMana(1500);

        SetDamage(10, 15);

        SetDamageType(ResistanceType.Fire, 100);
        SetDamageType(ResistanceType.Physical, 0);

        SetResistance(ResistanceType.Physical, 45);
        SetResistance(ResistanceType.Fire, 100);
        SetResistance(ResistanceType.Cold, 40);
        SetResistance(ResistanceType.Poison, 46);
        SetResistance(ResistanceType.Energy, 46);

        SetSkill(SkillName.MagicResist, 200);
        SetSkill(SkillName.Tactics, 0.0);
        SetSkill(SkillName.Wrestling, 80.0);
        SetSkill(SkillName.Anatomy, 0.0);
    }

    public override string CorpseName => "труп зайца";
    public override string DefaultName => "огненный кролик";

    public override bool IsScaryToPets => true;
    public override bool BardImmune => true;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.FilthyRich);
        AddLoot(LootPack.Rich, 3);
    }
}

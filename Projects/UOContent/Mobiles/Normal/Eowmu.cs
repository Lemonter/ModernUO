using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/Eowmu.cs). The statuette is a
/// BaseImprisonedMobile — see CoconutCrab.cs.</summary>
[SerializationGenerator(0, false)]
public partial class EowmuStatue : BaseImprisonedMobile
{
    [Constructible]
    public EowmuStatue() : base(0xA0C0)
    {
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1158082; // Eowmu

    public override BaseCreature Summon => new Eowmu();
}

[SerializationGenerator(0, false)]
public partial class Eowmu : BaseMount
{
    [Constructible]
    public Eowmu() : base(1440, 16079, AIType.AI_Animal, FightMode.Aggressor)
    {
        BaseSoundID = 0xA8;

        SetStr(400);
        SetDex(125);
        SetInt(50, 55);

        SetHits(240);
        SetMana(0);

        SetDamage(1, 4);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 40, 50);
        SetResistance(ResistanceType.Fire, 30, 40);
        SetResistance(ResistanceType.Cold, 30, 40);
        SetResistance(ResistanceType.Poison, 30, 40);
        SetResistance(ResistanceType.Energy, 30, 40);

        SetSkill(SkillName.MagicResist, 25.0, 30.0);
        SetSkill(SkillName.Tactics, 30.0, 40.0);
        SetSkill(SkillName.Wrestling, 30.0, 35.0);

        Fame = 300;
        Karma = 300;

        Tamable = true;
        ControlSlots = 1;
        MinTameSkill = 30.0;
    }

    public override string CorpseName => "труп эову";
    public override string DefaultName => "эову";

    public override int Meat => 3;
    public override int Hides => 10;
    public override FoodType FavoriteFood => FoodType.FruitsAndVeggies;
}

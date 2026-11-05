using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/CoconutCrab.cs). The ServUO statuette used
/// ICreatureStatuette + ConfirmMountStatuetteGump; here it derives from BaseImprisonedMobile, whose
/// confirmation gump is the same cliloc (1075084) and bonds the summoned pet.</summary>
[SerializationGenerator(0, false)]
public partial class CoconutCrabStatue : BaseImprisonedMobile
{
    [Constructible]
    public CoconutCrabStatue() : base(0xA335)
    {
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1159165; // Coconut Crab

    public override BaseCreature Summon => new CoconutCrab();
}

[SerializationGenerator(0, false)]
public partial class CoconutCrab : BaseMount
{
    [Constructible]
    public CoconutCrab() : base(1510, 16081, AIType.AI_Animal, FightMode.Aggressor)
    {
        BaseSoundID = 0x4F2;

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

    public override string CorpseName => "труп кокосового краба";
    public override string DefaultName => "кокосовый краб";

    public override int Meat => 3;
    public override int Hides => 10;
    public override FoodType FavoriteFood => FoodType.FruitsAndVeggies;
}

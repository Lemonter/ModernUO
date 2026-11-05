using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/HungryCoconutCrab.cs). The statuette is a
/// BaseImprisonedMobile; its "*Redeemable for a pet*" property line is kept. The pack-check
/// message differs upstream (1010095 instead of the base's 1042001) — the base wording is used.</summary>
[SerializationGenerator(0, false)]
public partial class HungryCoconutCrabStatue : BaseImprisonedMobile
{
    [Constructible]
    public HungryCoconutCrabStatue() : base(0xA336)
    {
        Hue = 2713;
    }

    public override int LabelNumber => 1159221; // Hungry Coconut Crab Statuette

    public override BaseCreature Summon => new HungryCoconutCrab();

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1159222); // *Redeemable for a pet*<br>*Requires High Seas Booster Pack*
    }
}

[SerializationGenerator(0, false)]
public partial class HungryCoconutCrab : BaseCreature
{
    [Constructible]
    public HungryCoconutCrab() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Body = 0x5E7;
        Hue = 2713;
        BaseSoundID = 0x4F2;

        SetStr(19);
        SetDex(15);
        SetInt(5);

        SetHits(12);

        SetDamage(3, 4);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 10, 20);

        SetSkill(SkillName.MagicResist, 5.0);
        SetSkill(SkillName.Tactics, 5.0);
        SetSkill(SkillName.Wrestling, 5.0);

        Fame = 0;
        Karma = 0;

        Tamable = true;
        ControlSlots = 1;
        MinTameSkill = 0.0;
    }

    public override string CorpseName => "труп кокосового краба";
    public override string DefaultName => "голодный кокосовый краб";

    public override bool DeleteOnRelease => true;
    public override FoodType FavoriteFood => FoodType.FruitsAndVeggies;
}

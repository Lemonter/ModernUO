using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/Icehound.cs). Warning kept in mind: this one
/// is a re-skin of a hound, not a dire wolf — it only trains Swords.</summary>
[SerializationGenerator(0, false)]
public partial class IceHound : BaseCreature
{
    [Constructible]
    public IceHound() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Body = 98;
        BaseSoundID = 229;
        Hue = 1153;

        SetStr(102, 150);
        SetDex(81, 105);
        SetInt(36, 60);

        SetHits(66, 125);

        SetDamage(11, 17);

        SetDamageType(ResistanceType.Physical, 20);
        SetDamageType(ResistanceType.Cold, 80);

        SetResistance(ResistanceType.Physical, 25, 35);
        SetResistance(ResistanceType.Cold, 40, 50);
        SetResistance(ResistanceType.Poison, 10, 20);
        SetResistance(ResistanceType.Energy, 10, 20);

        SetSkill(SkillName.Swords, 99.0);

        Fame = 3400;
        Karma = -3400;

        VirtualArmor = 30;

        Tamable = true;
        ControlSlots = 1;
        MinTameSkill = 85.5;
    }

    public override string CorpseName => "труп ледяной гончей";
    public override string DefaultName => "ледяная гончая";

    public override int Meat => 1;
    public override FoodType FavoriteFood => FoodType.Meat;
    public override PackInstinct PackInstinct => PackInstinct.Canine;

    public override WeaponAbility GetWeaponAbility() => WeaponAbility.ParalyzingBlow;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Average);
        AddLoot(LootPack.Meager);
    }
}

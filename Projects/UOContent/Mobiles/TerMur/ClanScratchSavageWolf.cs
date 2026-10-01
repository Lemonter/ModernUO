using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/ClanSSW.cs). SetWeaponAbility
/// converted to a GetWeaponAbility() override.</summary>
[SerializationGenerator(0, false)]
[CorpseName("a clan scratch savage wolf corpse")]
public partial class ClanScratchSavageWolf : BaseCreature
{
    [Constructible]
    public ClanScratchSavageWolf() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Body = 98;
        Hue = 0x2C;
        BaseSoundID = 229;

        SetStr(170);
        SetDex(244);
        SetInt(57);

        SetHits(65);

        SetDamage(8, 10);

        SetDamageType(ResistanceType.Physical, 20);
        SetDamageType(ResistanceType.Cold, 80);

        SetResistance(ResistanceType.Physical, 30, 35);
        SetResistance(ResistanceType.Cold, 40, 45);
        SetResistance(ResistanceType.Poison, 25, 30);
        SetResistance(ResistanceType.Energy, 20, 25);

        SetSkill(SkillName.Swords, 99.0, 100.0);
        SetSkill(SkillName.MagicResist, 41.5, 42.5);
        SetSkill(SkillName.Tactics, 65.1, 70.0);
        SetSkill(SkillName.Wrestling, 42.3, 45.5);

        Fame = 3400;
        Karma = -3400;
    }

    public override string DefaultName => "дикий волк клана Царапина";

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

using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Mobiles/ShipRat.cs). Spawned when a barrel of
///     the Gravewater Lake supply ring is broken open.
/// </summary>
[SerializationGenerator(0, false)]
public partial class ShipRat : BaseCreature
{
    [Constructible]
    public ShipRat() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Body = 0xD7;
        BaseSoundID = 0x188;

        SetStr(32, 74);
        SetDex(46, 65);
        SetInt(16, 30);

        SetHits(26, 39);
        SetMana(0);

        SetDamage(4, 8);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 15, 20);
        SetResistance(ResistanceType.Fire, 5, 10);
        SetResistance(ResistanceType.Poison, 25, 35);

        SetSkill(SkillName.MagicResist, 25.1, 30.0);
        SetSkill(SkillName.Tactics, 29.3, 44.0);
        SetSkill(SkillName.Wrestling, 29.3, 44.0);

        Fame = 300;
        Karma = -300;

        VirtualArmor = 18;
    }

    public override string CorpseName => "труп корабельной крысы";
    public override string DefaultName => "корабельная крыса";

    public override int Meat => 1;
    public override int Hides => 6;

    public override FoodType FavoriteFood =>
        FoodType.Fish | FoodType.Meat | FoodType.FruitsAndVeggies | FoodType.Eggs;

    public override void GenerateLoot() => AddLoot(LootPack.Poor);
}

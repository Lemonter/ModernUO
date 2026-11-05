using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/Macaw.cs). MacawSpawner does not exist here,
/// so the spawner bookkeeping (OnLocationChange leash check, Delete unhook) and the GoldFoil drop
/// are dropped. ServUO declares it in Server.Items; it is a creature, so it lives in Server.Mobiles.</summary>
[SerializationGenerator(0, false)]
public partial class Macaw : BaseCreature
{
    [Constructible]
    public Macaw() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Body = 5;
        Hue = Utility.RandomBirdHue();

        SetStr(100, 150);
        SetDex(400, 500);
        SetInt(80, 90);

        SetHits(700, 800);

        SetDamage(15, 25);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 80, 90);
        SetResistance(ResistanceType.Fire, 60, 77);
        SetResistance(ResistanceType.Cold, 70, 85);
        SetResistance(ResistanceType.Poison, 55, 85);
        SetResistance(ResistanceType.Energy, 50, 60);

        SetSkill(SkillName.Wrestling, 120.0, 140.0);
        SetSkill(SkillName.Tactics, 120.0, 140.0);
        SetSkill(SkillName.MagicResist, 95.0, 105.0);

        Fame = 7000;
        Karma = -7000;
    }

    public override string CorpseName => "труп ары";
    public override string DefaultName => "яростный ара";

    public override int GetIdleSound() => 0x2EF;
    public override int GetAttackSound() => 0x2EE;
    public override int GetAngerSound() => 0x2EF;
    public override int GetHurtSound() => 0x2F1;
    public override int GetDeathSound() => 0x2F2;

    public override MeatType MeatType => MeatType.Bird;
    public override int Feathers => 32;
    public override int Meat => 1;
    public override FoodType FavoriteFood => FoodType.FruitsAndVeggies | FoodType.GrainsAndHay;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.FilthyRich, 2);
    }
}

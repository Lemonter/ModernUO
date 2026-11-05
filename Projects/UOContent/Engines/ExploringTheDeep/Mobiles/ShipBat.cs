using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Mobiles/ShipBat.cs). Spawned when a barrel of
///     the Gravewater Lake supply ring is broken open.
/// </summary>
[SerializationGenerator(0, false)]
public partial class ShipBat : VampireBat
{
    [Constructible]
    public ShipBat()
    {
        Body = 317;
        BaseSoundID = 0x270;

        SetStr(120);
        SetDex(120);
        SetInt(101);

        SetHits(85, 96);

        SetDamage(4, 12);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 15, 20);
        SetResistance(ResistanceType.Fire, 15, 25);
        SetResistance(ResistanceType.Cold, 10, 15);
        SetResistance(ResistanceType.Poison, 15, 25);
        SetResistance(ResistanceType.Energy, 10, 15);

        SetSkill(SkillName.Tactics, 45.0, 55.0);
        SetSkill(SkillName.Wrestling, 90.0, 100.0);

        Fame = 1000;
        Karma = -1000;

        VirtualArmor = 14;
    }

    public override string CorpseName => "труп корабельной мыши";
    public override string DefaultName => "корабельная мышь";

    public override int GetIdleSound() => 0x29B;

    public override void GenerateLoot() => AddLoot(LootPack.Poor);
}

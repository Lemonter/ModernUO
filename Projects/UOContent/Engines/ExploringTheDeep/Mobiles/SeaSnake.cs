using System;
using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Mobiles/SeaSnake.cs). Spawned when a barrel of
///     the Gravewater Lake supply ring is broken open.
/// </summary>
[SerializationGenerator(0, false)]
public partial class SeaSnake : BaseCreature
{
    [Constructible]
    public SeaSnake() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Body = 92;
        BaseSoundID = 219;
        Hue = 2041;

        SetStr(261);
        SetDex(193);
        SetInt(39);

        SetHits(194);

        SetDamage(5, 21);

        SetDamageType(ResistanceType.Physical, 50);
        SetDamageType(ResistanceType.Poison, 50);

        SetResistance(ResistanceType.Physical, 35, 45);
        SetResistance(ResistanceType.Fire, 5, 10);
        SetResistance(ResistanceType.Cold, 5, 10);
        SetResistance(ResistanceType.Poison, 100);
        SetResistance(ResistanceType.Energy, 5, 10);

        SetSkill(SkillName.Poisoning, 90.1, 100.0);
        SetSkill(SkillName.MagicResist, 95.1, 100.0);
        SetSkill(SkillName.Tactics, 80.1, 95.0);
        SetSkill(SkillName.Wrestling, 85.1, 100.0);

        Fame = 7000;
        Karma = -7000;

        VirtualArmor = 40;
    }

    public override string CorpseName => "труп морской змеи";
    public override string DefaultName => "морская змея";

    public override bool DeathAdderCharmable => true;
    public override int Meat => 1;
    public override Poison PoisonImmune => Poison.Deadly;
    public override Poison HitPoison => Poison.Deadly;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Average);
        AddLoot(LootPack.Gems, 2);
    }
}

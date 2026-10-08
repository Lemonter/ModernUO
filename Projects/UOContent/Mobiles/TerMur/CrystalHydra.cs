using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/CrystalHydra.cs). SetSpecialAbility
/// dropped (see Rotworm.cs).
///
/// An earlier note here said LootPack.ArcanistScrolls had been dropped because it didn't exist;
/// checked against the source and the original's loot is UltraRich / HighScrolls / Parrot with
/// no ArcanistScrolls in it at all, so nothing was lost. The one thing still missing is Parrot,
/// which is commented out upstream in this codebase's own LootPack.cs pending ParrotItem.</summary>
[SerializationGenerator(0, false)]
[CorpseName("a crystal hydra corpse")]
public partial class CrystalHydra : BaseCreature
{
    [Constructible]
    public CrystalHydra() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Body = 0x109;
        Hue = 0x47E;
        BaseSoundID = 0x16A;

        SetStr(800, 830);
        SetDex(100, 120);
        SetInt(100, 120);

        SetHits(1450, 1500);

        SetDamage(21, 26);

        SetDamageType(ResistanceType.Physical, 5);
        SetDamageType(ResistanceType.Fire, 5);
        SetDamageType(ResistanceType.Cold, 80);
        SetDamageType(ResistanceType.Poison, 5);
        SetDamageType(ResistanceType.Energy, 5);

        SetResistance(ResistanceType.Physical, 65, 75);
        SetResistance(ResistanceType.Fire, 20, 30);
        SetResistance(ResistanceType.Cold, 80, 100);
        SetResistance(ResistanceType.Poison, 35, 45);
        SetResistance(ResistanceType.Energy, 80, 100);

        SetSkill(SkillName.Wrestling, 100.0, 120.0);
        SetSkill(SkillName.Tactics, 100.0, 110.0);
        SetSkill(SkillName.MagicResist, 80.0, 100.0);
        SetSkill(SkillName.Anatomy, 70.0, 80.0);

        Fame = 17000;
        Karma = -17000;
    }

    public override string DefaultName => "кристаллическая гидра";

    public override void GenerateLoot()
    {
        AddLoot(LootPack.UltraRich, 2);
        AddLoot(LootPack.HighScrolls);
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        if (Utility.RandomDouble() < 0.25)
        {
            c.DropItem(new ShatteredCrystals());
        }
    }

    public override int Hides => 40;
    public override int Meat => 19;
    public override int TreasureMapLevel => 5;
}

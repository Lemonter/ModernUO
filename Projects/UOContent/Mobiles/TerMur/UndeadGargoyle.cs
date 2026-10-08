using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/UndeadGargoyle.cs). The original's own
/// header admits the stats are a guess built off the plain gargoyle and that the body ID is
/// unconfirmed; ported as it stands rather than invented anew.</summary>
[SerializationGenerator(0, false)]
[CorpseName("an undead gargoyle corpse")]
public partial class UndeadGargoyle : BaseCreature
{
    [Constructible]
    public UndeadGargoyle() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Body = 722;
        BaseSoundID = 372;

        SetStr(250, 350);
        SetDex(120, 140);
        SetInt(250, 350);

        SetHits(200, 300);

        SetDamage(15, 27);

        SetDamageType(ResistanceType.Physical, 10);
        SetDamageType(ResistanceType.Cold, 50);
        SetDamageType(ResistanceType.Energy, 40);

        SetResistance(ResistanceType.Physical, 45, 55);
        SetResistance(ResistanceType.Fire, 30, 40);
        SetResistance(ResistanceType.Cold, 40, 55);
        SetResistance(ResistanceType.Poison, 55, 65);
        SetResistance(ResistanceType.Energy, 40, 50);

        SetSkill(SkillName.EvalInt, 90.1, 110.0);
        SetSkill(SkillName.Magery, 120.0);
        SetSkill(SkillName.MagicResist, 100.1, 120.0);
        SetSkill(SkillName.Tactics, 60.1, 70.0);
        SetSkill(SkillName.Wrestling, 60.1, 70.0);
        SetSkill(SkillName.Necromancy, 70.0, 120.0);
        SetSkill(SkillName.SpiritSpeak, 62.9, 113.7);

        Fame = 3500;
        Karma = -3500;

        VirtualArmor = 32;

        if (Utility.RandomDouble() < 0.025)
        {
            PackItem(new GargoylesPickaxe());
        }
    }

    public override string DefaultName => "нежить-горгулья";

    public override int TreasureMapLevel => 1;
    public override int Meat => 1;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Average);
        AddLoot(LootPack.MedScrolls);
        AddLoot(LootPack.Gems, Utility.RandomMinMax(1, 4));
    }
}

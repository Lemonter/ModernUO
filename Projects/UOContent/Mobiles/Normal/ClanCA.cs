using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/ClanCA.cs). The ServUO deserializer fixed an
/// old body id; a new type has no such history.</summary>
[SerializationGenerator(0, false)]
public partial class ClanCA : BaseCreature
{
    [Constructible]
    public ClanCA() : base(AIType.AI_Archer, FightMode.Closest, 10, 1)
    {
        Body = 0x8E;
        BaseSoundID = 437;

        SetStr(146, 175);
        SetDex(101, 130);
        SetInt(120, 135);

        SetHits(120, 145);

        SetDamage(4, 10);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 23, 35);
        SetResistance(ResistanceType.Fire, 20, 30);
        SetResistance(ResistanceType.Cold, 30, 50);
        SetResistance(ResistanceType.Poison, 15, 20);
        SetResistance(ResistanceType.Energy, 10, 20);

        SetSkill(SkillName.Anatomy, 0);
        SetSkill(SkillName.Archery, 80.1, 90.0);
        SetSkill(SkillName.MagicResist, 81.1, 90.0);
        SetSkill(SkillName.Tactics, 53.8, 75.0);
        SetSkill(SkillName.Wrestling, 62.3, 75.0);

        Fame = 6500;
        Karma = -6500;

        VirtualArmor = 56;

        AddItem(new Bow());
        PackItem(new Arrow(Utility.RandomMinMax(50, 70)));
    }

    public override string CorpseName => "труп помощника клана Щебета";
    public override string DefaultName => "помощник клана Щебета";

    public override bool CanRummageCorpses => true;
    public override int Hides => 8;
    public override HideType HideType => HideType.Spined;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Rich);
    }
}

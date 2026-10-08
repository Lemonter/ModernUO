using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/ClanSS.cs). Same SetWearable/Arrow-drop
/// fix as ClanChitterAssistant.cs.</summary>
[SerializationGenerator(0, false)]
[CorpseName("a clan scratch scrounger corpse")]
public partial class ClanScratchScrounger : BaseCreature
{
    [Constructible]
    public ClanScratchScrounger() : base(AIType.AI_Archer, FightMode.Closest, 10, 1)
    {
        Body = 0x8E;
        BaseSoundID = 437;

        SetStr(97, 100);
        SetDex(98, 100);
        SetInt(45, 50);

        SetHits(135);

        SetDamage(4, 5);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 25, 30);
        SetResistance(ResistanceType.Fire, 20, 25);
        SetResistance(ResistanceType.Cold, 49, 55);
        SetResistance(ResistanceType.Poison, 10, 20);
        SetResistance(ResistanceType.Energy, 10, 20);

        SetSkill(SkillName.Anatomy, 51.5, 55.5);
        SetSkill(SkillName.MagicResist, 65.1, 90.0);
        SetSkill(SkillName.Tactics, 59.1, 65.0);
        SetSkill(SkillName.Wrestling, 72.5, 75.0);

        Fame = 6500;
        Karma = -6500;

        AddItem(new Bow());
    }

    public override string DefaultName => "стервятник клана Царапина";

    public override bool CanRummageCorpses => true;
    public override int Hides => 8;
    public override HideType HideType => HideType.Spined;
    public override int TreasureMapLevel => 2;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Rich, 2);
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);
        c.DropItem(new Arrow(Utility.RandomMinMax(50, 70)));
    }
}

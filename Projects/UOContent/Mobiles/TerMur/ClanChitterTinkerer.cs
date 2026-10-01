using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/ClanCT.cs). Same SetWearable/Arrow-drop
/// fix as ClanChitterAssistant.cs.
///
/// NAME DISCREPANCY, left as-is pending a decision: the class was renamed from ClanCT to
/// ClanChitterTinkerer to match the name ModernUO's own spawn data asks for
/// (Data/Spawns/**), and the "CT" abbreviation alongside ClanCA/ClanRC/ClanRS/ClanSH/
/// ClanSS/ClanSSW reads as Chitter Tinkerer. But ServUO's own strings in this file say
/// "scratch" — probably a copy-paste from one of the Clan Scratch files, since no
/// ClanST.cs exists there. The player-visible strings below are therefore NOT touched:
/// flip them to "chitter" only if that's confirmed against OSI.</summary>
[SerializationGenerator(0, false)]
[CorpseName("a clan scratch tinkerer corpse")]
public partial class ClanChitterTinkerer : BaseCreature
{
    [Constructible]
    public ClanChitterTinkerer() : base(AIType.AI_Archer, FightMode.Closest, 10, 1)
    {
        Body = 0x8E;
        BaseSoundID = 437;

        SetStr(300, 330);
        SetDex(220, 240);
        SetInt(240, 275);

        SetHits(2025, 2068);

        SetDamage(4, 10);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 20, 30);
        SetResistance(ResistanceType.Fire, 20, 30);
        SetResistance(ResistanceType.Cold, 35, 50);
        SetResistance(ResistanceType.Poison, 10, 20);
        SetResistance(ResistanceType.Energy, 10, 20);

        SetSkill(SkillName.Anatomy, 62.5, 82.6);
        SetSkill(SkillName.Archery, 80.1, 90.0);
        SetSkill(SkillName.MagicResist, 76.8, 99.3);
        SetSkill(SkillName.Tactics, 64.2, 84.4);
        SetSkill(SkillName.Wrestling, 62.8, 85.0);

        Fame = 6500;
        Karma = -6500;

        AddItem(new Bow());
    }

    public override string DefaultName => "механик клана Царапина";

    public override bool CanRummageCorpses => true;
    public override int Hides => 8;
    public override HideType HideType => HideType.Spined;

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

using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/ClanCA.cs). SetWearable converted to
/// AddItem. LootPack.LootItem&lt;T&gt; doesn't exist here — the Arrow drop is a guaranteed
/// stack (not a chance roll) in the original, so converted to an always-on OnDeath drop of
/// Arrow(amount) rather than the chance-roll pattern used elsewhere (see WolfSpider.cs);
/// Arrow's int-amount constructor confirmed to exist.</summary>
[SerializationGenerator(0, false)]
[CorpseName("a clan chitter assistant corpse")]
public partial class ClanChitterAssistant : BaseCreature
{
    [Constructible]
    public ClanChitterAssistant() : base(AIType.AI_Archer, FightMode.Closest, 10, 1)
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

        AddItem(new Bow());
    }

    public override string DefaultName => "помощник клана Стрекот";

    public override bool CanRummageCorpses => true;
    public override int Hides => 8;
    public override HideType HideType => HideType.Spined;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Rich);
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);
        c.DropItem(new Arrow(Utility.RandomMinMax(50, 70)));
    }
}

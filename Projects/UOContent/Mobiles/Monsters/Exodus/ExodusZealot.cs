using ModernUO.Serialization;
using Server.Items;
using Server.Misc;

namespace Server.Mobiles;

/// <summary>The human half of the Exodus cult. Ported from ServUO
/// (Scripts/Services/Revamped Dungeons/TheExodusEncounter/Mobiles/ExodusZealot.cs).</summary>
[SerializationGenerator(0, false)]
[CorpseName("a human corpse")]
public partial class ExodusZealot : BaseCreature
{
    [Constructible]
    public ExodusZealot() : base(AIType.AI_Mage, FightMode.Closest, 10, 1)
    {
        Body = 401;
        Female = false;
        Hue = 33875;

        Name = NameList.RandomName("male");
        Title = "The Exodus Zealot";

        HairItemID = Race.Human.RandomHair(this);
        HairHue = Race.Human.RandomHairHue();

        SetStr(150, 210);
        SetDex(75, 90);
        SetInt(255, 310);

        SetHits(325, 390);

        SetDamage(6, 12);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 30, 40);
        SetResistance(ResistanceType.Fire, 20, 30);
        SetResistance(ResistanceType.Cold, 35, 40);
        SetResistance(ResistanceType.Poison, 30, 40);
        SetResistance(ResistanceType.Energy, 30, 40);

        SetSkill(SkillName.Wrestling, 70.0, 100.0);
        SetSkill(SkillName.Tactics, 80.0, 100.0);
        SetSkill(SkillName.MagicResist, 50.0, 70.0);
        SetSkill(SkillName.Anatomy, 70.0, 100.0);
        SetSkill(SkillName.Magery, 85.0, 100.0);
        SetSkill(SkillName.EvalInt, 80.0, 100.0);
        SetSkill(SkillName.Poisoning, 70.0, 100.0);

        Fame = 10000;
        Karma = -10000;

        VirtualArmor = 30;

        AddItem(new ThighBoots { Movable = false });
        AddItem(new HoodedShroudOfShadows(2702) { LootType = LootType.Blessed });
        AddItem(new Spellbook { LootType = LootType.Blessed });
    }

    public override bool AlwaysMurderer => true;
    public override bool ShowFameTitle => false;
    public override Poison PoisonImmune => Poison.Lethal;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.FilthyRich);
        AddLoot(LootPack.MedScrolls);
    }
}

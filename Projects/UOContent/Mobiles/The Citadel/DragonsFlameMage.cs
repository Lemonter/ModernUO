using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/DragonsFlameMage.cs) — Citadel
/// "Dragon's Flame Sect" rank-and-file mage. See SerpentsFangAssassin's class doc comment
/// for the SetWearable/ShowFameTitle conversion notes shared across this whole set.</summary>
[SerializationGenerator(0, false)]
[CorpseName("a black order mage corpse")]
public partial class DragonsFlameMage : BaseCreature
{
    [Constructible]
    public DragonsFlameMage() : base(AIType.AI_Mage, FightMode.Closest, 10, 1)
    {
        Title = "of the Dragon's Flame Sect";
        Female = Utility.RandomBool();
        Race = Race.Human;
        Hue = Race.RandomSkinHue();
        HairItemID = Race.RandomHair(Female);
        HairHue = Race.RandomHairHue();
        Race.RandomFacialHair(this);

        AddItem(new NinjaTabi());
        AddItem(new FancyShirt { Hue = 0x51D });
        AddItem(new Hakama { Hue = 0x51D });
        AddItem(new Kasa { Hue = 0x51D });

        SetStr(340, 360);
        SetDex(200, 215);
        SetInt(400, 415);

        SetHits(600, 615);

        SetDamage(13, 15);

        SetDamageType(ResistanceType.Physical, 10);
        SetDamageType(ResistanceType.Fire, 20);
        SetDamageType(ResistanceType.Cold, 20);
        SetDamageType(ResistanceType.Energy, 50);

        SetResistance(ResistanceType.Physical, 40, 50);
        SetResistance(ResistanceType.Fire, 30, 50);
        SetResistance(ResistanceType.Cold, 55, 60);
        SetResistance(ResistanceType.Poison, 50, 60);
        SetResistance(ResistanceType.Energy, 60, 70);

        SetSkill(SkillName.EvalInt, 70.1, 80.0);
        SetSkill(SkillName.Magery, 90.1, 100.0);
        SetSkill(SkillName.MagicResist, 85.1, 95.0);
        SetSkill(SkillName.Tactics, 70.1, 80.0);
        SetSkill(SkillName.Wrestling, 60.1, 80.0);

        Fame = 13000;
        Karma = -13000;
    }

    public override string DefaultName => "маг Чёрного ордена";

    public override bool AlwaysMurderer => true;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.FilthyRich, 4);
    }

    public override void AlterSpellDamageFrom(Mobile from, ref int damage)
    {
        from?.Damage(damage / 2, from);
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        if (Utility.RandomDouble() < 0.3)
        {
            c.DropItem(new DragonFlameSectBadge());
        }
    }
}

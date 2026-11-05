using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO's Wrong Revamped dungeon (Mobile/Archmage.cs) — the insane archmage at the top of
///     the prison. The scroll roll is the original's: four of sixteen outcomes give a necromancy scroll,
///     the other twelve give nothing. ServUO's <c>[CorpseName]</c> attribute became a
///     <c>CorpseName</c> override because nothing here reads the attribute.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Archmage : BaseCreature
{
    public override string CorpseName => "труп архимага";

    [Constructible]
    public Archmage() : base(AIType.AI_Mage, FightMode.Closest, 10, 1)
    {
        Name = RussianNames.RandomMale();
        Title = "безумный архимаг";
        Body = Utility.RandomList(125, 126);

        AddItem(new Robe(Utility.RandomMetalHue()));
        AddItem(new Sandals());
        Utility.AssignRandomHair(this);

        SetStr(85, 90);
        SetDex(194, 203);
        SetInt(237, 241);

        SetHits(3106, 3122);
        SetMana(578, 616);

        SetDamage(15, 20);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 65, 68);
        SetResistance(ResistanceType.Fire, 65, 66);
        SetResistance(ResistanceType.Cold, 62, 69);
        SetResistance(ResistanceType.Poison, 62, 67);
        SetResistance(ResistanceType.Energy, 64, 68);

        SetSkill(SkillName.EvalInt, 88.9, 94.1);
        SetSkill(SkillName.Magery, 96.9, 98.4);
        SetSkill(SkillName.Meditation, 89.9, 90.7);
        SetSkill(SkillName.MagicResist, 87.2, 88.7);
        SetSkill(SkillName.Tactics, 78.2, 79.9);
        SetSkill(SkillName.Wrestling, 84.8, 92.6);

        Fame = 14500;
        Karma = -14500;

        VirtualArmor = 16;

        switch (Utility.Random(16))
        {
            case 0:
                {
                    PackItem(new BloodOathScroll());
                    break;
                }
            case 1:
                {
                    PackItem(new CurseWeaponScroll());
                    break;
                }
            case 2:
                {
                    PackItem(new StrangleScroll());
                    break;
                }
            case 3:
                {
                    PackItem(new LichFormScroll());
                    break;
                }
        }

        PackReg(23);
        PackItem(new Sandals());

        if (Utility.RandomDouble() < 0.75)
        {
            PackItem(new SeveredHumanEars());
        }
    }

    public override bool CanRummageCorpses => true;
    public override bool AlwaysMurderer => true;
    public override int Meat => 1;
    public override int TreasureMapLevel => Core.AOS ? 2 : 0;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Average);
        AddLoot(LootPack.Meager);
        AddLoot(LootPack.MedScrolls, 2);
    }
}

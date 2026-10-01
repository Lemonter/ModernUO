using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

// TribeWarrior/TribeShaman/TribeChieftan ported from real OSI/ServUO content
// (Scripts/Mobiles/Normal/EodonTribesman.cs) — see Mobiles/Monsters/Reptile/Eodon/Dinosaurs.cs
// header for why an earlier from-scratch version was replaced. Simplifications: the
// skill-mastery `Masteries` override (ML feature tied to equipped-weapon skill) and the
// `Server.Engines.MyrmidexInvasion`-driven IsEnemy branch don't exist in this codebase, so
// both are dropped (falls back to default aggressor-based IsEnemy); `SetWearable(item, hue)`
// doesn't exist here either — replaced with `EquipItem(new X { Hue = h })`.
// Eodon.xml's XmlSpawner entries pass the tribe as a comma-separated string argument
// ("tribewarrior,Barrab") rather than ServUO's EodonTribe enum argument — string and
// parameterless (random-tribe) constructor overloads bridge that.
//
// BarakoHighChief/JukariHighChief/KurakHighChief/SakkhraHighChieftess/UraliHighChieftess,
// Hawkwind, and SirGeoffery below have NO real OSI/ServUO equivalent found anywhere
// searched — they remain original content (unlike the rest of this file).
public enum EodonTribe
{
    Jukari,
    Kurak,
    Barrab,
    Barako,
    Urali,
    Sakkhra
}

[SerializationGenerator(0, false)]
public abstract partial class BaseEodonTribesman : BaseCreature
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private EodonTribe _tribeType;

    [CommandProperty(AccessLevel.GameMaster)]
    public override Poison PoisonImmune => TribeType == EodonTribe.Barrab ? Poison.Deadly : null;

    public override bool InitialInnocent => true;
    public override int TreasureMapLevel => 2;

    protected static EodonTribe RandomTribe() => (EodonTribe)Utility.Random(6);

    protected static EodonTribe ParseTribe(string tribe) =>
        System.Enum.TryParse<EodonTribe>(tribe, true, out var result) ? result : RandomTribe();

    protected BaseEodonTribesman(AIType ai, EodonTribe type) : base(ai, FightMode.Closest)
    {
        TribeType = type;

        BuildBody();
        BuildEquipment();

        switch (type)
        {
            case EodonTribe.Jukari:
                SetResistance(ResistanceType.Physical, 20, 30);
                SetResistance(ResistanceType.Fire, 100);
                SetResistance(ResistanceType.Cold, 10, 20);
                SetResistance(ResistanceType.Poison, 40, 50);
                SetResistance(ResistanceType.Energy, 40, 50);
                break;
            case EodonTribe.Kurak:
                SetResistance(ResistanceType.Physical, 20, 30);
                SetResistance(ResistanceType.Fire, 10, 20);
                SetResistance(ResistanceType.Cold, 100);
                SetResistance(ResistanceType.Poison, 40, 50);
                SetResistance(ResistanceType.Energy, 40, 50);
                break;
            case EodonTribe.Barrab:
                SetResistance(ResistanceType.Physical, 20, 30);
                SetResistance(ResistanceType.Fire, 40, 50);
                SetResistance(ResistanceType.Cold, 40, 50);
                SetResistance(ResistanceType.Poison, 100);
                SetResistance(ResistanceType.Energy, 10, 20);
                break;
            case EodonTribe.Barako:
                SetResistance(ResistanceType.Physical, 20, 30);
                SetResistance(ResistanceType.Fire, 40, 50);
                SetResistance(ResistanceType.Cold, 40, 50);
                SetResistance(ResistanceType.Poison, 10, 20);
                SetResistance(ResistanceType.Energy, 40, 50);
                break;
            case EodonTribe.Urali:
                SetResistance(ResistanceType.Physical, 20, 30);
                SetResistance(ResistanceType.Fire, 40, 50);
                SetResistance(ResistanceType.Cold, 40, 50);
                SetResistance(ResistanceType.Poison, 10, 20);
                SetResistance(ResistanceType.Energy, 100);
                break;
            case EodonTribe.Sakkhra:
                SetResistance(ResistanceType.Physical, 100);
                SetResistance(ResistanceType.Fire, 40, 50);
                SetResistance(ResistanceType.Cold, 40, 50);
                SetResistance(ResistanceType.Poison, 60, 70);
                SetResistance(ResistanceType.Energy, 40, 50);
                break;
        }
    }

    public abstract void BuildBody();
    public abstract void BuildEquipment();

    protected void Wear(Item item, int hue = 0)
    {
        if (hue != 0)
        {
            item.Hue = hue;
        }

        EquipItem(item);
    }

    public override bool IsEnemy(Mobile m) => Aggressors.Exists(a => a.Attacker == m);
}

[SerializationGenerator(0, false)]
public partial class TribeWarrior : BaseEodonTribesman
{
    [Constructible]
    public TribeWarrior() : this(RandomTribe())
    {
    }

    [Constructible]
    public TribeWarrior(string tribe) : this(ParseTribe(tribe))
    {
    }

    public TribeWarrior(EodonTribe type) : base(AIType.AI_Melee, type)
    {
    }

    public override void BuildBody()
    {
        Name = $"{NameList.RandomName("savage")} из племени {TribeType}";

        SetStr(150);
        SetDex(150);
        SetInt(75);

        SetHits(2500);

        SetDamage(10, 23);

        SetDamageType(ResistanceType.Physical, 100);

        SetSkill(SkillName.Wrestling, 100, 120);
        SetSkill(SkillName.Fencing, 100, 120);
        SetSkill(SkillName.Swords, 100, 120);
        SetSkill(SkillName.Macing, 100, 120);
        SetSkill(SkillName.Archery, 100, 120);

        SetSkill(SkillName.Tactics, 100, 120);
        SetSkill(SkillName.Anatomy, 100, 120);
        SetSkill(SkillName.MagicResist, 100, 120);
        SetSkill(SkillName.Parry, 120);

        Female = TribeType != EodonTribe.Barrab && Utility.RandomBool();
        Body = Female ? 0x191 : 0x190;

        Fame = 12000;
        Karma = 8000;
    }

    public override void BuildEquipment()
    {
        Item weapon;

        switch (TribeType)
        {
            default:
            case EodonTribe.Jukari:
                weapon = new Pickaxe { Hue = 1175 };
                if (Female)
                {
                    Wear(new LeatherShorts(), 1175);
                    Wear(new LeatherBustierArms(), 1175);
                }
                else
                {
                    Wear(new LeatherLegs(), 1175);
                    Wear(new BodySash(), 1175);
                }

                Wear(new Torch());
                break;
            case EodonTribe.Kurak:
                weapon = new Tekagi();
                Wear(new LeatherDo());
                Wear(new PlateMempo(), 1192);
                Wear(new ShortPants(), 1192);
                Wear(new Sandals(), 1192);
                break;
            case EodonTribe.Barrab:
                weapon = new Spear();
                Wear(new PlateDo(), 1828);
                Wear(new Obi(), 1828);
                Wear(new PlateSuneate(), 1828);
                Wear(new DecorativePlateKabuto(), 1834);
                Wear(new SilverEarrings());
                Wear(new Sandals(), 1828);
                break;
            case EodonTribe.Barako:
                if (Female)
                {
                    weapon = new Maul { Hue = 2414 };
                    Wear(new DeerMask(), 2414);
                }
                else
                {
                    weapon = new WarMace { Hue = 2414 };
                    Wear(new BearMask(), 2414);
                }

                Wear(new StuddedChest(), 2414);
                Wear(new StuddedArms(), 2414);
                Wear(new StuddedLegs(), 2414);
                Wear(new StuddedGorget(), 2414);
                Wear(new LeatherGloves(), 2414);
                Wear(new Boots(), 2414);
                break;
            case EodonTribe.Urali:
                weapon = null;
                Wear(new DragonChest(), 2576);
                Wear(new LeatherJingasa(), 2576);
                Wear(new MetalShield(), 2576);
                Wear(new Waraji(), 2576);
                Wear(new ChainLegs(), 2576);
                break;
            case EodonTribe.Sakkhra:
                weapon = new Bow { Hue = 2125 };
                if (Female)
                {
                    Wear(new LeatherBustierArms(), 2128);
                    Wear(new LeatherSkirt(), 2125);
                }
                else
                {
                    Wear(new LeatherChest(), 2128);
                    Wear(new SkullCap(), 2125);
                    Wear(new Kilt(), 2125);
                }

                Wear(new ThighBoots(), 2129);
                break;
        }

        if (weapon != null)
        {
            weapon.LootType = LootType.Blessed;
            Wear(weapon);
        }
    }

    public override string CorpseName => "труп туземца";
    public override bool ShowFameTitle => false;

    public override void GenerateLoot() => AddLoot(LootPack.Rich, 2);
}

[SerializationGenerator(0, false)]
public partial class TribeShaman : BaseEodonTribesman
{
    [Constructible]
    public TribeShaman() : this(RandomTribe())
    {
    }

    [Constructible]
    public TribeShaman(string tribe) : this(ParseTribe(tribe))
    {
    }

    public TribeShaman(EodonTribe type) : base(AIType.AI_Mage, type)
    {
        RangeFight = 7;
    }

    public override void BuildBody()
    {
        Name = $"{NameList.RandomName("savage shaman")} из племени {TribeType}";

        SetStr(125);
        SetDex(75, 100);
        SetInt(200, 250);

        SetHits(2500);

        SetDamage(10, 15);

        SetDamageType(ResistanceType.Physical, 100);

        SetSkill(SkillName.Wrestling, 100);
        SetSkill(SkillName.Fencing, 100);
        SetSkill(SkillName.Swords, 100);
        SetSkill(SkillName.Macing, 100);
        SetSkill(SkillName.Archery, 100);

        SetSkill(SkillName.Tactics, 100);
        SetSkill(SkillName.Anatomy, 100);
        SetSkill(SkillName.MagicResist, 100);
        SetSkill(SkillName.Magery, 120);
        SetSkill(SkillName.EvalInt, 120);

        Female = TribeType is EodonTribe.Barrab or EodonTribe.Sakkhra || Utility.RandomBool();
        Body = Female ? 0x191 : 0x190;

        Fame = 12000;
        Karma = 8000;
    }

    public override void BuildEquipment()
    {
        var weapon = new WildStaff();

        switch (TribeType)
        {
            default:
            case EodonTribe.Jukari:
                Wear(new FemaleLeatherChest(), 1933);
                Wear(new LeatherSkirt(), 1933);
                Wear(new Torch());
                weapon.Hue = 1933;
                break;
            case EodonTribe.Kurak:
                Wear(new LeatherDo(), 1150);
                Wear(new PlateMempo(), 1150);
                Wear(new TattsukeHakama(), 1150);
                Wear(new Sandals(), 1150);
                weapon.Hue = 1150;
                break;
            case EodonTribe.Barrab:
                Wear(new Robe { ItemID = 9860 }, 1834);
                Wear(new Obi(), 1834);
                Wear(new Sandals(), 1831);
                weapon.Hue = 1831;
                break;
            case EodonTribe.Barako:
                Wear(new LeatherGloves(), 1518);
                Wear(new TribalMask(), 1518);
                Wear(new BoneChest(), 1518);
                Wear(new StuddedGorget(), 1518);
                Wear(new Boots(), 1518);
                weapon.Hue = 1518;
                break;
            case EodonTribe.Urali:
                Wear(new ChainLegs(), 2576);
                Wear(new GoldEarrings());
                Wear(new Sandals(), 2576);
                weapon.Hue = 2576;
                break;
            case EodonTribe.Sakkhra:
                Wear(new StuddedChest(), 2118);
                Wear(new LeatherArms(), 2106);
                Wear(new LeatherGloves(), 2106);
                Wear(new SkullCap(), 2118);
                Wear(new RingmailLegs(), 2106);
                Wear(new ThighBoots(), 2106);
                weapon.Hue = 2118;
                break;
        }

        weapon.LootType = LootType.Blessed;
        Wear(weapon);
    }

    public override string CorpseName => "труп шамана";
    public override bool ShowFameTitle => false;

    public override void GenerateLoot()
    {
        PackReg(5, 10);
        PackItem(new Bandage(Utility.RandomMinMax(3, 5)));
    }
}

[SerializationGenerator(0, false)]
public partial class TribeChieftan : BaseEodonTribesman
{
    [Constructible]
    public TribeChieftan() : this(RandomTribe())
    {
    }

    [Constructible]
    public TribeChieftan(string tribe) : this(ParseTribe(tribe))
    {
    }

    public TribeChieftan(EodonTribe type) : base(AIType.AI_Melee, type)
    {
    }

    public override void BuildBody()
    {
        Female = TribeType is EodonTribe.Barrab or EodonTribe.Urali || Utility.RandomBool();
        Body = Female ? 0x191 : 0x190;

        Name = $"{NameList.RandomName("savage")} — {(Female ? "вождиня" : "вождь")} племени {TribeType}";

        SetStr(200);
        SetDex(200);
        SetInt(200, 250);

        SetHits(4500);

        SetDamage(15, 28);

        SetDamageType(ResistanceType.Physical, 100);

        SetSkill(SkillName.Wrestling, 120);
        SetSkill(SkillName.Fencing, 120);
        SetSkill(SkillName.Swords, 120);
        SetSkill(SkillName.Macing, 120);
        SetSkill(SkillName.Archery, 120);

        SetSkill(SkillName.Tactics, 120);
        SetSkill(SkillName.Anatomy, 120);
        SetSkill(SkillName.MagicResist, 120);
        SetSkill(SkillName.Parry, 120);

        Fame = 18000;
        Karma = 8000;
    }

    public override void BuildEquipment()
    {
        Item weapon;

        switch (TribeType)
        {
            default:
            case EodonTribe.Jukari:
                Wear(new LeatherLegs(), 1175);
                Wear(new Shirt(), 1175);
                Wear(new Torch());
                weapon = new Bokuto { Hue = 1175 };
                break;
            case EodonTribe.Kurak:
                Wear(new LeatherDo(), 1175);
                Wear(new FancyShirt(), 1175);
                Wear(new TattsukeHakama());
                Wear(new Sandals(), 1175);
                weapon = new Tekagi { Hue = 1175 };
                break;
            case EodonTribe.Barrab:
                Wear(new PlateDo(), 1828);
                Wear(new PlateSuneate(), 1828);
                Wear(new DecorativePlateKabuto(), 1834);
                Wear(new Sandals(), 1828);
                weapon = new Spear { Hue = 1828 };
                break;
            case EodonTribe.Barako:
                Wear(new BoneChest(), 2407);
                Wear(new StuddedGorget(), 2407);
                Wear(new Boots(), 2407);
                weapon = new Scepter { Hue = 2407 };
                break;
            case EodonTribe.Urali:
                Wear(new ChainLegs(), 2576);
                Wear(new DragonChest(), 2576);
                Wear(new DragonArms(), 2576);
                Wear(new MetalShield(), 2576);
                Wear(new Circlet(), 2576);
                Wear(new Waraji(), 2576);
                weapon = null;
                break;
            case EodonTribe.Sakkhra:
                Wear(new StuddedChest(), 2118);
                Wear(new LeatherArms(), 2106);
                Wear(new LeatherGloves(), 2106);
                Wear(new SkullCap(), 2118);
                Wear(new RingmailLegs(), 2106);
                Wear(new ThighBoots(), 2106);
                weapon = new Yumi { Hue = 2118 };
                break;
        }

        if (weapon != null)
        {
            weapon.LootType = LootType.Blessed;
            Wear(weapon);
        }
    }

    public override string CorpseName => "труп вождя племени";

    public override void GenerateLoot() => AddLoot(LootPack.FilthyRich, 2);
}

// No real OSI/ServUO equivalent found — original content.
[SerializationGenerator(0, false)]
public partial class WanderingShaman : BaseCreature
{
    [Constructible]
    public WanderingShaman() : base(AIType.AI_Mage, FightMode.Closest)
    {
        Name = "странствующий шаман";

        Body = 0x190;
        Hue = Race.Human.RandomSkinHue();

        SetStr(126, 150);
        SetDex(86, 105);
        SetInt(156, 180);

        SetDamage(7, 14);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 35, 45);
        SetResistance(ResistanceType.Fire, 30, 40);
        SetResistance(ResistanceType.Poison, 30, 40);
        SetResistance(ResistanceType.Energy, 35, 45);

        SetSkill(SkillName.EvalInt, 90.0, 110.0);
        SetSkill(SkillName.Magery, 90.0, 110.0);
        SetSkill(SkillName.Meditation, 90.0, 110.0);
        SetSkill(SkillName.MagicResist, 90.0, 110.0);
        SetSkill(SkillName.Wrestling, 70.0, 90.0);

        Fame = 4500;
        Karma = -4500;

        PackReg(15, 25);

        AddItem(new BoneArms());
        AddItem(new BoneLegs());
        AddItem(new DeerMask());
    }

    public override string CorpseName => "труп шамана";
    public override bool AlwaysMurderer => true;
    public override bool ShowFameTitle => false;

    public override void GenerateLoot() => AddLoot(LootPack.FilthyRich);
}

[SerializationGenerator(0, false)]
public abstract partial class HighChiefBase : BaseCreature
{
    protected HighChiefBase(AIType ai) : base(ai)
    {
        Hue = Race.Human.RandomSkinHue();

        SetStr(186, 220);
        SetDex(106, 130);
        SetInt(96, 120);

        SetDamage(15, 25);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 45, 55);
        SetResistance(ResistanceType.Fire, 20, 30);
        SetResistance(ResistanceType.Poison, 30, 40);
        SetResistance(ResistanceType.Energy, 20, 30);

        SetSkill(SkillName.Fencing, 90.0, 105.0);
        SetSkill(SkillName.Macing, 90.0, 105.0);
        SetSkill(SkillName.MagicResist, 95.0, 115.0);
        SetSkill(SkillName.Swords, 90.0, 105.0);
        SetSkill(SkillName.Tactics, 95.0, 115.0);

        Fame = 12000;
        Karma = -12000;

        ControlSlots = 5;
        Tamable = false;
    }

    public override string CorpseName => "труп верховного вождя";
    public override bool AlwaysMurderer => true;
    public override bool ShowFameTitle => false;

    public override void GenerateLoot() => AddLoot(LootPack.SuperBoss);
}

[SerializationGenerator(0, false)]
public partial class BarakoHighChief : HighChiefBase
{
    [Constructible]
    public BarakoHighChief() : base(AIType.AI_Melee)
    {
        Name = "верховный вождь Барако";
        Body = 0x190;

        AddItem(new WarAxe());
        AddItem(new BoneArms());
        AddItem(new BoneLegs());
        AddItem(new SavageMask());
    }
}

[SerializationGenerator(0, false)]
public partial class JukariHighChief : HighChiefBase
{
    [Constructible]
    public JukariHighChief() : base(AIType.AI_Melee)
    {
        Name = "верховный вождь Джукари";
        Body = 0x190;

        AddItem(new Kryss());
        AddItem(new BoneArms());
        AddItem(new BoneLegs());
        AddItem(new SavageMask());
    }
}

[SerializationGenerator(0, false)]
public partial class KurakHighChief : HighChiefBase
{
    [Constructible]
    public KurakHighChief() : base(AIType.AI_Melee)
    {
        Name = "верховный вождь Курак";
        Body = 0x190;

        AddItem(new WarMace());
        AddItem(new BoneArms());
        AddItem(new BoneLegs());
        AddItem(new SavageMask());
    }
}

[SerializationGenerator(0, false)]
public partial class SakkhraHighChieftess : HighChiefBase
{
    [Constructible]
    public SakkhraHighChieftess() : base(AIType.AI_Mage)
    {
        Name = "верховная вождиня Саккхра";
        Female = true;
        Body = 0x191;

        AddItem(new BoneArms());
        AddItem(new BoneLegs());
        AddItem(new DeerMask());
    }
}

[SerializationGenerator(0, false)]
public partial class UraliHighChieftess : HighChiefBase
{
    [Constructible]
    public UraliHighChieftess() : base(AIType.AI_Melee)
    {
        Name = "верховная вождиня Урали";
        Female = true;
        Body = 0x191;

        AddItem(new Spear());
        AddItem(new BoneArms());
        AddItem(new BoneLegs());
        AddItem(new SavageMask());
    }
}

// Peaceful, unique named NPCs referenced by Eodon.xml — not combatants, so no loot/damage setup.
[SerializationGenerator(0, false)]
public partial class Hawkwind : BaseCreature
{
    [Constructible]
    public Hawkwind() : base(AIType.AI_Vendor, FightMode.None)
    {
        Name = "Ястребиный Ветер";
        Title = "провидец";

        Body = 0x190;
        Hue = Race.Human.RandomSkinHue();

        SetStr(60, 60);
        SetDex(60, 60);
        SetInt(120, 120);

        SetHits(60, 60);

        SetSkill(SkillName.Meditation, 100.0);
        SetSkill(SkillName.Magery, 100.0);

        Blessed = true;

        AddItem(new Robe(0x479));
        AddItem(new GnarledStaff());
    }

    public override bool AlwaysMurderer => false;
    public override bool ShowFameTitle => false;
    public override bool Commandable => false;

    public override void GenerateLoot()
    {
    }
}

[SerializationGenerator(0, false)]
public partial class SirGeoffery : BaseCreature
{
    [Constructible]
    public SirGeoffery() : base(AIType.AI_Vendor, FightMode.None)
    {
        Name = "сэр Джеффри";
        Title = "пленный рыцарь";

        Body = 0x190;
        Hue = Race.Human.RandomSkinHue();

        SetStr(80, 80);
        SetDex(60, 60);
        SetInt(60, 60);

        SetHits(80, 80);

        Blessed = true;

        AddItem(new PlateChest());
        AddItem(new PlateArms());
        AddItem(new PlateLegs());
    }

    public override bool AlwaysMurderer => false;
    public override bool ShowFameTitle => false;
    public override bool Commandable => false;

    public override void GenerateLoot()
    {
    }
}

using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO's Shame Revamped dungeon (Mobiles/Creatures.cs) — the corrupted mage quartet.
///
///     The burning mage's <c>SetSpecialAbility(SpecialAbility.DragonBreath)</c> comes through
///     <see cref="GetMonsterAbilities" /> as <see cref="MonsterAbilities.FireBreath" />. Still dropped:
///     each class's <c>static Dictionary&lt;Mobile, Timer&gt; Table</c> plus its
///     verbatim <c>Timer.DelayCall</c> repeat — that curse now lives in one shared
///     <see cref="ManaSapEffect" /> (see its own file), applied and cancelled from the same hooks, with
///     the token cancelled in <see cref="OnAfterDelete" />. ServUO's <c>BodyValue</c> is <c>Body</c> here,
///     and <c>OnDamagedBySpell</c> takes the damage amount as a second argument (see TrapdoorSpider).
/// </summary>
[SerializationGenerator(0, false)]
public partial class BurningMage : BaseCreature
{
    public override string CorpseName => "труп пылающего мага";

    private static readonly MonsterAbility[] _abilities = [MonsterAbilities.FireBreath];

    public override MonsterAbility[] GetMonsterAbilities() => _abilities;

    [Constructible]
    public BurningMage() : base(AIType.AI_Mage, FightMode.Weakest, 10, 1)
    {
        Name = RussianNames.RandomMale();
        Title = "пылающий";
        SetStr(100, 125);

        Body = 0x190;
        Hue = 1281;

        SetHits(3000);
        SetMana(600, 800);
        SetDamage(10, 15);

        SetDamageType(ResistanceType.Physical, 50);
        SetDamageType(ResistanceType.Fire, 50);

        SetResistance(ResistanceType.Physical, 50, 60);
        SetResistance(ResistanceType.Fire, 50, 60);
        SetResistance(ResistanceType.Cold, 50, 60);
        SetResistance(ResistanceType.Poison, 50, 60);
        SetResistance(ResistanceType.Energy, 50, 60);

        SetSkill(SkillName.MagicResist, 125, 140);
        SetSkill(SkillName.Tactics, 100, 120);
        SetSkill(SkillName.Wrestling, 110, 130);
        SetSkill(SkillName.Magery, 120, 130);
        SetSkill(SkillName.EvalInt, 120, 130);

        AddItem(new Robe(1156));
        AddItem(new Sandals());

        PackReg(31);

        Utility.AssignRandomHair(this);

        Fame = 22000;
        Karma = -22000;
    }

    public override bool CanRummageCorpses => true;
    public override bool AlwaysMurderer => true;

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        if (0.33 > Utility.RandomDouble() && Region.Find(c.Location, c.Map)?.IsPartOf("Shame") == true)
        {
            c.DropItem(new ShameCrystal(4));
        }
    }

    public override void OnDamagedBySpell(Mobile from, int damage)
    {
        base.OnDamagedBySpell(from, damage);

        if (!ManaSapEffect.IsUnderEffects(from) && 0.50 > Utility.RandomDouble())
        {
            ManaSapEffect.Begin(from, this);
        }
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        ManaSapEffect.EndAllFrom(this);
    }

    public override void GenerateLoot()
    {
        AddLoot(LootPack.UltraRich, 2);
        AddLoot(LootPack.HighScrolls, Utility.RandomMinMax(5, 20));
    }
}

[SerializationGenerator(0, false)]
public partial class CrazedMage : BaseCreature
{
    public override string CorpseName => "труп безумца";

    [Constructible]
    public CrazedMage() : base(AIType.AI_Mystic, FightMode.Weakest, 10, 1)
    {
        Name = RussianNames.RandomMale();
        Title = "безумный";

        Body = 0x190;
        SetStr(225, 400);

        SetHits(3500);
        SetMana(600, 800);
        SetDamage(15, 21);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 60, 80);
        SetResistance(ResistanceType.Fire, 50, 60);
        SetResistance(ResistanceType.Cold, 50, 60);
        SetResistance(ResistanceType.Poison, 50, 60);
        SetResistance(ResistanceType.Energy, 50, 60);

        SetSkill(SkillName.MagicResist, 125, 140);
        SetSkill(SkillName.Tactics, 100, 120);
        SetSkill(SkillName.Wrestling, 140);
        SetSkill(SkillName.Anatomy, 100, 120);
        SetSkill(SkillName.Magery, 100, 110);
        SetSkill(SkillName.EvalInt, 100, 110);

        AddItem(new Robe(1157));
        AddItem(new Sandals());

        Utility.AssignRandomHair(this);
        Hue = Race.RandomSkinHue();

        Fame = 15000;
        Karma = -15000;
    }

    public override bool CanRummageCorpses => true;
    public override bool AlwaysMurderer => true;

    public override void OnDamagedBySpell(Mobile from, int damage)
    {
        base.OnDamagedBySpell(from, damage);

        if (!ManaSapEffect.IsUnderEffects(from) && 0.50 > Utility.RandomDouble())
        {
            ManaSapEffect.Begin(from, this);
        }
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        if (0.33 > Utility.RandomDouble() && Region.Find(c.Location, c.Map)?.IsPartOf("Shame") == true)
        {
            c.DropItem(new ShameCrystal(5));
        }
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        ManaSapEffect.EndAllFrom(this);
    }

    public override void GenerateLoot()
    {
        AddLoot(LootPack.FilthyRich, 2);
    }
}

[SerializationGenerator(0, false)]
public partial class CorruptedMage : EvilMage
{
    public override string CorpseName => "труп искажённого мага";

    [Constructible]
    public CorruptedMage()
    {
        Title = "искажённый маг";

        SetStr(150, 170);
        SetInt(100, 120);
        SetDex(110, 120);

        SetHits(1200, 1250);
        SetMana(800, 900);
        SetDamage(14, 17);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 55, 70);
        SetResistance(ResistanceType.Fire, 70, 80);
        SetResistance(ResistanceType.Cold, 60, 70);
        SetResistance(ResistanceType.Poison, 60, 70);
        SetResistance(ResistanceType.Energy, 65, 75);

        SetSkill(SkillName.MagicResist, 115, 120);
        SetSkill(SkillName.Tactics, 110, 120);
        SetSkill(SkillName.Wrestling, 100, 110);
        SetSkill(SkillName.Magery, 120, 130);
        SetSkill(SkillName.EvalInt, 120, 130);
        SetSkill(SkillName.Meditation, 100, 110);
    }

    public override bool CanRummageCorpses => true;
    public override bool AlwaysMurderer => true;
    public override int TreasureMapLevel => 2;

    public override void OnDamagedBySpell(Mobile from, int damage)
    {
        base.OnDamagedBySpell(from, damage);

        if (!ManaSapEffect.IsUnderEffects(from) && 0.10 > Utility.RandomDouble())
        {
            ManaSapEffect.Begin(from, this);
        }
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        if (0.33 > Utility.RandomDouble() && Region.Find(c.Location, c.Map)?.IsPartOf("Shame") == true)
        {
            c.DropItem(new ShameCrystal(3));
        }
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        ManaSapEffect.EndAllFrom(this);
    }

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Rich, 2);
    }
}

[SerializationGenerator(0, false)]
public partial class VileMage : CorruptedMage
{
    public override string CorpseName => "труп мерзкого мага";

    [Constructible]
    public VileMage()
    {
        Title = "мерзкий маг";

        SetStr(150, 170);
        SetInt(150, 170);
        SetDex(100, 110);

        SetHits(500, 900);
        SetMana(550, 600);
        SetDamage(11, 13);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 55, 70);
        SetResistance(ResistanceType.Fire, 55, 65);
        SetResistance(ResistanceType.Cold, 60, 70);
        SetResistance(ResistanceType.Poison, 55, 65);
        SetResistance(ResistanceType.Energy, 65, 75);

        SetSkill(SkillName.MagicResist, 110, 115);
        SetSkill(SkillName.Tactics, 110, 115);
        SetSkill(SkillName.Wrestling, 100, 110);
        SetSkill(SkillName.Magery, 110, 115);
        SetSkill(SkillName.EvalInt, 115, 125);
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        if (0.33 > Utility.RandomDouble() && Region.Find(c.Location, c.Map)?.IsPartOf("Shame") == true)
        {
            c.DropItem(new ShameCrystal(3));
        }
    }

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Rich, 2);
    }
}

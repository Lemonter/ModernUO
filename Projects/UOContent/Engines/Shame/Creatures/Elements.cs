using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO's Shame Revamped dungeon (Mobiles/Creatures.cs) — the earth-family elementals
///     of the upper levels.
///
///     Of the five <c>SetSpecialAbility</c> registrations they carried, only the molten one's
///     DragonBreath has a counterpart — <see cref="MonsterAbilities.FireBreath" /> through
///     <see cref="GetMonsterAbilities" />. ColossalRage (stone/greater earth/mud elementals) and
///     SearingWounds have none and stay dropped. The dungeon's
///     <c>Region.IsPartOf("Shame")</c> guard on the crystal drop is kept, so they only pay out inside the
///     dungeon.
/// </summary>
[SerializationGenerator(0, false)]
public partial class StoneElemental : EarthElemental
{
    public override string CorpseName => "труп каменного элементаля";

    [Constructible]
    public StoneElemental()
    {
        Name = "каменный элементаль";
        Hue = 2401;

        SetStr(140, 210);
        SetDex(80, 110);
        SetInt(90, 120);

        SetHits(900, 1000);

        SetDamage(15, 17);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 60, 65);
        SetResistance(ResistanceType.Fire, 50, 60);
        SetResistance(ResistanceType.Cold, 45, 55);
        SetResistance(ResistanceType.Poison, 55, 60);
        SetResistance(ResistanceType.Energy, 45, 55);

        SetSkill(SkillName.MagicResist, 100.0);
        SetSkill(SkillName.Tactics, 80.0, 96.0);
        SetSkill(SkillName.Wrestling, 80.0, 97.0);

        Fame = 4000;
        Karma = -4000;

        PackReg(1, 2);
        PackGem(1, 2);

        PackItem(new Granite());
        PackItem(new Sand());
    }

    public override int TreasureMapLevel => 2;

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        if (0.15 > Utility.RandomDouble() && Region.Find(c.Location, c.Map)?.IsPartOf("Shame") == true)
        {
            c.DropItem(new ShameCrystal());
        }
    }

    public override void GenerateLoot()
    {
        AddLoot(LootPack.FilthyRich, 2);
    }
}

[SerializationGenerator(0, false)]
public partial class GreaterEarthElemental : EarthElemental
{
    public override string CorpseName => "труп большого элементаля земли";

    [Constructible]
    public GreaterEarthElemental()
    {
        Name = "большой элементаль земли";
        Hue = 1143;

        SetHits(500, 600);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 50, 65);
        SetResistance(ResistanceType.Fire, 35, 45);
        SetResistance(ResistanceType.Cold, 35, 45);
        SetResistance(ResistanceType.Poison, 45, 55);
        SetResistance(ResistanceType.Energy, 25, 35);

        SetSkill(SkillName.MagicResist, 40, 70);
        SetSkill(SkillName.Tactics, 70, 90);
        SetSkill(SkillName.Wrestling, 80, 95);

        Fame = 2500;
        Karma = -2500;
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        if (0.08 > Utility.RandomDouble() && Region.Find(c.Location, c.Map)?.IsPartOf("Shame") == true)
        {
            c.DropItem(new ShameCrystal());
        }
    }

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Rich, 1);
    }
}

[SerializationGenerator(0, false)]
public partial class MudElemental : EarthElemental
{
    public override string CorpseName => "труп грязевого элементаля";

    [Constructible]
    public MudElemental()
    {
        Name = "грязевой элементаль";
        Hue = 542;

        SetStr(400, 550);
        SetHits(650, 850);
        SetDamage(17, 19);

        SetDamageType(ResistanceType.Physical, 50);
        SetDamageType(ResistanceType.Fire, 50);

        SetResistance(ResistanceType.Physical, 50, 65);
        SetResistance(ResistanceType.Fire, 55, 65);
        SetResistance(ResistanceType.Cold, 45, 50);
        SetResistance(ResistanceType.Poison, 55, 65);
        SetResistance(ResistanceType.Energy, 50, 60);

        SetSkill(SkillName.MagicResist, 100);
        SetSkill(SkillName.Tactics, 100);
        SetSkill(SkillName.Wrestling, 120);
        SetSkill(SkillName.Parry, 120);

        Fame = 3500;
        Karma = -3500;

        PackItem(new FertileDirt());
        PackItem(new ExecutionersCap());
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        if (0.08 > Utility.RandomDouble() && Region.Find(c.Location, c.Map)?.IsPartOf("Shame") == true)
        {
            c.DropItem(new ShameCrystal());
        }
    }

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Rich, 2);
    }
}

[SerializationGenerator(0, false)]
public partial class MoltenEarthElemental : GreaterEarthElemental
{
    public override string CorpseName => "труп расплавленного элементаля земли";

    private static readonly MonsterAbility[] _abilities = [MonsterAbilities.FireBreath];

    public override MonsterAbility[] GetMonsterAbilities() => _abilities;

    [Constructible]
    public MoltenEarthElemental()
    {
        Hue = 442;
        Name = "расплавленный элементаль земли";

        SetStr(400, 550);
        SetHits(1200, 1400);
        SetDamage(17, 19);

        SetDamageType(ResistanceType.Physical, 50);
        SetDamageType(ResistanceType.Fire, 50);

        SetResistance(ResistanceType.Physical, 50, 70);
        SetResistance(ResistanceType.Fire, 50, 60);
        SetResistance(ResistanceType.Cold, 40, 50);
        SetResistance(ResistanceType.Poison, 55, 65);
        SetResistance(ResistanceType.Energy, 50, 60);

        SetSkill(SkillName.MagicResist, 100);
        SetSkill(SkillName.Tactics, 100);
        SetSkill(SkillName.Wrestling, 120);
        SetSkill(SkillName.Parry, 120);

        Fame = 5000;
        Karma = -5000;
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        if (0.10 > Utility.RandomDouble() && Region.Find(c.Location, c.Map)?.IsPartOf("Shame") == true)
        {
            c.DropItem(new ShameCrystal());
        }
    }

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Rich, 2);
    }
}

/// <summary>The dungeon's baseline earth elemental — the one the crystal wing was tuned around.</summary>
[SerializationGenerator(0, false)]
public partial class ShameEarthElemental : EarthElemental
{
    [Constructible]
    public ShameEarthElemental()
    {
        SetHits(300, 400);
        SetDamage(11, 13);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 30, 35);
        SetResistance(ResistanceType.Fire, 25, 30);
        SetResistance(ResistanceType.Cold, 25, 30);
        SetResistance(ResistanceType.Poison, 25, 30);
        SetResistance(ResistanceType.Energy, 20, 25);

        SetSkill(SkillName.MagicResist, 65, 85);
        SetSkill(SkillName.Tactics, 65, 90);
        SetSkill(SkillName.Wrestling, 80, 85);

        Fame = 3500;
        Karma = -3500;
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        if (0.08 > Utility.RandomDouble() && Region.Find(c.Location, c.Map)?.IsPartOf("Shame") == true)
        {
            c.DropItem(new ShameCrystal());
        }
    }

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Rich, 1);
    }
}

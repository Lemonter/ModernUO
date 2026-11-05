using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO's Shame Revamped dungeon (Mobiles/Creatures.cs) — the solo mobs that do not
///     belong to a family: the mud pie, the clay golem and the eternal gazer.
///
///     ServUO's <c>SetSpecialAbility(SpecialAbility.StickySkin)</c> has no counterpart in this codebase's
///     monster-ability framework and stays dropped, as does the unused
///     <c>static Dictionary&lt;Mobile, Timer&gt; Table</c> MudPie carried and ClayGolem's
///     <c>SpawnPackItems()</c> override — this codebase's Golem packs its ingots/gems inside its own
///     constructor rather than in an overridable method, so that suppression has nothing to override; the
///     golem's own executioner's cap is still added. ServUO's <c>BodyValue</c> is <c>Body</c> here.
/// </summary>
[SerializationGenerator(0, false)]
public partial class MudPie : BaseCreature
{
    public override string CorpseName => "труп грязевого пирога";

    [Constructible]
    public MudPie() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Name = "грязевой пирог";
        Body = 779;
        BaseSoundID = 422;

        Hue = 2012;

        SetStr(140, 210);
        SetDex(70, 100);
        SetInt(90, 110);

        SetHits(280, 340);

        SetDamage(9, 12);

        SetDamageType(ResistanceType.Physical, 80);
        SetDamageType(ResistanceType.Poison, 20);

        SetResistance(ResistanceType.Physical, 30, 45);
        SetResistance(ResistanceType.Fire, 35, 40);
        SetResistance(ResistanceType.Cold, 30, 40);
        SetResistance(ResistanceType.Poison, 35, 45);
        SetResistance(ResistanceType.Energy, 40);

        SetSkill(SkillName.MagicResist, 65, 85);
        SetSkill(SkillName.Tactics, 65, 85);
        SetSkill(SkillName.Wrestling, 65, 85);

        Fame = 500;
        Karma = -500;

        PackReg(1, 2);
        PackGem(1, 2);

        PackItem(new ExecutionersCap());

        if (0.33 > Utility.RandomDouble())
        {
            PackItem(new ExecutionersCap());
        }
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        if (0.05 > Utility.RandomDouble() && Region.Find(c.Location, c.Map)?.IsPartOf("Shame") == true)
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
public partial class ClayGolem : Golem
{
    public override string CorpseName => "труп глиняного голема";

    [Constructible]
    public ClayGolem()
    {
        Name = "глиняный голем";
        Hue = 654;

        SetStr(450, 600);
        SetDex(100, 150);
        SetInt(100, 150);

        SetHits(700, 900);

        SetDamage(13, 24);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 45, 55);
        SetResistance(ResistanceType.Fire, 50, 60);
        SetResistance(ResistanceType.Cold, 45, 55);
        SetResistance(ResistanceType.Poison, 99);
        SetResistance(ResistanceType.Energy, 35, 45);

        SetSkill(SkillName.MagicResist, 150, 200);
        SetSkill(SkillName.Tactics, 80, 120);
        SetSkill(SkillName.Wrestling, 80, 110);
        SetSkill(SkillName.Parry, 70, 80);
        SetSkill(SkillName.DetectHidden, 70.0, 80.0);

        Fame = 4500;
        Karma = -4500;

        PackItem(new ExecutionersCap());
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        if (0.2 > Utility.RandomDouble() && Region.Find(c.Location, c.Map)?.IsPartOf("Shame") == true)
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
public partial class EternalGazer : ElderGazer
{
    public override string CorpseName => "труп вечного созерцателя";

    [Constructible]
    public EternalGazer()
    {
        Name = "вечный созерцатель";
        SetStr(450, 600);
        SetDex(125, 165);
        SetInt(350, 550);

        SetHits(7250, 7600);
        SetMana(2500, 2900);
        SetDamage(18, 21);

        SetDamageType(ResistanceType.Physical, 50);
        SetDamageType(ResistanceType.Energy, 50);

        SetResistance(ResistanceType.Physical, 65, 75);
        SetResistance(ResistanceType.Fire, 60, 70);
        SetResistance(ResistanceType.Cold, 70, 75);
        SetResistance(ResistanceType.Poison, 65, 75);
        SetResistance(ResistanceType.Energy, 65, 75);

        SetSkill(SkillName.MagicResist, 125, 140);
        SetSkill(SkillName.Tactics, 115, 130);
        SetSkill(SkillName.Wrestling, 110, 130);
        SetSkill(SkillName.Anatomy, 75, 90);
        SetSkill(SkillName.Magery, 120, 130);
        SetSkill(SkillName.EvalInt, 120, 130);
    }

    public override MeatType MeatType => MeatType.Ribs;
    public override int Meat => 1;

    public override void AlterSpellDamageFrom(Mobile from, ref int damage)
    {
        if (from is BaseCreature creature && (creature.Summoned || creature.Controlled))
        {
            damage /= 2;
        }

        base.AlterSpellDamageFrom(from, ref damage);
    }

    public override void AlterMeleeDamageFrom(Mobile from, ref int damage)
    {
        if (from is BaseCreature creature && (creature.Summoned || creature.Controlled))
        {
            damage /= 2;
        }

        base.AlterMeleeDamageFrom(from, ref damage);
    }

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
        AddLoot(LootPack.Rich, 3);
    }
}

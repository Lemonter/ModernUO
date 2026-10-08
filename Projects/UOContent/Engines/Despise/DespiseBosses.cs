using System;
using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Despise;

/// <summary>Ported from ServUO's Despise Revamped dungeon (Scripts/Mobiles/Bosses/
/// DespiseBosses.cs). Only DespiseCreature can hurt a DespiseBoss directly (see Damage
/// override) — players fight through their possessed creature; the boss's own "wisp" summon
/// (Ensorcled/Corrupted) shields/empowers it while alive.</summary>
[SerializationGenerator(0, false)]
public partial class DespiseBoss : BaseCreature
{
    public const int ArtifactChance = 5;

    public virtual BaseCreature SummonWisp => null;
    public virtual double WispScalar => 0.33;

    [SerializableField(0)]
    private BaseCreature _wisp;

    private TimerExecutionToken _summonTimer;
    private bool _summonTimerActive;

    // Trailing speed args dropped — see DespiseCreature's matching constructor comment.
    public DespiseBoss(AIType ai, FightMode fightmode) : base(ai, fightmode, 10, 1)
    {
        Timer.StartTimer(TimeSpan.FromSeconds(5), SummonWispCallback, out _summonTimer);
        _summonTimerActive = true;

        FollowersMax = 100;
    }

    // Was int Damage(...) returning 0 to mean "no damage" in the original — this codebase's
    // Mobile.Damage/BaseCreature.Damage is void, so "no damage" is just not calling base at
    // all instead of returning a sentinel value.
    public override void Damage(int amount, Mobile from = null, bool informMount = true, bool ignoreEvilOmen = false)
    {
        if (from is DespiseCreature)
        {
            base.Damage(amount, from, informMount, ignoreEvilOmen);
        }
    }

    public override void OnKilledBy(Mobile mob)
    {
        if (mob is PlayerMobile pm)
        {
            var chance = ArtifactChance + Math.Min(10, pm.Luck / 180);

            if (chance >= Utility.Random(100))
            {
                var t = _artifacts[Utility.Random(_artifacts.Length)];
                var arty = Loot.Construct(t);

                if (arty != null)
                {
                    var pack = mob.Backpack;

                    if (pack == null || !pack.TryDropItem(mob, arty, false))
                    {
                        mob.BankBox.DropItem(arty);
                        mob.SendMessage("Артефакт положен в твой банковский ящик!");
                    }
                    else
                    {
                        mob.SendLocalizedMessage(1153440); // An artifact has been placed in your backpack!
                    }
                }
            }
        }

        base.OnKilledBy(mob);
    }

    public override void AlterMeleeDamageTo(Mobile to, ref int damage)
    {
        base.AlterMeleeDamageTo(to, ref damage);

        if (_wisp is { Deleted: false, Alive: true })
        {
            damage += (int)(damage * WispScalar);
        }
    }

    public override void AlterMeleeDamageFrom(Mobile from, ref int damage)
    {
        base.AlterMeleeDamageFrom(from, ref damage);

        if (_wisp is { Deleted: false, Alive: true })
        {
            damage -= (int)(damage * WispScalar);
        }
    }

    public override void AlterSpellDamageTo(Mobile to, ref int damage)
    {
        base.AlterSpellDamageTo(to, ref damage);

        if (_wisp is { Deleted: false, Alive: true })
        {
            damage += (int)(damage * WispScalar);
        }
    }

    public override void AlterSpellDamageFrom(Mobile from, ref int damage)
    {
        base.AlterSpellDamageFrom(from, ref damage);

        if (_wisp is { Deleted: false, Alive: true })
        {
            damage -= (int)(damage * WispScalar);
        }
    }

    public override void OnThink()
    {
        base.OnThink();

        if (!_summonTimerActive && (_wisp == null || !_wisp.Alive || _wisp.Deleted))
        {
            Timer.StartTimer(TimeSpan.FromSeconds(Utility.RandomMinMax(40, 60)), SummonWispCallback, out _summonTimer);
            _summonTimerActive = true;
        }
    }

    private void SummonWispCallback()
    {
        _wisp = SummonWisp;
        Summon(_wisp, true, this, Location, 0, TimeSpan.FromMinutes(90));
        _summonTimerActive = false;
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        _summonTimer.Cancel();

        if (_wisp is { Alive: true })
        {
            _wisp.Kill();
        }
    }

    public static Type[] Artifacts => _artifacts;

    private static readonly Type[] _artifacts =
    {
        typeof(CompassionsEye),
        typeof(UnicornManeWovenSandals),
        typeof(UnicornManeWovenTalons),
        typeof(DespicableQuiver),
        typeof(UnforgivenVeil),
        typeof(HailstormHuman),
        typeof(HailstormGargoyle)
    };
}

[SerializationGenerator(0, false)]
public partial class AdrianTheGloriousLord : DespiseBoss
{
    [Constructible]
    public AdrianTheGloriousLord() : base(AIType.AI_Mage, FightMode.Closest)
    {
        Title = "the Glorious Lord";

        Race = Race.Human;
        Body = 0x190;
        Female = false;

        Hue = Race.RandomSkinHue();
        HairItemID = 8252;
        HairHue = 153;

        SetStr(900, 1200);
        SetDex(500, 600);
        SetInt(500, 600);

        SetHits(60000);
        SetStam(415);
        SetMana(22000);

        SetDamage(18, 28);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 40, 60);
        SetResistance(ResistanceType.Fire, 40, 60);
        SetResistance(ResistanceType.Cold, 40, 60);
        SetResistance(ResistanceType.Poison, 40, 60);
        SetResistance(ResistanceType.Energy, 40, 60);

        SetSkill(SkillName.MagicResist, 120);
        SetSkill(SkillName.Tactics, 120);
        SetSkill(SkillName.Wrestling, 120);
        SetSkill(SkillName.Anatomy, 120);
        SetSkill(SkillName.Magery, 120);
        SetSkill(SkillName.EvalInt, 120);
        SetSkill(SkillName.Mysticism, 120);
        SetSkill(SkillName.Focus, 160);

        Fame = 22000;
        Karma = 22000;

        AddItem(new ThighBoots { Hue = 1 });
        AddItem(new Item(5046) { Hue = 1818, Layer = Layer.OneHanded });
        AddItem(new LongPants { Hue = 1818 });
        AddItem(new FancyShirt { Hue = 194 });
        AddItem(new Doublet { Hue = 1281 });
    }

    public override string DefaultName => "Адриан";

    public override bool InitialInnocent => true;
    public override BaseCreature SummonWisp => new EnsorcledWisp();

    public override void GenerateLoot()
    {
        AddLoot(LootPack.SuperBoss, 3);
    }
}

[SerializationGenerator(0, false)]
public partial class AndrosTheDreadLord : DespiseBoss
{
    [Constructible]
    public AndrosTheDreadLord() : base(AIType.AI_Mage, FightMode.Closest)
    {
        Title = "the Dread Lord";

        Race = Race.Human;
        Body = 0x190;
        Female = false;

        Hue = Race.RandomSkinHue();
        HairItemID = 0;

        SetStr(900, 1200);
        SetDex(500, 600);
        SetInt(500, 600);

        SetHits(60000);
        SetStam(415);
        SetMana(22000);

        SetDamage(18, 28);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 40, 60);
        SetResistance(ResistanceType.Fire, 40, 60);
        SetResistance(ResistanceType.Cold, 40, 60);
        SetResistance(ResistanceType.Poison, 40, 60);
        SetResistance(ResistanceType.Energy, 40, 60);

        SetSkill(SkillName.MagicResist, 120);
        SetSkill(SkillName.Tactics, 120);
        SetSkill(SkillName.Wrestling, 120);
        SetSkill(SkillName.Anatomy, 120);
        SetSkill(SkillName.Magery, 120);
        SetSkill(SkillName.EvalInt, 120);
        SetSkill(SkillName.Mysticism, 120);
        SetSkill(SkillName.Focus, 160);

        Fame = 22000;
        Karma = -22000;

        AddItem(new ThighBoots { Hue = 1 });
        AddItem(new LongPants { Hue = 1818 });
        AddItem(new FancyShirt { Hue = 2726 });
        AddItem(new Doublet { Hue = 1153 });
        AddItem(new Item(3721) { Layer = Layer.TwoHanded });
    }

    public override string DefaultName => "Андрос";

    public override bool AlwaysMurderer => true;
    public override BaseCreature SummonWisp => new CorruptedWisp();

    public override void GenerateLoot()
    {
        AddLoot(LootPack.SuperBoss, 3);
    }
}

[SerializationGenerator(0, false)]
public partial class EnsorcledWisp : BaseCreature
{
    [Constructible]
    public EnsorcledWisp() : base(AIType.AI_Melee, FightMode.None, 10, 1)
    {
        Body = 165;
        Hue = 0x901;
        BaseSoundID = 466;

        SetStr(600, 700);
        SetDex(500, 600);
        SetInt(500, 600);

        SetHits(7000, 8000);

        SetDamage(12, 19);

        SetDamageType(ResistanceType.Physical, 40);
        SetDamageType(ResistanceType.Fire, 30);
        SetDamageType(ResistanceType.Energy, 30);

        SetResistance(ResistanceType.Physical, 50);
        SetResistance(ResistanceType.Fire, 60, 70);
        SetResistance(ResistanceType.Cold, 60, 70);
        SetResistance(ResistanceType.Poison, 50, 60);
        SetResistance(ResistanceType.Energy, 60, 70);

        SetSkill(SkillName.MagicResist, 110, 125);
        SetSkill(SkillName.Tactics, 110, 125);
        SetSkill(SkillName.Wrestling, 110, 125);
        SetSkill(SkillName.Anatomy, 110, 125);

        Fame = 8000;
        Karma = 8000;
    }

    public override string DefaultName => "зачарованный огонёк";

    public override void OnThink()
    {
        base.OnThink();

        if (ControlTarget != ControlMaster || ControlOrder != OrderType.Follow)
        {
            ControlTarget = ControlMaster;
            ControlOrder = OrderType.Follow;
        }
    }

    public override void GenerateLoot()
    {
        AddLoot(LootPack.FilthyRich, 3);
    }

    public override bool OnBeforeDeath()
    {
        Summoned = false;
        return base.OnBeforeDeath();
    }

    public override bool InitialInnocent => true;
}

[SerializationGenerator(0, false)]
public partial class CorruptedWisp : BaseCreature
{
    [Constructible]
    public CorruptedWisp() : base(AIType.AI_Melee, FightMode.None, 10, 1)
    {
        Body = 165;
        Hue = 1955;
        BaseSoundID = 466;

        SetStr(600, 700);
        SetDex(500, 600);
        SetInt(500, 600);

        SetHits(7000, 8000);

        SetDamage(12, 19);

        SetDamageType(ResistanceType.Physical, 40);
        SetDamageType(ResistanceType.Fire, 30);
        SetDamageType(ResistanceType.Energy, 30);

        SetResistance(ResistanceType.Physical, 50);
        SetResistance(ResistanceType.Fire, 60, 70);
        SetResistance(ResistanceType.Cold, 60, 70);
        SetResistance(ResistanceType.Poison, 50, 60);
        SetResistance(ResistanceType.Energy, 60, 70);

        SetSkill(SkillName.MagicResist, 110, 125);
        SetSkill(SkillName.Tactics, 110, 125);
        SetSkill(SkillName.Wrestling, 110, 125);
        SetSkill(SkillName.Anatomy, 110, 125);

        Fame = 8000;
        Karma = -8000;
    }

    public override string DefaultName => "испорченный огонёк";

    public override void OnThink()
    {
        base.OnThink();

        if (ControlTarget != ControlMaster || ControlOrder != OrderType.Follow)
        {
            ControlTarget = ControlMaster;
            ControlOrder = OrderType.Follow;
        }
    }

    public override void GenerateLoot()
    {
        AddLoot(LootPack.FilthyRich, 3);
    }

    public override bool OnBeforeDeath()
    {
        Summoned = false;
        return base.OnBeforeDeath();
    }

    public override bool AlwaysMurderer => true;
}

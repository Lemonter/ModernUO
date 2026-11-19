using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
/// Хозяин логова в Сумраке. Маг-некромант: колдует магию и некромантию, а в бою каждые
/// полминуты поднимает из праха двух-трёх мертвецов. Поднятые им рассыпаются, когда он падает:
/// они держатся на его воле, а не на собственной.
/// </summary>
[SerializationGenerator(0, false)]
public partial class SumrakNecromancer : BaseCreature
{
    private static readonly Type[] Raised = [typeof(Skeleton), typeof(Zombie), typeof(Ghoul), typeof(BoneMagi), typeof(SkeletalKnight)];

    private static readonly string[] RaiseLines =
    [
        "Встаньте, мёртвые! Хозяин зовёт!",
        "Прах к праху... и снова в строй!",
        "Мои слуги не знают усталости.",
        "Ещё одна пара рук мне не помешает."
    ];

    private readonly List<BaseCreature> _raised = [];
    private long _nextRaise;
    private bool _greeted;

    [Constructible]
    public SumrakNecromancer() : base(AIType.AI_Mage)
    {
        Name = "Вельзар Безглазый";
        Title = "некромант Сумрака";
        Body = 0x190;
        Hue = 0x3E9;
        HairItemID = 0x203C;
        HairHue = 0x47E;
        FacialHairItemID = 0x204B;
        FacialHairHue = 0x47E;

        SetStr(320, 380);
        SetDex(90, 110);
        SetInt(620, 720);

        SetHits(1600, 1900);
        SetMana(1200, 1400);

        SetDamage(12, 18);

        SetDamageType(ResistanceType.Physical, 20);
        SetDamageType(ResistanceType.Cold, 40);
        SetDamageType(ResistanceType.Poison, 40);

        SetResistance(ResistanceType.Physical, 45, 55);
        SetResistance(ResistanceType.Fire, 30, 40);
        SetResistance(ResistanceType.Cold, 60, 70);
        SetResistance(ResistanceType.Poison, 60, 70);
        SetResistance(ResistanceType.Energy, 40, 50);

        SetSkill(SkillName.EvalInt, 110.0, 120.0);
        SetSkill(SkillName.Magery, 110.0, 120.0);
        SetSkill(SkillName.Necromancy, 110.0, 120.0);
        SetSkill(SkillName.SpiritSpeak, 110.0, 120.0);
        SetSkill(SkillName.Meditation, 100.0, 110.0);
        SetSkill(SkillName.MagicResist, 120.0, 130.0);
        SetSkill(SkillName.Tactics, 90.0, 100.0);
        SetSkill(SkillName.Wrestling, 90.0, 100.0);

        Fame = 20000;
        Karma = -20000;
        VirtualArmor = 50;

        EquipItem(new HoodedShroudOfShadows { Movable = false });
        EquipItem(new Sandals(0x455) { Movable = false });

        PackItem(new BatWing(Utility.RandomMinMax(10, 20)));
        PackItem(new GraveDust(Utility.RandomMinMax(10, 20)));
        PackItem(new DaemonBlood(Utility.RandomMinMax(10, 20)));
        PackItem(new NoxCrystal(Utility.RandomMinMax(10, 20)));
        PackItem(new PigIron(Utility.RandomMinMax(10, 20)));
    }

    public override string CorpseName => "труп некроманта Вельзара";
    public override bool AlwaysMurderer => true;
    public override bool BardImmune => true;
    public override bool CanBeParagon => false;
    public override Poison PoisonImmune => Poison.Lethal;
    public override int TreasureMapLevel => 5;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.FilthyRich, 2);
        AddLoot(LootPack.Gems, 5);
        AddLoot(LootPack.HighScrolls, 2);
    }

    public override void OnThink()
    {
        base.OnThink();

        if (Combatant is not Mobile foe || foe.Deleted || !foe.Alive || Map == null)
        {
            return;
        }

        if (!_greeted)
        {
            _greeted = true;
            Say("Ещё один живой в моём доме? Тем лучше — мне нужен новый слуга.");
        }

        if (Core.TickCount - _nextRaise < 0)
        {
            return;
        }

        _nextRaise = Core.TickCount + Utility.RandomMinMax(25_000, 35_000);
        _raised.RemoveAll(c => c.Deleted || !c.Alive);
        if (_raised.Count >= 6)
        {
            return;
        }

        Say(RaiseLines.RandomElement());
        Animate(17, 7, 1, true, false, 0);

        var count = Utility.RandomMinMax(2, 3);
        for (var i = 0; i < count; i++)
        {
            if (Activator.CreateInstance(Raised.RandomElement()) is not BaseCreature undead)
            {
                continue;
            }

            var p = Location;
            for (var tries = 0; tries < 10; tries++)
            {
                var q = new Point3D(X + Utility.RandomMinMax(-3, 3), Y + Utility.RandomMinMax(-3, 3), Z);
                if (Map.CanSpawnMobile(q))
                {
                    p = q;
                    break;
                }
            }

            undead.Team = Team;
            undead.MoveToWorld(p, Map);
            undead.Combatant = foe;
            Effects.SendLocationParticles(EffectItem.Create(p, Map, EffectItem.DefaultDuration), 0x3709, 10, 30, 0x455, 0, 5052, 0);
            Effects.PlaySound(p, Map, 0x1FB);
            _raised.Add(undead);
        }
    }

    public override bool OnBeforeDeath()
    {
        Say("Сумрак... не отпускает... своих...");
        return base.OnBeforeDeath();
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        // Поднятые держались на его воле: падает хозяин — рассыпаются и они.
        foreach (var undead in _raised)
        {
            if (!undead.Deleted && undead.Alive)
            {
                Effects.SendLocationParticles(
                    EffectItem.Create(undead.Location, undead.Map, EffectItem.DefaultDuration), 0x3728, 8, 20, 0x455, 0, 2023, 0
                );
                undead.Delete();
            }
        }

        _raised.Clear();
    }
}

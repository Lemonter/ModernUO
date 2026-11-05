using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Mobiles/TheMasterInstructor.cs). The lich that
///     appears in the Sorcerer's Dungeon once the five reagent plates are answered correctly; killing it
///     yields the strongbox key and re-arms the puzzle after ten minutes.
///
///     The original required a controller in its only constructor, which cannot be serialized back into
///     existence on its own; a parameterless <c>[Constructible]</c> constructor is added and the controller
///     is a serialized field.
/// </summary>
[SerializationGenerator(0, false)]
public partial class TheMasterInstructor : BaseCreature
{
    private static readonly List<TheMasterInstructor> _instances = new();

    [SerializableField(0, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private SorcerersPlateController _controller;

    private TimerExecutionToken _selfDelete;

    [Constructible]
    public TheMasterInstructor() : this(null)
    {
    }

    [Constructible]
    public TheMasterInstructor(SorcerersPlateController controller)
        : base(AIType.AI_NecroMage, FightMode.Closest, 10, 1)
    {
        _instances.Add(this);
        _controller = controller;

        Name = "Аншу";
        Title = "мастер-наставник";
        Body = 0x4E;
        BaseSoundID = 412;
        Hue = 1284;

        SetStr(216, 305);
        SetDex(96, 115);
        SetInt(966, 1045);

        SetHits(700, 800);

        SetDamage(15, 27);

        SetDamageType(ResistanceType.Physical, 20);
        SetDamageType(ResistanceType.Cold, 40);
        SetDamageType(ResistanceType.Energy, 40);

        SetResistance(ResistanceType.Physical, 55, 65);
        SetResistance(ResistanceType.Fire, 30, 40);
        SetResistance(ResistanceType.Cold, 50, 60);
        SetResistance(ResistanceType.Poison, 50, 60);
        SetResistance(ResistanceType.Energy, 50, 60);

        SetSkill(SkillName.EvalInt, 120.1, 130.0);
        SetSkill(SkillName.Magery, 120.1, 130.0);
        SetSkill(SkillName.Meditation, 100.1, 101.0);
        SetSkill(SkillName.Poisoning, 100.1, 101.0);
        SetSkill(SkillName.MagicResist, 175.2, 200.0);
        SetSkill(SkillName.Tactics, 90.1, 100.0);
        SetSkill(SkillName.Wrestling, 75.1, 100.0);
        SetSkill(SkillName.Necromancy, 120.0);
        SetSkill(SkillName.SpiritSpeak, 120.0);
        SetSkill(SkillName.DetectHidden, 100.0);

        Fame = 23000;
        Karma = -23000;

        VirtualArmor = 60;
        PackNecroReg(30, 275);

        Timer.StartTimer(TimeSpan.FromMinutes(60.0), Delete, out _selfDelete);
    }

    public static IReadOnlyList<TheMasterInstructor> Instances => _instances;

    public override string CorpseName => "труп древнего лича";

    public override bool AlwaysMurderer => true;
    public override OppositionGroup OppositionGroup => OppositionGroup.FeyAndUndead;
    public override bool Unprovokable => true;
    public override bool BleedImmune => true;
    public override Poison PoisonImmune => Poison.Lethal;
    public override int TreasureMapLevel => 5;
    public override bool AutoDispel => true;

    public override int GetIdleSound() => 0x19D;
    public override int GetAngerSound() => 0x175;
    public override int GetDeathSound() => 0x108;
    public override int GetAttackSound() => 0xE2;
    public override int GetHurtSound() => 0x28B;

    public static TheMasterInstructor Spawn(Point3D platLoc, Map platMap, SorcerersPlateController controller)
    {
        if (_instances.Count > 0)
        {
            return null;
        }

        var creature = new TheMasterInstructor(controller)
        {
            Home = platLoc,
            RangeHome = 4
        };

        creature.MoveToWorld(platLoc, platMap);

        return creature;
    }

    public override bool OnBeforeDeath()
    {
        var killer = DemonKnight.FindRandomPlayer(this);

        if (killer != null)
        {
            Item item = new StrongboxKey();

            if (killer.Backpack == null || !killer.Backpack.TryDropItem(killer, item, false))
            {
                killer.BankBox?.DropItem(item);
            }

            killer.SendLocalizedMessage(1154489); // You received a Quest Item!
        }

        if (_controller is { Deleted: false })
        {
            Timer.StartTimer(TimeSpan.FromMinutes(10.0), _controller.CreateSorcerersPlates);
        }

        return base.OnBeforeDeath();
    }

    public override void GenerateLoot() => AddLoot(LootPack.FilthyRich, 3);

    public override void OnAfterDelete()
    {
        _selfDelete.Cancel();
        _instances.Remove(this);

        base.OnAfterDelete();
    }
}

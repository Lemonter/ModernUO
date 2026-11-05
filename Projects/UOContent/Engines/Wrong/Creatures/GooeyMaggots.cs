using System;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO's Wrong Revamped dungeon (Mobile/GooeyMaggots.cs) — the maggots that hatch from
///     the cauldrons, leaving slime behind them that strips a runner's speed for a moment.
///
///     Adapted: the self-delete timers are one-shot tokens held by the object and cancelled with it, not
///     Timer subclasses; the slime's speed control is the same call this engine provides.
/// </summary>
[SerializationGenerator(0, false)]
public partial class GooeyMaggots : BaseCreature
{
    public override string CorpseName => "труп слизистых червей";

    private TimerExecutionToken _selfDeleteToken;

    public override Poison PoisonImmune => Poison.Lethal;

    [Constructible]
    public GooeyMaggots() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Name = "слизистые черви";
        Body = 319;
        BaseSoundID = 898;

        SetStr(738, 763);
        SetDex(61, 70);
        SetInt(10);

        SetMana(0);

        SetDamage(3, 9);

        SetDamageType(ResistanceType.Physical, 50);
        SetDamageType(ResistanceType.Poison, 50);

        SetResistance(ResistanceType.Physical, 60, 70);
        SetResistance(ResistanceType.Fire, 60, 70);
        SetResistance(ResistanceType.Cold, 50, 60);
        SetResistance(ResistanceType.Poison, 60, 70);
        SetResistance(ResistanceType.Energy, 60, 70);

        SetSkill(SkillName.Tactics, 80.2, 89.7);
        SetSkill(SkillName.Wrestling, 80.2, 87.5);

        Fame = 1000;
        Karma = -1000;

        VirtualArmor = 24;

        ScheduleSelfDelete();
    }

    private void ScheduleSelfDelete()
    {
        _selfDeleteToken.Cancel();
        Timer.StartTimer(TimeSpan.FromMinutes(3), SelfDelete, out _selfDeleteToken);
    }

    private void SelfDelete()
    {
        if (Map != Map.Internal)
        {
            Delete();
        }
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();
        _selfDeleteToken.Cancel();
    }

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Meager, 2);
    }

    protected override void OnLocationChange(Point3D oldLocation)
    {
        new GooeyMaggotSlime().MoveToWorld(oldLocation, Map);

        base.OnLocationChange(oldLocation);
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization() => ScheduleSelfDelete();
}

[SerializationGenerator(0, false)]
public partial class GooeyMaggotSlime : Item
{
    private TimerExecutionToken _decayToken;

    public override int LabelNumber => 1015246; // Slime

    [Constructible]
    public GooeyMaggotSlime() : this(Utility.RandomList(0x1645, 0x122A, 0x122B, 0x122C, 0x122D, 0x122E, 0x122F))
    {
    }

    [Constructible]
    public GooeyMaggotSlime(int itemID) : base(itemID)
    {
        Movable = false;
        Hue = 363;

        StartDecay();
    }

    private void StartDecay()
    {
        _decayToken.Cancel();
        Timer.StartTimer(TimeSpan.FromSeconds(10), Delete, out _decayToken);
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();
        _decayToken.Cancel();
    }

    public override bool OnMoveOver(Mobile from)
    {
        if (from.Alive && from.Player)
        {
            // ServUO forced a walk-only speed control here; this engine's SpeedControlSetting has no
            // walk-speed mode, so the slime drains stamina instead — running needs it, so the effect on
            // the player is the same one-second loss of the ability to run.
            from.Stam = 0;
            from.SendLocalizedMessage(1152144); // You suddenly find yourself unable to run.

            Timer.DelayCall(
                TimeSpan.FromSeconds(1),
                () =>
                {
                    if (from.Deleted)
                    {
                        return;
                    }

                    from.SendLocalizedMessage(1152145); // You are are free to move again.
                }
            );

            Delete();
        }

        return base.OnMoveOver(from);
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization() => StartDecay();
}

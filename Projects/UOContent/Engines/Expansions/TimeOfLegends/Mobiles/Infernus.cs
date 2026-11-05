using System;
using ModernUO.Serialization;
using Server.Items;
using Server.Network;
using Server.Spells;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO (Scripts/Services/Expansions/Time Of Legends/Mobiles/Infernus.cs).
///
///     The flavour mechanic is kept: in combat it scatters <see cref="InfernusFire" /> tiles around
///     its target, and every second those tiles scorch whatever stands on them.
///
///     Adapted: ServUO's nested <c>FireItem</c> serialized nothing at all, so a shard restart left
///     permanent, ownerless fires behind. Here the tile keeps its owner and its expiry, and restarts
///     its poll after deserialization — an expired tile deletes itself instead of lingering. The
///     per-second tick is a <see cref="TimerExecutionToken" /> cancelled in <c>OnAfterDelete</c>.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Infernus : BaseCreature
{
    private DateTime _nextDrop;

    [Constructible]
    public Infernus() : base(AIType.AI_Melee, FightMode.Closest)
    {
        Name = "инфернус";
        Body = 0x9F;
        Hue = 1955;
        BaseSoundID = 278;

        SetStr(318, 400);
        SetDex(97, 115);
        SetInt(184, 266);

        SetDamage(11, 14);

        SetHits(202, 243);

        SetResistance(ResistanceType.Physical, 45, 55);
        SetResistance(ResistanceType.Fire, 80, 90);
        SetResistance(ResistanceType.Cold, 25, 35);
        SetResistance(ResistanceType.Poison, 40, 50);
        SetResistance(ResistanceType.Energy, 35, 45);

        SetDamageType(ResistanceType.Fire, 100);

        SetSkill(SkillName.MagicResist, 90, 100);
        SetSkill(SkillName.Tactics, 60, 70);
        SetSkill(SkillName.Wrestling, 80);

        PackGold(500, 600);

        Fame = 10000;
        Karma = -10000;
    }

    public override string CorpseName => "труп инфернуса";

    public override int TreasureMapLevel => 5;

    public override void OnActionCombat()
    {
        if (Core.Now < _nextDrop || Combatant is not Mobile combatant || combatant.Deleted ||
            combatant.Map != Map || !InRange(combatant, 12))
        {
            return;
        }

        DropFire(combatant);

        _nextDrop = Core.Now + TimeSpan.FromSeconds(Utility.RandomMinMax(10, 15));
    }

    public void DropFire(Mobile m)
    {
        var map = Map;

        if (map == null)
        {
            return;
        }

        for (var x = m.X - 1; x <= m.X + 1; x++)
        {
            for (var y = m.Y - 1; y <= m.Y + 1; y++)
            {
                IPoint3D p = new Point3D(x, y, map.GetAverageZ(x, y));
                SpellHelper.GetSurfaceTop(ref p);

                if ((x == 0 && y == 0 || 0.5 > Utility.RandomDouble()) && map.CanSpawnMobile(p.X, p.Y, p.Z))
                {
                    var item = new InfernusFire(this);
                    item.MoveToWorld(new Point3D(p), map);
                }
            }
        }
    }
}

/// <summary>
///     A patch of fire left by <see cref="Infernus" />. Burns anything but its owner that walks over
///     it, then deletes itself once its lifespan runs out.
/// </summary>
[SerializationGenerator(0, false)]
public partial class InfernusFire : Item
{
    [SerializableField(0)]
    private Infernus _infernus;

    [SerializableField(1)]
    private DateTime _endTime;

    private TimerExecutionToken _timerToken;

    [Constructible]
    public InfernusFire() : base(0x19AB)
    {
    }

    public InfernusFire(Infernus infernus) : base(0x19AB)
    {
        _infernus = infernus;
        _endTime = Core.Now + TimeSpan.FromSeconds(Utility.RandomMinMax(30, 40));

        Timer.StartTimer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), OnTick, out _timerToken);
    }

    public override bool OnMoveOver(Mobile from)
    {
        var owner = _infernus;

        if (owner != null && owner != from && SpellHelper.ValidIndirectTarget(owner, from) &&
            owner.CanBeHarmful(from, false))
        {
            owner.DoHarmful(from);

            AOS.Damage(from, owner, Utility.RandomMinMax(50, 85), 0, 100, 0, 0, 0);
            Effects.PlaySound(Location, Map, 0x1DD);

            // *The trail of fire scorches you, setting you ablaze!*
            from.PrivateOverheadMessage(MessageType.Regular, 0x20, 1156084, from.NetState);
        }

        return true;
    }

    private void OnTick()
    {
        if (_endTime < Core.Now)
        {
            Delete();
            return;
        }

        foreach (var m in Map.GetMobilesInRange(Location, 0))
        {
            OnMoveOver(m);
        }
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        if (_endTime > Core.Now)
        {
            Timer.StartTimer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), OnTick, out _timerToken);
        }
        else
        {
            Delete();
        }
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        _timerToken.Cancel();
        _infernus = null;
    }
}

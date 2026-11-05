using System;
using ModernUO.Serialization;
using Server.Engines.ExploringTheDeep;
using Server.Mobiles;
using Server.Spells;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Items/Teleporter/Whirlpool.cs). The way into
///     Gravewater Lake: anyone who dives in without the full Salvager's Suit drowns.
///
///     Substitutions:
///     <list type="bullet">
///         <item>
///             The equipment check was a LINQ query repeated inline; it is
///             <see cref="ExploringDeepChain.WearingSuit" /> here, which the underwater region also uses.
///         </item>
///         <item>
///             The original completed the old-style <c>ExploringTheDeepQuest</c> on arrival and granted a
///             reward title. This codebase's quest system completes quests at turn-in, so that block is
///             dropped; the title it granted lived on the same old quest system and has no counterpart.
///         </item>
///         <item>
///             The eight <c>[CommandProperty]</c> accessors all called <c>InvalidateProperties</c>; the
///             generated setters do that instead.
///         </item>
///     </list>
/// </summary>
[SerializationGenerator(0, false)]
public partial class Whirlpool : Item
{
    [SerializableField(0, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    [InvalidateProperties]
    private bool _sourceEffect;

    [SerializableField(1, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    [InvalidateProperties]
    private bool _destEffect;

    [SerializableField(2, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    [InvalidateProperties]
    private int _soundId;

    [SerializableField(3, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    [InvalidateProperties]
    private TimeSpan _delay;

    [SerializableField(4, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    [InvalidateProperties]
    private bool _active;

    [SerializableField(5, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    [InvalidateProperties]
    private Point3D _pointDest;

    [SerializableField(6, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    [InvalidateProperties]
    private Map _mapDest;

    [SerializableField(7, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    [InvalidateProperties]
    private bool _creatures;

    [SerializableField(8, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    [InvalidateProperties]
    private bool _combatCheck;

    [SerializableField(9, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    [InvalidateProperties]
    private bool _criminalCheck;

    [Constructible]
    public Whirlpool() : base(0x3789)
    {
        Movable = false;
        Hue = 2592;
        _active = true;
    }

    [Constructible]
    public Whirlpool(Point3D pointDest, Map mapDest) : this(pointDest, mapDest, false)
    {
    }

    [Constructible]
    public Whirlpool(Point3D pointDest, Map mapDest, bool creatures) : base(0x3789)
    {
        Movable = false;
        Hue = 2592;

        _active = true;
        _pointDest = pointDest;
        _mapDest = mapDest;
        _creatures = creatures;
    }

    public override int LabelNumber => 1033459; // whirlpool

    public override void OnDoubleClick(Mobile from)
    {
        if (from.Holding != null)
        {
            from.SendLocalizedMessage(1071955); // You cannot teleport while dragging an object.
            return;
        }

        if (from is not PlayerMobile mobile)
        {
            return;
        }

        if (mobile.AccessLevel > AccessLevel.Player)
        {
            StartTeleport(mobile);
            return;
        }

        if (!mobile.Alive || !mobile.Player)
        {
            return;
        }

        if (!_active || !CanTeleport(from))
        {
            return;
        }

        if (!ExploringDeepChain.WearingSuit(mobile))
        {
            mobile.Kill();
            return;
        }

        StartTeleport(from);
    }

    public virtual bool CanTeleport(Mobile m)
    {
        if (!_creatures && !m.Player)
        {
            return false;
        }

        if (_criminalCheck && m.Criminal)
        {
            m.SendLocalizedMessage(1005561, "", 0x22); // Thou'rt a criminal and cannot escape so easily.
            return false;
        }

        if (_combatCheck && SpellHelper.CheckCombat(m))
        {
            m.SendLocalizedMessage(1005564, "", 0x22); // Wouldst thou flee during the heat of battle??
            return false;
        }

        return true;
    }

    public virtual void StartTeleport(Mobile m)
    {
        if (m.Holding != null)
        {
            m.SendLocalizedMessage(1071955); // You cannot teleport while dragging an object.
            return;
        }

        if (_delay == TimeSpan.Zero)
        {
            DoTeleport(m);
        }
        else
        {
            Timer.StartTimer(_delay, () => DoTeleport(m));
        }
    }

    public virtual void DoTeleport(Mobile m)
    {
        var map = _mapDest;

        if (map == null || map == Map.Internal)
        {
            map = m.Map;
        }

        var p = _pointDest;

        if (p == Point3D.Zero)
        {
            p = m.Location;
        }

        BaseCreature.TeleportPets(m, p, map);

        var sendEffect = !m.Hidden || m.Player;

        if (_sourceEffect && sendEffect)
        {
            Effects.SendLocationEffect(m.Location, m.Map, 0x3728, 10, 10);
        }

        m.MoveToWorld(p, map);

        if (_destEffect && sendEffect)
        {
            Effects.SendLocationEffect(m.Location, m.Map, 0x3728, 10, 10);
        }

        if (_soundId > 0 && sendEffect)
        {
            Effects.PlaySound(m.Location, m.Map, _soundId);
        }
    }
}

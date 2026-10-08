using System.Collections.Generic;
using Server.Collections;
using Server.ContextMenus;
using Server.Mobiles;

namespace Server.Engines.Despise;

/// <summary>
///     Ported from ServUO's Despise Revamped dungeon (Scripts/Services/Dungeons/
///     DespiseRevamped/AI.cs). Overrides the normal melee/mage AI loop so a possessed
///     DespiseCreature obeys its WispOrb's Aggression setting (follow/defend/attack) instead
///     of the vanilla pet-command AI. IPooledEnumerable-based enumeration (Map.
///     GetMobilesInRange returning an eable you .Free() afterward) is gone in this engine —
///     converted to plain foreach, same as everywhere else this session's ports have done.
/// </summary>
public class DespiseMeleeAI : MeleeAI
{
    private long _nextAggressorCheck;

    private readonly DespiseCreature _creature;

    public DespiseMeleeAI(DespiseCreature m) : base(m)
    {
        _creature = m;
    }

    public override bool Obey()
    {
        if (_creature.Orb == null || !_creature.Controlled)
        {
            return base.Obey();
        }

        switch (_creature.Orb.Aggression)
        {
            default:
                if (_creature.ControlOrder != OrderType.Follow)
                {
                    _creature.ControlOrder = OrderType.Follow;
                }

                DoOrderFollow();
                break;

            case Aggression.Defensive:
                if (_creature.Combatant != null)
                {
                    if (_creature.ControlOrder == OrderType.Follow)
                    {
                        _creature.ControlOrder = OrderType.Attack;
                        Action = ActionType.Combat;
                    }

                    break;
                }

                if (_nextAggressorCheck <= Core.TickCount)
                {
                    var p = _creature.Orb.GetAnchorActual();
                    double range = _creature.RangePerception;

                    Mobile closest = null;

                    foreach (var m in _creature.Map.GetMobilesInRange(new Point3D(p), (int)range))
                    {
                        if (m.Combatant == _creature || m.Combatant == _creature.ControlMaster)
                        {
                            var dist = closest == null ? range : closest.GetDistanceToSqrt(_creature);

                            if (closest == null || dist < range)
                            {
                                range = dist;
                                closest = m;
                            }
                        }
                    }

                    if (closest != null)
                    {
                        _creature.ControlTarget = closest;
                        _creature.ControlOrder = OrderType.Attack;
                        _creature.Combatant = closest;
                        DebugSay("But -that- is not dead. Here we go again...");

                        Action = ActionType.Combat;
                    }

                    _nextAggressorCheck = Core.TickCount + 1000;
                }

                break;

            case Aggression.Aggressive:
                if (_creature.Combatant != null)
                {
                    if (_creature.ControlOrder == OrderType.Follow)
                    {
                        _creature.ControlOrder = OrderType.Attack;
                        Action = ActionType.Combat;
                    }

                    break;
                }

                if (AcquireFocusMob(_creature.RangePerception, _creature.FightMode, false, false, true))
                {
                    if (_creature.FocusMob == _creature.ControlMaster)
                    {
                        break;
                    }

                    if (_creature.Debug)
                    {
                        DebugSay($"I have detected {_creature.FocusMob.Name}, attacking");
                    }

                    _creature.ControlOrder = OrderType.Attack;
                    _creature.Combatant = _creature.FocusMob;

                    Action = ActionType.Combat;
                }

                break;
        }

        if (_creature.Combatant == null)
        {
            if (_creature.ControlOrder != OrderType.Follow)
            {
                _creature.ControlOrder = OrderType.Follow;
            }

            _creature.ControlTarget = _creature.ControlMaster;
            Action = ActionType.Guard;
            DoOrderFollow();
        }

        Think();
        return true;
    }

    public override bool DoOrderFollow()
    {
        _creature.Orb?.InvalidateHue();
        return base.DoOrderFollow();
    }

    public override bool AcquireFocusMob(int iRange, FightMode acqType, bool bPlayerOnly, bool bFacFriend, bool bFacFoe)
    {
        if (_creature.Orb == null || _creature.ControlMaster == null)
        {
            return base.AcquireFocusMob(iRange, acqType, bPlayerOnly, bFacFriend, bFacFoe);
        }

        if (_creature.Orb.Aggression != Aggression.Aggressive)
        {
            return false;
        }

        if (Core.TickCount - _creature.NextReacquireTime < 0)
        {
            _creature.FocusMob = null;
            return false;
        }

        _creature.NextReacquireTime = Core.TickCount + (int)_creature.ReacquireDelay.TotalMilliseconds;

        var range = _creature.RangePerception;
        var p = _creature.Orb.Anchor as IPoint3D ?? _creature;

        var focus = GetFocus(p, range);

        if (focus != null)
        {
            _creature.FocusMob = focus;
            return true;
        }

        return false;
    }

    private Mobile GetFocus(IPoint3D p, int range)
    {
        Mobile focus = null;
        var dist = range;

        foreach (var m in _creature.Map.GetMobilesInRange(new Point3D(p), range))
        {
            if (_creature.CanSee(m) && _creature.InLOS(m) && m is DespiseCreature or DespiseBoss)
            {
                var dc = m as DespiseCreature;

                if (m is DespiseBoss || dc != null && (dc.Orb == null && !dc.Controlled || dc.Alignment != _creature.Alignment))
                {
                    var distance = (int)_creature.GetDistanceToSqrt(m);

                    if (focus == null || distance < dist)
                    {
                        focus = m;
                        dist = distance;
                    }
                }
            }
        }

        return focus;
    }

    // Was IPoint3D p in the original (ServUO's own WalkMobileRange takes any IPoint3D) —
    // this codebase's BaseAI.WalkMobileRange only accepts a Mobile, narrower than ServUO's.
    // The anchor is usually the ControlMaster (a Mobile) anyway; the rare case where a
    // player anchored their possessed creature to an Item/location instead just falls back
    // to walking toward whatever Mobile was originally requested.
    public override bool WalkMobileRange(Mobile p, int iSteps, int iWantDistMin, int iWantDistMax)
    {
        if (_creature.Orb == null || _creature.ControlMaster == null)
        {
            return base.WalkMobileRange(p, iSteps, iWantDistMin, iWantDistMax);
        }

        var range = _creature.GetLeashLength();

        if (p == _creature.ControlMaster)
        {
            var anchor = _creature.Orb.GetAnchorActual();

            if (anchor is Mobile anchorMobile)
            {
                if (_creature.InRange(anchorMobile, range + 1))
                {
                    return false;
                }

                return base.WalkMobileRange(anchorMobile, iSteps, range, range);
            }
        }

        return base.WalkMobileRange(p, iSteps, iWantDistMin, iWantDistMax);
    }

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
    }

    public override void OnSpeech(SpeechEventArgs e)
    {
    }
}

public class DespiseMageAI : MageAI
{
    private long _nextAggressorCheck;

    private readonly DespiseCreature _creature;

    public DespiseMageAI(DespiseCreature m) : base(m)
    {
        _creature = m;
    }

    public override bool Obey()
    {
        if (_creature.Orb == null || !_creature.Controlled)
        {
            return base.Obey();
        }

        switch (_creature.Orb.Aggression)
        {
            default:
                if (_creature.ControlOrder != OrderType.Follow)
                {
                    _creature.ControlOrder = OrderType.Follow;
                }

                DoOrderFollow();
                break;

            case Aggression.Defensive:
                if (_creature.Combatant != null)
                {
                    if (_creature.ControlOrder == OrderType.Follow)
                    {
                        _creature.ControlOrder = OrderType.Attack;
                        Action = ActionType.Combat;
                    }

                    break;
                }

                if (_nextAggressorCheck <= Core.TickCount)
                {
                    var p = _creature.Orb.GetAnchorActual();
                    double range = _creature.RangePerception;

                    Mobile closest = null;

                    foreach (var m in _creature.Map.GetMobilesInRange(new Point3D(p), (int)range))
                    {
                        if (m.Combatant == _creature || m.Combatant == _creature.ControlMaster)
                        {
                            var dist = closest == null ? range : closest.GetDistanceToSqrt(_creature);

                            if (closest == null || dist < range)
                            {
                                range = dist;
                                closest = m;
                            }
                        }
                    }

                    if (closest != null)
                    {
                        _creature.ControlTarget = closest;
                        _creature.ControlOrder = OrderType.Attack;
                        _creature.Combatant = closest;
                        DebugSay("But -that- is not dead. Here we go again...");

                        Action = ActionType.Combat;
                    }

                    _nextAggressorCheck = Core.TickCount + 1000;
                }

                break;

            case Aggression.Aggressive:
                if (_creature.Combatant != null)
                {
                    if (_creature.ControlOrder == OrderType.Follow)
                    {
                        _creature.ControlOrder = OrderType.Attack;
                        Action = ActionType.Combat;
                    }

                    break;
                }

                if (AcquireFocusMob(_creature.RangePerception, _creature.FightMode, false, false, true))
                {
                    if (_creature.FocusMob == _creature.ControlMaster)
                    {
                        break;
                    }

                    if (_creature.Debug)
                    {
                        DebugSay($"I have detected {_creature.FocusMob.Name}, attacking");
                    }

                    _creature.ControlOrder = OrderType.Attack;
                    _creature.Combatant = _creature.FocusMob;

                    Action = ActionType.Combat;
                }

                break;
        }

        if (_creature.Combatant == null)
        {
            if (_creature.ControlOrder != OrderType.Follow)
            {
                _creature.ControlOrder = OrderType.Follow;
            }

            _creature.ControlTarget = _creature.ControlMaster;
            Action = ActionType.Guard;
            DoOrderFollow();
        }

        Think();
        return true;
    }

    public override bool DoOrderFollow()
    {
        _creature.Orb?.InvalidateHue();
        return base.DoOrderFollow();
    }

    // Was a 3-arg override in the original — MageAI in this codebase never declares its own
    // AcquireFocusMob (every call site just uses BaseAI's 5-arg version), so matching that
    // signature here instead of ServUO's 3-arg one.
    public override bool AcquireFocusMob(int iRange, FightMode acqType, bool bPlayerOnly, bool bFacFriend, bool bFacFoe)
    {
        if (_creature.Orb == null || _creature.ControlMaster == null)
        {
            return base.AcquireFocusMob(iRange, acqType, bPlayerOnly, bFacFriend, bFacFoe);
        }

        if (_creature.Orb.Aggression != Aggression.Aggressive)
        {
            return false;
        }

        if (Core.TickCount - _creature.NextReacquireTime < 0)
        {
            _creature.FocusMob = null;
            return false;
        }

        _creature.NextReacquireTime = Core.TickCount + (int)_creature.ReacquireDelay.TotalMilliseconds;

        var range = _creature.RangePerception;
        var p = _creature.Orb.Anchor as IPoint3D ?? _creature;

        var focus = GetFocus(p, range);

        if (focus != null)
        {
            _creature.FocusMob = focus;
            return true;
        }

        return false;
    }

    private Mobile GetFocus(IPoint3D p, int range)
    {
        Mobile focus = null;
        var dist = range;

        foreach (var m in _creature.Map.GetMobilesInRange(new Point3D(p), range))
        {
            if (_creature.CanSee(m) && _creature.InLOS(m) && m is DespiseCreature or DespiseBoss)
            {
                var dc = m as DespiseCreature;

                if (m is DespiseBoss || dc != null && (dc.Orb == null && !dc.Controlled || dc.Alignment != _creature.Alignment))
                {
                    var distance = (int)_creature.GetDistanceToSqrt(m);

                    if (focus == null || distance < dist)
                    {
                        focus = m;
                        dist = distance;
                    }
                }
            }
        }

        return focus;
    }

    // Was IPoint3D p in the original (ServUO's own WalkMobileRange takes any IPoint3D) —
    // this codebase's BaseAI.WalkMobileRange only accepts a Mobile, narrower than ServUO's.
    // The anchor is usually the ControlMaster (a Mobile) anyway; the rare case where a
    // player anchored their possessed creature to an Item/location instead just falls back
    // to walking toward whatever Mobile was originally requested.
    public override bool WalkMobileRange(Mobile p, int iSteps, int iWantDistMin, int iWantDistMax)
    {
        if (_creature.Orb == null || _creature.ControlMaster == null)
        {
            return base.WalkMobileRange(p, iSteps, iWantDistMin, iWantDistMax);
        }

        var range = _creature.GetLeashLength();

        if (p == _creature.ControlMaster)
        {
            var anchor = _creature.Orb.GetAnchorActual();

            if (anchor is Mobile anchorMobile)
            {
                if (_creature.InRange(anchorMobile, range + 1))
                {
                    return false;
                }

                return base.WalkMobileRange(anchorMobile, iSteps, range, range);
            }
        }

        return base.WalkMobileRange(p, iSteps, iWantDistMin, iWantDistMax);
    }

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
    }

    public override void OnSpeech(SpeechEventArgs e)
    {
    }
}

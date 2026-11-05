using System;
using ModernUO.Serialization;
using Server.Engines.ExploringTheDeep;
using Server.Mobiles;
using Server.Spells;
using Server.Targeting;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Items/SpecialSalvageHook.cs). Sold by the
///     Salvage Master: cast into deep water, it either snags the broken shipwreck remains or drags up
///     something hostile.
///
///     Substitutions:
///     <list type="bullet">
///         <item>
///             <c>SpecialFishingNet.ValidateDeepWater</c> / <c>ValidateUndeepWater</c> are private in this
///             codebase, so only the public <c>FullValidation</c> is used. ServUO's <c>RequireDeepWater</c>
///             is a virtual property that is never overridden and always true, so the shallow-water branch
///             could never run anyway; the property and that branch are dropped.
///         </item>
///         <item>
///             The effect timer used a <c>TimerStateCallback</c> carrying an <c>object[]</c>; it is a
///             <see cref="TimerExecutionToken" /> plus a small closure over the same two values.
///         </item>
///         <item>
///             ServUO gated use on "any quest progress at all" via the quest enum; that is
///             <see cref="ExploringDeepChain.SalvageMasterDone" /> here, the same state.
///         </item>
///     </list>
/// </summary>
[SerializationGenerator(0, false)]
public partial class SpecialSalvageHook : Item
{
    [SerializableField(0, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _inUse;

    private int _ticks;
    private TimerExecutionToken _effectTimer;

    [Constructible]
    public SpecialSalvageHook() : base(0x14F7)
    {
        Weight = 25.0;
        Hue = 2654;
    }

    public override int LabelNumber => 1154215; // A Special Salvage Hook

    public override void OnDoubleClick(Mobile from)
    {
        if (from is not PlayerMobile pm)
        {
            return;
        }

        if (!ExploringDeepChain.SalvageMasterDone(pm) && !ExploringDeepChain.ComponentStage(pm) &&
            !ExploringDeepChain.IsDoing(pm, typeof(Server.Engines.MLQuests.Definitions.SalvageMaster)))
        {
            // *You aren't quite sure what to do with this. If you spoke to the Salvage Master at the Sons
            // of the Sea in Trinsic you might have a better understanding of its use...*
            from.PublicOverheadMessage(MessageType.Regular, 0x3B2, 1154274);
            return;
        }

        if (_inUse)
        {
            return;
        }

        from.SendLocalizedMessage(1154219); // Where do you wish to use this?
        from.BeginTarget(-1, true, TargetFlags.None, OnTarget);
    }

    private void OnTarget(Mobile from, object obj)
    {
        if (Deleted || _inUse || obj is not IPoint3D p3D)
        {
            return;
        }

        var map = from.Map;

        if (map == null || map == Map.Internal)
        {
            return;
        }

        int x = p3D.X, y = p3D.Y;
        var z = map.GetAverageZ(x, y);

        if (!from.InLOS(obj))
        {
            from.SendLocalizedMessage(500979); // You cannot see that location.
            return;
        }

        if (!SpecialFishingNet.FullValidation(map, x, y))
        {
            from.SendLocalizedMessage(1010485); // You can only use this in deep water!
            return;
        }

        var p = new Point3D(x, y, z);

        for (var i = 1; i < Amount; ++i) // these were stackable before
        {
            from.AddToBackpack(new SpecialSalvageHook());
        }

        _ticks = 0;
        _inUse = true;
        Movable = false;
        MoveToWorld(p, map);

        SpellHelper.Turn(from, p);
        from.Animate(12, 5, 1, true, false, 0);

        Effects.SendLocationEffect(p, map, 0x352D, 16, 4);
        Effects.PlaySound(p, map, 0x364);

        Timer.StartTimer(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.25), () => DoEffect(p, from), out _effectTimer);

        // *You cast the mighty hook into the sea!*
        from.PublicOverheadMessage(MessageType.Regular, 0x3B2, 1154220);
    }

    private int GetSpawnCount()
    {
        var count = Utility.RandomMinMax(1, 3);

        if (Hue != 0x8A0)
        {
            count += Utility.RandomMinMax(1, 2);
        }

        return count;
    }

    private void Spawn(Point3D p, Map map, BaseCreature spawn)
    {
        if (map == null)
        {
            spawn.Delete();
            return;
        }

        int x = p.X, y = p.Y;

        for (var j = 0; j < 20; ++j)
        {
            var tx = p.X - 2 + Utility.Random(5);
            var ty = p.Y - 2 + Utility.Random(5);

            var t = map.Tiles.GetLandTile(tx, ty);

            if (t.Z == p.Z && (t.ID >= 0xA8 && t.ID <= 0xAB || t.ID >= 0x136 && t.ID <= 0x137) &&
                !SpellHelper.CheckMulti(new Point3D(tx, ty, p.Z), map))
            {
                x = tx;
                y = ty;
                break;
            }
        }

        spawn.MoveToWorld(new Point3D(x, y, p.Z), map);
    }

    private void SpawnBaddies(Point3D p, Map map, Mobile from)
    {
        if (from == null || map == null)
        {
            return;
        }

        from.RevealingAction();

        var count = GetSpawnCount();

        for (var i = 0; i < count; ++i)
        {
            BaseCreature spawn = Utility.Random(4) switch
            {
                0 => new SeaSerpent(),
                1 => new DeepSeaSerpent(),
                2 => new WaterElemental(),
                _ => new Kraken()
            };

            Spawn(p, map, spawn);
            spawn.Combatant = from;
        }
    }

    private void DoEffect(Point3D p, Mobile from)
    {
        if (Deleted || Map == null)
        {
            return;
        }

        if (_ticks == 1)
        {
            Effects.SendLocationEffect(p, Map, 0x352D, 16, 4);
            Effects.PlaySound(p, Map, 0x364);
        }
        else if (_ticks <= 7 || _ticks == 14)
        {
            for (var i = 0; i < 3; ++i)
            {
                int x, y;

                do
                {
                    x = Utility.RandomMinMax(-1, 1);
                    y = Utility.RandomMinMax(-1, 1);
                }
                while (x == 0 && y == 0);

                Effects.SendLocationEffect(new Point3D(p.X + x, p.Y + y, p.Z), Map, 0x352D, 16, 4);
            }

            if (_ticks == 14)
            {
                if (0.6 >= Utility.RandomDouble())
                {
                    if (0.5 >= Utility.RandomDouble())
                    {
                        // *The line snaps tight as you snare a piece of wreckage from the sea floor!*
                        from?.PublicOverheadMessage(MessageType.Regular, 0x3B2, 1154218);
                        from?.AddToBackpack(new BrokenShipwreckRemains());
                    }
                    else
                    {
                        SpawnBaddies(p, Map, from);
                    }
                }

                Delete();
                return;
            }

            if (Utility.RandomBool())
            {
                Effects.PlaySound(p, Map, 0x364);
            }

            Z -= 1;
        }

        _ticks++;
    }

    public override void OnAfterDelete()
    {
        _effectTimer.Cancel();
        base.OnAfterDelete();
    }
}

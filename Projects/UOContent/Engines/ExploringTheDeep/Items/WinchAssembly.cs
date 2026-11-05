using System;
using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Items/WinchAssembly.cs). The winch on the
///     sunken ship at Gravewater Lake: feed it the four spare parts, pull the lever, and the cargo hatch
///     grinds open and lets the Shadowlord out.
///
///     Substitutions:
///     <list type="bullet">
///         <item>
///             <c>Item.DoDownEffect(object[])</c> was driven by a <c>TimerStateCallback</c>; it takes the
///             caster explicitly and is scheduled through <see cref="TimerExecutionToken" />.
///         </item>
///         <item>
///             The four <c>[CommandProperty]</c> booleans of the winch each invalidated properties; the
///             generated setters do that through <c>[InvalidateProperties]</c>.
///         </item>
///     </list>
/// </summary>
[SerializationGenerator(0, false)]
public partial class WinchAssembly : Item
{
    public static readonly string EntityName = "winchassemply";

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    [InvalidateProperties]
    private bool _hasFlyWheel;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    [InvalidateProperties]
    private bool _hasWireSpool;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    [InvalidateProperties]
    private bool _hasBearingAssembly;

    [SerializableField(3)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    [InvalidateProperties]
    private bool _hasPowerCore;

    [Constructible]
    public WinchAssembly() : base(0x280E)
    {
        Movable = false;
        Hue = 2101;
    }

    public override int LabelNumber => 1154433; // Winch Assembly

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(HasFlyWheel ? 1154448 : 1154432);
        list.Add(HasWireSpool ? 1154449 : 1154434);
        list.Add(HasPowerCore ? 1154450 : 1154435);
        list.Add(HasBearingAssembly ? 1154451 : 1154436);
    }

    public override bool OnDragDrop(Mobile from, Item dropped)
    {
        switch (dropped)
        {
            case BearingAssembly when !_hasBearingAssembly:
                {
                    dropped.Delete();
                    HasBearingAssembly = true;
                    return true;
                }
            case FlyWheel when !_hasFlyWheel:
                {
                    dropped.Delete();
                    HasFlyWheel = true;
                    return true;
                }
            case PowerCore when !_hasPowerCore:
                {
                    dropped.Delete();
                    HasPowerCore = true;
                    return true;
                }
            case WireSpool when !_hasWireSpool:
                {
                    dropped.Delete();
                    HasWireSpool = true;
                    return true;
                }
        }

        return false;
    }

    /// <summary>Places the winch, its lever and the cargo hatch, replacing any previous copy.</summary>
    public static void Generate()
    {
        WeakEntityCollection.Delete(EntityName);

        var winch = new WinchAssembly();
        WeakEntityCollection.Add(EntityName, winch);

        var hatch = new Hatch();
        WeakEntityCollection.Add(EntityName, hatch);

        var lever = new WinchAssemblyLever(winch, hatch);
        WeakEntityCollection.Add(EntityName, lever);

        lever.MoveToWorld(new Point3D(6310, 1705, 0), Map.Trammel);
        winch.MoveToWorld(new Point3D(6310, 1704, 0), Map.Trammel);
        hatch.MoveToWorld(new Point3D(6303, 1711, 10), Map.Trammel);
    }

    public void ConsumeParts()
    {
        HasBearingAssembly = false;
        HasFlyWheel = false;
        HasWireSpool = false;
        HasPowerCore = false;
    }
}

[SerializationGenerator(0, false)]
public partial class WinchAssemblyLever : Item
{
    [SerializableField(0, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private WinchAssembly _winch;

    [SerializableField(1, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Hatch _hatch;

    [Constructible]
    public WinchAssemblyLever() : base(0x108E) => Movable = false;

    [Constructible]
    public WinchAssemblyLever(WinchAssembly winch, Hatch hatch) : base(0x108E)
    {
        Movable = false;
        _winch = winch;
        _hatch = hatch;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (_winch == null || _hatch == null)
        {
            return;
        }

        if (!_winch.HasBearingAssembly || !_winch.HasFlyWheel || !_winch.HasWireSpool || !_winch.HasPowerCore)
        {
            return;
        }

        Timer.StartTimer(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0), 3, () => _hatch.DoDownEffect(from));

        Shadowlord.Spawn(new Point3D(6417, 1649, 0), Map.Trammel);

        _winch.ConsumeParts();
    }
}

/// <summary>
///     The cargo hatch over the Shadowlord's hold, built from <see cref="HatchTile" /> components. Pulling
///     the lever grinds it down a few steps at a time; it climbs back up after half an hour.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Hatch : BaseAddon
{
    private TimerExecutionToken _timer;

    [Constructible]
    public Hatch()
    {
        for (var x = 2; x <= 5; x++)
        {
            for (var y = 2; y <= 7; y++)
            {
                AddComponent(new HatchTile(), x, y, 0);
            }

            for (var y = -6; y <= -1; y++)
            {
                AddComponent(new HatchTile(), x, y, 0);
            }
        }

        for (var x = -4; x <= -1; x++)
        {
            for (var y = 2; y <= 7; y++)
            {
                AddComponent(new HatchTile(), x, y, 0);
            }

            for (var y = -6; y <= -1; y++)
            {
                AddComponent(new HatchTile(), x, y, 0);
            }
        }
    }

    public void DoDownEffect(Mobile from)
    {
        if (Deleted)
        {
            return;
        }

        for (var i = 0; i < 3; ++i)
        {
            int x, y;

            switch (Utility.Random(8))
            {
                default:
                case 0:
                    {
                        x = -1;
                        y = -1;
                        break;
                    }
                case 1:
                    {
                        x = -1;
                        y = 0;
                        break;
                    }
                case 2:
                    {
                        x = -1;
                        y = +1;
                        break;
                    }
                case 3:
                    {
                        x = 0;
                        y = -1;
                        break;
                    }
                case 4:
                    {
                        x = 0;
                        y = +1;
                        break;
                    }
                case 5:
                    {
                        x = +1;
                        y = -1;
                        break;
                    }
                case 6:
                    {
                        x = +1;
                        y = 0;
                        break;
                    }
                case 7:
                    {
                        x = +1;
                        y = +1;
                        break;
                    }
            }

            Effects.SendLocationEffect(new Point3D(X + x, Y + y, Z), Map, 0x36CB, 16, 4, 1362, 0);

            Z -= 1;

            if (Z == 1)
            {
                Hue = 1;
                Timer.StartTimer(TimeSpan.FromMinutes(30.0), Restore, out _timer);
            }
        }
    }

    private void Restore()
    {
        Z = 10;
        Hue = 2969;
    }

    public override void OnAfterDelete()
    {
        _timer.Cancel();
        base.OnAfterDelete();
    }
}

[SerializationGenerator(0, false)]
public partial class HatchTile : AddonComponent
{
    [Constructible]
    public HatchTile() : base(0x07CD) => Hue = 2969;

    [CommandProperty(AccessLevel.GameMaster)]
    public bool Active => Z == 1;

    public override bool HandlesOnMovement => true;

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        base.OnMovement(m, oldLocation);

        if (Active && m.Player && Utility.InRange(Location, m.Location, 0) && !Utility.InRange(Location, oldLocation, 0))
        {
            m.MoveToWorld(new Point3D(6415, 1647, 0), Map.Trammel);
        }
    }
}

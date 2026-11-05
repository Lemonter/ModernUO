using System;
using System.Collections.Generic;
using ModernUO.Serialization;

namespace Server.Items;

public enum Parts
{
    None = -1,
    Flywheel,
    WireSpool,
    PowerCore,
    BearingAssembly
}

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Items/QuestItems/StorageLocker.cs). The four
///     supply lockers around the sunken ship at Gravewater Lake. Each one, when active, scatters a ring of
///     wooden barrels around itself; smashing the right barrel yields the key that opens the locker, which
///     then holds one of the four spare parts the winch assembly needs.
///
///     Serialization follows the house layout: the barrels list, the active flag, the part type and the
///     restart deadline are generated fields. ServUO stored the restart timer itself plus a delta time;
///     here the deadline is a <see cref="Core.Now" /> value and the timer is rebuilt from it, so a
///     restart survives a server save.
/// </summary>
[SerializationGenerator(0, false)]
[Furniture]
[Flippable(0x285D, 0x285E)]
public partial class StorageLocker : FillableContainer
{
    [SerializableField(0, setter: "private")]
    private bool _activeState;

    [SerializableField(1, setter: "private")]
    [InvalidateProperties]
    private Parts _type;

    [SerializableField(2, setter: "private")]
    private List<Item> _barrels;

    private TimerExecutionToken _restartTimer;

    [Constructible]
    public StorageLocker() : this(Parts.None)
    {
    }

    [Constructible]
    public StorageLocker(Parts type) : base(0x285E)
    {
        _barrels = new List<Item>();
        _type = type;

        Locked = true;
        Hue = 2301;
        Movable = false;
    }

    public override int LabelNumber => 1154431; // Storage Locker

    public override bool IsDecoContainer => false;

    /// <summary>The eight barrels that ring the locker itself.</summary>
    public static readonly int[][] WoodenToMetalBarrelCoordinate =
    {
        new[] { 0, 1 },
        new[] { 1, 1 },
        new[] { 1, 0 },
        new[] { 1, -1 },
        new[] { 0, -1 },
        new[] { -1, -1 },
        new[] { -1, 0 },
        new[] { -1, 1 }
    };

    [CommandProperty(AccessLevel.GameMaster)]
    public DateTime RestartTime { get; private set; }

    /// <summary>
    ///     Turning this on scatters the barrel rings (that is what ServUO's <c>Active</c> setter did), so the
    ///     flag is stored in its own field and driven from here rather than from a generated setter — a
    ///     generated setter would already have flipped the flag before <see cref="Start" /> could run.
    /// </summary>
    [CommandProperty(AccessLevel.GameMaster)]
    public bool Active
    {
        get => _activeState;
        set
        {
            if (_activeState == value)
            {
                return;
            }

            if (value)
            {
                Start();
            }
            else
            {
                Stop();
            }

            InvalidateProperties();
        }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        // *You barely make out some words on a rusted nameplate*<BR>REPLACEMENT PARTS: ~1_PART~
        list.Add(1154425, $"#{1154427 + (int)_type}");
    }

    public void Start()
    {
        if (_activeState || Deleted)
        {
            return;
        }

        _activeState = true;
        _restartTimer.Cancel();

        var index = Utility.Random(0, 8);
        var randomKey = Utility.Random(-4, 4);
        var loot = false;

        for (var k = 0; k < 8; k++)
        {
            var itemX = X + WoodenToMetalBarrelCoordinate[k][0];
            var itemY = Y + WoodenToMetalBarrelCoordinate[k][1];
            var z = Map.GetAverageZ(itemX, itemY);

            Item barrel;

            if (index == k)
            {
                barrel = new WoodenKeyBarrel(Parts.None);
            }
            else
            {
                barrel = new WoodenToMetalBarrel(this);
            }

            _barrels.Add(barrel);
            barrel.MoveToWorld(new Point3D(itemX, itemY, z), Map);
        }

        for (var x = -4; x < 5; x++)
        {
            for (var y = 4; y > -5; y--)
            {
                if (x >= -1 && x <= 1 && y >= -1 && y <= 1)
                {
                    continue;
                }

                var itemX = X + x;
                var itemY = Y + y;
                var z = Map.GetAverageZ(itemX, itemY);

                var key = Parts.None;

                if (!loot && x == randomKey)
                {
                    key = _type;
                    loot = true;
                }

                var barrel = new WoodenKeyBarrel(key);

                if (key != Parts.None)
                {
                    barrel.StorageLocker = this;
                }

                _barrels.Add(barrel);
                barrel.MoveToWorld(new Point3D(itemX, itemY, z), Map);
            }
        }
    }

    public void Stop()
    {
        if (Deleted)
        {
            return;
        }

        _activeState = false;
        _restartTimer.Cancel();

        if (_barrels != null)
        {
            for (var i = 0; i < _barrels.Count; ++i)
            {
                _barrels[i]?.Delete();
            }

            _barrels.Clear();
        }

        for (var i = Items.Count - 1; i >= 0; --i)
        {
            if (i < Items.Count)
            {
                Items[i].Delete();
            }
        }
    }

    public void BeginRestart(TimeSpan ts)
    {
        _restartTimer.Cancel();

        RestartTime = Core.Now + ts;
        Timer.StartTimer(ts, Restart, out _restartTimer);
    }

    private void Restart()
    {
        Stop();
        Start();
    }

    public override void OnAfterDelete()
    {
        Stop();

        base.OnAfterDelete();
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        _barrels ??= new List<Item>();

        if (RestartTime != DateTime.MinValue)
        {
            var delay = RestartTime - Core.Now;

            if (delay < TimeSpan.Zero)
            {
                delay = TimeSpan.Zero;
            }

            Timer.StartTimer(delay, Restart, out _restartTimer);
        }
    }
}

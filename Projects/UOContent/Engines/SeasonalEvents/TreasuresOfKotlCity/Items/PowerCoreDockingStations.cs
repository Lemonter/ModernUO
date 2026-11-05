using System.Collections.Generic;
using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKotlCity/Items/PowerCoreDockingStations.cs).
///     Dropped: the <c>KotlBattleSimulator</c> activation broadcast (the simulator is not ported) and the
///     <c>Region.GetEnumeratedMobiles</c> walk (this codebase's <c>Region</c> has no such method), so activating the
///     last linked station only switches its own visuals. <see cref="StasisChamberPowerCore" /> is ported with the
///     pack because the station accepts nothing else.
/// </summary>
[SerializationGenerator(0, false)]
public partial class PowerCoreDockingStation : BaseAddon
{
    public static List<PowerCoreDockingStation> Stations { get; set; }

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private InternalContainer _chest1;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private InternalContainer _chest2;

    [SerializableField(2)]
    private bool _link;

    [SerializableProperty(3)]
    [CommandProperty(AccessLevel.GameMaster)]
    public bool Active
    {
        get => _active;
        set
        {
            var active = _active;
            _active = value;

            if (!active && _active)
            {
                Activate();
            }
            else if (active && !_active)
            {
                Deactivate();
            }
        }
    }

    public override BaseAddonDeed Deed => null;

    [Constructible]
    public PowerCoreDockingStation() : this(false)
    {
    }

    [Constructible]
    public PowerCoreDockingStation(bool link)
    {
        _link = link;

        if (link)
        {
            AddStation();
        }

        AddComplexComponent(40157, 0, 0, 36, 0, 1124171);
        AddComplexComponent(40157, 1, 0, 36, 0, 1124171);
        AddComplexComponent(40157, 0, 1, 36, 0, 1124171);
        AddComplexComponent(40157, 1, 1, 36, 0, 1124171);
        AddComplexComponent(40146, 0, 0, 11, 2591, 1157023);

        AddComplexComponent(40173, 1, -1, 0, 2591);
        AddComplexComponent(40173, 2, 0, 0, 2591);
        AddComplexComponent(40174, 0, 2, 0, 2591);

        AddComplexComponent(39736, 1, 0, 11, 2591);
        AddComplexComponent(39728, 0, 1, 11, 2591);
        AddComplexComponent(39734, 1, 1, 11, 2591);
        AddComplexComponent(39730, 0, 0, 11, 2591);
        AddComplexComponent(39714, -1, -1, 0, 2591);
        AddComplexComponent(39713, -1, 1, 0, 2591);
        AddComplexComponent(39713, -1, 0, 0, 2591);
        AddComplexComponent(39712, 0, -1, 0, 2591);
        AddComplexComponent(39712, 1, -1, 0, 2591);
        AddComplexComponent(39713, 1, 0, 0, 2591);
        AddComplexComponent(39712, 0, 1, 0, 2591);
        AddComplexComponent(39711, 1, 1, 0, 2591);

        AddComponent(new AddonComponent(0x9CE5), -1, 2, 0);

        _chest1 = new InternalContainer { Addon = this };
        _chest2 = new InternalContainer { Addon = this };
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (_link)
        {
            AddStation();
        }

        if (_chest1 != null)
        {
            _chest1.Addon = this;
        }

        if (_chest2 != null)
        {
            _chest2.Addon = this;
        }
    }

    public void AddStation()
    {
        Stations ??= new List<PowerCoreDockingStation>();

        if (!Stations.Contains(this))
        {
            Stations.Add(this);
        }
    }

    public override void OnLocationChange(Point3D oldLoc)
    {
        base.OnLocationChange(oldLoc);

        if (_chest1 is { Deleted: false })
        {
            _chest1.MoveToWorld(new Point3D(X + 1, Y + 1, Z + 11), Map);
        }

        if (_chest2 is { Deleted: false })
        {
            _chest2.MoveToWorld(new Point3D(X, Y + 1, Z + 11), Map);
        }
    }

    public override void OnMapChange()
    {
        base.OnMapChange();

        if (_chest1 != null)
        {
            _chest1.Map = Map;
        }

        if (_chest2 != null)
        {
            _chest2.Map = Map;
        }
    }

    private void Activate()
    {
        ToggleVisuals(40146, 40147);
        ToggleVisuals(40173, 40142);
        ToggleVisuals(40174, 40169);
    }

    private void Deactivate()
    {
        ToggleVisuals(40147, 40146);
        ToggleVisuals(40142, 40173);
        ToggleVisuals(40169, 40174);

        if (_chest1 != null)
        {
            for (var i = _chest1.Items.Count - 1; i >= 0; i--)
            {
                _chest1.Items[i].Delete();
            }
        }

        if (_chest2 != null)
        {
            for (var i = _chest2.Items.Count - 1; i >= 0; i--)
            {
                _chest2.Items[i].Delete();
            }
        }

        _active = false;
    }

    private void ToggleVisuals(int fromId, int toId)
    {
        foreach (var component in Components)
        {
            if (component.ItemID == fromId)
            {
                component.ItemID = toId;
            }
        }
    }

    private void AddComplexComponent(int item, int xOffset, int yOffset, int zOffset, int hue, int localization = 0)
    {
        AddonComponent component = localization == 0
            ? new AddonComponent(item)
            : new LocalizedAddonComponent(item, localization);

        if (hue != 0)
        {
            component.Hue = hue;
        }

        AddComponent(component, xOffset, yOffset, zOffset);
    }

    public override void Delete()
    {
        base.Delete();

        if (_chest1 is { Deleted: false })
        {
            _chest1.Delete();
        }

        if (_chest2 is { Deleted: false })
        {
            _chest2.Delete();
        }

        Stations?.Remove(this);
    }

    [SerializationGenerator(0, false)]
    public partial class InternalContainer : BaseContainer
    {
        [SerializableField(0)]
        private PowerCoreDockingStation _addon;

        [Constructible]
        public InternalContainer() : base(16421)
        {
            Movable = false;
            Hue = 2591;
        }

        public override bool OnDragDrop(Mobile from, Item dropped) => CheckDrop(from, dropped);

        public override bool OnDragDropInto(Mobile from, Item item, Point3D p) => CheckDrop(from, item);

        private bool CheckDrop(Mobile from, Item dropped)
        {
            if (dropped is not StasisChamberPowerCore)
            {
                from.SendLocalizedMessage(1157024); // The docking station only accepts power cores.
                return false;
            }

            if (_addon == null)
            {
                return false;
            }

            if (_addon.Active)
            {
                from.SendLocalizedMessage(1157025); // This power station is already active.
                return false;
            }

            base.OnDragDrop(from, dropped);
            dropped.Movable = false;

            _addon.Active = true;
            return true;
        }

        public override void Delete()
        {
            base.Delete();

            if (_addon is { Deleted: false })
            {
                _addon.Delete();
            }
        }
    }
}

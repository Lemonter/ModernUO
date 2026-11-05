using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Carried in from ServUO (Scripts/Items/Decorative/BaseStatuette.cs). It is not part of the
///     artifacts tree, but eight ported statuettes (the museum and library collection pieces)
///     inherit its on-movement "turned on" behaviour, so the class comes along with them.
/// </summary>
[SerializationGenerator(0, false)]
public partial class BaseStatuette : Item
{
    private bool _turnedOn;

    [Constructible]
    public BaseStatuette(int itemID) : base(itemID) => LootType = LootType.Blessed;

    public override bool HandlesOnMovement => _turnedOn && IsLockedDown;

    [CommandProperty(AccessLevel.GameMaster)]
    public bool TurnedOn
    {
        get => _turnedOn;
        set
        {
            _turnedOn = value;
            InvalidateProperties();
        }
    }

    public override double DefaultWeight => 1.0;

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        if (_turnedOn && IsLockedDown && (!m.Hidden || m.Player) &&
            Utility.InRange(m.Location, Location, 2) && !Utility.InRange(oldLocation, Location, 2))
        {
            PlaySound(m);
        }

        base.OnMovement(m, oldLocation);
    }

    public virtual void PlaySound(Mobile to)
    {
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(_turnedOn ? 502695 : 502696); // turned on / turned off
    }
}

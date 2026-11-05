using ModernUO.Serialization;
using Server.Multis;
using Server.Network;

namespace Server.Items;

[Furniture]
[Flippable]
[SerializationGenerator(0, false)]
public partial class WineRack : LockableContainer, IDyable
{
    private const int SouthId = 0xA568;
    private const int SouthEmptyId = 0xA567;
    private const int EastId = 0xA56A;
    private const int EastEmptyId = 0xA569;

    [Constructible]
    public WineRack(int id = SouthEmptyId) : base(id)
    {
    }

    public override string DefaultName => "винный стеллаж";

    public override int DefaultGumpID => 0x44;

    public bool IsEmpty => Items.Count == 0;

    public override bool DisplaysContent => false;

    public bool Dye(Mobile from, DyeTub sender)
    {
        if (Deleted)
        {
            return false;
        }

        Hue = sender.DyedHue;

        return true;
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public override int ItemID
    {
        get => base.ItemID;
        set
        {
            base.ItemID = value;

            CheckWineRack();
        }
    }

    public void CheckWineRack()
    {
        if (IsEmpty)
        {
            if (ItemID == SouthId)
            {
                base.ItemID = SouthEmptyId;
            }
            else if (ItemID == EastId)
            {
                base.ItemID = EastEmptyId;
            }
        }
        else if (ItemID == SouthEmptyId)
        {
            base.ItemID = SouthId;
        }
        else if (ItemID == EastEmptyId)
        {
            base.ItemID = EastId;
        }
    }

    public void Flip()
    {
        if (ItemID == SouthId)
        {
            base.ItemID = EastId;
        }
        else if (ItemID == EastId)
        {
            base.ItemID = SouthId;
        }
        else if (ItemID == SouthEmptyId)
        {
            base.ItemID = EastEmptyId;
        }
        else if (ItemID == EastEmptyId)
        {
            base.ItemID = SouthEmptyId;
        }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        // Contents: ~1_COUNT~/~2_MAXCOUNT~ items, ~3_WEIGHT~/~4_MAXWEIGHT~ stones
        list.Add(1072241, $"{TotalItems}\t{MaxItems}\t{TotalWeight}\t{MaxWeight}");
    }

    public override bool OnDragDropInto(Mobile from, Item item, Point3D p)
    {
        var dropped = base.OnDragDropInto(from, item, p);
        var house = BaseHouse.FindHouseAt(this);

        if (house != null && dropped)
        {
            OnItemDropped(from, item, house);
        }

        return dropped;
    }

    public override bool TryDropItem(Mobile from, Item dropped, bool sendFullMessage)
    {
        if (!CheckHold(from, dropped, true, true))
        {
            return false;
        }

        var house = BaseHouse.FindHouseAt(this);

        if (house != null && IsLockedDown && !house.CheckAccessibility(this, from))
        {
            // You are not allowed to access this!
            PublicOverheadMessage(MessageType.Regular, 0x21, 1061637);
            from.SendLocalizedMessage(501727); // You cannot lock that down!
            return false;
        }

        DropItem(dropped);

        if (house != null && !IsLockedDown)
        {
            OnItemDropped(from, dropped, house);
        }

        return true;
    }

    public override bool IsAccessibleTo(Mobile m) => true;

    public override bool CheckLift(Mobile from, Item item, ref LRReason reject)
    {
        if (item != this)
        {
            var house = BaseHouse.FindHouseAt(this);

            if (house != null && IsSecure)
            {
                return house.CheckSecureAccess(from, this) == SecureAccessResult.Accessible;
            }
        }

        return base.CheckLift(from, item, ref reject);
    }

    public virtual void OnItemDropped(Mobile from, Item item, BaseHouse house)
    {
        if (house.CheckSecureAccess(from, this) == SecureAccessResult.Inaccessible)
        {
            item.InvalidateProperties();
        }
    }

    public override void OnItemAdded(Item item)
    {
        base.OnItemAdded(item);

        CheckWineRack();
    }

    public override void OnItemRemoved(Item item)
    {
        base.OnItemRemoved(item);

        CheckWineRack();
    }
}

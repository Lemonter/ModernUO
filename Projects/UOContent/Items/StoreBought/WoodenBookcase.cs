using ModernUO.Serialization;

namespace Server.Items;

[Furniture]
[Flippable(0x0A9D, 0x0A9E)]
[SerializationGenerator(0, false)]
public partial class WoodenBookcase : BaseContainer
{
    [Constructible]
    public WoodenBookcase() : base(0x0A9D)
    {
    }

    public override int LabelNumber => 1071102; // Wooden Bookcase
    public override int DefaultGumpID => 0x4D;

    public bool IsEmpty => Items.Count == 0;

    [CommandProperty(AccessLevel.GameMaster)]
    public override int ItemID
    {
        get => base.ItemID;
        set
        {
            if (!IsEmpty && value is 0x0A9D or 0x0A9E)
            {
                base.ItemID = value == 0x0A9D
                    ? Utility.RandomList(0x0A97, 0x0A98, 0x0A9B)
                    : Utility.RandomList(0x0A99, 0x0A9A, 0x0A9C);
            }
            else if (IsEmpty && value is not (0x0A9D or 0x0A9E))
            {
                switch (value)
                {
                    case 0x0A97:
                    case 0x0A98:
                    case 0x0A9B:
                        base.ItemID = 0x0A9D;
                        break;
                    case 0x0A99:
                    case 0x0A9A:
                    case 0x0A9C:
                        base.ItemID = 0x0A9E;
                        break;
                    default:
                        base.ItemID = value;
                        break;
                }
            }
            else
            {
                base.ItemID = value;
            }
        }
    }

    public override void OnItemAdded(Item item)
    {
        base.OnItemAdded(item);

        switch (ItemID)
        {
            case 0x0A9D:
                ItemID = Utility.RandomList(0x0A97, 0x0A98, 0x0A9B);
                break;
            case 0x0A9E:
                ItemID = Utility.RandomList(0x0A99, 0x0A9A, 0x0A9C);
                break;
        }
    }

    public override void OnItemRemoved(Item item)
    {
        base.OnItemRemoved(item);

        if (!IsEmpty)
        {
            return;
        }

        switch (ItemID)
        {
            case 0x0A97:
            case 0x0A98:
            case 0x0A9B:
                base.ItemID = 0x0A9D;
                break;
            case 0x0A99:
            case 0x0A9A:
            case 0x0A9C:
                base.ItemID = 0x0A9E;
                break;
        }
    }

    public void Flip()
    {
        ItemID = ItemID switch
        {
            0x0A97 => 0x0A99,
            0x0A98 => 0x0A9A,
            0x0A99 => 0x0A97,
            0x0A9A => 0x0A98,
            0x0A9B => 0x0A9C,
            0x0A9C => 0x0A9B,
            0x0A9D => 0x0A9E,
            0x0A9E => 0x0A9D,
            _      => ItemID
        };
    }
}

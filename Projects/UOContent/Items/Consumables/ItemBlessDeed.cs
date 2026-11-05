using ModernUO.Serialization;
using Server.Targeting;

namespace Server.Items;

public class ItemBlessTarget : Target
{
    private readonly ItemBlessDeed _deed;

    public ItemBlessTarget(ItemBlessDeed deed) : base(1, false, TargetFlags.None) => _deed = deed;

    protected override void OnTarget(Mobile from, object target)
    {
        if (_deed.Deleted || _deed.RootParent != from)
        {
            return;
        }

        if (target is not Item item)
        {
            from.SendLocalizedMessage(500509); // You cannot bless that object
            return;
        }

        if (item.RootParent != from)
        {
            from.SendLocalizedMessage(500508); // You may only bless objects that you are carrying.
        }
        else if (item.Stackable || item is BaseContainer)
        {
            from.SendLocalizedMessage(500509); // You cannot bless that object
        }
        else if (item.LootType == LootType.Blessed || item.BlessedFor == from || item.Insured)
        {
            from.SendLocalizedMessage(1045113); // That item is already blessed
        }
        else if (item.LootType != LootType.Regular)
        {
            from.SendLocalizedMessage(1045114); // You can not bless that item
        }
        else
        {
            item.LootType = LootType.Blessed;
            from.SendLocalizedMessage(1075281); // Your item has been blessed

            _deed.Delete();
        }
    }
}

[SerializationGenerator(0, false)]
public partial class ItemBlessDeed : Item
{
    [Constructible]
    public ItemBlessDeed() : base(0x14F0)
    {
        Weight = 1.0;
        LootType = LootType.Blessed;
    }

    public override string DefaultName => "грамота благословения предмета";

    public override bool DisplayLootType => true;

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return;
        }

        from.SendLocalizedMessage(500506); // Which item do you wish to bless?
        from.Target = new ItemBlessTarget(this);
    }
}

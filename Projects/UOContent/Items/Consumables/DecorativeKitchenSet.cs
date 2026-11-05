using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Портировано из ServUO. PieSafe и ChinaCabinet в этом репозитории отсутствуют, поэтому
///     набор содержит только существующую утварь.
/// </summary>
[SerializationGenerator(0, false)]
public partial class DecorativeKitchenSet : Backpack
{
    [Constructible]
    public DecorativeKitchenSet()
    {
        DropItem(new ButcherBlock());
        DropItem(new WashBasinDeed());
        DropItem(new WoodStoveDeed());

        var bag = new Bag();
        bag.DropItem(new Countertop());
        bag.DropItem(new Countertop());
        bag.DropItem(new Countertop());
        bag.DropItem(new Countertop());

        DropItem(bag);
    }

    public override int LabelNumber => 1158970; // Decorative Kitchen Set
}

using ModernUO.Serialization;
using Server.Engines.Distillation;
using Server.Multis;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/Distillery.cs).
///     Dropped APIs: none — <see cref="DistillationGump" /> is available in
///     <c>Server.Engines.Distillation</c>.
/// </summary>
[SerializationGenerator(0, false)]
public partial class DistilleryEastAddon : BaseAddon
{
    [Constructible]
    public DistilleryEastAddon()
    {
        AddComponent(new LocalizedAddonComponent(15802, 1150640), 0, 0, 0);
        AddComponent(new LocalizedAddonComponent(15803, 1150640), 0, 1, 0);
    }

    public override BaseAddonDeed Deed => new DistilleryEastAddonDeed();

    public override void OnComponentUsed(AddonComponent c, Mobile from)
    {
        var house = BaseHouse.FindHouseAt(c);

        if (from.InRange(c.Location, 2) && (house == null || house.IsCoOwner(from)))
        {
            DistillationGump.DisplayTo(from);
        }
    }
}

[SerializationGenerator(0, false)]
public partial class DistilleryEastAddonDeed : BaseAddonDeed
{
    [Constructible]
    public DistilleryEastAddonDeed()
    {
    }

    public override BaseAddon Addon => new DistilleryEastAddon();
    public override int LabelNumber => 1150664; // distillery (east)
}

[SerializationGenerator(0, false)]
public partial class DistillerySouthAddon : BaseAddon
{
    [Constructible]
    public DistillerySouthAddon()
    {
        AddComponent(new LocalizedAddonComponent(15800, 1150640), 0, 0, 0);
        AddComponent(new LocalizedAddonComponent(15801, 1150640), -1, 0, 0);
    }

    public override BaseAddonDeed Deed => new DistillerySouthAddonDeed();

    public override void OnComponentUsed(AddonComponent c, Mobile from)
    {
        var house = BaseHouse.FindHouseAt(c);

        if (house == null || house.IsCoOwner(from))
        {
            DistillationGump.DisplayTo(from);
        }
    }
}

[SerializationGenerator(0, false)]
public partial class DistillerySouthAddonDeed : BaseAddonDeed
{
    [Constructible]
    public DistillerySouthAddonDeed()
    {
    }

    public override BaseAddon Addon => new DistillerySouthAddon();
    public override int LabelNumber => 1150663; // distillery (south)
}

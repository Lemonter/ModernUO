using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/AdvancedTrainingDummy.cs).
/// </summary>
[Flippable(0x971C, 0x9821)]
[SerializationGenerator(0, false)]
public partial class AdvancedTrainingDummy : TrainingDummy
{
    [Constructible]
    public AdvancedTrainingDummy() : this(0x971C)
    {
    }

    public AdvancedTrainingDummy(int itemID) : base(itemID)
    {
        MinSkill = -25.0;
        MaxSkill = +60.0;
    }
}

[SerializationGenerator(0, false)]
public partial class AdvancedTrainingDummyEastAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new AdvancedTrainingDummyEastDeed();

    [Constructible]
    public AdvancedTrainingDummyEastAddon()
    {
        AddComponent(new AdvancedTrainingDummy(38945), 0, 0, 0);
        AddComponent(new AddonComponent(38962), 0, -1, 0);
        AddComponent(new AddonComponent(38960), 0, 0, 0);
        AddComponent(new AddonComponent(38958), 0, 1, 0);

        AddComponent(new AddonComponent(38959), -1, 1, 0);
        AddComponent(new AddonComponent(38961), 1, 1, 0);
        AddComponent(new AddonComponent(38963), -1, -1, 0);
        AddComponent(new AddonComponent(38957), 1, -1, 0);
    }
}

[SerializationGenerator(0, false)]
public partial class AdvancedTrainingDummyEastDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new AdvancedTrainingDummyEastAddon();
    public override int LabelNumber => 1150596; // advanced training dummy (east)

    [Constructible]
    public AdvancedTrainingDummyEastDeed()
    {
    }
}

[SerializationGenerator(0, false)]
public partial class AdvancedTrainingDummySouthAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new AdvancedTrainingDummySouthDeed();

    [Constructible]
    public AdvancedTrainingDummySouthAddon()
    {
        AddComponent(new AdvancedTrainingDummy(38940), 0, 0, 0);
        AddComponent(new AddonComponent(38955), -1, 0, 0);
        AddComponent(new AddonComponent(38953), 0, 0, 0);
        AddComponent(new AddonComponent(38951), 1, 0, 0);

        AddComponent(new AddonComponent(38954), -1, 1, 0);
        AddComponent(new AddonComponent(38956), -1, -1, 0);

        AddComponent(new AddonComponent(38950), 1, 1, 0);
        AddComponent(new AddonComponent(38952), 1, -1, 0);
    }
}

[SerializationGenerator(0, false)]
public partial class AdvancedTrainingDummySouthDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new AdvancedTrainingDummySouthAddon();
    public override int LabelNumber => 1150595; // advanced training dummy (south)

    [Constructible]
    public AdvancedTrainingDummySouthDeed()
    {
    }
}

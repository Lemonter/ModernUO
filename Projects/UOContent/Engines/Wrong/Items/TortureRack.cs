using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO's Wrong Revamped dungeon (Items/Artifacts/TortureRackArtifact.cs) — the artifact
///     torture rack, its two orientations and their deeds.
///
///     Adapted: the original's <c>IArtifact</c> interface does not exist here, so the component implements
///     nothing extra; <c>ObjectPropertyList.Enabled</c> (used for <c>ForceShowProperties</c>) has no
///     counterpart either, and tooltips are always on, so the component simply always shows its
///     properties. Properties moved from <c>ObjectPropertyList</c> to <see cref="IPropertyList" />.
/// </summary>
[SerializationGenerator(0, false)]
public partial class TortureRackComponent : AddonComponent
{
    public override int LabelNumber => 1152307; // Torture Rack

    public override bool ForceShowProperties => true;

    public virtual bool ShowArtifactRarity => true;

    public virtual int ArtifactRarity => 10;

    [Constructible]
    public TortureRackComponent(int itemID) : base(itemID)
    {
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (ShowArtifactRarity)
        {
            list.Add(1061078, ArtifactRarity); // artifact rarity ~1_val~
        }
    }
}

[SerializationGenerator(0, false)]
public partial class TortureRackEast : BaseAddon
{
    [Constructible]
    public TortureRackEast()
    {
        AddComponent(new TortureRackComponent(0x4AAB), 0, 0, 0);
        AddComponent(new TortureRackComponent(0x4AA3), 0, 1, 0);
        AddComponent(new TortureRackComponent(0x4AA2), 0, 2, 0);
    }

    public override BaseAddonDeed Deed => new TortureRackEastDeed();
}

[SerializationGenerator(0, false)]
public partial class TortureRackEastDeed : BaseAddonDeed
{
    public override int LabelNumber => 1152305;

    [Constructible]
    public TortureRackEastDeed()
    {
    }

    public override BaseAddon Addon => new TortureRackEast();
}

[SerializationGenerator(0, false)]
public partial class TortureRackSouth : BaseAddon
{
    [Constructible]
    public TortureRackSouth()
    {
        AddComponent(new TortureRackComponent(0x4AA0), 0, 0, 0);
        AddComponent(new TortureRackComponent(0x4AA1), 1, 0, 0);
        AddComponent(new TortureRackComponent(0x4AAD), 2, 0, 0);
    }

    public override BaseAddonDeed Deed => new TortureRackSouthDeed();
}

[SerializationGenerator(0, false)]
public partial class TortureRackSouthDeed : BaseAddonDeed
{
    public override int LabelNumber => 1152306;

    [Constructible]
    public TortureRackSouthDeed()
    {
    }

    public override BaseAddon Addon => new TortureRackSouth();
}

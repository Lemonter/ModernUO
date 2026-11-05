using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/StoneAnvilEastAddon.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class StoneAnvilEastAddon : BaseAddon
{
    [Constructible]
    public StoneAnvilEastAddon()
    {
        this.AddComponent(new AnvilComponent(0x2DD6), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new StoneAnvilEastDeed();
    public override bool RetainDeedHue => true;
}

[SerializationGenerator(0, false)]
public partial class StoneAnvilEastDeed : BaseAddonDeed
{
    [Constructible]
    public StoneAnvilEastDeed()
    {
    }

    public override BaseAddon Addon => new StoneAnvilEastAddon();
    public override int LabelNumber => 1073392; // stone anvil (east)
}

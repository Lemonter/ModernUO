using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/NexusAddon.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class NexusComponent : AddonComponent
{
    public override int LabelNumber => 1152442; // Nexus

    public NexusComponent(int itemID)
        : base(itemID)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class NexusAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new NexusAddonDeed();

    [Constructible]
    public NexusAddon()
    {
        this.AddComponent(new NexusComponent(19324), -1, 1, 0);
        this.AddComponent(new NexusComponent(19326), 0, 0, 0);
        this.AddComponent(new NexusComponent(19319), 1, 2, 0);
        this.AddComponent(new NexusComponent(19321), 2, 1, 0);
        this.AddComponent(new NexusComponent(19319), 1, 2, 0);
        this.AddComponent(new NexusComponent(19323), 0, 1, 0);
        this.AddComponent(new NexusComponent(19327), 1, -1, 0);
        this.AddComponent(new NexusComponent(19316), 1, 1, 0);
        this.AddComponent(new NexusComponent(19322), 0, -1, 0);
        this.AddComponent(new NexusComponent(19320), 2, 2, 0);
        this.AddComponent(new NexusComponent(19325), 1, 0, 0);
        this.AddComponent(new NexusComponent(19318), 0, 2, 0);
        this.AddComponent(new NexusComponent(19317), -1, 2, 0);
    }
}

[SerializationGenerator(0, false)]
public partial class NexusAddonDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new NexusAddon();

    [Constructible]
    public NexusAddonDeed()
    {
        this.Name = "дело Нексуса";
    }
}

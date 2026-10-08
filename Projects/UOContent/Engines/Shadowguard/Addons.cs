using ModernUO.Serialization;
using Server.Items;

namespace Server.Engines.Shadowguard;

// Placeholder single-component "prop" addons — ServUO's real versions are presumably real
// multi-component decorative structures (a campfire/statue-style marker for each encounter
// room), but their component layouts weren't in any of the files pulled for this port. These
// exist so ShadowguardEncounter.CheckAddon() (which needs one BaseAddon instance per encounter
// type, internalized between rooms) has something concrete to construct; swap in the real
// layouts later if/when available.

[SerializationGenerator(0, false)]
public partial class BarAddon : BaseAddon
{
    public override BaseAddonDeed Deed => null;

    [Constructible]
    public BarAddon() => AddComponent(new AddonComponent(0x1F14), 0, 0, 0);
}

[SerializationGenerator(0, false)]
public partial class OrchardAddon : BaseAddon
{
    public override BaseAddonDeed Deed => null;

    [Constructible]
    public OrchardAddon() => AddComponent(new AddonComponent(0x1F14), 0, 0, 0);
}

[SerializationGenerator(0, false)]
public partial class ArmoryAddon : BaseAddon
{
    public override BaseAddonDeed Deed => null;

    [Constructible]
    public ArmoryAddon() => AddComponent(new AddonComponent(0x1F14), 0, 0, 0);
}

[SerializationGenerator(0, false)]
public partial class BelfryAddon : BaseAddon
{
    public override BaseAddonDeed Deed => null;

    [Constructible]
    public BelfryAddon() => AddComponent(new AddonComponent(0x1F14), 0, 0, 0);
}

[SerializationGenerator(0, false)]
public partial class ShadowguardFountainAddon : BaseAddon
{
    public override BaseAddonDeed Deed => null;

    [Constructible]
    public ShadowguardFountainAddon() => AddComponent(new AddonComponent(0x1F14), 0, 0, 0);
}

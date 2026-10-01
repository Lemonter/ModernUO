using Server.Items;

namespace Server.Engines.VvV;

// Ported from real OSI/ServUO content (Scripts/Services/ViceVsVirtue/Items/VvVItem.cs), but
// heavily simplified: the original normalizes ~20 pre-existing TOL/HS artifact drops
// (PrimerOnArmsTalisman, ClaininsSpellbook, CrimsonCincture, etc.) to fixed VvV-balanced
// attribute values on pickup. Most of those artifact types don't exist in this codebase at
// all (see VvVRewards.cs for exactly which do), and normalizing the ones that do exist would
// mutate shared base-game item classes outside VvV's own scope — out of scope for this port.
// Kept as a no-op hook so ViceVsVirtueSystem.AddVvVItem's call site doesn't need special-casing.
public static class VvVEquipment
{
    public static void CheckProperties(Item item)
    {
    }
}

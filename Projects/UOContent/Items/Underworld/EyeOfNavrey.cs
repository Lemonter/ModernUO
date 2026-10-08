using ModernUO.Serialization;

namespace Server.Items;

// Ported from JustUO (github.com/JustUO/JustUO, a ServUO fork) — see
// Regions/UnderworldRegion.cs header for why ServUO itself wasn't the source here.
// ServUO's original ties this to a "Green with Envy" quest given by an NPC named Vernix —
// that quest chain doesn't exist in this codebase, so it's a plain rare trophy drop from
// Navrey instead (see Navrey.OnDeath).

[SerializationGenerator(0, false)]
public partial class EyeOfNavrey : Item
{
    [Constructible]
    public EyeOfNavrey() : base(0x318D)
    {
        Weight = 1;
        Hue = 68;
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1095154; // Eye of Navrey Night-Eyes
}

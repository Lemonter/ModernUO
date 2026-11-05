using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Virtuosos/VirtuososEarpieces.cs).
///     VirtuososEarpieces: Dropped ISetItem — not present in this codebase.
/// </summary>
[SerializationGenerator(0, false)]
public partial class VirtuososEarpieces : GargishEarrings
{
    public override int LabelNumber => 1151557; // Virtuoso's Earpieces

    public override int InitMinHits => 125;

    public override int InitMaxHits => 125;

    [Constructible]
    public VirtuososEarpieces()
    {
        this.Hue = 1374;
        this.Weight = 1;
    }
}

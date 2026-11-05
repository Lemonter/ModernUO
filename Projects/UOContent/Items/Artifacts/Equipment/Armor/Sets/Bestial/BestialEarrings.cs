using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Bestial/BestialEarrings.cs).
///     BestialEarrings: Dropped ISetItem — not present in this codebase.
/// </summary>
[SerializationGenerator(0, false)]
public partial class BestialEarrings : GargishEarrings
{
    public override int LabelNumber => 1151543; // Bestial Earrings

    public override int InitMinHits => 125;

    public override int InitMaxHits => 125;

    [Constructible]
    public BestialEarrings()
    {
        this.Hue = 2010;
        this.Weight = 1;
    }
}

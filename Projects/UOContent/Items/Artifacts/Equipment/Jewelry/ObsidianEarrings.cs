using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Jewelry/ObsidianEarrings.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class ObsidianEarrings : GargishEarrings
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    public override int LabelNumber => 1113820; // Obsidian Earrings

    [Constructible]
    public ObsidianEarrings()
    {
        Attributes.BonusMana = 8;
        Attributes.RegenMana = 2;
        Attributes.RegenStam = 2;
        Attributes.SpellDamage = 8;
    }

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}

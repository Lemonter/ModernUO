using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Jewelry/Necklace.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class OctopusNecklace : BaseNecklace
{
    [Constructible]
    public OctopusNecklace()
        : base(0xA349)
    {
        AssignRandomGem();
    }

    private void AssignRandomGem()
    {
        var ran = Utility.RandomMinMax(1, 9);
        GemType = (GemType)ran;
    }
}

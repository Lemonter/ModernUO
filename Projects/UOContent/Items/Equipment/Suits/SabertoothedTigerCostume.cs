using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Suits/SabertoothedTigerCostume.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class SabertoothedTigerCostume : BaseCostume
{
    public override string CreatureName => "saber-toothed tiger";

    [Constructible]
    public SabertoothedTigerCostume() : base()
    {
        CostumeBody = 0x588;
    }

    public override string DefaultName => "a saber-toothed tiger costume";
}

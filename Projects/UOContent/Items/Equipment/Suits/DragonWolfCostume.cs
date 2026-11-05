using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Suits/DragonWolfCostume.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class DragonWolfCostume : BaseCostume
{
    public override string CreatureName => "dragon wolf";

    [Constructible]
    public DragonWolfCostume() : base()
    {
        CostumeBody = 719;
    }

    public override string DefaultName => "a dragon wolf costume";
}

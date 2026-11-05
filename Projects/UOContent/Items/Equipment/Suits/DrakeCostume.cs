using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Suits/DrakeCostume.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class DrakeCostume : BaseCostume
{
    public override string CreatureName => "drake";

    [Constructible]
    public DrakeCostume() : base( )
    {
        this.CostumeBody = 60;
    }

    public override int LabelNumber => 1114245; // drake costume
}

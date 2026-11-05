using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Suits/CyclopsCostume.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class CyclopsCostume : BaseCostume
{
    public override string CreatureName => "cyclops";

    [Constructible]
    public CyclopsCostume() : base()
    {
        this.CostumeBody = 75;
    }

    public override int LabelNumber => 1114234; // cyclops costume
}

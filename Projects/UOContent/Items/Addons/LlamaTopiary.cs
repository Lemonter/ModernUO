using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/LlamaTopiary.cs).
/// </summary>
[Flippable(0x491B, 0x4930)]
[SerializationGenerator(0, false)]
public partial class LlamaTopiary : Item
{
    public override int LabelNumber => 1070878; // a decorative topiary

    [Constructible]
    public LlamaTopiary() : base(0x491B)
    {
        this.Weight = 1.0;
        this.Name = ("a llama topiary");
    }
}

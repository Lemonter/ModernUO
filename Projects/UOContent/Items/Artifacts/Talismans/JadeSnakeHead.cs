using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Talismans/JadeSnakeHead.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class JadeSnakeHead : BaseTalisman
{
    public override int LabelNumber => 1115647; // Jade Snake Head

    public override int PoisonResistance => 3;

    [Constructible]
    public JadeSnakeHead()
        : base(0x2F59)
    {
        Hue = 0x48A;
        Weight = 1.0;
        Attributes.RegenStam = 2;
        Attributes.LowerManaCost = 5;
    }
}

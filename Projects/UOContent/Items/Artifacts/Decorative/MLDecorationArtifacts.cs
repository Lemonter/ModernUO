using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Decorative/MLDecorationArtifacts.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class AncientShipModelOfTheHMSCape : Item
{
    public override int LabelNumber => 1063476;

    [Constructible]
    public AncientShipModelOfTheHMSCape()
        : base(0x14F3)
    {
        Name = "древняя модель корабля «Мыс»";
        Hue = 0x37B;
    }
}

using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Decorative/CoffinPiece.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class CoffinPiece : Item
{
    public override int LabelNumber => 1116783;

    [Constructible]
    public CoffinPiece() : base(Utility.RandomList(7481, 7480, 7479, 7452, 7451, 7450))
    {
    }
}

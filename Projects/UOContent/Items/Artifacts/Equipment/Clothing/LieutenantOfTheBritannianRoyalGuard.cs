using ModernUO.Serialization;
using System;
using Server.Engines.Craft;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Clothing/LieutenantOfTheBritannianRoyalGuard.cs).
///     GargishLieutenantOfTheBritannianRoyalGuard: GargishSash has no counterpart here; re-based on BaseClothing with its ServUO art/stat line (`0x0405, Layer.MiddleTorso`).
/// </summary>
[SerializationGenerator(0, false)]
public partial class GargishLieutenantOfTheBritannianRoyalGuard : BaseClothing
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    [Constructible]
    public GargishLieutenantOfTheBritannianRoyalGuard() : base(0x0405, Layer.MiddleTorso)
    {
        Hue = 0xe8;
        Attributes.BonusInt = 5;
        Attributes.RegenMana = 2;
        Attributes.LowerRegCost = 10;
    }

    public override int LabelNumber => 1094910; // Lieutenant of the Britannian Royal Guard [Replica]

    public override int InitMinHits => 150;

    public override int InitMaxHits => 150;

    public override bool CanFortify => false;
}

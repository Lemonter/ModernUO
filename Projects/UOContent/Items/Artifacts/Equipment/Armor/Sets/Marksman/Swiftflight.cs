using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Marksman/Swiftflight.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class Swiftflight : Bow
{
    [Constructible]
    public Swiftflight()
        : base()
    {
        this.Attributes.WeaponDamage = 40;

    }

    public override int LabelNumber => 1074308; // Swiftflight (Marksman Set)
}

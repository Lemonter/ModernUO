using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Marksman/Feathernock.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class Feathernock : BaseQuiver
{
    [Constructible]
    public Feathernock()
        : base()
    {
        this.Attributes.WeaponDamage = 10;
        this.WeightReduction = 30;

    }

    public override int LabelNumber => 1074324; // Feathernock (Marksman Set)
}

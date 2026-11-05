using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Juggernaut/Evocaricus.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class Evocaricus : VikingSword
{
    [Constructible]
    public Evocaricus()
        : base()
    {
        this.Attributes.WeaponDamage = 50;

    }

    public override int LabelNumber => 1074309; // Evocaricus (Juggernaut Set)
}

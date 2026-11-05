using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Weapons/IronwoodCompositeBow.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class IronwoodCompositeBow : CompositeBow
{
    public override int LabelNumber => 1113759; // Ironwood Composite Bow

    [Constructible]
    public IronwoodCompositeBow()
        : base()
    {
        Hue = 1410;
        Slayer = SlayerName.Fey;
        WeaponAttributes.HitFireball = 40;
        WeaponAttributes.HitLowerDefend = 30;
        Attributes.BonusDex = 5;
        Attributes.WeaponSpeed = 25;
        Attributes.WeaponDamage = 45;
        Velocity = 30;
    }

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}

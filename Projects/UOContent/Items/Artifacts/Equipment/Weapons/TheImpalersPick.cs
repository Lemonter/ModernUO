using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Weapons/TheImpalersPick.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class TheImpalersPick : HammerPick
{
    public override int LabelNumber => 1113822; // The Impaler's Pick

    [Constructible]
    public TheImpalersPick()
    {
        Hue = 2101;
        Slayer = SlayerName.Repond;
        WeaponAttributes.HitLightning = 40;
        WeaponAttributes.HitLowerDefend = 40;
        Attributes.WeaponSpeed = 30;
        Attributes.WeaponDamage = 45;
    }

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}

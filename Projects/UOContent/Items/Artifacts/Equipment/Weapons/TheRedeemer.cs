using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Weapons/TheRedeemer.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class TheRedeemer : PaladinSword
{
    public override int LabelNumber => 1077442; // The Redeemer

    public override int ArtifactRarity => 7;

    public override int InitMinHits => 100;

    public override int InitMaxHits => 100;

    [Constructible]
    public TheRedeemer()
    {
        Hue = 2304;
        Slayer = SlayerName.Silver;
        Slayer2 = SlayerName.Exorcism;
        Attributes.WeaponDamage = 55;
    }

    public override bool CanFortify => false;
}

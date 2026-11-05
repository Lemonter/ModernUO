using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Weapons/LightInTheVoid.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class LightInTheVoid : GargishTalwar
{
    public override int LabelNumber => 1113521; // Light in the Void

    [Constructible]
    public LightInTheVoid()
    {
        Slayer = SlayerName.Silver;
        WeaponAttributes.HitLightning = 45;
        WeaponAttributes.HitLowerDefend = 30;
        Attributes.BonusStr = 8;
        Attributes.AttackChance = 10;
        Attributes.CastSpeed = 1;
        Attributes.WeaponSpeed = 20;
        Attributes.WeaponDamage = 35;
        Hue = 1072; //Hue not exact
    }

    public override int ArtifactRarity => 5;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}

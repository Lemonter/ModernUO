using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Weapons/KatrinasCrook.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class KatrinasCrook : ShepherdsCrook
{
    [Constructible]
    public KatrinasCrook()
        : base()
    {
        WeaponAttributes.HitLeechStam = 40;
        WeaponAttributes.HitLeechMana = 55;
        WeaponAttributes.HitLeechHits = 55;
        Attributes.WeaponDamage = 60;
        Attributes.DefendChance = 15;
    }

    public override int LabelNumber => 1079789; // Katrina's Crook

    public override int ArtifactRarity => 11;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}

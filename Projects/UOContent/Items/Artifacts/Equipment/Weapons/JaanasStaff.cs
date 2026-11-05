using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Weapons/JaanasStaff.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class JaanasStaff : GnarledStaff
{
    [Constructible]
    public JaanasStaff()
        : base()
    {
        Hue = 0x58C;
        WeaponAttributes.MageWeapon = 10;
        Attributes.SpellChanneling = 1;
        Attributes.Luck = 220;
        Attributes.DefendChance = 15;
    }

    public override int LabelNumber => 1079790; // Jaana's Staff

    public override int ArtifactRarity => 11;

    public override int InitMinHits => 225;

    public override int InitMaxHits => 225;
}

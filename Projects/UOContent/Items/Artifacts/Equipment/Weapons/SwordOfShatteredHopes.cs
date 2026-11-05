using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Weapons/SwordOfShatteredHopes.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class SwordOfShatteredHopes : GlassSword
{
    public override int LabelNumber => 1112770; // Sword of Shattered Hopes

    [Constructible]
    public SwordOfShatteredHopes()
        : base()
    {
        Hue = 91;
        WeaponAttributes.HitDispel = 25;
        Attributes.WeaponSpeed = 30;
        Attributes.WeaponDamage = 50;
        WeaponAttributes.ResistFireBonus = 15;
    }

    public override int ArtifactRarity => 10;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}

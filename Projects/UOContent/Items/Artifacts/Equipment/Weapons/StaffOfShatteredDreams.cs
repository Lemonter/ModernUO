using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Weapons/StaffOfShatteredDreams.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class StaffOfShatteredDreams : GlassStaff
{
    public override int LabelNumber => 1112771; // Staff of Shattered Dreams

    [Constructible]
    public StaffOfShatteredDreams()
        : base()
    {
        Hue = 1151;
        WeaponAttributes.HitDispel = 25;
        Attributes.WeaponDamage = 50;
        WeaponAttributes.ResistFireBonus = 15;
        Attributes.CastSpeed = -1;
        Attributes.SpellChanneling = 1;
    }

    public override int ArtifactRarity => 11;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}

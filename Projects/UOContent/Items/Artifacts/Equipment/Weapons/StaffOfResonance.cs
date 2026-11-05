using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Weapons/StaffOfResonance.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class StaffOfResonance : GlassStaff
{
    public override int LabelNumber => 1113527; // Staff of Resonance

    [Constructible]
    public StaffOfResonance()
    {
        switch (Utility.Random(5)) // Random resonance property
        {
        }
        Attributes.SpellChanneling = 1;
        WeaponAttributes.MageWeapon = 10;
        WeaponAttributes.HitHarm = 50;
        Attributes.DefendChance = 10;
        Attributes.WeaponSpeed = 20;
        Attributes.WeaponDamage = -40;
        Attributes.LowerManaCost = 5;
        AosElementDamages.Poison = 100;
        Hue = 1451; //Hue not exact
    }

    public override int ArtifactRarity => 5;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}

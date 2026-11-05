using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Weapons/DraconisWrath.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class DraconisWrath : Katana
{
    public override int LabelNumber => 1114789; // Draconi's Wrath

    [Constructible]
    public DraconisWrath()
    {
        Hue = 1177;
        WeaponAttributes.HitFireball = 60;
        Attributes.AttackChance = 15;
        Attributes.WeaponDamage = 50;
        WeaponAttributes.UseBestSkill = 1;
    }

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}

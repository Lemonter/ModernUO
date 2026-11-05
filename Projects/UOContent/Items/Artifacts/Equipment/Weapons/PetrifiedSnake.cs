using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Weapons/PetrifiedSnake.cs).
///     PetrifiedSnake: SerpentStoneStaff has no counterpart here; re-based on SerpentstoneStaff.
/// </summary>
[SerializationGenerator(0, false)]
public partial class PetrifiedSnake : SerpentstoneStaff
{
    public override int LabelNumber => 1113528; // Petrified Snake

    [Constructible]
    public PetrifiedSnake()
        : base()
    {
        Hue = 460;
        Slayer = SlayerName.ReptilianDeath;
        WeaponAttributes.HitMagicArrow = 30;
        WeaponAttributes.HitLowerDefend = 30;
        Attributes.WeaponSpeed = 30;
        Attributes.WeaponDamage = 50;
        AosElementDamages.Poison = 100;
        WeaponAttributes.ResistPoisonBonus = 10;
    }

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}

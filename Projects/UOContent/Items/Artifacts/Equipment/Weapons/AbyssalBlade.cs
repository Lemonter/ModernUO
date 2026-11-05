using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Weapons/AbyssalBlade.cs).
///     AbyssalBlade: StoneWarSword has no counterpart here; re-based on BaseSword with its ServUO art/stat line (`0x900`).
/// </summary>
[SerializationGenerator(0, false)]
public partial class AbyssalBlade : BaseSword
{
    public override double DefaultWeight => 6.0;

    public override int LabelNumber => 1113520; // Abyssal Blade

    [Constructible]
    public AbyssalBlade() : base(0x900)
    {
        Hue = 2404;
        WeaponAttributes.HitLeechHits = 60;
        WeaponAttributes.HitLeechStam = 60;
        WeaponAttributes.HitLeechMana = 60;
        Attributes.WeaponSpeed = 20;
        Attributes.WeaponDamage = 60;
        AosElementDamages.Chaos = 100;
    }

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}

using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Weapons/ChannelersDefender.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class ChannelersDefender : GlassSword
{
    public override int LabelNumber => 1113518; // Channeler's Defender

    [Constructible]
    public ChannelersDefender()
    {
        Hue = 95;
        Attributes.DefendChance = 10;
        Attributes.AttackChance = 5;
        Attributes.LowerManaCost = 5;
        Attributes.WeaponSpeed = 20;
        Attributes.CastRecovery = 1;
        Attributes.SpellChanneling = 1;
        WeaponAttributes.HitLowerAttack = 60;
        AosElementDamages.Energy = 100;
    }

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}

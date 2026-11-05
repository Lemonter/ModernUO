using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/CastOffZombieSkin.cs).
///     CastOffZombieSkin: GargishLeatherArms has no counterpart here; re-based on GargishLeatherArmsType1.
/// </summary>
[SerializationGenerator(0, false)]
public partial class CastOffZombieSkin : GargishLeatherArmsType1
{
    [Constructible]
    public CastOffZombieSkin()
    {
        Hue = 1893;
        SkillBonuses.SetValues(0, SkillName.Necromancy, 5.0);
        SkillBonuses.SetValues(1, SkillName.SpiritSpeak, 5.0);
        Attributes.LowerManaCost = 5;
        Attributes.LowerRegCost = 8;
        Attributes.IncreasedKarmaLoss = 5;
    }

    public override int LabelNumber => 1113538; // Cast-off Zombie Skin

    public override int BasePhysicalResistance => 13;

    public override int BaseFireResistance => -2;

    public override int BaseColdResistance => 17;

    public override int BasePoisonResistance => 18;

    public override int BaseEnergyResistance => 6;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}

using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Assassin/AssassinArms.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class AssassinArms : LeatherArms
{
    [Constructible]
    public AssassinArms()
        : base()
    {
        this.Attributes.BonusStam = 2;
        this.Attributes.WeaponSpeed = 5;

    }

    public override int LabelNumber => 1074304; // Assassin Armor

    public override int BasePhysicalResistance => 9;

    public override int BaseFireResistance => 6;

    public override int BaseColdResistance => 3;

    public override int BasePoisonResistance => 8;

    public override int BaseEnergyResistance => 4;
}

using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Armor/AssassinsCowl.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class AssassinsCowl : BaseHat
{
    public override int LabelNumber => 1126024; // assassin's cowl

    public override int BasePhysicalResistance => 2;
    public override int BaseFireResistance => 4;
    public override int BaseColdResistance => 4;
    public override int BasePoisonResistance => 3;
    public override int BaseEnergyResistance => 2;

    public override int InitMinHits => 40;
    public override int InitMaxHits => 60;

    [Constructible]
    public AssassinsCowl()
        : this(0)
    {
    }

    public AssassinsCowl(int hue)
        : base(0xA410, hue)
    {
        Weight = 3.0;
        StrRequirement = 45;
    }
}

using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/HumanSignOfOrder.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class HumanSignOfOrder : OrderShield
{
    [Constructible]
    public HumanSignOfOrder()
        : base()
    {
        SkillBonuses.SetValues(0, SkillName.Chivalry, 10.0);
        Attributes.AttackChance = 5;
        Attributes.DefendChance = 10;
        Attributes.CastSpeed = 1;
        Attributes.CastRecovery = 1;
    }

    public override int LabelNumber => 1113534; // Sign of Order

    public override int BasePhysicalResistance => 1;

    public override int BaseFireResistance => 0;

    public override int BaseColdResistance => 5;

    public override int BasePoisonResistance => 5;

    public override int BaseEnergyResistance => 0;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}

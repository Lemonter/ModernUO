using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/MuseumArmor.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class MinaxsArmor : FemaleStuddedChest
{
    [Constructible]
    public MinaxsArmor()
        : base()
    {
        Hue = 0x453;
        Attributes.RegenMana = 2;
        ArmorAttributes.MageArmor = 1;
    }

    public override int LabelNumber => 1073257; // Minax's Armor - Museum of Vesper Replica

    public override int BasePhysicalResistance => 15;

    public override int BaseFireResistance => 5;

    public override int BaseColdResistance => 10;

    public override int BasePoisonResistance => 15;

    public override int BaseEnergyResistance => 25;

    public override int InitMinHits => 150;

    public override int InitMaxHits => 150;
}

[SerializationGenerator(0, false)]
public partial class KeeoneansChainMail : ChainChest
{
    [Constructible]
    public KeeoneansChainMail()
        : base()
    {
        Hue = 0x84E;
        Attributes.RegenHits = 3;
        Attributes.NightSight = 1;
        ArmorAttributes.MageArmor = 1;
    }

    public override int LabelNumber => 1073264; // Keeonean's Chain Mail - Museum of Vesper Replica

    public override int BasePhysicalResistance => 15;

    public override int BaseFireResistance => 20;

    public override int BaseColdResistance => 15;

    public override int BasePoisonResistance => 10;

    public override int BaseEnergyResistance => 15;

    public override int InitMinHits => 150;

    public override int InitMaxHits => 150;
}

[SerializationGenerator(0, false)]
public partial class VesperOrderShield : OrderShield
{
    [Constructible]
    public VesperOrderShield()
        : base()
    {
        Hue = 0x835;
        Attributes.SpellChanneling = 1;
        Attributes.Luck = 80;
        Attributes.CastSpeed = -1;
        Attributes.AttackChance = 15;
        Attributes.DefendChance = 15;
    }

    public override int LabelNumber => 1073258; // Order Shield - Museum of Vesper Replica

    public override int BasePhysicalResistance => 1;

    public override int InitMinHits => 80;

    public override int InitMaxHits => 80;
}

[SerializationGenerator(0, false)]
public partial class VesperChaosShield : ChaosShield
{
    [Constructible]
    public VesperChaosShield()
        : base()
    {
        Hue = 0xFA;
        Attributes.SpellChanneling = 1;
        Attributes.CastRecovery = 2;
        Attributes.CastSpeed = 1;
        ArmorAttributes.SelfRepair = 1;
    }

    public override int LabelNumber => 1073259; // Chaos Shield - Museum of Vesper Replica

    public override int BasePhysicalResistance => 1;

    public override int InitMinHits => 80;

    public override int InitMaxHits => 80;
}

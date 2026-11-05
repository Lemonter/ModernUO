using ModernUO.Serialization;
using System;
using System.Collections.Generic;
using Server.Engines.Craft;
using Server.Network;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Clothing/Hats.cs).
/// </summary>
#region Reward Clothing
[SerializationGenerator(0, false)]
public partial class ZooMemberBonnet : Bonnet
{
    public override int LabelNumber => 1073221; // Britannia Royal Zoo Member

    [Constructible]
    public ZooMemberBonnet()
        : this(0)
    {
    }

    public ZooMemberBonnet(int hue)
        : base(hue)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class ZooMemberFloppyHat : FloppyHat
{
    public override int LabelNumber => 1073221; // Britannia Royal Zoo Member

    [Constructible]
    public ZooMemberFloppyHat()
        : this(0)
    {
    }

    public ZooMemberFloppyHat(int hue)
        : base(hue)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class LibraryFriendFeatheredHat : FeatheredHat
{
    public override int LabelNumber => 1073347; // Friends of the Library Feathered Hat

    [Constructible]
    public LibraryFriendFeatheredHat()
        : this(0)
    {
    }

    public LibraryFriendFeatheredHat(int hue)
        : base(hue)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class NystulsWizardsHat : WizardsHat
{
    public override int LabelNumber => 1073255; // Nystul's Wizard's Hat - Museum of Vesper Replica

    public override int BasePhysicalResistance => 10;
    public override int BaseFireResistance => 10;
    public override int BaseColdResistance => 10;
    public override int BasePoisonResistance => 10;
    public override int BaseEnergyResistance => 25;

    public override int InitMinHits => 100;
    public override int InitMaxHits => 100;

    [Constructible]
    public NystulsWizardsHat()
        : this(0)
    {
    }

    public NystulsWizardsHat(int hue)
        : base(hue)
    {
        Attributes.LowerManaCost = 15;
    }
}

[SerializationGenerator(0, false)]
public partial class GypsyHeaddress : SkullCap
{
    public override int LabelNumber => 1073254; // Gypsy Headdress - Museum of Vesper Replica

    public override int BasePhysicalResistance => 15;
    public override int BaseFireResistance => 20;
    public override int BaseColdResistance => 20;
    public override int BasePoisonResistance => 15;
    public override int BaseEnergyResistance => 15;

    public override int InitMinHits => 100;
    public override int InitMaxHits => 100;

    [Constructible]
    public GypsyHeaddress()
        : this(0)
    {
    }

    public GypsyHeaddress(int hue)
        : base(hue)
    {
    }
}
#endregion

[SerializationGenerator(0, false)]
public partial class OrcMask : BaseHat
{
    public CraftSystem RepairSystem => DefTailoring.CraftSystem;

    public override int BasePhysicalResistance => 1;
    public override int BaseFireResistance => 1;
    public override int BaseColdResistance => 7;
    public override int BasePoisonResistance => 7;
    public override int BaseEnergyResistance => 8;

    public override int InitMinHits => 20;
    public override int InitMaxHits => 30;

    public override bool Dye(Mobile from, DyeTub sender)
    {
        from.SendLocalizedMessage(sender.FailMessage);
        return false;
    }

    public override int LabelNumber
    {
        get
        {
            return 1025147; // orc mask
        }
    }

    [Constructible]
    public OrcMask()
        : base(0x141B)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class ChefsToque : BaseHat
{
    public override int LabelNumber => 1109618; // Chef's Toque

    public override int BasePhysicalResistance => 0;
    public override int BaseFireResistance => 5;
    public override int BaseColdResistance => 9;
    public override int BasePoisonResistance => 5;
    public override int BaseEnergyResistance => 5;

    public override int InitMinHits => 20;
    public override int InitMaxHits => 30;

    [Constructible]
    public ChefsToque()
        : this(0)
    {
    }

    public ChefsToque(int hue)
        : base(0x781A, hue)
    {
    }
}

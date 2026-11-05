using ModernUO.Serialization;
using System;
using Server.Engines.VeteranRewards;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Clothing/OuterTorso.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class ZooMemberSkirt : PlainDress
{
    public override int LabelNumber => 1073221; // Britannia Royal Zoo Member

    [Constructible]
    public ZooMemberSkirt()
        : this(0)
    {
    }

    public ZooMemberSkirt(int hue)
        : base(hue)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class ZooMemberBodySash : BodySash
{
    public override int LabelNumber => 1073221; // Britannia Royal Zoo Member

    [Constructible]
    public ZooMemberBodySash()
        : this(0)
    {
    }

    public ZooMemberBodySash(int hue)
        : base(hue)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class ZooMemberRobe : Robe
{
    public override int LabelNumber => 1073221; // Britannia Royal Zoo Member

    [Constructible]
    public ZooMemberRobe()
        : this(0)
    {
    }

    public ZooMemberRobe(int hue)
        : base(hue)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class ZooMemberCloak : Cloak
{
    public override int LabelNumber => 1073221; // Britannia Royal Zoo Member

    [Constructible]
    public ZooMemberCloak()
        : this(0)
    {
    }

    public ZooMemberCloak(int hue)
        : base(hue)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class LibraryFriendBodySash : BodySash
{
    public override int LabelNumber => 1073346; // Friends of the Library Sash

    [Constructible]
    public LibraryFriendBodySash()
        : this(0)
    {
    }

    public LibraryFriendBodySash(int hue)
        : base(hue)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class LibraryFriendDoublet : Doublet
{
    public override int LabelNumber => 1073351; // Friends of the Library Doublet

    [Constructible]
    public LibraryFriendDoublet()
        : this(0)
    {
    }

    public LibraryFriendDoublet(int hue)
        : base(hue)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class LibraryFriendSurcoat : Surcoat
{
    public override int LabelNumber => 1073348; // Friends of the Library Surcoat

    [Constructible]
    public LibraryFriendSurcoat()
        : this(0)
    {
    }

    public LibraryFriendSurcoat(int hue)
        : base(hue)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class LibraryFriendCloak : Cloak
{
    public override int LabelNumber => 1073350; // Friends of the Library Cloak

    [Constructible]
    public LibraryFriendCloak()
        : this(0)
    {
    }

    public LibraryFriendCloak(int hue)
        : base(hue)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class Adranath : BodySash
{
    public override int LabelNumber => 1073253; // Adranath - Museum of Vesper Replica

    [Constructible]
    public Adranath()
        : this(0)
    {
    }

    public Adranath(int hue)
        : base(hue)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class OdricsRobe : Robe
{
    public override int LabelNumber => 1073250; // Odric's Robe - Museum of Vesper Replica

    [Constructible]
    public OdricsRobe()
        : this(0)
    {
    }

    public OdricsRobe(int hue)
        : base(hue)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class BaronLenshiresCloak : Cloak
{
    public override int LabelNumber => 1073252; // Baron Lenshire's Cloak - Museum of Vesper Replica

    [Constructible]
    public BaronLenshiresCloak()
        : this(0)
    {
    }

    public BaronLenshiresCloak(int hue)
        : base(hue)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class RewardGargishRobe : BaseOuterTorso, IRewardItem
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    private int m_LabelNumber;
    private bool m_IsRewardItem;

    [CommandProperty(AccessLevel.GameMaster)]
    public bool IsRewardItem { get { return m_IsRewardItem; } set { m_IsRewardItem = value; } }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Number
    {
        get { return m_LabelNumber; }
        set
        {
            m_LabelNumber = value;
            InvalidateProperties();
        }
    }

    public override int LabelNumber
    {
        get
        {
            if (m_LabelNumber > 0)
                return m_LabelNumber;

            return base.LabelNumber;
        }
    }

    public override int BasePhysicalResistance => 3;

    public override bool Dye(Mobile from, DyeTub sender)
    {
        from.SendLocalizedMessage(sender.FailMessage);
        return false;
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (m_IsRewardItem)
            list.Add(RewardSystem.GetRewardYearLabel(this, new object[] { Hue, m_LabelNumber })); // X Year Veteran Reward
    }

    public override bool CanEquip(Mobile m)
    {
        if (!base.CanEquip(m))
            return false;

        return !m_IsRewardItem || RewardSystem.CheckIsUsableBy(m, this, new object[] { Hue, m_LabelNumber });
    }

    [Constructible]
    public RewardGargishRobe()
        : this(0)
    {
    }

    public RewardGargishRobe(int hue)
        : this(hue, 0)
    {
    }

    public RewardGargishRobe(int hue, int labelNumber)
        : base(0x4000, hue)
    {
        Weight = 3.0;
        LootType = LootType.Blessed;

        m_LabelNumber = labelNumber;
    }
}

[SerializationGenerator(0, false)]
public partial class RewardGargishFancyRobe : BaseOuterTorso, IRewardItem
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    private int m_LabelNumber;
    private bool m_IsRewardItem;

    [CommandProperty(AccessLevel.GameMaster)]
    public bool IsRewardItem { get { return m_IsRewardItem; } set { m_IsRewardItem = value; } }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Number
    {
        get { return m_LabelNumber; }
        set
        {
            m_LabelNumber = value;
            InvalidateProperties();
        }
    }

    public override int LabelNumber
    {
        get
        {
            if (m_LabelNumber > 0)
                return m_LabelNumber;

            return base.LabelNumber;
        }
    }

    public override int BasePhysicalResistance => 3;

    public override bool Dye(Mobile from, DyeTub sender)
    {
        from.SendLocalizedMessage(sender.FailMessage);
        return false;
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (m_IsRewardItem)
            list.Add(RewardSystem.GetRewardYearLabel(this, new object[] { Hue, m_LabelNumber })); // X Year Veteran Reward
    }

    public override bool CanEquip(Mobile m)
    {
        if (!base.CanEquip(m))
            return false;

        return !m_IsRewardItem || RewardSystem.CheckIsUsableBy(m, this, new object[] { Hue, m_LabelNumber });
    }

    [Constructible]
    public RewardGargishFancyRobe()
        : this(0)
    {
    }

    public RewardGargishFancyRobe(int hue)
        : this(hue, 0)
    {
    }

    public RewardGargishFancyRobe(int hue, int labelNumber)
        : base(0x4002, hue)
    {
        Weight = 3.0;
        LootType = LootType.Blessed;

        m_LabelNumber = labelNumber;
    }
}

[SerializationGenerator(0, false)]
public partial class FloweredDress : BaseOuterTorso
{
    public override int LabelNumber => 1109622; // Flowered Dress

    [Constructible]
    public FloweredDress()
        : this(0)
    {
    }

    public FloweredDress(int hue)
        : base(0x781E, hue)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class EveningGown : BaseOuterTorso
{
    public override int LabelNumber => 1109625; // Evening Gown

    [Constructible]
    public EveningGown()
        : this(0)
    {
    }

    public EveningGown(int hue)
        : base(0x7821, hue)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class Epaulette : BaseClothing
{
    public override int LabelNumber => 1123325; // Epaulette

    [Constructible]
    public Epaulette()
        : this(0)
    {
    }

    public Epaulette(int hue)
        : base(0x9985, Layer.OuterTorso, hue)
    {
        Weight = 1.0;
    }
}

[SerializationGenerator(0, false)]
public partial class GargishEpaulette : BaseClothing
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    public override int LabelNumber => 1123326; // Gargish Epaulette

    [Constructible]
    public GargishEpaulette()
        : this(0)
    {
    }

    public GargishEpaulette(int hue)
        : base(0x9986, Layer.OuterTorso, hue)
    {
        Weight = 1.0;
    }
}

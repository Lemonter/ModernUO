using ModernUO.Serialization;
using System;
using Server.Engines.Craft;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Clothing/Shoes.cs).
/// </summary>
#region Reward Clothing
[SerializationGenerator(0, false)]
public partial class ZooMemberThighBoots : ThighBoots
{
    public override int LabelNumber => 1073221; // Britannia Royal Zoo Member

    [Constructible]
    public ZooMemberThighBoots()
        : this(0)
    {
    }

    public ZooMemberThighBoots(int hue)
        : base(hue)
    {
    }

    public override bool Dye(Mobile from, DyeTub sender)
    {
        from.SendLocalizedMessage(sender.FailMessage);
        return false;
    }
}

#endregion

[SerializationGenerator(0, false)]
public partial class JesterShoes : BaseShoes
{
    public override int LabelNumber => 1109617; // Jester Shoes

    [Constructible]
    public JesterShoes()
        : this(0)
    {
    }

    public JesterShoes(int hue)
        : base(0x7819, hue)
    {
    }
}

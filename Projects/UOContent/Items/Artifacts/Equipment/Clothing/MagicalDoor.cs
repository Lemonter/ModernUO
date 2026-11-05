using ModernUO.Serialization;
using System;
using Server.Network;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Clothing/MagicalDoor.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class MagicalDoor : Item
{
    [Constructible]
    public MagicalDoor() : this(Utility.RandomList(7905, 7914, 7923, 7932))
    {
    }

    public MagicalDoor(int id) : base( id )
    {
    }

    public override int LabelNumber => 1112410; //Magical Door [Replica]

    public override int ItemID
    {
        get
        {
            if (Parent == null && Map != Map.Internal)
            {
                return base.ItemID - 8;
            }

            return base.ItemID;
        }
        set
        {
            base.ItemID = value;
        }
    }
}

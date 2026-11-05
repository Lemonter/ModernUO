using Server.Mobiles;
using ModernUO.Serialization;
using Server.Engines.ExploringTheDeep;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Items/Decorate/IceCrystals.cs). The decorative
///     frost crystals around the Ice Dungeon lair, and the door of Cousteau Perron's house, which only
///     opens once the Salvage Master has sent the player looking for the tinker.
/// </summary>
[SerializationGenerator(0, false)]
public partial class IceCrystals : Item
{
    private static readonly int[] _itemIDs =
    {
        0x2208, 0x221D, 0x2FDC, 0x2228, 0x2209, 0x2FDD, 0x2FDC
    };

    [Constructible]
    public IceCrystals() : base(Utility.RandomList(_itemIDs))
    {
        Hue = 2729;
        Movable = false;
    }

    public override int LabelNumber => 1028710; // crystal
}

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Items/Decorate/CusteauPerronHouseDoor.cs).
///     The original compared against the "HeplerPaulsonComplete" quest state; here the door opens for
///     anyone who has finished the Salvage Master's first errand, or who is already inside the house.
/// </summary>
[SerializationGenerator(0, false)]
public partial class CusteauPerronHouseDoor : DarkWoodDoor
{
    [Constructible]
    public CusteauPerronHouseDoor() : base(DoorFacing.WestCCW)
    {
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from is not PlayerMobile pm)
        {
            return;
        }

        if (ExploringDeepChain.SalvageMasterDone(pm) || from.Region?.Name == "Custeau Perron House")
        {
            base.OnDoubleClick(from);

            // *You knock on the door but there is no answer. You decide to let yourself in...*
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1154223);
        }
        else
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 502503); // That is locked.
        }
    }
}

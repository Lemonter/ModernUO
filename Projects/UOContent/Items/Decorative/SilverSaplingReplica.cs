using ModernUO.Serialization;
using Server.Gumps;
using Server.Network;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SilverSaplingReplica : Item
{
    [Constructible]
    public SilverSaplingReplica(int id) : base(id)
    {
        Weight = 5.0;
    }

    public override int LabelNumber => ItemID == 17095 ? 1095967 : 1095968;

    // silver sapling replica - potted silver sapling replica
}

[SerializationGenerator(0, false)]
public partial class SilverSaplingReplicaDeed : Item
{
    [Constructible]
    public SilverSaplingReplicaDeed() : base(0x14F0)
    {
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1113934; // Silver Sapling Deed

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042010); // You must have the object in your backpack to use it.
            return;
        }

        from.SendGump(new InternalGump(this));
    }

    private class InternalGump : Gump
    {
        private readonly Item _deed;

        public InternalGump(Item deed) : base(100, 180)
        {
            _deed = deed;

            AddPage(0);

            AddBackground(25, 0, 190, 200, 0xA28);
            AddLabel(70, 12, 0x3E3, "Выберите один:");

            AddPage(1);

            AddItem(62, 70, 0x42C7);
            AddButton(75, 50, 0x845, 0x846, 17095, GumpButtonType.Reply, 0);
            AddItem(123, 70, 0x42C8);
            AddButton(135, 50, 0x845, 0x846, 17096, GumpButtonType.Reply, 0);
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            if (_deed == null || _deed.Deleted || info.ButtonID == 0)
            {
                return;
            }

            var m = sender.Mobile;

            m.AddToBackpack(new SilverSaplingReplica(info.ButtonID));
            _deed.Delete();
        }
    }
}

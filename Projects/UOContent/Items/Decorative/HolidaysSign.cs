using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Gumps;
using Server.Multis;

namespace Server.Items;

[Flippable]
[SerializationGenerator(0, false)]
public partial class HolidaysSign : Item
{
    private static readonly Dictionary<int, string> _signText = new()
    {
        { 1673, "Счастливых праздников" },
        { 1674, "С Рождеством" },
        { 1675, "Праздничные поздравления" },
        { 1676, "С Ханукой" }
    };

    [SerializableField(0)]
    private int _gumpID;

    [Constructible]
    public HolidaysSign() : base(0xA130) => _gumpID = 1673;

    public override int LabelNumber => 1024759; // sign

    public void Flip()
    {
        if (ItemID is 0xA130 or 0xA131)
        {
            ItemID += 4;
        }
        else
        {
            ItemID -= 4;
        }
    }

    public override void OnDoubleClick(Mobile m)
    {
        if (!m.InRange(GetWorldLocation(), 3))
        {
            m.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            return;
        }

        var gump = new Gump(100, 100);
        gump.AddImage(0, 0, _gumpID);

        m.SendGump(gump);
    }

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        var house = BaseHouse.FindHouseAt(from);

        if (house == null || !house.IsCoOwner(from))
        {
            from.SendLocalizedMessage(502092); // You must be in your house to do this.
            return;
        }

        list.Add(new ChangeSignText(this, from));
        list.Add(new ToggleOnOff(this));
    }

    private class ChangeSignText : ContextMenuEntry
    {
        private readonly HolidaysSign _sign;

        public ChangeSignText(HolidaysSign sign, Mobile from) : base(1158829) // Change Sign Text
        {
            _sign = sign;
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            _sign.GumpID = _sign.GumpID == 1676 ? 1673 : _sign.GumpID + 1;

            if (_signText.TryGetValue(_sign.GumpID, out var text))
            {
                // The sign text has been set to: ~1_TEXT~
                from.SendLocalizedMessage(1158830, text);
            }
        }
    }

    private class ToggleOnOff : ContextMenuEntry
    {
        private readonly Item _sign;

        public ToggleOnOff(Item sign) : base(1155742) // Turn On/Off
        {
            _sign = sign;
        }

        public override void OnClick(Mobile from, IEntity target) =>
            _sign.ItemID = _sign.ItemID is 0xA130 or 0xA134 ? _sign.ItemID + 1 : _sign.ItemID - 1;
    }
}

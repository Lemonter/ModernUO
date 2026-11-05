using ModernUO.Serialization;
using Server.Gumps;
using Server.Network;

namespace Server.Items;

/// <summary>
///     Токен смены причёски, привязанный к аккаунту. Гумпы переведены на DynamicGump, потому
///     что набор вариантов зависит от расы и пола персонажа.
/// </summary>
[SerializationGenerator(0, false)]
public partial class HABPromotionalToken : Item, IAccountRestricted
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _account;

    [Constructible]
    public HABPromotionalToken(string account = null) : base(0x2AAA)
    {
        LootType = LootType.Blessed;
        Light = LightType.Circle300;
        Weight = 5.0;

        _account = account;
    }

    public override int LabelNumber => 1070997; // A promotional token

    public enum StyleType
    {
        Hair = 0,
        Beard = 1
    }

    public class ChangeHairstyleEntry
    {
        public ChangeHairstyleEntry(int gumpID, int tooltip, StyleType type, int itemID)
        {
            GumpID = gumpID;
            Tooltip = tooltip;
            Type = type;
            ItemID = itemID;
        }

        public int GumpID { get; }
        public int Tooltip { get; }
        public StyleType Type { get; }
        public int ItemID { get; }
    }

    public static readonly ChangeHairstyleEntry[] HumanMaleEntries =
    [
        new(0xC8F7, 1125404, StyleType.Hair, 0xA1A4),
        new(0xC8F8, 1125405, StyleType.Hair, 0xA1A5),
        new(0xC8F9, 1125406, StyleType.Hair, 0xA1A6),
        new(0xC8FA, 1125407, StyleType.Hair, 0xA1A7),
        new(0xC8FB, 1125408, StyleType.Beard, 0xA1A8),
        new(0xC8FC, 1125409, StyleType.Beard, 0xA1A9),
        new(0xC8FD, 1125410, StyleType.Beard, 0xA1AA),
        new(0xC8FE, 1125411, StyleType.Beard, 0xA1AB)
    ];

    public static readonly ChangeHairstyleEntry[] HumanFemaleEntries =
    [
        new(0xF00F, 1125412, StyleType.Hair, 0xA1AC),
        new(0xF010, 1125413, StyleType.Hair, 0xA1AD),
        new(0xF011, 1125414, StyleType.Hair, 0xA1AE),
        new(0xF012, 1125415, StyleType.Hair, 0xA1AF)
    ];

    public static readonly ChangeHairstyleEntry[] ElfMaleEntries =
    [
        new(0xC903, 1125416, StyleType.Hair, 0xA1B0),
        new(0xC904, 1125417, StyleType.Hair, 0xA1B1),
        new(0xC905, 1125418, StyleType.Hair, 0xA1B2),
        new(0xC906, 1125419, StyleType.Hair, 0xA1B3)
    ];

    public static readonly ChangeHairstyleEntry[] ElfFemaleEntries =
    [
        new(0xF017, 1125420, StyleType.Hair, 0xA1B4),
        new(0xF018, 1125421, StyleType.Hair, 0xA1B5),
        new(0xF019, 1125422, StyleType.Hair, 0xA1B6),
        new(0xF01A, 1125423, StyleType.Hair, 0xA1B7)
    ];

    public static readonly ChangeHairstyleEntry[] GargoyleMaleEntries =
    [
        new(0xC90B, 1125424, StyleType.Hair, 0xA1B8),
        new(0xC90C, 1125425, StyleType.Hair, 0xA1B9),
        new(0xC90D, 1125426, StyleType.Hair, 0xA1BA),
        new(0xC90E, 1125427, StyleType.Hair, 0xA1BB),
        new(0xC90F, 1125428, StyleType.Beard, 0xA1BC),
        new(0xC910, 1125429, StyleType.Beard, 0xA1BD),
        new(0xC911, 1125430, StyleType.Beard, 0xA1BE),
        new(0xC912, 1125431, StyleType.Beard, 0xA1BF)
    ];

    public static readonly ChangeHairstyleEntry[] GargoyleFemaleEntries =
    [
        new(0xF023, 1125432, StyleType.Hair, 0xA1C0),
        new(0xF024, 1125433, StyleType.Hair, 0xA1C1),
        new(0xF025, 1125434, StyleType.Hair, 0xA1C2),
        new(0xF026, 1125435, StyleType.Hair, 0xA1C3)
    ];

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1070998, $"#{1158657}"); // Use this to redeem<br>your ~1_PROMO~
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
        }
        else if (_account == null)
        {
            // You cannot redeem this promotional hairstyle change token right now.
            from.SendLocalizedMessage(1158650);
        }
        else if (from.Account.Username != _account)
        {
            from.SendLocalizedMessage(1116257); // This token does not belong to this character.
        }
        else
        {
            from.SendGump(new TokenGump(this));
        }
    }

    private static ChangeHairstyleEntry[] GetEntries(Mobile m)
    {
        if (m.Race == Race.Elf)
        {
            return m.Female ? ElfFemaleEntries : ElfMaleEntries;
        }

        if (m.Race == Race.Gargoyle)
        {
            return m.Female ? GargoyleFemaleEntries : GargoyleMaleEntries;
        }

        return m.Female ? HumanFemaleEntries : HumanMaleEntries;
    }

    private class TokenGump : StaticGump<TokenGump>
    {
        private readonly Item _token;

        public override bool Singleton => true;

        public TokenGump(Item token) : base(170, 150) => _token = token;

        protected override void BuildLayout(ref StaticGumpBuilder builder)
        {
            builder.AddPage();

            builder.AddBackground(0, 0, 291, 99, 0x13BE);
            builder.AddImageTiled(5, 6, 280, 20, 0xA40);
            builder.AddHtmlLocalized(9, 8, 280, 20, 1158655, 0x7FFF); // Change your character's hairstyle.
            builder.AddImageTiled(5, 31, 280, 40, 0xA40);
            // Are you absolutely sure you wish to change your character's hairstyle?
            builder.AddHtmlLocalized(9, 35, 272, 40, 1158651, 0x7FFF);
            builder.AddButton(125, 73, 0xFB7, 0xFB8, 1);
            builder.AddHtmlLocalized(160, 75, 155, 20, 1158654, 0x7FFF); // Make me fabulous!
            builder.AddButton(5, 73, 0xFB1, 0xFB2, 0);
            builder.AddHtmlLocalized(40, 75, 100, 20, 1060051, 0x7FFF); // CANCEL
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            if (info.ButtonID != 1)
            {
                return;
            }

            var from = sender.Mobile;

            if (!_token.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
                return;
            }

            from.SendGump(new HairstyleGump(_token, GetEntries(from)));
        }
    }

    private class HairstyleGump : DynamicGump
    {
        private readonly Item _token;
        private readonly ChangeHairstyleEntry[] _entries;

        public override bool Singleton => true;

        public HairstyleGump(Item token, ChangeHairstyleEntry[] entries) : base(50, 50)
        {
            _token = token;
            _entries = entries;
        }

        protected override void BuildLayout(ref DynamicGumpBuilder builder)
        {
            builder.AddPage();

            if (_entries.Length > 4)
            {
                builder.AddBackground(0, 0, 560, 400, 0xA28);

                builder.AddButton(117, 345, 0xFA5, 0xFA7, 1);
                builder.AddHtmlLocalized(149, 345, 90, 35, 1006044); // OK
                builder.AddButton(324, 345, 0xFA5, 0xFA7, 0);
                builder.AddHtmlLocalized(356, 345, 90, 35, 1006045); // Cancel

                builder.AddHtmlLocalized(50, 25, 460, 20, 1018353); // <center>New Hairstyle</center>
            }
            else
            {
                builder.AddBackground(0, 0, 300, 250, 0xA28);

                builder.AddButton(69, 207, 0xFA5, 0xFA7, 1);
                builder.AddHtmlLocalized(101, 207, 90, 35, 1006044); // OK
                builder.AddButton(191, 207, 0xFA5, 0xFA7, 0);
                builder.AddHtmlLocalized(223, 207, 90, 35, 1006045); // Cancel

                builder.AddHtmlLocalized(50, 25, 230, 20, 1018353); // <center>New Hairstyle</center>
            }

            for (var i = 0; i < _entries.Length; ++i)
            {
                if (i > 3)
                {
                    builder.AddBackground(70 + 124 * (i - 4), 135, 60, 64, 0xA3C);
                    builder.AddTooltip(_entries[i].Tooltip);
                    builder.AddImage(10 + 124 * (i - 4), 95, _entries[i].GumpID);
                    builder.AddRadio(30 + 124 * (i - 4), 155, 0xD0, 0xD1, false, 41400 + i);
                }
                else if (i > 1 && _entries.Length < 5)
                {
                    builder.AddBackground(70 + 124 * (i - 2), 135, 60, 64, 0xA3C);
                    builder.AddTooltip(_entries[i].Tooltip);
                    builder.AddImage(10 + 124 * (i - 2), 95, _entries[i].GumpID);
                    builder.AddRadio(30 + 124 * (i - 2), 155, 0xD0, 0xD1, false, 41400 + i);
                }
                else
                {
                    builder.AddBackground(70 + 124 * i, 60, 60, 64, 0xA3C);
                    builder.AddTooltip(_entries[i].Tooltip);
                    builder.AddImage(10 + 124 * i, 20, _entries[i].GumpID);
                    builder.AddRadio(30 + 124 * i, 80, 0xD0, 0xD1, false, 41400 + i);
                }
            }
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            var from = sender.Mobile;

            if (!_token.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
                return;
            }

            if (info.ButtonID != 1)
            {
                from.SendLocalizedMessage(1013009); // You decide not to change your hairstyle.
                return;
            }

            var switches = info.Switches;

            if (switches.Length <= 0)
            {
                return;
            }

            var index = switches[0] - 41400;

            if (index < 0 || index >= _entries.Length)
            {
                return;
            }

            var entry = _entries[index];

            if (entry.Type == StyleType.Hair)
            {
                from.HairItemID = entry.ItemID;
            }
            else
            {
                from.FacialHairItemID = entry.ItemID;
            }

            from.SendLocalizedMessage(1158661); // You have successfully changed your hairstyle.

            _token.Delete();
        }
    }
}

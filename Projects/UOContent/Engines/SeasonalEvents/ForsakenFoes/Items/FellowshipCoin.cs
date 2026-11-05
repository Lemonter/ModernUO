using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/ForsakenFoes/Items/FellowshipCoin.cs).
///     ServUO's <c>HasGump</c> check is dropped (that helper does not exist here); the gump itself is unchanged.
/// </summary>
[SerializationGenerator(0, false)]
public partial class FellowshipCoin : Item
{
    public override int LabelNumber => 1159036; // The Fellowship Coin

    [Constructible]
    public FellowshipCoin() : base(0x2F60)
    {
        Weight = 1.0;
        Hue = 1912;
        Light = LightType.Circle300;
    }

    public override void OnDoubleClick(Mobile from)
    {
        from.SendGump(new FellowshipCoinGump());
        // *Your proficiency in ~1_SKILL~ reveals more about the item*
        from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1157722, "its origin");
        from.PlaySound(1050);
    }

    private class FellowshipCoinGump : Gumps.Gump
    {
        public FellowshipCoinGump() : base(100, 100)
        {
            AddPage(0);

            AddBackground(0, 0, 454, 400, 0x24A4);
            AddItem(75, 120, 0x2F60);
            AddHtmlLocalized(177, 50, 250, 18, 1114513, "#1159036", 0x3442, false, false);
            AddHtmlLocalized(177, 77, 250, 36, 1114513, "#1159033", 0x3442, false, false);
            // The coin's gilded brilliance is striking. Engraved on the coin are the letters T, W, U.
            AddHtmlLocalized(177, 122, 250, 228, 1159037, 0xC63, true, true);
        }
    }
}


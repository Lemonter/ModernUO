using ModernUO.Serialization;
using Server.Gumps;
using Server.Mobiles;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class AuctionStone : Item
{
    [SerializableField(0)]
    private string _city;

    [Constructible]
    public AuctionStone(string city = "") : base(0xED4)
    {
        Movable = false;
        _city = city;
        Name = string.IsNullOrEmpty(city) ? "аукционный камень" : $"аукционный камень: {city}";
        Hue = 0x481;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(GetWorldLocation(), 3))
        {
            from.SendMessage("Слишком далеко, чтобы использовать аукционный камень.");
            return;
        }

        if (from is not PlayerMobile pm)
        {
            return;
        }

        from.SendGump(new AuctionBrowseGump(pm, _city, 0));
    }
}

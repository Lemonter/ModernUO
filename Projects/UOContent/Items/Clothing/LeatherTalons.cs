using ModernUO.Serialization;

namespace Server.Items;

// Graphic ID not sourced from a real ServUO/OSI reference (only reachable via web search this
// session, no primary source found) — reuses the human Boots graphic as a safe placeholder.
// Swap ItemID for the real Gargoyle "Leather Talons" graphic if/when known.
[SerializationGenerator(0, false)]
public partial class LeatherTalons : BaseShoes
{
    [Constructible]
    public LeatherTalons(int hue = 0) : base(0x170B, hue)
    {
    }

    public override double DefaultWeight => 3.0;

    public override CraftResource DefaultResource => CraftResource.RegularLeather;

    public override int RequiredRaces => Race.AllowGargoylesOnly;
}

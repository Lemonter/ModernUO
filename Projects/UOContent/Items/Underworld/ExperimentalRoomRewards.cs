using ModernUO.Serialization;

namespace Server.Items;

// Ported from real OSI/ServUO content (Scripts/Services/Underworld/ExperimentalRoom/
// ExperimentalRoomRewards.cs) — the 8 trophy/decoration drops handed out by
// ExperimentalRoomChest for completing the hue-matching puzzle. All plain decoration Items,
// verbatim from ServUO.

[Flippable(12287, 12288)]
[SerializationGenerator(0, false)]
public partial class TwoStoryBanner : Item
{
    [Constructible]
    public TwoStoryBanner() : base(12287)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class Stalagmite : Item
{
    [Constructible]
    public Stalagmite() : base(Utility.RandomList(2272, 2273, 2276, 2277, 2279, 2281, 2282))
    {
    }
}

[SerializationGenerator(0, false)]
public partial class Flowstone : Item
{
    [Constructible]
    public Flowstone() : base(Utility.RandomList(2274, 2275, 2278, 2280))
    {
    }
}

[SerializationGenerator(0, false)]
public partial class HangingChainmailLegs : Item
{
    [Constructible]
    public HangingChainmailLegs() : base(5052)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class HangingRingmailTunic : Item
{
    [Constructible]
    public HangingRingmailTunic() : base(5095)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class PluckedChicken : Item
{
    [Constructible]
    public PluckedChicken() : base(7819)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class ColorfulTapestry : Item
{
    [Constructible]
    public ColorfulTapestry() : base(17092)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class CanvaslessEasel : Item
{
    public override int LabelNumber => 123467;

    [Constructible]
    public CanvaslessEasel() : base(Utility.RandomBool() ? 3943 : 3945)
    {
    }
}

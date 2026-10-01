using ModernUO.Serialization;

namespace Server.Items;

/// <summary>The three reward boxes of the Soulforge quest line, ported from ServUO
/// (Scripts/Items/Containers/ScrollBox*.cs). Each holds one Imbuing power scroll — 115 from
/// Aurvidlem's box, 105 or 110 from Beninort's, 120 from Ansikart's — and a one-in-twenty
/// chance of a runic mallet and chisel on top.
///
/// The runic roll is repeated verbatim in all three upstream; here it is one helper. The
/// original's ScrollBox also carries a private PlaceItemIn method nothing calls — dropped.
/// Names are the original's, including the numbered ones.</summary>
[SerializationGenerator(0, false)]
public partial class ScrollBox : WoodenBox
{
    [Constructible]
    public ScrollBox()
    {
        Movable = true;
        Hue = 1151;

        DropItem(new PowerScroll(SkillName.Imbuing, 115.0));
        DropRunic(this);
    }

    public override string DefaultName => "Reward Scroll Box";

    /// <summary>One chance in twenty at a masonry runic, weighted to the cheap ores.</summary>
    internal static void DropRunic(Container box)
    {
        if (Utility.RandomDouble() > 0.05)
        {
            return;
        }

        var roll = Utility.RandomDouble();

        var (resource, charges) = roll switch
        {
            <= 0.25 => (CraftResource.DullCopper, 50),
            <= 0.40 => (CraftResource.ShadowIron, 45),
            <= 0.55 => (CraftResource.Copper, 40),
            <= 0.65 => (CraftResource.Bronze, 35),
            <= 0.75 => (CraftResource.Gold, 30),
            <= 0.85 => (CraftResource.Agapite, 25),
            <= 0.98 => (CraftResource.Verite, 20),
            _       => (CraftResource.Valorite, 15)
        };

        box.DropItem(new RunicMalletAndChisel(resource, charges));
    }
}

[SerializationGenerator(0, false)]
public partial class ScrollBox2 : WoodenBox
{
    [Constructible]
    public ScrollBox2()
    {
        Movable = true;
        Hue = 1266;

        DropItem(new PowerScroll(SkillName.Imbuing, 120.0));
        ScrollBox.DropRunic(this);
    }

    public override string DefaultName => "Reward Scroll Box";
}

[SerializationGenerator(0, false)]
public partial class ScrollBox3 : WoodenBox
{
    [Constructible]
    public ScrollBox3()
    {
        Movable = true;
        Hue = 1159;

        DropItem(new PowerScroll(SkillName.Imbuing, Utility.RandomBool() ? 105.0 : 110.0));
        ScrollBox.DropRunic(this);
    }

    public override string DefaultName => "Reward Scroll Box";
}

/// <summary>Ansikart's smaller reward: one imbuing ingredient in a randomly hued bag. Ported
/// from ServUO (Scripts/Items/Containers/MeagerImbuingBag.cs).
///
/// Upstream it derives from BaseRewardBag but leaves ItemAmount at the base's zero, so the
/// weapons-and-jewellery fill loop never runs and the bag holds only the ingredient; that is
/// what it is here, without the inherited machinery that never fires. The hue table is
/// BaseReward.RewardBagHue's, including its 1-in-200 chance of no hue at all.</summary>
[SerializationGenerator(0, false)]
public partial class MeagerImbuingBag : Bag
{
    private static readonly int[] _hueRanges =
    [
        0x385, 0x3E9, 0x4B0, 0x4E6, 0x514, 0x54A, 0x578, 0x5AE,
        0x5DC, 0x612, 0x640, 0x676, 0x6A5, 0x6DA, 0x708, 0x774
    ];

    [Constructible]
    public MeagerImbuingBag()
    {
        Hue = RandomRewardHue();

        DropItem(
            Utility.Random(4) switch
            {
                0 => new SlithTongue(),
                1 => new GoblinBlood(),
                2 => new ReflectiveWolfEye(),
                _ => (Item)new RaptorTeeth()
            }
        );
    }

    public override int LabelNumber => 1112994; // Meager Imbuing Bag

    internal static int RandomRewardHue()
    {
        if (Utility.RandomDouble() < 0.005)
        {
            return 0;
        }

        var row = Utility.Random(_hueRanges.Length / 2) * 2;

        return Utility.RandomMinMax(_hueRanges[row], _hueRanges[row + 1]);
    }
}

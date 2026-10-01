using System.Collections.Generic;
using ModernUO.Serialization;

namespace Server.Items;

public enum MahaonPandorasBoxColor
{
    Red,    // самый частый, самый скромный лут
    Blue,
    Green,
    Yellow  // самый редкий сам ларец, но и самый щедрый лут внутри
}

/// <summary>
///     Dug up on graveyard ground (Systems.MahaonWorld.MahaonGraveyardDigSystem, triggered
///     from Shovel.OnDoubleClick). One-time use — opens into exactly one item, then deletes
///     itself. Color is rolled at dig time based on the digger's Luck and never changes
///     after that; it only affects which rarity tier gets rolled on open, it doesn't limit
///     which tiers are possible outright (a Red box can still roll Legendary, just rarely).
/// </summary>
[SerializationGenerator(0, false)]
public partial class MahaonPandorasBox : Item
{
    [SerializableField(0)]
    private MahaonPandorasBoxColor _color;

    [Constructible]
    public MahaonPandorasBox(MahaonPandorasBoxColor color = MahaonPandorasBoxColor.Red) : base(0x9AA)
    {
        _color = color;
        Hue = HueFor(color);
        Name = $"ларец Пандоры ({ColorNameRu(color)})";
    }

    public static int HueFor(MahaonPandorasBoxColor color) => color switch
    {
        MahaonPandorasBoxColor.Red    => 0x21,
        MahaonPandorasBoxColor.Blue   => 0x489,
        MahaonPandorasBoxColor.Green  => 0x59,
        MahaonPandorasBoxColor.Yellow => 0x8A5,
        _                              => 0
    };

    public static string ColorNameRu(MahaonPandorasBoxColor color) => color switch
    {
        MahaonPandorasBoxColor.Red    => "красный",
        MahaonPandorasBoxColor.Blue   => "синий",
        MahaonPandorasBoxColor.Green  => "зелёный",
        MahaonPandorasBoxColor.Yellow => "жёлтый",
        _                              => "?"
    };

    private enum Tier
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    // Weight per tier, per box color — higher color quality shifts weight toward the
    // rarer tiers without ever fully excluding the common ones (still mostly junk even
    // from a Yellow box, just meaningfully less so than from a Red one).
    private static readonly Dictionary<MahaonPandorasBoxColor, (int common, int uncommon, int rare, int epic, int legendary)> TierWeights = new()
    {
        [MahaonPandorasBoxColor.Red]    = (70, 20, 8, 2, 0),
        [MahaonPandorasBoxColor.Blue]   = (50, 28, 15, 6, 1),
        [MahaonPandorasBoxColor.Green]  = (30, 30, 25, 12, 3),
        [MahaonPandorasBoxColor.Yellow] = (10, 20, 30, 28, 12)
    };

    // Curated, not reflected — walking every Item-derived type in the assembly and blindly
    // calling Activator.CreateInstance would eventually hit one that needs specific
    // constructor args and crash the open attempt (or worse). This list is deliberately
    // broad across categories (weapons/armor/resources/gems/decor/currency) and trivially
    // extensible — just add more types to whichever tier they belong in.
    private static readonly System.Type[] CommonTier =
    {
        typeof(IronOre), typeof(Log), typeof(Leather), typeof(Wool), typeof(Feather),
        typeof(Bandage), typeof(GreaterHealPotion), typeof(BlankScroll)
    };

    private static readonly System.Type[] UncommonTier =
    {
        typeof(Longsword), typeof(Katana), typeof(RingmailChest), typeof(Amber),
        typeof(Tourmaline), typeof(Kindling), typeof(Bolt), typeof(Arrow)
    };

    private static readonly System.Type[] RareTier =
    {
        typeof(PlateChest), typeof(Ruby), typeof(Emerald), typeof(Sapphire),
        typeof(ValoriteOre), typeof(FrostwoodLog)
    };

    private static readonly System.Type[] EpicTier =
    {
        typeof(Diamond), typeof(StarSapphire), typeof(GoldBricks)
    };

    private static readonly System.Type[] LegendaryTier =
    {
        typeof(GoldBricks) // placeholder slot — replace/extend with real Mahaon artifacts as they're built
    };

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendMessage("Это должно быть у тебя в рюкзаке, чтобы открыть.");
            return;
        }

        var weights = TierWeights[_color];
        var total = weights.common + weights.uncommon + weights.rare + weights.epic + weights.legendary;
        var roll = Utility.Random(total);

        Tier tier;

        if (roll < weights.common)
        {
            tier = Tier.Common;
        }
        else if (roll < weights.common + weights.uncommon)
        {
            tier = Tier.Uncommon;
        }
        else if (roll < weights.common + weights.uncommon + weights.rare)
        {
            tier = Tier.Rare;
        }
        else if (roll < weights.common + weights.uncommon + weights.rare + weights.epic)
        {
            tier = Tier.Epic;
        }
        else
        {
            tier = Tier.Legendary;
        }

        var table = tier switch
        {
            Tier.Common    => CommonTier,
            Tier.Uncommon  => UncommonTier,
            Tier.Rare      => RareTier,
            Tier.Epic      => EpicTier,
            _              => LegendaryTier
        };

        var chosenType = table[Utility.Random(table.Length)];
        Item prize = null;

        try
        {
            prize = chosenType.CreateInstance<Item>();
        }
        catch
        {
            // ignored — fall through to the null check below
        }

        if (prize == null)
        {
            from.SendMessage(0x22, "Ларец рассыпался в пыль, не оставив ничего — не повезло.");
            Delete();
            return;
        }

        if (from.Backpack?.TryDropItem(from, prize, false) != true)
        {
            prize.MoveToWorld(from.Location, from.Map);
        }

        from.SendMessage(0x59, $"Ларец Пандоры открыт! Внутри: {prize.Name ?? prize.GetType().Name}.");
        Delete();
    }
}

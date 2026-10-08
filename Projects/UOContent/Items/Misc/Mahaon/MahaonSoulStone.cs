using ModernUO.Serialization;

namespace Server.Items;

public enum SoulStoneSize
{
    Small,
    Medium,
    Large,
    Giant
}

public enum SoulStoneColor
{
    Black, // Poisoning/Fencing/Hiding + Dex
    Red,   // Wrestling/Tactics/Macing + Str
    Blue,  // Magery/ItemID/EvalInt + Int
    Green, // Archery/MagicResist/Tracking + Dex
    Gold   // Healing/Anatomy/Swords + Str/Dex/Int all three — deliberately the strongest color
}

/// <summary>
///     Mahaon soul stones. Drop chance/correlation with the killed monster was reportedly
///     unknown even to the original players — see <c>SoulStoneDropSystem</c> for our own
///     (necessarily invented) drop rule, since the real one isn't recoverable.
/// </summary>
[SerializationGenerator(0, false)]
public partial class MahaonSoulStone : Item
{
    [SerializableField(0)]
    private SoulStoneSize _size;

    [SerializableField(1)]
    private SoulStoneColor _color;

    [Constructible]
    public MahaonSoulStone(SoulStoneSize size, SoulStoneColor color) : base(SizeToItemID(size))
    {
        _size = size;
        _color = color;
        Hue = ColorToHue(color);
    }

    /// <summary>
    ///     Mahaon: every size used to share one graphic — and 0x4F3D is not even a stone,
    ///     it is a house roof section from a recent publish, so all four sizes showed the
    ///     same wrong tile. These four are the classic mountain rocks, picked because they
    ///     are the same style at four genuinely different pixel heights (32 / 46 / 62 / 98),
    ///     so the size reads at a glance in a backpack, and because they are close to
    ///     greyscale, which means the colour hue lands cleanly on all of them.
    /// </summary>
    public static int SizeToItemID(SoulStoneSize size) => size switch
    {
        SoulStoneSize.Small  => 0x136C, // 45x32 pebble
        SoulStoneSize.Medium => 0x1363, // 45x46 rock
        SoulStoneSize.Large  => 0x1355, // 45x62 boulder
        SoulStoneSize.Giant  => 0x1350, // 45x98 great boulder
        _                    => 0x1363
    };

    // Stones already in players' packs carry the old graphic and the old hue in their save
    // data, so bring them up to date on load rather than leaving a mix of old and new.
    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        ItemID = SizeToItemID(_size);
        Hue = ColorToHue(_color);
    }

    public static int SizeToBonus(SoulStoneSize size) => size switch
    {
        SoulStoneSize.Small  => 5,
        SoulStoneSize.Medium => 10,
        SoulStoneSize.Large  => 15,
        SoulStoneSize.Giant  => 20,
        _                    => 0
    };

    // Shard owner's own spec: each color grants THREE fixed skills at once (not one random
    // pick), plus the stat(s) below — Gold gets all three stats instead of just one, making
    // it deliberately the strongest color rather than a coin-flip "stat instead of skill".
    public static SkillName[] ColorToSkills(SoulStoneColor color) => color switch
    {
        SoulStoneColor.Black => new[] { SkillName.Poisoning, SkillName.Fencing, SkillName.Hiding },
        SoulStoneColor.Red   => new[] { SkillName.Wrestling, SkillName.Tactics, SkillName.Macing },
        SoulStoneColor.Blue  => new[] { SkillName.Magery, SkillName.ItemID, SkillName.EvalInt },
        SoulStoneColor.Green => new[] { SkillName.Archery, SkillName.MagicResist, SkillName.Tracking },
        SoulStoneColor.Gold  => new[] { SkillName.Healing, SkillName.Anatomy, SkillName.Swords },
        _                    => System.Array.Empty<SkillName>()
    };

    public static StatType[] ColorToStats(SoulStoneColor color) => color switch
    {
        SoulStoneColor.Black => new[] { StatType.Dex },
        SoulStoneColor.Red   => new[] { StatType.Str },
        SoulStoneColor.Blue  => new[] { StatType.Int },
        SoulStoneColor.Green => new[] { StatType.Dex },
        SoulStoneColor.Gold  => new[] { StatType.Str, StatType.Dex, StatType.Int },
        _                    => System.Array.Empty<StatType>()
    };

    // Public: also reused by SoulStoneSocketing to dye the FIRST socketed item to this
    // color ("камни души должны красить в свой цвет вещи. Цвет первого камня души").
    // Rendered every one of these against the real hues.mul ramps before picking them.
    // Black was 0x0001 — the very first hue, a ramp that collapses to pure black, so the
    // stone came out as a flat silhouette with no shape at all. Gold was 0x0008, which is
    // not gold: that ramp renders blue-violet. Blue was 0x005A, which renders teal rather
    // than blue. The three replacements below are the ramps that actually look like their
    // names on this art, with the stone's shading still visible.
    public static int ColorToHue(SoulStoneColor color) => color switch
    {
        SoulStoneColor.Black => 0x0455, // near-black grey that keeps the shading
        SoulStoneColor.Red   => 0x0021,
        SoulStoneColor.Blue  => 0x0063,
        SoulStoneColor.Green => 0x0044,
        SoulStoneColor.Gold  => 0x0501,
        _                    => 0
    };

    public override double DefaultWeight => 1.0;

    public override string DefaultName => $"{SizeRu(_size)} {ColorRu(_color)} камень души";

    private static string SizeRu(SoulStoneSize size) => size switch
    {
        SoulStoneSize.Small  => "малый",
        SoulStoneSize.Medium => "средний",
        SoulStoneSize.Large  => "большой",
        SoulStoneSize.Giant  => "гигантский",
        _                    => size.ToString()
    };

    private static string ColorRu(SoulStoneColor color) => color switch
    {
        SoulStoneColor.Black => "чёрный",
        SoulStoneColor.Red   => "красный",
        SoulStoneColor.Blue  => "синий",
        SoulStoneColor.Green => "зелёный",
        SoulStoneColor.Gold  => "золотой",
        _                    => color.ToString()
    };

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendMessage("Это должно быть у тебя в рюкзаке, чтобы использовать.");
            return;
        }

        from.SendMessage("Укажи оружие, броню или украшение, в которое хочешь вставить камень.");
        from.Target = new Systems.MahaonSoulStones.SoulStoneSocketTarget(this);
    }
}

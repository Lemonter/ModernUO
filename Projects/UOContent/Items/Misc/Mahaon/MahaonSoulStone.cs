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
    Black, // thief skills
    Red, // warrior skills
    Blue, // mage skills
    Green, // archer skills
    Gold // homebrew addition — no one remembered what these did originally, so: a flat
         // bonus to a random stat (Str/Dex/Int) instead of a skill, scaled the same as
         // the others. Easy to change once/if the real effect surfaces from memory.
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
    public MahaonSoulStone(SoulStoneSize size, SoulStoneColor color) : base(0x4F3D)
    {
        _size = size;
        _color = color;
        Hue = ColorToHue(color);
    }

    public static int SizeToBonus(SoulStoneSize size) => size switch
    {
        SoulStoneSize.Small  => 5,
        SoulStoneSize.Medium => 10,
        SoulStoneSize.Large  => 15,
        SoulStoneSize.Giant  => 20,
        _                    => 0
    };

    public static SkillName? ColorToSkill(SoulStoneColor color) => color switch
    {
        SoulStoneColor.Black => SkillName.Stealing,
        SoulStoneColor.Red   => SkillName.Tactics,
        SoulStoneColor.Blue  => SkillName.Magery,
        SoulStoneColor.Green => SkillName.Archery,
        SoulStoneColor.Gold  => null, // stat bonus instead, handled by the socketing system
        _                    => null
    };

    private static int ColorToHue(SoulStoneColor color) => color switch
    {
        SoulStoneColor.Black => 0x0001,
        SoulStoneColor.Red   => 0x0021,
        SoulStoneColor.Blue  => 0x005A,
        SoulStoneColor.Green => 0x0044,
        SoulStoneColor.Gold  => 0x0008,
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

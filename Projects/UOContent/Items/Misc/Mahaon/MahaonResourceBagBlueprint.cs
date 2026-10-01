using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     A one-time-use recipe item for one of the five MahaonResourceBag types — same
///     "double-click to consume and produce the result, entirely in Russian" pattern as
///     MahaonMysteryBlueprint, chosen over registering these in DefTinkering specifically
///     because the vanilla craft menu can only show localized (English) cliloc names —
///     wedging a Russian-named result into an English list is exactly the kind of
///     half-translated UI this project avoids everywhere else (see the house-sign
///     context-menu writeup in info.html for the same reasoning).
/// </summary>
[SerializationGenerator(0, false)]
public partial class MahaonResourceBagBlueprint : Item
{
    private const double RequiredSkill = 70.0;
    private const int LeatherCost = 20;
    private const int GoldCost = 500;

    [SerializableField(0)]
    private MahaonResourceCategory _category;

    [Constructible]
    public MahaonResourceBagBlueprint(MahaonResourceCategory category = MahaonResourceCategory.Ore) : base(0x2831)
    {
        _category = category;
        Name = $"чертёж: {BagNameRu(category)}";
    }

    public static string BagNameRu(MahaonResourceCategory category) => category switch
    {
        MahaonResourceCategory.Ore     => "сумка шахтёра",
        MahaonResourceCategory.Wood    => "сумка лесоруба",
        MahaonResourceCategory.Fish    => "сумка рыбака",
        MahaonResourceCategory.Reagent => "сумка алхимика",
        MahaonResourceCategory.Crop    => "сумка фермера",
        MahaonResourceCategory.Hunter  => "сумка охотника",
        _                               => "сумка"
    };

    private static MahaonResourceBag CreateBag(MahaonResourceCategory category) => category switch
    {
        MahaonResourceCategory.Ore     => new MahaonMinerBag(),
        MahaonResourceCategory.Wood    => new MahaonLumberjackBag(),
        MahaonResourceCategory.Fish    => new MahaonFishermanBag(),
        MahaonResourceCategory.Reagent => new MahaonAlchemistBag(),
        MahaonResourceCategory.Hunter  => new MahaonHunterBag(),
        _                               => new MahaonFarmerBag()
    };

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendMessage("Это должно быть у тебя в рюкзаке, чтобы использовать.");
            return;
        }

        if (from.Skills[SkillName.Tinkering].Value < RequiredSkill)
        {
            from.SendMessage(0x22, $"Нужно {RequiredSkill:0} навыка тинкеринга, чтобы разобраться в чертеже.");
            return;
        }

        var backpack = from.Backpack;

        if (backpack == null || backpack.GetAmount(typeof(Leather)) < LeatherCost)
        {
            from.SendMessage(0x22, $"Не хватает кожи — нужно {LeatherCost} штук.");
            return;
        }

        if (!backpack.ConsumeTotal(typeof(Gold), GoldCost))
        {
            from.SendMessage(0x22, $"Не хватает золота — нужно {GoldCost}.");
            return;
        }

        backpack.ConsumeTotal(typeof(Leather), LeatherCost);

        var bag = CreateBag(_category);

        if (backpack.TryDropItem(from, bag, false) != true)
        {
            bag.MoveToWorld(from.Location, from.Map);
        }

        from.SendMessage(0x59, $"Готово: {BagNameRu(_category)} у тебя в рюкзаке.");
        Delete();
    }
}

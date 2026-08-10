using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;
using Server.Systems.MahaonSoulStones;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class MahaonLibrarian : BaseVendor
{
    private readonly List<SBInfo> _sbInfos = new();

    [Constructible]
    public MahaonLibrarian() : base("библиотекарь")
    {
        SetSkill(SkillName.EvalInt, 60.0, 100.0);
        Title = "Библиотекарь";
    }

    protected override List<SBInfo> SBInfos => _sbInfos;

    public override void InitOutfit()
    {
        base.InitOutfit();
        AddItem(new Robe(Utility.RandomNondyedHue()));
    }

    public override void InitSBInfo()
    {
        _sbInfos.Add(new SBMahaonLibrarian());
    }
}

public class SBMahaonLibrarian : SBInfo
{
    public override IShopSellInfo SellInfo { get; } = new InternalSellInfo();

    public override List<GenericBuyInfo> BuyInfo { get; } = new InternalBuyInfo();

    public class InternalBuyInfo : List<GenericBuyInfo>
    {
        public InternalBuyInfo()
        {
            foreach (var (type, price) in SakuroBlueprintKnowledge.LibrarianPrice)
            {
                Add(new SakuroBlueprintBuyInfo(type, price));
            }
        }
    }

    public class InternalSellInfo : GenericSellInfo
    {
    }
}

/// <summary>
///     GenericBuyInfo constructs its stock item via a type + args, which doesn't fit a
///     per-entry constructor argument (the SakuroType) cleanly through the normal
///     `typeof(X)` shorthand — this subclass overrides item construction directly instead.
/// </summary>
public class SakuroBlueprintBuyInfo : GenericBuyInfo
{
    private readonly SakuroType _type;

    public SakuroBlueprintBuyInfo(SakuroType type, int price)
        : base($"чертёж: сакуро-перстень «{SakuroTypeRu(type)}»", typeof(SakuroBlueprint), price, 20, 0x0FF0, 0) =>
        _type = type;

    public override IEntity GetEntity() => new SakuroBlueprint(_type);
    private static string SakuroTypeRu(SakuroType type) => type switch
    {
        SakuroType.Power   => "Сила",
        SakuroType.Agility => "Ловкость",
        SakuroType.Wisdom  => "Мудрость",
        SakuroType.Balance => "Баланс",
        SakuroType.Fox     => "Лис",
        SakuroType.Bear    => "Медведь",
        SakuroType.Owl     => "Сова",
        SakuroType.Titan   => "Титан",
        _                  => type.ToString()
    };
}

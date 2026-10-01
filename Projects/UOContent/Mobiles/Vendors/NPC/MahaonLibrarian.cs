using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Gumps;
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

    // Separate from the vendor's own buy/sell window (Sakuro ring blueprints, via
    // SBMahaonLibrarian below) — the shard's other blueprint system (real craft-recipe
    // blueprints, Gumps.MahaonBlueprintCategoryGump) is reached through a context-menu
    // entry instead of hijacking OnDoubleClick, since that already opens the vendor shop.
    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        if (from.Alive)
        {
            list.Add(new CraftBlueprintsEntry());
        }
    }

    private class CraftBlueprintsEntry : ContextMenuEntry
    {
        // 9000002 — sentinel cliloc id (not a real one), resolved to real Russian text
        // client-side by ClassicUO.Game.Managers.MahaonContextMenuText — see that file for
        // why (the classic context-menu protocol only carries a numeric cliloc, no raw
        // string option; same pattern already used for the pet-bag "В мешок" entry).
        public CraftBlueprintsEntry() : base(9000002)
        {
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            if (from.CheckAlive())
            {
                from.SendGump(new Gumps.MahaonBlueprintCategoryGump(from));
            }
        }
    }

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

    // The vendor buy-LIST name specifically (not the item's own real name once bought,
    // which is DefaultName in SakuroBlueprint.cs and stays Russian) has to be Latin1 —
    // OutgoingVendorBuyPackets.SendVendorBuyList (packet 0x74) writes this field via
    // WriteLatin1Null, a single-byte encoding that can't represent Cyrillic at all
    // (silently becomes "?????" on the wire, showing as an unrecognized/garbled label
    // client-side). No sentinel-id workaround like the context-menu fix exists here —
    // this packet embeds the string directly, not a numeric reference to look up
    // client-side, so plain English/ASCII is the only thing that actually renders.
    public SakuroBlueprintBuyInfo(SakuroType type, int price)
        : base($"Sakuro Blueprint ({type})", typeof(SakuroBlueprint), price, 20, 0x0FF0, 0) =>
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

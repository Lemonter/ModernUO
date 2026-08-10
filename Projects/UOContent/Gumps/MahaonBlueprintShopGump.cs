using System.Collections.Generic;
using Server.Engines.Craft;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Systems.MahaonRecipes;

namespace Server.Gumps;

public class MahaonBlueprintCategoryGump : StaticGump<MahaonBlueprintCategoryGump>
{
    private readonly Mobile _player;

    public override bool Singleton => true;

    public static readonly (string name, CraftSystem system)[] Categories =
    {
        ("Кузнечное дело", DefBlacksmithy.CraftSystem),
        ("Плотницкое дело", DefCarpentry.CraftSystem),
        ("Портняжное дело", DefTailoring.CraftSystem),
        ("Слесарное дело", DefTinkering.CraftSystem),
        ("Алхимия", DefAlchemy.CraftSystem),
        ("Каллиграфия", DefInscription.CraftSystem),
        ("Картография", DefCartography.CraftSystem),
        ("Изготовление луков", DefBowFletching.CraftSystem),
        ("Кулинария", DefCooking.CraftSystem),
        ("Каменная кладка", DefMasonry.CraftSystem),
        ("Стеклодувное дело", DefGlassblowing.CraftSystem)
    };

    public MahaonBlueprintCategoryGump(Mobile player) : base(50, 50) => _player = player;

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        var height = 60 + Categories.Length * 25;

        builder.AddPage();
        builder.AddBackground(0, 0, 300, height, 5054);
        builder.AddAlphaRegion(10, 10, 280, height - 20);
        builder.AddHtml(15, 15, 270, 20, "Магазин чертежей — выбери ремесло:");

        for (var i = 0; i < Categories.Length; i++)
        {
            var y = 45 + i * 25;
            builder.AddButton(15, y, 4005, 4007, i + 1);
            builder.AddLabelPlaceholder(45, y, 0x480, $"cat{i}");
        }
    }

    protected override void BuildStrings(ref GumpStringsBuilder builder)
    {
        for (var i = 0; i < Categories.Length; i++)
        {
            builder.SetStringSlot($"cat{i}", Categories[i].name);
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var index = info.ButtonID - 1;
        if (index < 0 || index >= Categories.Length)
        {
            return;
        }

        _player.SendGump(new MahaonBlueprintShopGump(_player, Categories[index].system, Categories[index].name, 0));
    }
}

public class MahaonBlueprintShopGump : DynamicGump
{
    private const int PageSize = 10;

    private readonly Mobile _player;
    private readonly CraftSystem _system;
    private readonly string _categoryName;
    private readonly int _page;
    private readonly List<CraftItem> _pageItems = new();

    public override bool Singleton => true;

    public MahaonBlueprintShopGump(Mobile player, CraftSystem system, string categoryName, int page) : base(50, 50)
    {
        _player = player;
        _system = system;
        _categoryName = categoryName;
        _page = page;

        if (system == null)
        {
            return;
        }

        var start = page * PageSize;
        for (var i = start; i < system.CraftItems.Count && i < start + PageSize; i++)
        {
            _pageItems.Add(system.CraftItems[i]);
        }
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();
        builder.AddBackground(0, 0, 420, 400, 5054);
        builder.AddAlphaRegion(10, 10, 400, 380);

        var totalPages = _system == null ? 1 : (_system.CraftItems.Count + PageSize - 1) / PageSize;
        builder.AddHtml(15, 15, 390, 20, $"{_categoryName} — стр. {_page + 1}/{totalPages}");

        for (var i = 0; i < _pageItems.Count; i++)
        {
            var y = 45 + i * 32;
            var craftItem = _pageItems[i];
            var recipe = craftItem.Recipe;

            var known = recipe == null || (_player as PlayerMobile)?.HasRecipe(recipe) == true;

            if (known)
            {
                builder.AddItem(15, y, craftItem.ItemId, craftItem.ItemHue);
            }
            else
            {
                builder.AddLabel(15, y, 0x3B2, "???");
            }

            builder.AddLabel(55, y, known ? 0x480 : 0x3B2, recipe != null ? RecipeNameHelper.GetName(recipe) : craftItem.NameString);

            if (!known && recipe != null)
            {
                var price = MahaonBlueprint.GetPrice(recipe);
                builder.AddLabel(280, y, 0x59, $"{price} мон.");
                builder.AddButton(360, y, 4005, 4007, i + 1);
            }
            else
            {
                builder.AddLabel(280, y, 0x480, "изучено");
            }
        }

        if (_page > 0)
        {
            builder.AddButton(15, 365, 4014, 4016, 9001);
            builder.AddLabel(50, 365, 0x480, "Назад");
        }

        if ((_page + 1) * PageSize < (_system?.CraftItems.Count ?? 0))
        {
            builder.AddButton(150, 365, 4005, 4007, 9002);
            builder.AddLabel(185, 365, 0x480, "Далее");
        }

        builder.AddButton(300, 365, 4017, 4019, 9000);
        builder.AddLabel(335, 365, 0x480, "Назад");
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        switch (info.ButtonID)
        {
            case 9000:
                _player.SendGump(new MahaonBlueprintCategoryGump(_player));
                return;
            case 9001:
                _player.SendGump(new MahaonBlueprintShopGump(_player, _system, _categoryName, _page - 1));
                return;
            case 9002:
                _player.SendGump(new MahaonBlueprintShopGump(_player, _system, _categoryName, _page + 1));
                return;
        }

        var index = info.ButtonID - 1;
        if (index < 0 || index >= _pageItems.Count)
        {
            return;
        }

        var recipe = _pageItems[index].Recipe;
        if (recipe == null || _player is not PlayerMobile pm || pm.HasRecipe(recipe))
        {
            _player.SendGump(new MahaonBlueprintShopGump(_player, _system, _categoryName, _page));
            return;
        }

        var price = MahaonBlueprint.GetPrice(recipe);
        var backpack = pm.Backpack;
        var cost = CurrencyHelper.ToCopperValue(price, 0, 0);

        if (backpack == null || !CurrencyHelper.TryWithdrawCopperValue(backpack, cost))
        {
            pm.SendMessage(0x22, $"Нужно {price} золота на этот чертёж.");
        }
        else
        {
            pm.AcquireRecipe(recipe);
            pm.SendMessage(0x59, $"Ты изучаешь чертёж: {RecipeNameHelper.GetName(recipe)}.");
        }

        _player.SendGump(new MahaonBlueprintShopGump(_player, _system, _categoryName, _page));
    }
}

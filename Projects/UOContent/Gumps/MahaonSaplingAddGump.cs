using Server.Commands;
using Server.Engines.Spawners;
using Server.Items;
using Server.Network;
using Server.Systems.MahaonSeasons;

namespace Server.Gumps;

public enum MahaonMenuCategory
{
    Main,
    Saplings,
    SpawnPoints,
    Stones,
    DungeonMarkers,
    Crops,
    Raids,
    Quests,
    Areas
}

/// <summary>Our own "[AddLem" menu — not the base engine [add. Nested categories, each
/// paginated at 10 entries per page so it stays readable as more gets added later.</summary>
public class MahaonSaplingAddGump : StaticGump<MahaonSaplingAddGump>
{
    private const int PerPage = 10;

    private readonly MahaonMenuCategory _category;
    private readonly int _page;

    public override bool Singleton => true;
    protected override bool Cached => false;

    public MahaonSaplingAddGump(MahaonMenuCategory category = MahaonMenuCategory.Main, int page = 0) : base(80, 80)
    {
        _category = category;
        _page = page;
    }

    public static void Configure()
    {
        CommandSystem.Register("AddLem", AccessLevel.GameMaster, AddLem_OnCommand);
    }

    [Usage("AddLem")]
    [Description("Opens our own Mahaon add menu — not the base engine one.")]
    private static void AddLem_OnCommand(CommandEventArgs e)
    {
        e.Mobile.SendGump(new MahaonSaplingAddGump());
    }

    // -- Entry lists per category -------------------------------------------------------

    private static (string label, int actionId)[] MainEntries() =>
    [
        ("Саженцы", 1000 + (int)MahaonMenuCategory.Saplings),
        ("Точки спауна", 1000 + (int)MahaonMenuCategory.SpawnPoints),
        ("Камни", 1000 + (int)MahaonMenuCategory.Stones),
        ("Маркеры данжей (невидимые)", 1000 + (int)MahaonMenuCategory.DungeonMarkers),
        ("Урожай (сезонные грядки)", 1000 + (int)MahaonMenuCategory.Crops),
        ("Набеги", 1000 + (int)MahaonMenuCategory.Raids),
        ("Квесты", 1000 + (int)MahaonMenuCategory.Quests),
        ("Области (разметка)", 1000 + (int)MahaonMenuCategory.Areas)
    ];

    private static (string label, int actionId)[] SaplingEntries()
    {
        var species = MahaonTreeSpeciesTable.Data;
        var entries = new (string, int)[species.Length];

        for (var i = 0; i < species.Length; i++)
        {
            entries[i] = (species[i].NameRu, 2000 + i);
        }

        return entries;
    }

    private static (string label, int actionId)[] SpawnPointEntries() =>
    [
        ("Маяк ботов", 3000),
        ("Торговцы (родной спавнер)", 3001),
        ("Монстры (родной спавнер)", 3002),
        ("Живность (родной спавнер)", 3003)
    ];

    private static (string label, int actionId)[] StoneEntries() =>
    [
        ("Камень выбора профессии", 4000),
        ("Камень телепортации по городам", 4001),
        ("Камень аукциона", 4002)
    ];

    private static (string label, int actionId)[] DungeonMarkerEntries()
    {
        var known = Systems.MahaonBots.DungeonTarget.Known;
        var entries = new (string, int)[known.Length];

        for (var i = 0; i < known.Length; i++)
        {
            entries[i] = ($"Маркер: {known[i].Name}", 5000 + i);
        }

        return entries;
    }

    private static (string label, int actionId)[] CropEntries()
    {
        var crops = Systems.MahaonWorld.MahaonCropTable.Data;
        var entries = new (string, int)[crops.Length];

        for (var i = 0; i < crops.Length; i++)
        {
            entries[i] = (crops[i].NameRu, 6000 + i);
        }

        return entries;
    }

    private static (string label, int actionId)[] RaidEntries() =>
    [
        ("Метка города (невидимая)", 7000),
        ("Спаунер набега", 7001),
        ("Спаунер защитников города", 7002)
    ];

    private static (string label, int actionId)[] QuestEntries() =>
    [
        ("Мэр (выдаёт задание \"убей N\")", 8000),
        ("Доска охотников за головами", 8001),
        ("Доска заказов ремесленников", 8002),
        ("Сержант Гвидо (гвардия, звание, задания)", 8003),
        ("Библиотекарь (продажа чертежей)", 8004),
        ("Лесничий Питэр (задания на головы животных)", 8005),
        ("Придворный маг (звание, бонус к мане, задания)", 8006)
    ];

    private static (string label, int actionId)[] AreaEntries() =>
    [
        ("Жезл разметки областей", 9000)
    ];

    private (string label, int actionId)[] CurrentEntries() => _category switch
    {
        MahaonMenuCategory.Saplings       => SaplingEntries(),
        MahaonMenuCategory.SpawnPoints    => SpawnPointEntries(),
        MahaonMenuCategory.Stones         => StoneEntries(),
        MahaonMenuCategory.DungeonMarkers => DungeonMarkerEntries(),
        MahaonMenuCategory.Crops          => CropEntries(),
        MahaonMenuCategory.Raids          => RaidEntries(),
        MahaonMenuCategory.Quests         => QuestEntries(),
        MahaonMenuCategory.Areas          => AreaEntries(),
        _                                 => MainEntries()
    };

    private static string CategoryTitle(MahaonMenuCategory category) => category switch
    {
        MahaonMenuCategory.Saplings       => "Mahaon — саженцы",
        MahaonMenuCategory.SpawnPoints    => "Mahaon — точки спауна",
        MahaonMenuCategory.Stones         => "Mahaon — камни",
        MahaonMenuCategory.DungeonMarkers => "Mahaon — маркеры данжей (невидимые для игроков)",
        MahaonMenuCategory.Crops          => "Mahaon — грядки (сезонный урожай)",
        MahaonMenuCategory.Raids          => "Mahaon — набеги",
        MahaonMenuCategory.Quests         => "Mahaon — квесты",
        MahaonMenuCategory.Areas          => "Mahaon — разметка областей",
        _                                 => "Mahaon — меню"
    };

    // -- Layout ---------------------------------------------------------------------------

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        var all = CurrentEntries();
        var totalPages = System.Math.Max(1, (all.Length + PerPage - 1) / PerPage);
        var page = System.Math.Clamp(_page, 0, totalPages - 1);
        var pageEntries = all[(page * PerPage)..System.Math.Min(all.Length, (page + 1) * PerPage)];

        var height = 90 + pageEntries.Length * 28 + (totalPages > 1 || _category != MahaonMenuCategory.Main ? 35 : 0);

        builder.AddPage();
        builder.AddBackground(0, 0, 320, height, 5054);
        builder.AddAlphaRegion(10, 10, 300, height - 20);

        builder.AddHtml(20, 15, 280, 20, CategoryTitle(_category));

        if (totalPages > 1)
        {
            builder.AddHtml(20, 38, 280, 20, $"Страница {page + 1} / {totalPages}");
        }

        var y = totalPages > 1 ? 62 : 45;

        foreach (var (label, actionId) in pageEntries)
        {
            builder.AddButton(20, y, 4005, 4007, actionId);
            builder.AddLabel(55, y + 2, 0x480, label);
            y += 28;
        }

        y += 5;

        if (_category != MahaonMenuCategory.Main)
        {
            builder.AddButton(20, y, 4005, 4007, 9998); // back to main menu
            builder.AddLabel(55, y + 2, 0x480, "Назад");
            y += 28;
        }

        if (totalPages > 1)
        {
            if (page > 0)
            {
                builder.AddButton(20, y, 4005, 4007, 9990 + page - 1); // prev page
                builder.AddLabel(55, y + 2, 0x480, "Пред. страница");
            }

            if (page < totalPages - 1)
            {
                builder.AddButton(150, y, 4005, 4007, 9990 + page + 1); // next page
                builder.AddLabel(185, y + 2, 0x480, "След. страница");
            }
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var mobile = sender.Mobile;
        if (mobile == null)
        {
            return;
        }

        var buttonId = info.ButtonID;

        // Navigation: back to main menu
        if (buttonId == 9998)
        {
            mobile.SendGump(new MahaonSaplingAddGump());
            return;
        }

        // Navigation: page change within the same category
        if (buttonId is >= 9990 and < 10000)
        {
            mobile.SendGump(new MahaonSaplingAddGump(_category, buttonId - 9990));
            return;
        }

        // Navigation: open a submenu from the main menu
        if (buttonId is >= 1000 and < 2000)
        {
            mobile.SendGump(new MahaonSaplingAddGump((MahaonMenuCategory)(buttonId - 1000)));
            return;
        }

        // Saplings
        if (buttonId is >= 2000 and < 3000)
        {
            var index = buttonId - 2000;
            if (index < 0 || index >= MahaonTreeSpeciesTable.Data.Length)
            {
                return;
            }

            var sapling = new MahaonSapling((MahaonTreeSpecies)index);
            if (!mobile.Backpack.TryDropItem(mobile, sapling, false))
            {
                sapling.MoveToWorld(mobile.Location, mobile.Map);
            }

            mobile.SendMessage(0x59, $"Добавлен саженец: {MahaonTreeSpeciesTable.Data[index].NameRu}.");
            mobile.SendGump(new MahaonSaplingAddGump(MahaonMenuCategory.Saplings, _page));
            return;
        }

        // Spawn points
        if (buttonId is >= 3000 and < 4000)
        {
            switch (buttonId)
            {
                case 3000:
                    PlaceAtFeet(mobile, new MahaonBotBeacon());
                    break;

                case 3001:
                case 3002:
                case 3003:
                    var spawner = new Spawner();
                    spawner.MoveToWorld(mobile.Location, mobile.Map);
                    spawner.OnDoubleClick(mobile);
                    mobile.SendMessage(
                        0x59,
                        buttonId switch
                        {
                            3001 => "Спавнер поставлен — настрой на торговцев в открывшемся гампе.",
                            3002 => "Спавнер поставлен — настрой на монстров в открывшемся гампе.",
                            _    => "Спавнер поставлен — настрой на живность в открывшемся гампе."
                        }
                    );
                    break;
            }

            return;
        }

        // Stones
        if (buttonId is >= 4000 and < 5000)
        {
            switch (buttonId)
            {
                case 4000:
                    PlaceAtFeet(mobile, new ProfessionStone());
                    break;

                case 4001:
                    PlaceAtFeet(mobile, new CityTravelStone());
                    break;

                case 4002:
                    PlaceAtFeet(mobile, new AuctionStone());
                    break;
            }

            return;
        }

        // Dungeon markers — invisible, placed wherever the GM is standing (walk into the
        // real dungeon first, then click this button there).
        if (buttonId is >= 5000 and < 6000)
        {
            var index = buttonId - 5000;
            var known = Systems.MahaonBots.DungeonTarget.Known;
            if (index < 0 || index >= known.Length)
            {
                return;
            }

            var marker = new MahaonDungeonMarker(known[index].Name);
            marker.MoveToWorld(mobile.Location, mobile.Map);
            mobile.SendMessage(0x59, $"Невидимый маркер данжа '{known[index].Name}' поставлен здесь.");
            return;
        }

        // Crops
        if (buttonId is >= 6000 and < 7000)
        {
            var index = buttonId - 6000;
            var crops = Systems.MahaonWorld.MahaonCropTable.Data;
            if (index < 0 || index >= crops.Length)
            {
                return;
            }

            var tile = new MahaonCropTile((Systems.MahaonWorld.MahaonCropType)index);
            tile.MoveToWorld(mobile.Location, mobile.Map);
            mobile.SendMessage(0x59, $"Грядка поставлена: {crops[index].NameRu}.");
            return;
        }

        // Raids
        if (buttonId is >= 7000 and < 8000)
        {
            switch (buttonId)
            {
                case 7000:
                    PlaceAtFeet(mobile, new MahaonRaidMarker());
                    break;

                case 7001:
                    PlaceAtFeet(mobile, new MahaonRaidSpawner());
                    break;

                case 7002:
                    var guardSpawner = new Spawner();
                    guardSpawner.MoveToWorld(mobile.Location, mobile.Map);
                    guardSpawner.AddEntry("MahaonTownDefender", 100, 3);
                    guardSpawner.OnDoubleClick(mobile);
                    mobile.SendMessage(0x59, "Спавнер защитников города поставлен и настроен на MahaonTownDefender.");
                    break;
            }

            return;
        }

        // Quests
        if (buttonId is >= 8000 and < 9000)
        {
            switch (buttonId)
            {
                case 8000:
                    var mayor = new Mobiles.MahaonMayor();
                    mayor.MoveToWorld(mobile.Location, mobile.Map);
                    mobile.SendMessage(0x59, "Мэр поставлен здесь — ПКМ → Properties, задай CityName.");
                    break;

                case 8001:
                    PlaceAtFeet(mobile, new Items.MahaonBountyBoard());
                    break;

                case 8002:
                    PlaceAtFeet(mobile, new Items.MahaonCraftingOrderBoard());
                    break;

                case 8003:
                    var guido = new Mobiles.MahaonGuardSergeant();
                    guido.MoveToWorld(mobile.Location, mobile.Map);
                    mobile.SendMessage(0x59, "Сержант Гвидо поставлен здесь.");
                    break;

                case 8004:
                    var librarian = new Mobiles.MahaonLibrarian();
                    librarian.MoveToWorld(mobile.Location, mobile.Map);
                    mobile.SendMessage(0x59, "Библиотекарь поставлен здесь.");
                    break;

                case 8005:
                    var ranger = new Mobiles.MahaonRanger();
                    ranger.MoveToWorld(mobile.Location, mobile.Map);
                    mobile.SendMessage(0x59, "Лесничий Питэр поставлен здесь.");
                    break;

                case 8006:
                    var courtMage = new Mobiles.MahaonCourtMage();
                    courtMage.MoveToWorld(mobile.Location, mobile.Map);
                    mobile.SendMessage(0x59, "Придворный маг поставлен здесь.");
                    break;
            }

            return;
        }

        // Areas
        if (buttonId is >= 9000 and < 9100)
        {
            if (buttonId == 9000)
            {
                PlaceAtFeet(mobile, new Items.MahaonAreaMarkingTool());
            }
        }
    }

    private static void PlaceAtFeet(Mobile mobile, Item item)
    {
        item.MoveToWorld(mobile.Location, mobile.Map);
        mobile.SendMessage(0x59, $"Поставлено: {item.Name ?? item.GetType().Name}.");
    }
}

using System.Collections.Generic;
using Server.Items;
using Server.Network;
using Server.Systems.MahaonCities;

namespace Server.Gumps;

// Two-step travel stone: pick a facet, then a city on that facet. Used to be one flat
// Felucca-only list (CityControlSystem.Cities). That list stays exactly as-is — it's also
// what guards/vendors/wildlife seeding key off, all Felucca-specific by design — this gump
// just adds a second, wider destination table of its own covering every facet actually
// available on this shard. Coordinates for Trammel come from CharacterCreation.cs's own
// TrammelStartingCities/NewHavenStartingCities (already real, already used elsewhere in
// this codebase); Ilshenar/Malas/Tokuno/TerMur come straight from this shard's own
// Distribution/Data/regions.json TownRegion GoLocation entries — the same points the engine
// itself treats as "the real city center" — rather than guessed.
public class CityTravelGump : StaticGump<CityTravelGump>
{
    // Was 50 copper (0.05 gold) back when copper/silver existed — a trivial convenience
    // fee, not a real economic sink. 1 gold is the closest equivalent now that copper no
    // longer exists as a denomination.
    private const int TravelCost = 1;

    private const int BackButtonId = 999;
    private const int FacetButtonBase = 500;

    private readonly Mobile _player;
    private readonly string _facet;

    public override bool Singleton => true;
    protected override bool Cached => false;

    private static readonly (string facet, (string city, Point3D loc, Map map)[] cities)[] Facets =
    {
        ("Felucca", FeluccaCities()),
        ("Trammel", new[]
        {
            ("Yew", new Point3D(633, 858, 0), Map.Trammel),
            ("Minoc", new Point3D(2476, 413, 15), Map.Trammel),
            ("Britain", new Point3D(1602, 1591, 20), Map.Trammel),
            ("Moonglow", new Point3D(4408, 1168, 0), Map.Trammel),
            ("Trinsic", new Point3D(1845, 2745, 0), Map.Trammel),
            ("Magincia", new Point3D(3734, 2222, 20), Map.Trammel),
            ("Jhelom", new Point3D(1374, 3826, 0), Map.Trammel),
            ("Skara Brae", new Point3D(618, 2234, 0), Map.Trammel),
            ("Vesper", new Point3D(2771, 976, 0), Map.Trammel)
        }),
        ("Ilshenar", new[]
        {
            ("Gargoyle City", new Point3D(840, 571, 0), Map.Ilshenar)
        }),
        ("Malas", new[]
        {
            ("Luna", new Point3D(989, 520, -50), Map.Malas),
            ("Umbra", new Point3D(2049, 1344, -85), Map.Malas)
        }),
        ("Tokuno", new[]
        {
            ("Zento", new Point3D(736, 1256, 30), Map.Tokuno)
        }),
        ("TerMur", new[]
        {
            ("Royal City", new Point3D(750, 3440, -20), Map.TerMur)
        }),

        // Подземелья — отдельными строками на фасет, а не третьим уровнем меню. Уровней и
        // так два, и третий ради одного разветвления сделал бы камень неудобнее ровно там,
        // где им пользуются чаще всего. Списки берутся из данных шарда, см.
        // DungeonTravelTargets.
        ("Подземелья: Felucca", DungeonTravelTargets.Felucca),
        ("Подземелья: Trammel", DungeonTravelTargets.Trammel),
        ("Подземелья: Ilshenar", DungeonTravelTargets.Ilshenar),
        ("Подземелья: Malas", DungeonTravelTargets.Malas),
        ("Подземелья: TerMur", DungeonTravelTargets.TerMur)
    };

    private static (string, Point3D, Map)[] FeluccaCities()
    {
        var list = new List<(string, Point3D, Map)>();

        foreach (var (name, info) in CityControlSystem.Cities)
        {
            list.Add((name, info.spawn, info.map));
        }

        return list.ToArray();
    }

    public CityTravelGump(Mobile player, string facet = null) : base(50, 50)
    {
        _player = player;
        _facet = facet;
    }

    private static (string city, Point3D loc, Map map)[] GetCities(string facet)
    {
        foreach (var (name, cities) in Facets)
        {
            if (name == facet)
            {
                return cities;
            }
        }

        return null;
    }

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        var cities = _facet == null ? null : GetCities(_facet);

        if (cities == null)
        {
            var height = 60 + Facets.Length * 25;

            builder.AddPage();
            builder.AddBackground(0, 0, 260, height, 5054);
            builder.AddAlphaRegion(10, 10, 240, height - 20);
            builder.AddHtml(15, 15, 230, 20, "Выбери фасет:");

            for (var i = 0; i < Facets.Length; i++)
            {
                var y = 45 + i * 25;
                builder.AddButton(15, y, 4005, 4007, FacetButtonBase + i);
                builder.AddLabelPlaceholder(45, y, 0x480, $"facet{i}");
            }
        }
        else
        {
            var height = 90 + cities.Length * 25;

            builder.AddPage();
            builder.AddBackground(0, 0, 260, height, 5054);
            builder.AddAlphaRegion(10, 10, 240, height - 20);
            builder.AddHtml(15, 15, 230, 20, $"Отправиться в ({_facet}):");

            for (var i = 0; i < cities.Length; i++)
            {
                var y = 45 + i * 25;
                builder.AddButton(15, y, 4005, 4007, i + 1);
                builder.AddLabelPlaceholder(45, y, 0x480, $"city{i}");
            }

            var backY = 50 + cities.Length * 25;
            builder.AddButton(15, backY, 4014, 4016, BackButtonId);
            builder.AddHtml(45, backY + 2, 190, 20, "Назад к фасетам");
        }
    }

    protected override void BuildStrings(ref GumpStringsBuilder builder)
    {
        var cities = _facet == null ? null : GetCities(_facet);

        if (cities == null)
        {
            for (var i = 0; i < Facets.Length; i++)
            {
                builder.SetStringSlot($"facet{i}", Facets[i].facet);
            }
        }
        else
        {
            for (var i = 0; i < cities.Length; i++)
            {
                builder.SetStringSlot($"city{i}", cities[i].city);
            }
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (_facet == null)
        {
            var facetIndex = info.ButtonID - FacetButtonBase;

            if (facetIndex < 0 || facetIndex >= Facets.Length)
            {
                return;
            }

            _player.SendGump(new CityTravelGump(_player, Facets[facetIndex].facet));
            return;
        }

        if (info.ButtonID == BackButtonId)
        {
            _player.SendGump(new CityTravelGump(_player));
            return;
        }

        var cities = GetCities(_facet);
        var index = info.ButtonID - 1;

        if (cities == null || index < 0 || index >= cities.Length)
        {
            return;
        }

        var (cityName, loc, map) = cities[index];

        if (_player.Map == map && _player.Location == loc)
        {
            _player.SendMessage("Ты уже там.");
            return;
        }

        var backpack = _player.Backpack;
        if (backpack == null || !backpack.ConsumeTotal(typeof(Gold), TravelCost))
        {
            _player.SendMessage($"Использование камня телепорта стоит {TravelCost} золота.");
            return;
        }

        _player.MoveToWorld(loc, map);
        _player.SendMessage(0x59, $"Ты телепортируешься в {cityName} ({_facet}).");
    }
}

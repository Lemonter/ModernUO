using ModernUO.Serialization;
using Server.Engines.CityLoyalty;

namespace Server.Items;

public enum CargoQuality
{
    Grandmaster,
    Exalted,
    Legendary,
    Mythical
}

public enum CargoType
{
    Cloth = 1257,
    Jewelry = 353,
    Wood = 1155,
    Metal = 1175,
    Munitions = 1157,
    Granite = 2498,
    Reagents = 1156,
    Glassware = 1158
}

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/RisingTide/MaritimeCargo.cs).
///     This item itself has no High Seas dependency (only the drop sources do), so it ports whole. The property
///     block uses <c>[SerializableProperty]</c> because both setters restyle the item and refresh the tooltip.
/// </summary>
[SerializationGenerator(0, false)]
[Flippable(0xA2C4, 0xA2C5)]
public partial class MaritimeCargo : Item
{
    [SerializableProperty(0)]
    [CommandProperty(AccessLevel.GameMaster)]
    public CargoQuality CargoQuality
    {
        get => _cargoQuality;
        set
        {
            _cargoQuality = value;

            if (_cargoQuality == CargoQuality.Mythical)
            {
                Hue = 1177;
            }

            InvalidateProperties();
        }
    }

    [SerializableProperty(1)]
    [CommandProperty(AccessLevel.GameMaster)]
    public CargoType CargoType
    {
        get => _cargoType;
        set
        {
            _cargoType = value;

            if (_cargoQuality != CargoQuality.Mythical && Hue != (int)_cargoType)
            {
                Hue = (int)_cargoType;
            }

            InvalidateProperties();
        }
    }

    [SerializableProperty(2)]
    [CommandProperty(AccessLevel.GameMaster)]
    public City City
    {
        get => _city;
        set
        {
            _city = value;
            InvalidateProperties();
        }
    }

    [Constructible]
    public MaritimeCargo() : this(RandomQuality(), RandomCity(), RandomType())
    {
    }

    [Constructible]
    public MaritimeCargo(CargoQuality quality) : this(quality, RandomCity(), RandomType())
    {
    }

    [Constructible]
    public MaritimeCargo(CargoQuality quality, City city, CargoType type) : base(0xA2C4)
    {
        CargoQuality = quality;
        City = city;
        CargoType = type;
    }

    private static CargoQuality RandomQuality()
    {
        var random = Utility.RandomDouble();

        if (random < 0.05)
        {
            return CargoQuality.Legendary;
        }

        return random < 0.33 ? CargoQuality.Exalted : CargoQuality.Grandmaster;
    }

    private static City RandomCity() => (City)Utility.Random(9);

    private static CargoType RandomType() => Utility.Random(8) switch
    {
        1 => CargoType.Jewelry,
        2 => CargoType.Wood,
        3 => CargoType.Metal,
        4 => CargoType.Munitions,
        5 => CargoType.Granite,
        6 => CargoType.Reagents,
        7 => CargoType.Glassware,
        _ => CargoType.Cloth
    };

    public int GetAwardAmount() => _cargoQuality switch
    {
        CargoQuality.Exalted   => Utility.RandomMinMax(500, 600),
        CargoQuality.Legendary => Utility.RandomMinMax(1000, 1100),
        CargoQuality.Mythical  => Utility.RandomMinMax(10000, 15000),
        _                      => Utility.RandomMinMax(100, 200)
    };

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1158906, $"#{CityLoyaltySystem.GetCityLocalization(_city)}"); // Maritime Trade Cargo Destined for ~1_CITY~
        list.Add(
            _cargoQuality < CargoQuality.Mythical ? 1158903 + (int)_cargoQuality : 1158969,
            $"#{TypeLabel(_cargoType)}"
        );
    }

    public static int TypeLabel(CargoType type) => type switch
    {
        CargoType.Jewelry   => 1011172,
        CargoType.Wood      => 1079435,
        CargoType.Metal     => 1049567,
        CargoType.Munitions => 1158902,
        CargoType.Granite   => 1158900,
        CargoType.Reagents  => 1002127,
        CargoType.Glassware => 1158901,
        _                   => 1044286
    };
}

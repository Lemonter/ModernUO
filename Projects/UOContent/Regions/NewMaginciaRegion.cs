using System.Text.Json.Serialization;

namespace Server.Regions;

/// <summary>
///     Городской регион Новой Маджинции. Единственное отличие от базового <see cref="TownRegion" /> —
///     размещение домов: на участке, за который у игрока есть действующий жетон аренды, дом ставить можно,
///     а внутри свободных зон застройки — по обычным правилам.
///
///     Портировано из ServUO (Scripts/Services/New Magincia/Housing Lotto/Core/Region.cs), где это был
///     отдельный класс <c>NewMaginciaRegion</c>. В этом репозитории такой регион уже существует и
///     регистрируется через <c>RegionJsonRegistration</c>, поэтому здесь добавлено только поведение.
/// </summary>
public class NewMaginciaRegion : TownRegion
{
    [JsonConstructor] // Don't include parent, since it is special
    public NewMaginciaRegion(string name, Map map, int priority, params Rectangle3D[] area) : base(name, map, priority, area)
    {
    }

    public NewMaginciaRegion(string name, Map map, Region parent, params Rectangle3D[] area)
        : base(name, map, parent, area)
    {
    }

    public NewMaginciaRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area)
        : base(name, map, parent, priority, area)
    {
    }

    public override bool AllowHousing(Mobile from, Point3D p)
    {
        if (Server.Engines.NewMagincia.MaginciaLottoSystem.Enabled && from?.Backpack != null)
        {
            foreach (var lease in from.Backpack.FindItemsByType<Server.Engines.NewMagincia.WritOfLease>())
            {
                if (lease is { Expired: false, Plot: not null } && lease.Plot.Bounds.Contains(p) &&
                    from.Map == lease.Plot.Map)
                {
                    return true;
                }
            }
        }

        return Server.Engines.NewMagincia.MaginciaLottoSystem.IsFreeHousingZone(p, Map);
    }
}

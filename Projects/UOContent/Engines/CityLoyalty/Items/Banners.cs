/// <summary>
///     Ported from ServUO (Scripts/Services/City Loyalty System/Items/Banners.cs).
///
///     Substitutions:
///     <list type="bullet">
///         <item>
///             ServUO's <c>InterchangeableAddon</c>/<c>InterchangeableAddonDeed</c> (a banner that flips between its
///             east and south art at placement time) do not exist here, and <c>Flippable</c> cannot flip an addon
///             component. The banner is therefore a plain <see cref="BaseAddon" /> placed facing south; the deed's
///             east id is kept only for the item art.
///         </item>
///         <item><c>PlayerMobile.AddRewardTitle</c>-based checks dropped — loyalty is the only precondition.</item>
///     </list>
/// </summary>
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.CityLoyalty;

public static class CityBannerInfo
{
    /// <summary>Item id (south facing) and cliloc per city, matching ServUO's <c>BannerInfo</c> table.</summary>
    public static readonly Dictionary<City, int[]> Table = new()
    {
        [City.Moonglow] = new[] { 0x4B63, 1098171 },
        [City.Britain] = new[] { 0x4B64, 1098172 },
        [City.Jhelom] = new[] { 0x4B65, 1098173 },
        [City.Yew] = new[] { 0x4B66, 1098174 },
        [City.Minoc] = new[] { 0x4B67, 1098175 },
        [City.Trinsic] = new[] { 0x4B62, 1098170 },
        [City.SkaraBrae] = new[] { 0x4B6A, 1098178 },
        [City.NewMagincia] = new[] { 0x4B69, 1098177 },
        [City.Vesper] = new[] { 0x4B68, 1098176 }
    };

    public static int SouthId(City city) => Table.TryGetValue(city, out var value) ? value[0] : 0x4B63;

    public static int EastId(City city) => SouthId(city) + 9;

    public static int Localization(City city) => Table.TryGetValue(city, out var value) ? value[1] : 1098171;
}

[SerializationGenerator(0, false)]
public partial class CityBanner : BaseAddon
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private City _city;

    [Constructible]
    public CityBanner() : this(City.Britain)
    {
    }

    [Constructible]
    public CityBanner(City city)
    {
        _city = city;

        AddComponent(new LocalizedAddonComponent(CityBannerInfo.SouthId(city), CityBannerInfo.Localization(city)), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new CityBannerDeed(_city);

    public override bool ForceShowProperties => true;
}

[SerializationGenerator(0, false)]
public partial class CityBannerDeed : BaseAddonDeed
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private City _city;

    [Constructible]
    public CityBannerDeed() : this(City.Britain)
    {
    }

    [Constructible]
    public CityBannerDeed(City city)
    {
        _city = city;
        LootType = LootType.Blessed;
    }

    public override BaseAddon Addon => new CityBanner(_city);

    public override int LabelNumber => CityBannerInfo.Localization(_city);

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return;
        }

        var sys = CityLoyaltySystem.GetCityInstance(_city);

        if (sys != null && CityLoyaltySystem.HasCitizenship(from, _city) &&
            sys.GetLoyaltyRating(from) >= LoyaltyRating.Commended)
        {
            base.OnDoubleClick(from);
            return;
        }

        // You are not sufficiently loyal to ~1_CITY~ to use this.
        from.SendLocalizedMessage(1152361, $"#{CityLoyaltySystem.GetCityLocalization(_city)}");
    }
}

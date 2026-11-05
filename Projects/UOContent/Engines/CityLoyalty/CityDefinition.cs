/// <summary>
///     Ported from ServUO (Scripts/Services/City Loyalty System/CityDefinition.cs and the enum block of
///     CityLoyaltySystem.cs).
///
///     Substitutions:
///     <list type="bullet">
///         <item>
///             ServUO looked the city <see cref="Region" /> up by <c>Name</c>, which was also the string shown to
///             players. Here the display name is Russian (the client cliloc is Russian) while the region lookup
///             needs the English region name from <c>Distribution/Data/regions.json</c>, so the two are split into
///             <see cref="Name" /> and <see cref="RegionName" />.
///         </item>
///         <item>
///             <c>Console.WriteLine</c> for a missing region became a logged warning.
///         </item>
///     </list>
/// </summary>
using System;
using System.Linq;
using Server.Logging;

namespace Server.Engines.CityLoyalty;

public enum City
{
    Moonglow,
    Britain,
    Jhelom,
    Yew,
    Minoc,
    Trinsic,
    SkaraBrae,
    NewMagincia,
    Vesper
}

public enum LoyaltyRating
{
    Disfavored,
    Disliked,
    Detested,
    Loathed,
    Despised,
    Reviled,
    Scorned,
    Shunned,
    Villified,
    Abhorred,
    Unknown,
    Doubted,
    Distrusted,
    Disgraced,
    Denigrated,
    Commended,
    Esteemed,
    Respected,
    Honored,
    Admired,
    Adored,
    Lauded,
    Exalted,
    Revered,
    Venerated
}

[Flags]
public enum CityTitle
{
    None = 0x00000000,
    Citizen = 0x00000002,
    Knight = 0x00000004,
    Baronet = 0x00000008,
    Baron = 0x00000010,
    Viscount = 0x00000020,
    Earl = 0x00000040,
    Marquis = 0x00000080,
    Duke = 0x00000100
}

public enum TradeDeal
{
    None = 0,
    GuildOfArcaneArts = 1154044,
    SocietyOfClothiers = 1154045,
    BardicCollegium = 1154046,
    OrderOfEngineers = 1154048,
    GuildOfHealers = 1154049,
    MaritimeGuild = 1154050,
    MerchantsAssociation = 1154051,
    MiningCooperative = 1154052,
    LeagueOfRangers = 1154053,
    GuildOfAssassins = 1154054,
    WarriorsGuild = 1154055
}

[PropertyObject]
public class CityDefinition
{
    private Region _region;

    public CityDefinition(
        City city, string regionName, Point3D stoneLoc, Point3D tradeLoc, Point3D heraldLoc, Point3D guardsmanLoc,
        Point3D boardLoc, string name, int localizedName, int battleMessage
    )
    {
        City = city;
        RegionName = regionName;
        StoneLocation = stoneLoc;
        TradeMinisterLocation = tradeLoc;
        HeraldLocation = heraldLoc;
        GuardsmanLocation = guardsmanLoc;
        BoardLocation = boardLoc;
        Name = name;
        LocalizedName = localizedName;
        BattleMessage = battleMessage;
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public City City { get; }

    [CommandProperty(AccessLevel.GameMaster)]
    public string RegionName { get; }

    [CommandProperty(AccessLevel.GameMaster)]
    public Point3D StoneLocation { get; }

    [CommandProperty(AccessLevel.GameMaster)]
    public Point3D TradeMinisterLocation { get; }

    [CommandProperty(AccessLevel.GameMaster)]
    public Point3D HeraldLocation { get; }

    [CommandProperty(AccessLevel.GameMaster)]
    public Point3D GuardsmanLocation { get; }

    [CommandProperty(AccessLevel.GameMaster)]
    public Point3D BoardLocation { get; }

    [CommandProperty(AccessLevel.GameMaster)]
    public string Name { get; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int LocalizedName { get; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int BattleMessage { get; }

    [CommandProperty(AccessLevel.GameMaster)]
    public Region Region
    {
        get
        {
            if (_region == null)
            {
                _region = Region.Regions.FirstOrDefault(r => r.Name == RegionName && r.Map == CityLoyaltySystem.SystemMap);

                if (_region == null)
                {
                    Logger.Warning("City Loyalty: region {Region} not found on {Map}", RegionName, CityLoyaltySystem.SystemMap);
                }
            }

            return _region;
        }
    }

    private static readonly ILogger Logger = LogFactory.GetLogger(typeof(CityDefinition));

    public override string ToString() => Name;
}

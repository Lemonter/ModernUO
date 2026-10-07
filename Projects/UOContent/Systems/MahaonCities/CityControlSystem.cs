using System.Collections.Generic;
using Server.Commands;
using Server.Guilds;
using Server.Mobiles;

namespace Server.Systems.MahaonCities;

/// <summary>
///     Mahaon city control: a guild can capture a city, collect a tax rate from it, and
///     station guards there that patrol normally (no teleporting town-guard nonsense) and
///     fight members of guilds it's at war with — reusing the engine's own
///     Guild.Enemies/war-declaration system rather than inventing a parallel rivalry
///     concept.
/// </summary>
public class CityControlSystem : GenericPersistence
{
    private static CityControlSystem _instance;

    // Real canonical Felucca town-center coordinates — the classic "starting city" inn
    // locations used across RunUO/ServUO-family shards. Good stand-ins for "town center"
    // until/unless the real Mahaon layout differs.
    public static readonly Dictionary<string, (Point3D spawn, Map map)> Cities = new()
    {
        ["Yew"] = (new Point3D(633, 858, 0), Map.Felucca),
        ["Minoc"] = (new Point3D(2476, 413, 15), Map.Felucca),
        ["Britain"] = (new Point3D(1496, 1628, 10), Map.Felucca),
        ["Moonglow"] = (new Point3D(4408, 1168, 0), Map.Felucca),
        ["Trinsic"] = (new Point3D(1845, 2745, 0), Map.Felucca),
        ["Magincia"] = (new Point3D(3734, 2222, 20), Map.Felucca),
        ["Jhelom"] = (new Point3D(1374, 3826, 0), Map.Felucca),
        ["Skara Brae"] = (new Point3D(618, 2234, 0), Map.Felucca),
        ["Vesper"] = (new Point3D(2771, 976, 0), Map.Felucca),
        ["Ocllo"] = (new Point3D(3667, 2625, 0), Map.Felucca)
    };

    private const int GuardsPerCity = 6;

    // How far a guard is allowed to wander from its post while patrolling — the stock
    // WalkRandomWithHome AI (Mobiles/AI/BaseAI/WalkRandomLogic.cs) already walks a mobile
    // back toward Home once it exceeds RangeHome, no teleporting involved, so this alone is
    // what keeps guards inside the city instead of drifting into the wilderness.
    public const int GuardPatrolRadius = 20;

    private static readonly Dictionary<string, Guild> Control = new();
    private static readonly Dictionary<string, int> TaxRate = new();
    private static readonly Dictionary<string, int> GuardLevel = new();

    /// <summary>One step of a city's guard: the metal its gear is forged of, the ingots of that
    /// metal and the gold the step costs the holding guild.</summary>
    public readonly record struct GuardStep(Systems.MahaonMetals.MahaonMetal Metal, int Ingots, int Gold);

    // One step per metal, from iron to the last mythic one; each costs more gold and more ingots
    // of a rarer metal.
    public static readonly GuardStep[] GuardSteps = BuildGuardSteps();

    private static GuardStep[] BuildGuardSteps()
    {
        var metals = System.Enum.GetValues<Systems.MahaonMetals.MahaonMetal>();
        var steps = new GuardStep[metals.Length];
        for (var i = 0; i < metals.Length; i++)
        {
            steps[i] = new GuardStep(metals[i], 1000 + 500 * i, 10_000 + 4_000 * i * i);
        }

        return steps;
    }

    public const int MaxMages = 3;

    public static int GetGuardLevel(string city) => GuardLevel.GetValueOrDefault(city, 0);

    /// <summary>The metal guards of this level are armed in; none before the first step.</summary>
    public static Systems.MahaonMetals.MahaonMetal? MetalFor(int level) =>
        level >= 1 && level <= GuardSteps.Length ? GuardSteps[level - 1].Metal : null;

    /// <summary>The next step for a city's guard, or null at the top.</summary>
    public static GuardStep? NextStep(string city) =>
        GetGuardLevel(city) < GuardSteps.Length ? GuardSteps[GetGuardLevel(city)] : null;

    /// <summary>What hiring one more battle mage costs at the city's guard level.</summary>
    public static int MageCost(string city) => 20_000 + GetGuardLevel(city) * 5_000;

    public static int MagesIn(string city)
    {
        var count = 0;
        foreach (var guard in CityGuard.Of(city))
        {
            if (guard is CityMageGuard { Alive: true, Deleted: false })
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>Raises a city's guard one step, paid from the holding guild's bank.</summary>
    public static bool UpgradeGuards(string city, Guild guild)
    {
        if (GetController(city) != guild || NextStep(city) is not { } step ||
            !MahaonBots.GuildBank.TrySpend(guild.Name, step.Gold, step.Metal, step.Ingots))
        {
            return false;
        }

        var level = GetGuardLevel(city) + 1;
        GuardLevel[city] = level;

        foreach (var guard in CityGuard.Of(city))
        {
            guard.ApplyLevel(level);
        }

        return true;
    }

    /// <summary>Hires a battle mage for a city, paid from the holding guild's bank.</summary>
    public static bool HireMage(string city, Guild guild)
    {
        if (GetController(city) != guild || MagesIn(city) >= MaxMages || !Cities.TryGetValue(city, out var info) ||
            !MahaonBots.GuildBank.TrySpend(guild.Name, MageCost(city), Systems.MahaonMetals.MahaonMetal.Iron, 0))
        {
            return false;
        }

        var radians = Utility.RandomDouble() * 2 * System.Math.PI;
        var radius = Utility.RandomMinMax(6, 14);
        var loc = FindGuardSpot(
            info.spawn.X + (int)(System.Math.Cos(radians) * radius),
            info.spawn.Y + (int)(System.Math.Sin(radians) * radius),
            info.map
        );

        var mage = new CityMageGuard(city, guild);
        mage.MoveToWorld(loc, info.map);
        mage.StationAt(loc, GuardPatrolRadius);
        mage.ApplyLevel(GetGuardLevel(city));
        return true;
    }

    public CityControlSystem() : base("MahaonCityControl", 1)
    {
    }

    public static void Configure()
    {
        _instance = new CityControlSystem();

        CommandSystem.Register("ClaimCity", AccessLevel.Player, ClaimCity_OnCommand);
        CommandSystem.Register("SetCityTax", AccessLevel.Player, SetCityTax_OnCommand);
        CommandSystem.Register("SetCityCenter", AccessLevel.GameMaster, SetCityCenter_OnCommand);
        CommandSystem.Register("CityStatus", AccessLevel.Player, CityStatus_OnCommand);
        CommandSystem.Register("UpgradeCityGuards", AccessLevel.Player, UpgradeCityGuards_OnCommand);
        CommandSystem.Register("HireCityMage", AccessLevel.Player, HireCityMage_OnCommand);
        CommandSystem.Register("GuildDeposit", AccessLevel.Player, GuildDeposit_OnCommand);
        CommandSystem.Register("GuildTreasury", AccessLevel.Player, GuildTreasury_OnCommand);
    }

    public static void Initialize()
    {
        // Apply any hand-marked city centers over the guessed defaults — see
        // [MarkCitySpot <city> center. Also runs here so a restart doesn't lose it.
        foreach (var city in new List<string>(Cities.Keys))
        {
            if (CityMarkers.TryGetMarker(city, "center", out var loc, out var map))
            {
                Cities[city] = (loc, map);
            }
        }
    }

    [Usage("SetCityCenter <city>")]
    [Description("Overrides a city's center point with your current location — use this if the built-in coordinates land somewhere wrong/empty on your map.")]
    private static void SetCityCenter_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;

        if (e.Length < 1 || !Cities.ContainsKey(e.GetString(0)))
        {
            from.SendMessage($"Использование: [SetCityCenter <{string.Join("|", Cities.Keys)}>");
            return;
        }

        var city = e.GetString(0);
        Cities[city] = (from.Location, from.Map);

        // Persist it the same way [MarkCitySpot does, so it survives a restart too.
        CityMarkers.SetMarker(city, "center", from.Location, from.Map);

        from.SendMessage(0x59, $"Центр города {city} обновлён на твою текущую позицию. Запусти [SeedCities заново, чтобы расставить торговцев по новому месту.");
    }

    public static Guild GetController(string city) => Control.GetValueOrDefault(city);

    // The town region each claimable city lies in, found from its centre and refound if the
    // centre is moved. A city found outside any town isn't remembered, so a region added later counts.
    private static readonly Dictionary<string, (Point3D at, Map map, Region region)> _townRegions = new();

    private static Region TownRegionOf(string city)
    {
        var (spawn, map) = Cities[city];
        if (_townRegions.TryGetValue(city, out var known) && known.at == spawn && known.map == map && known.region.Registered)
        {
            return known.region;
        }

        var region = map == null || map == Map.Internal ? null : Region.Find(spawn, map)?.GetRegion<Regions.TownRegion>();
        if (region != null)
        {
            _townRegions[city] = (spawn, map, region);
        }

        return region;
    }

    /// <summary>The claimable city this mobile stands in, if any.</summary>
    public static string CityAt(Mobile m)
    {
        foreach (var (city, info) in Cities)
        {
            if (info.map == m.Map && TownRegionOf(city) is { } region && m.Region?.IsPartOf(region) == true)
            {
                return city;
            }
        }

        return null;
    }

    /// <summary>The city key for a name spelled any which way ("skara brae"), or null.</summary>
    public static string Find(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        foreach (var city in Cities.Keys)
        {
            if (string.Equals(city, name, System.StringComparison.OrdinalIgnoreCase))
            {
                return city;
            }
        }

        return null;
    }

    /// <summary>The tax a held city puts on prices; nothing in a city nobody holds.</summary>
    public static int TaxIn(string city) => city != null && GetController(city) != null ? GetTaxRate(city) : 0;

    /// <summary>Hands the city's holder its tax.</summary>
    public static void CollectTax(string city, long amount)
    {
        if (amount > 0 && city != null && GetController(city) is { } guild)
        {
            MahaonBots.GuildBank.DepositGold(guild.Name, amount);
        }
    }

    public static int GetTaxRate(string city) => TaxRate.GetValueOrDefault(city, 0);

    /// <summary>
    ///     Whether a city's guards should treat this mobile as hostile: a member of a guild
    ///     at war with the controlling guild, by a war declared through the guild gump or set
    ///     between bot guilds.
    /// </summary>
    public static bool IsHostileToCity(string city, Mobile m)
    {
        var controller = GetController(city);
        if (controller == null || m is not PlayerMobile pm || pm.Guild is not Guild g || g == controller)
        {
            return false;
        }

        // Whoever is claiming the city at its banner is fought by its guards.
        return Items.MahaonCityClaimPoint.Of(city) is { Contested: true } point && point.Contender == g ||
               controller.Enemies.Contains(g) || g.Enemies.Contains(controller) ||
               MahaonBots.BotGuilds.GetRelation(controller.Name, g.Name) == MahaonBots.BotGuildRelation.War;
    }

    private static bool IsCityVulnerable(string city)
    {
        foreach (var guard in CityGuard.Of(city))
        {
            if (!guard.Deleted && guard.Alive)
            {
                return false;
            }
        }

        return true;
    }

    public static void Capture(string city, Guild guild)
    {
        DespawnGuards(city);
        Control[city] = guild;
        TaxRate[city] = 0;

        // The guard is the city's, trained and armed by whoever held it: a new holder starts over.
        GuardLevel.Remove(city);
        SpawnGuards(city, guild);
        Server.Systems.MahaonAi.MahaonForumBridge.OnCityCaptured(city, guild);

        // The site's city map would otherwise show the old owner until the next scheduled
        // snapshot — up to 20 minutes of being plainly wrong about the one thing that page
        // exists to show.
        Server.Systems.MahaonAi.MahaonWorldSnapshotBridge.PushNow();
    }

    private static void SpawnGuards(string city, Guild guild)
    {
        if (!Cities.TryGetValue(city, out var info))
        {
            return;
        }

        var markedSpots = CityMarkers.GetMarkersByPrefix(city, "guard");

        if (markedSpots.Count > 0)
        {
            foreach (var (loc, map) in markedSpots)
            {
                var guard = new CityGuard(city, guild);
                guard.MoveToWorld(loc, map);
                guard.StationAt(loc, GuardPatrolRadius);
            }

            return;
        }

        // No hand-marked posts for this city yet — fall back to a guessed ring around the
        // center point. Mark real spots with [MarkCitySpot <city> guard1 (guard2, ...) and
        // this branch stops being used for that city.
        var angleStep = 360.0 / GuardsPerCity;

        for (var i = 0; i < GuardsPerCity; i++)
        {
            var angle = i * angleStep + Utility.RandomMinMax(-10, 10);
            var radius = Utility.RandomMinMax(12, 22);
            var radians = angle * System.Math.PI / 180.0;

            var loc = FindGuardSpot(
                info.spawn.X + (int)(System.Math.Cos(radians) * radius),
                info.spawn.Y + (int)(System.Math.Sin(radians) * radius),
                info.map
            );

            var guard = new CityGuard(city, guild);
            guard.MoveToWorld(loc, info.map);
            guard.StationAt(loc, GuardPatrolRadius);
        }
    }

    private static Point3D FindGuardSpot(int x, int y, Map map)
    {
        for (var attempt = 0; attempt < 6; attempt++)
        {
            var z = map.GetAverageZ(x, y);
            var candidate = new Point3D(x, y, z);

            if (map.CanSpawnMobile(candidate))
            {
                return candidate;
            }

            // Nudge slightly and try again — a crude way to avoid spawning inside a wall.
            x += Utility.RandomMinMax(-3, 3);
            y += Utility.RandomMinMax(-3, 3);
        }

        return new Point3D(x, y, map.GetAverageZ(x, y));
    }

    private static void DespawnGuards(string city)
    {
        foreach (var guard in new List<CityGuard>(CityGuard.Of(city)))
        {
            guard.Delete();
        }
    }

    [Usage("ClaimCity <city>")]
    [Description("Lays your guild's claim to a city at its banner; held for ten minutes, the city is yours.")]
    private static void ClaimCity_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;

        if (from is not PlayerMobile pm || pm.Guild is not Guild guild)
        {
            from.SendMessage("Чтобы захватить город, нужно состоять в гильдии.");
            return;
        }

        if (guild.Leader != from)
        {
            from.SendMessage("Захватить город может только лидер гильдии.");
            return;
        }

        if (e.Length < 1 || !Cities.ContainsKey(e.GetString(0)))
        {
            from.SendMessage($"Использование: [ClaimCity <{string.Join("|", Cities.Keys)}>");
            return;
        }

        var city = e.GetString(0);

        if (GetController(city) == guild)
        {
            from.SendMessage("Твоя гильдия уже контролирует этот город.");
            return;
        }

        if (Items.MahaonCityClaimPoint.Of(city) is not { } point)
        {
            from.SendMessage($"В городе {city} нет знамени — захватить его нельзя.");
            return;
        }

        if (point.Map != from.Map || !from.InRange(point.GetWorldLocation(), Items.MahaonCityClaimPoint.ClaimRange))
        {
            from.SendMessage("Город берут у его знамени — подойди к нему.");
            return;
        }

        if (point.Contested)
        {
            from.SendMessage($"{city} уже оспаривает гильдия {point.Contender.Name}.");
            return;
        }

        point.StartContest(guild);
        from.SendMessage(0x59, "Права заявлены. Продержитесь у знамени десять минут.");
    }

    [Usage("SetCityTax <city> <percent>")]
    [Description("Sets the tax rate for a city your guild controls (0-30).")]
    private static void SetCityTax_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;

        if (from is not PlayerMobile pm || pm.Guild is not Guild guild)
        {
            from.SendMessage("Нужно состоять в гильдии.");
            return;
        }

        if (e.Length < 2 || !Cities.ContainsKey(e.GetString(0)) || !int.TryParse(e.GetString(1), out var percent))
        {
            from.SendMessage("Использование: [SetCityTax <город> <процент>");
            return;
        }

        var city = e.GetString(0);

        if (GetController(city) != guild)
        {
            from.SendMessage("Твоя гильдия не контролирует этот город.");
            return;
        }

        if (guild.Leader != from)
        {
            from.SendMessage("Установить налог может только лидер гильдии.");
            return;
        }

        TaxRate[city] = System.Math.Clamp(percent, 0, 30);
        from.SendMessage(0x59, $"Налог в {city} теперь {TaxRate[city]}%.");
    }

    [Usage("CityStatus")]
    [Description("Shows who controls each city and its tax rate.")]
    private static void CityStatus_OnCommand(CommandEventArgs e)
    {
        foreach (var city in Cities.Keys)
        {
            if (GetController(city) is not { } controller)
            {
                e.Mobile.SendMessage($"{city}: не контролируется");
            }
            else if (IsCityVulnerable(city))
            {
                e.Mobile.SendMessage(
                    $"{city}: {controller.Name} (налог {GetTaxRate(city)}%, стража {GetGuardLevel(city)} ур.) [уязвим]"
                );
            }
            else
            {
                e.Mobile.SendMessage(
                    $"{city}: {controller.Name} (налог {GetTaxRate(city)}%, стража {GetGuardLevel(city)} ур., магов {MagesIn(city)})"
                );
            }
        }
    }

    // A guild leader acting for a city the guild holds: the city named, or null with the reason told.
    private static string LeaderCity(CommandEventArgs e, string usage, out Guild guild)
    {
        var from = e.Mobile;
        guild = (from as PlayerMobile)?.Guild as Guild;

        if (guild == null || guild.Leader != from)
        {
            from.SendMessage("Это решает только лидер гильдии.");
            return null;
        }

        if (e.Length < 1 || Find(e.ArgString.Trim().Trim('"')) is not { } city)
        {
            from.SendMessage($"Использование: {usage}");
            return null;
        }

        if (GetController(city) != guild)
        {
            from.SendMessage("Твоя гильдия не контролирует этот город.");
            return null;
        }

        return city;
    }

    [Usage("UpgradeCityGuards <city>")]
    [Description("Raises the guard of a city your guild holds one step, paid in gold and ingots from the guild bank.")]
    private static void UpgradeCityGuards_OnCommand(CommandEventArgs e)
    {
        if (LeaderCity(e, "[UpgradeCityGuards <город>", out var guild) is not { } city)
        {
            return;
        }

        if (NextStep(city) is not { } step)
        {
            e.Mobile.SendMessage("Стража этого города уже на высшем уровне.");
            return;
        }

        var metalName = Systems.MahaonMetals.MahaonMetalTable.Get(step.Metal).RuName;
        if (!UpgradeGuards(city, guild))
        {
            e.Mobile.SendMessage(
                0x22,
                $"В казне гильдии не хватает: нужно {step.Gold} золота и {step.Ingots} слитков ({metalName})."
            );
            return;
        }

        e.Mobile.SendMessage(0x59, $"Стража {city} поднята до {GetGuardLevel(city)} уровня, доспехи — {metalName}.");
    }

    [Usage("HireCityMage <city>")]
    [Description("Hires a battle mage for a city your guild holds, paid from the guild bank.")]
    private static void HireCityMage_OnCommand(CommandEventArgs e)
    {
        if (LeaderCity(e, "[HireCityMage <город>", out var guild) is not { } city)
        {
            return;
        }

        if (MagesIn(city) >= MaxMages)
        {
            e.Mobile.SendMessage($"У {city} уже {MaxMages} боевых мага.");
            return;
        }

        var cost = MageCost(city);
        if (!HireMage(city, guild))
        {
            e.Mobile.SendMessage(0x22, $"В казне гильдии не хватает: маг стоит {cost} золота.");
            return;
        }

        e.Mobile.SendMessage(0x59, $"Боевой маг нанят в {city} за {cost} золота.");
    }

    [Usage("GuildDeposit")]
    [Description("Puts gold or ingots from your pack into your guild's bank.")]
    private static void GuildDeposit_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile.Guild is not Guild)
        {
            e.Mobile.SendMessage("Ты не в гильдии.");
            return;
        }

        e.Mobile.SendMessage("Что положить в казну гильдии? (золото или слитки)");
        e.Mobile.Target = new GuildDepositTarget();
    }

    private sealed class GuildDepositTarget : Server.Targeting.Target
    {
        public GuildDepositTarget() : base(2, false, Server.Targeting.TargetFlags.None)
        {
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (from.Guild is not Guild guild)
            {
                return;
            }

            if (targeted is not Item item || !item.IsChildOf(from.Backpack))
            {
                from.SendMessage("Положить можно только из своего рюкзака.");
                return;
            }

            var amount = item.Amount;
            if (!MahaonBots.GuildBank.Deposit(guild.Name, item))
            {
                from.SendMessage("Казна принимает только золото и слитки.");
                return;
            }

            from.SendMessage(0x59, $"В казну гильдии внесено: {amount}.");
        }
    }

    [Usage("GuildTreasury")]
    [Description("Shows the gold and ingots in your guild's bank.")]
    private static void GuildTreasury_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile.Guild is not Guild guild)
        {
            e.Mobile.SendMessage("Ты не в гильдии.");
            return;
        }

        e.Mobile.SendMessage(0x59, $"Казна гильдии {guild.Name}: {MahaonBots.GuildBank.GetGoldValue(guild.Name)} золота.");
        foreach (var (metal, info) in Systems.MahaonMetals.MahaonMetalTable.Data)
        {
            var ingots = MahaonBots.GuildBank.GetIngots(guild.Name, metal);
            if (ingots > 0)
            {
                e.Mobile.SendMessage($"{info.RuName}: {ingots} слитков");
            }
        }
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(1); // version
        writer.WriteEncodedInt(Control.Count);

        foreach (var (city, guild) in Control)
        {
            writer.Write(city);
            writer.Write(guild);
            writer.WriteEncodedInt(TaxRate.GetValueOrDefault(city, 0));
            writer.WriteEncodedInt(GuardLevel.GetValueOrDefault(city, 0));
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        var version = reader.ReadEncodedInt();

        var count = reader.ReadEncodedInt();
        for (var i = 0; i < count; i++)
        {
            var city = reader.ReadString();
            var guild = reader.ReadEntity<Guild>();
            var tax = reader.ReadEncodedInt();
            var level = version >= 1 ? reader.ReadEncodedInt() : 0;

            if (guild != null)
            {
                Control[city] = guild;
                TaxRate[city] = tax;
                GuardLevel[city] = level;
            }
        }
    }
}

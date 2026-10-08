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
/// <summary>Teachers a guild hires for its city's guard, each once.</summary>
[System.Flags]
public enum GuardTeachers
{
    None = 0,
    Bushido = 1,
    Shield = 2,
    Anatomy = 4,
    Resist = 8,
    Meditation = 16,
    Healer = 32,
    Blademaster = 64,
    Athlete = 128,
    Endurance = 256,
    Scholar = 512
}

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

    public const int GuardsPerCity = 6;

    // How far a guard is allowed to wander from its post while patrolling — the stock
    // WalkRandomWithHome AI (Mobiles/AI/BaseAI/WalkRandomLogic.cs) already walks a mobile
    // back toward Home once it exceeds RangeHome, no teleporting involved, so this alone is
    // what keeps guards inside the city instead of drifting into the wilderness.
    public const int GuardPatrolRadius = 20;

    private static readonly Dictionary<string, Guild> Control = new();
    private static readonly Dictionary<string, int> TaxRate = new();
    private static readonly Dictionary<string, int> GuardLevel = new();

    // Teachers the holding guild has hired for the city's guard.
    private static readonly Dictionary<string, GuardTeachers> Teachers = new();

    public static GuardTeachers TeachersOf(string city) => Teachers.GetValueOrDefault(city);

    /// <summary>What a teacher costs at the city's guard level.</summary>
    public static int TeacherCost(string city) => 25_000 + GetGuardLevel(city) * 5_000;

    /// <summary>Hires a teacher for a city's guard, paid from the holding guild's bank; the
    /// guards learn at once.</summary>
    public static bool HireTeacher(string city, Guild guild, GuardTeachers teacher)
    {
        if (GetController(city) != guild || (TeachersOf(city) & teacher) != 0 ||
            !MahaonBots.GuildBank.TrySpend(guild.Name, TeacherCost(city), Systems.MahaonMetals.MahaonMetal.Iron, 0))
        {
            return false;
        }

        Teachers[city] = TeachersOf(city) | teacher;
        CityGuardChatter.OnTeacher(city);
        foreach (var guard in CityGuard.Of(city))
        {
            guard.ApplyLevel(GetGuardLevel(city));
        }

        return true;
    }

    // Battle mages the holding guild has hired; the dead are replaced from the guild bank.
    private static readonly Dictionary<string, int> MageSlots = new();

    public static int GetMageSlots(string city) => MageSlots.GetValueOrDefault(city, 0);

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

    public static int SwordGuardsIn(string city)
    {
        var count = 0;
        foreach (var guard in CityGuard.Of(city))
        {
            if (guard is not CityMageGuard && guard.Alive && !guard.Deleted)
            {
                count++;
            }
        }

        return count;
    }

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
        CityGuardChatter.OnNewArmour(city, Systems.MahaonMetals.MahaonMetalTable.Get(step.Metal).RuName.ToLowerInvariant());

        foreach (var guard in CityGuard.Of(city))
        {
            guard.ApplyLevel(level);
        }

        return true;
    }

    /// <summary>Hires a battle mage for a city, paid from the holding guild's bank.</summary>
    public static bool HireMage(string city, Guild guild)
    {
        if (GetController(city) != guild || GetMageSlots(city) >= MaxMages ||
            !MahaonBots.GuildBank.TrySpend(guild.Name, MageCost(city), Systems.MahaonMetals.MahaonMetal.Iron, 0))
        {
            return false;
        }

        MageSlots[city] = GetMageSlots(city) + 1;
        SpawnGuard(city, guild, true);
        return true;
    }

    public CityControlSystem() : base("MahaonCityControl", 1)
    {
    }

    public static void Configure()
    {
        _instance = new CityControlSystem();

        // Players claim and run a city through its city stone; only staff tools are commands.
        CommandSystem.Register("SetCityCenter", AccessLevel.GameMaster, SetCityCenter_OnCommand);
        CommandSystem.Register("CityStatus", AccessLevel.GameMaster, CityStatus_OnCommand);
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
        MageSlots.Remove(city);
        Teachers.Remove(city);
        SpawnGuards(city, guild);
        CityGuardChatter.OnNewHolder(city, guild.Name);
        Server.Systems.MahaonAi.MahaonForumBridge.OnCityCaptured(city, guild);

        // The site's city map would otherwise show the old owner until the next scheduled
        // snapshot — up to 20 minutes of being plainly wrong about the one thing that page
        // exists to show.
        Server.Systems.MahaonAi.MahaonWorldSnapshotBridge.PushNow();
    }

    private static void SpawnGuards(string city, Guild guild)
    {
        for (var i = 0; i < GuardRoster(city); i++)
        {
            SpawnGuard(city, guild, false);
        }
    }

    /// <summary>How many sword guards the city keeps: one per hand-marked post, or a ring of six.</summary>
    public static int GuardRoster(string city)
    {
        var marked = CityMarkers.GetMarkersByPrefix(city, "guard").Count;
        return marked > 0 ? marked : GuardsPerCity;
    }

    /// <summary>
    /// Posts one guard: a sword guard at a hand-marked post nobody holds (else somewhere in the
    /// ring around the centre), a mage nearer the centre. Trained to the city's level.
    /// </summary>
    public static CityGuard SpawnGuard(string city, Guild guild, bool mage)
    {
        if (!Cities.TryGetValue(city, out var info) || info.map == null)
        {
            return null;
        }

        Point3D? post = null;
        var map = info.map;

        if (!mage)
        {
            foreach (var (loc, markMap) in CityMarkers.GetMarkersByPrefix(city, "guard"))
            {
                var taken = false;
                foreach (var guard in CityGuard.Of(city))
                {
                    if (guard is not CityMageGuard && guard.Alive && guard.Post == loc)
                    {
                        taken = true;
                        break;
                    }
                }

                if (!taken)
                {
                    post = loc;
                    map = markMap;
                    break;
                }
            }
        }

        if (post == null)
        {
            var radians = Utility.RandomDouble() * 2 * System.Math.PI;
            var radius = mage ? Utility.RandomMinMax(6, 14) : Utility.RandomMinMax(12, 22);
            post = FindGuardSpot(
                info.spawn.X + (int)(System.Math.Cos(radians) * radius),
                info.spawn.Y + (int)(System.Math.Sin(radians) * radius),
                info.map
            );
        }

        CityGuard spawned = mage ? new CityMageGuard(city, guild) : new CityGuard(city, guild);
        spawned.MoveToWorld(post.Value, map);
        spawned.StationAt(post.Value, GuardPatrolRadius);
        spawned.ApplyLevel(GetGuardLevel(city));
        return spawned;
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

    public const int MaxTax = 30;

    /// <summary>
    /// Lays a guild leader's claim to the city at its stone. Null when the claim is laid, else
    /// why not, in words for the claimant.
    /// </summary>
    public static string TryClaim(Mobile from, Items.MahaonCityClaimPoint point)
    {
        if (from is not PlayerMobile { Guild: Guild guild } || guild.Leader != from)
        {
            return "Заявить права на город может только лидер гильдии.";
        }

        var city = point.City;
        if (city == null || !Cities.ContainsKey(city))
        {
            return "Этот камень не привязан к городу.";
        }

        if (GetController(city) == guild)
        {
            return "Твоя гильдия уже держит этот город.";
        }

        if (point.Map != from.Map || !from.InRange(point.GetWorldLocation(), Items.MahaonCityClaimPoint.ClaimRange))
        {
            return "Город берут у его камня — подойди ближе.";
        }

        if (point.Contested)
        {
            return $"{city} уже оспаривает гильдия {point.Contender.Name}.";
        }

        point.StartContest(guild);
        return null;
    }

    /// <summary>Sets a held city's tax, from 0 to <see cref="MaxTax"/> percent, by its holder's leader.</summary>
    public static bool SetTax(string city, Mobile leader, int percent)
    {
        if (leader?.Guild is not Guild guild || guild.Leader != leader || GetController(city) != guild)
        {
            return false;
        }

        TaxRate[city] = System.Math.Clamp(percent, 0, MaxTax);
        return true;
    }

    /// <summary>Whether this mobile leads the guild holding the city.</summary>
    public static bool Rules(Mobile m, string city) =>
        city != null && m?.Guild is Guild guild && guild.Leader == m && GetController(city) == guild;

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

    /// <summary>The cursor that puts a pile from the pack into the guild bank.</summary>
    public sealed class GuildDepositTarget : Server.Targeting.Target
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
                from.SendMessage("Казна принимает только золото, слитки, бинты, зелья и свитки.");
                return;
            }

            from.SendMessage(0x59, $"В казну гильдии внесено: {amount}.");
        }
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(3); // version
        writer.WriteEncodedInt(Control.Count);

        foreach (var (city, guild) in Control)
        {
            writer.Write(city);
            writer.Write(guild);
            writer.WriteEncodedInt(TaxRate.GetValueOrDefault(city, 0));
            writer.WriteEncodedInt(GuardLevel.GetValueOrDefault(city, 0));
            writer.WriteEncodedInt(MageSlots.GetValueOrDefault(city, 0));
            writer.WriteEncodedInt((int)Teachers.GetValueOrDefault(city));
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
            var mages = version >= 2 ? reader.ReadEncodedInt() : 0;
            var teachers = version >= 3 ? (GuardTeachers)reader.ReadEncodedInt() : GuardTeachers.None;

            if (guild != null)
            {
                Control[city] = guild;
                TaxRate[city] = tax;
                GuardLevel[city] = level;
                MageSlots[city] = mages;
                Teachers[city] = teachers;
            }
        }
    }
}

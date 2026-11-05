/// <summary>
///     Ported from ServUO (Scripts/Services/City Loyalty System/Trading/TradeSystem.cs).
///
///     Structural changes:
///     <list type="bullet">
///         <item>
///             ServUO's <c>CityTradeSystem</c> derived from <c>PointsSystem</c>. That framework does not exist here, so
///             player trade records live in a static <c>Dictionary&lt;PlayerMobile, CityTradeEntry&gt;</c> saved by
///             <see cref="CityLoyaltyPersistence" /> (see CityLoyaltySystem.cs for the rationale).
///         </item>
///         <item><c>Config.Get</c> became <c>ServerConfiguration.GetSetting</c>.</item>
///         <item>
///             The Krampus seasonal-event integration was dropped: it would need <c>KrampusEncounter</c>'s spawn
///             location/map and per-creature type tables, which are event-specific and not part of this port. Trade
///             orders therefore stay available during that event.
///         </item>
///         <item>
///             <c>ProcessKill</c> (a points-framework hook) became a code-generated
///             <c>CreatureEvents.CreatureDeathEvent</c> handler using <c>BaseCreature.LastKiller</c>.
///         </item>
///         <item>
///             <c>Map.GetRandomSpawnPoint(Rectangle2D)</c> and the hand-rolled <c>CanFit</c> copy do not exist / are
///             not needed here — ambushers are placed with <c>Map.CanFit</c> on random points inside the ambush box.
///         </item>
///         <item>
///             Reward titles (<c>PlayerMobile.AddRewardTitle</c>) have no equivalent; <c>CheckTitle</c> is kept only to
///             raise nothing. <c>OnPlayerAdded</c> (which handed out a mysterious note on the points system's first
///             entry) is dropped — there is no "player added" event for this system any more.
///         </item>
///         <item><c>DateTime.UtcNow</c> for deadlines became <c>Core.Now</c>.</item>
///     </list>
/// </summary>
using System;
using System.Collections.Generic;
using System.Linq;
using ModernUO.CodeGeneratedEvents;
using Server.Items;
using Server.Multis;
using Server.Mobiles;
using Server.Regions;

namespace Server.Engines.CityLoyalty;

public class CityTradeSystem
{
    public static readonly int TurnInGold = ServerConfiguration.GetSetting("cityTrading.turnInGold", 10000);
    public static readonly int CrateDuration = ServerConfiguration.GetSetting("cityTrading.crateDuration", 24);
    public static readonly int AmbushWaitDuration = ServerConfiguration.GetSetting("cityTrading.ambushWaitDuration", 5);
    public static readonly int AmbusherDelete = ServerConfiguration.GetSetting("cityTrading.ambusherDelete", 10);

    private static readonly Type[] _seaTypes =
    {
        typeof(SeaSerpent), typeof(DeepSeaSerpent), typeof(Kraken), typeof(WaterElemental)
    };

    private static readonly Type[] _landTypes =
    {
        typeof(Troll), typeof(Ettin), typeof(GiantSpider), typeof(Brigand)
    };

    private static Dictionary<Type, string> _nameBuffer = new();

    public static Dictionary<Mobile, TradeOrderCrate> ActiveTrades { get; } = new();

    public static Dictionary<BaseCreature, DateTime> Ambushers { get; private set; } = new();

    public static Dictionary<PlayerMobile, CityTradeEntry> Entries { get; } = new();

    public static void Initialize()
    {
    }

    public CityTradeEntry GetEntry(Mobile m, bool create = false)
    {
        if (m is not PlayerMobile pm)
        {
            return null;
        }

        if (Entries.TryGetValue(pm, out var entry))
        {
            return entry;
        }

        if (!create)
        {
            return null;
        }

        entry = new CityTradeEntry(pm);
        Entries[pm] = entry;
        return entry;
    }

    public int GetMaxTrades(Mobile m) => Math.Min(8, Math.Max(1, (GetEntry(m)?.Completed ?? 0) / 25 + 1));

    public static bool HasTrade(Mobile from) => ActiveTrades.ContainsKey(from);

    public bool HasTurnIn(Mobile from, TradeMinister minister)
    {
        if (from == null || minister == null || !ActiveTrades.TryGetValue(from, out var crate) || crate == null)
        {
            return false;
        }

        return crate.Entry != null && crate.Entry.Origin != minister.City;
    }

    public bool TryOfferTrade(Mobile from, TradeMinister minister)
    {
        if (from == null || from.Backpack == null)
        {
            return true;
        }

        if (ActiveTrades.ContainsKey(from))
        {
            // It appears you are already delivering a trade order. Deliver your current order before requesting another.
            minister.SayTo(from, 1151722);
            return false;
        }

        var origin = minister.City;
        City destination;

        do
        {
            destination = CityLoyaltySystem.GetRandomCity();
        }
        while (destination == origin);

        var distance = GetDistance(minister, destination);
        var trades = Utility.RandomMinMax(1, GetMaxTrades(from));
        var entry = new TradeEntry(destination, origin, distance);
        var crate = new TradeOrderCrate(from, entry);

        GetEntry(from, true);

        for (var i = 0; i < trades; i++)
        {
            var worth = 1;
            string name = null;

            var type = GetRandomTrade(origin, destination, ref worth, ref name);

            if (type == null)
            {
                minister.SayTo(from, "Сейчас нет доступных торговых заказов.");
                crate.Delete();
                return false;
            }

            var amount = Utility.RandomList(5, 10, 15, 20);
            entry.Details.Add(new TradeEntry.TradeDetails(type, worth, amount, name));
        }

        if (!from.Backpack.TryDropItem(from, crate, false))
        {
            crate.Delete();
            // Your backpack cannot hold the Trade Order. Free up space and speak to the Trade Minister again.
            from.SendLocalizedMessage(114456);
            return false;
        }

        ActiveTrades[from] = crate;

        return true;
    }

    public bool TryTurnIn(Mobile from, TradeOrderCrate order, Mobile turninMobile)
    {
        if (order?.Entry == null || from == null || turninMobile == null)
        {
            return false;
        }

        var entry = order.Entry;
        var minister = turninMobile as TradeMinister;

        if (from.AccessLevel == AccessLevel.Player && minister != null && minister.City != entry.Destination)
        {
            // Begging thy pardon, but those goods are destined for the City of ~1_city~
            turninMobile.SayTo(from, 1151738, $"#{CityLoyaltySystem.GetCityLocalization(entry.Destination)}");
            return false;
        }

        if (!order.Fulfilled)
        {
            // This trade order has not been fulfilled. Fill the trade order with all necessary items and try again.
            turninMobile.SayTo(from, 1151732);
            return false;
        }

        CityLoyaltySystem.OnTradeComplete(from, entry);

        var pentry = GetEntry(from);

        if (pentry != null)
        {
            pentry.Points++;
            pentry.DistanceTraveled += entry.Distance;
            pentry.Completed++;
            CheckTitle(pentry);
        }

        order.Delete();
        return true;
    }

    public bool TryTurnInToSlim(Mobile from, TradeOrderCrate order, SlimTheFence slim)
    {
        if (order?.Entry == null || from == null || slim == null)
        {
            return false;
        }

        var entry = order.Entry;

        if (!order.Fulfilled)
        {
            slim.SayTo(from, 1151732);
            return false;
        }

        CityLoyaltySystem.OnSlimTradeComplete(from, entry);

        var pentry = GetEntry(from, true);

        if (pentry != null)
        {
            pentry.Points++;
            pentry.DistanceTraveled += entry.Distance;
            pentry.CompletedSlim++;
            CheckTitle(pentry);
        }

        // Haha! These goods will fetch me quite a bit o' coin! Thanks fer yer help!
        slim.SayTo(from, 1151736);

        order.Delete();
        return true;
    }

    /// <summary>ServUO granted reward titles here; this codebase has no reward-title system, so only counts change.</summary>
    public void CheckTitle(CityTradeEntry entry)
    {
    }

    public static void CancelTradeOrder(Mobile from, TradeOrderCrate crate)
    {
        if (crate == null)
        {
            return;
        }

        from ??= crate.Owner;

        if (from?.Backpack != null)
        {
            foreach (var item in new List<Item>(crate.Items))
            {
                from.Backpack.DropItem(item);
            }

            var entry = CityLoyaltySystem.CityTrading?.GetEntry(from, true);

            if (entry != null)
            {
                entry.Canceled++;
            }
        }

        crate.Delete();
    }

    public static string GetNameFor(Type t, string fallbackName)
    {
        if (_nameBuffer.TryGetValue(t, out var cached))
        {
            return cached;
        }

        var item = Loot.Construct(t);

        if (item == null)
        {
            return fallbackName;
        }

        var name = item.Name ?? item.LabelNumber.ToString();

        _nameBuffer[t] = name;
        item.Delete();

        return name;
    }

    public void RemoveCrate(Mobile from, TradeOrderCrate crate) => ActiveTrades.Remove(from);

    public static void OnPublicMoongateUsed(Mobile from)
    {
        if (ActiveTrades.TryGetValue(from, out var crate) && crate.Entry != null)
        {
            crate.Entry.Distance = 0;
        }
    }

    public static int GetDistance(TradeMinister origin, City destination)
    {
        var destMinister = TradeMinister.Ministers.Find(m => m.City == destination);

        return destMinister != null ? (int)origin.GetDistanceToSqrt(destMinister.Location) : 0;
    }

    public static Type GetRandomTrade(City originCity, City dest, ref int worth, ref string name)
    {
        var region = CityLoyaltySystem.GetCityInstance(originCity)?.Definition?.Region;

        if (region == null)
        {
            return null;
        }

        var list = region.GetMobiles()
            .OfType<BaseVendor>()
            .Where(bv => bv.GetBuyInfo() is { Length: > 0 })
            .ToList();

        while (list.Count > 0)
        {
            var vendor = list[Utility.Random(list.Count)];
            var buyInfo = vendor.GetBuyInfo();
            var info = buyInfo[Utility.Random(buyInfo.Length)] as GenericBuyInfo;

            if (info is not (null or BeverageBuyInfo or AnimalBuyInfo) && info.Type != null && info.Args == null &&
                info.Price < 5000)
            {
                worth = info.Price;
                name = info.Name;

                return info.Type;
            }

            list.Remove(vendor);
        }

        return null;
    }

    public static void OnTick()
    {
        var crates = new List<TradeOrderCrate>(ActiveTrades.Values);

        foreach (var crate in crates)
        {
            if (crate.Expired)
            {
                CancelTradeOrder(crate.Owner, crate);
            }
            else if (crate.Entry != null)
            {
                CheckAmbush(crate);
            }
        }

        if (Ambushers.Count == 0)
        {
            return;
        }

        var toDelete = new List<BaseCreature>();

        foreach (var (creature, expiry) in Ambushers)
        {
            if (expiry < Core.Now)
            {
                toDelete.Add(creature);
            }
        }

        foreach (var creature in toDelete)
        {
            if (!creature.Deleted)
            {
                creature.Delete();
            }

            Ambushers.Remove(creature);
        }
    }

    public static void CheckAmbush(TradeOrderCrate crate)
    {
        if (crate == null || crate.Deleted || crate.Entry == null || crate.Expired ||
            crate.Entry.LastAmbush + TimeSpan.FromMinutes(AmbushWaitDuration) > Core.Now)
        {
            return;
        }

        if (crate.RootParent is not Mobile m || m.Region.IsPartOf<GuardedRegion>())
        {
            return;
        }

        if (m.NetState == null || m.Map == null || m.Map == Map.Internal)
        {
            return;
        }

        var chance = crate.Entry.LastAmbush == DateTime.MinValue ? 0.25 : 0.05;

        if (chance <= Utility.RandomDouble())
        {
            return;
        }

        var difficulty = (double)(Math.Min(7200, m.SkillsTotal) + m.RawStr + m.RawInt + m.RawDex) / 10000;

        m.RevealingAction();

        SpawnCreatures(m, difficulty);
        crate.Entry.LastAmbush = Core.Now;
    }

    public static void SpawnCreatures(Mobile m, double difficulty)
    {
        var boat = BaseBoat.FindBoatAt(m.Location, m.Map);
        var types = GetCreatureType(m, boat != null);

        if (types == null)
        {
            return;
        }

        var amount = Utility.RandomMinMax(3, 5);

        for (var i = 0; i < amount; i++)
        {
            if (Activator.CreateInstance(types[Utility.Random(types.Length)]) is not BaseCreature bc)
            {
                continue;
            }

            Rectangle2D zone;

            if (boat != null)
            {
                if (boat.Facing is Direction.North or Direction.South)
                {
                    zone = new Rectangle2D(Utility.RandomBool() ? boat.X - 7 : boat.X + 4, m.Y - 4, 3, 3);
                }
                else
                {
                    zone = new Rectangle2D(m.X + 4, Utility.RandomBool() ? boat.Y - 7 : boat.Y + 4, 3, 3);
                }
            }
            else
            {
                zone = new Rectangle2D(m.X - 3, m.Y - 3, 6, 6);
            }

            var p = m.Location;

            if (m.Map != null)
            {
                for (var j = 0; j < 25; j++)
                {
                    var check = new Point3D(
                        Utility.RandomMinMax(zone.X, zone.X + zone.Width - 1),
                        Utility.RandomMinMax(zone.Y, zone.Y + zone.Height - 1),
                        m.Z
                    );

                    if (m.Map.CanFit(check.X, check.Y, check.Z, 16, false, true, false))
                    {
                        p = check;
                        break;
                    }
                }
            }

            foreach (var skill in bc.Skills)
            {
                if (skill.Base > 0)
                {
                    skill.Base += skill.Base * difficulty;
                }
            }

            bc.RawStr += (int)(bc.RawStr * difficulty);
            bc.RawInt += (int)(bc.RawInt * difficulty);
            bc.RawDex += (int)(bc.RawDex * difficulty);

            if (bc.HitsMaxSeed == -1)
            {
                bc.HitsMaxSeed = bc.RawStr;
            }

            if (bc.StamMaxSeed == -1)
            {
                bc.StamMaxSeed = bc.RawDex;
            }

            if (bc.ManaMaxSeed == -1)
            {
                bc.ManaMaxSeed = bc.RawInt;
            }

            bc.HitsMaxSeed += (int)(bc.HitsMaxSeed * difficulty);
            bc.StamMaxSeed += (int)(bc.StamMaxSeed * difficulty);
            bc.ManaMaxSeed += (int)(bc.ManaMaxSeed * difficulty);

            bc.Hits = bc.HitsMaxSeed;
            bc.Stam = bc.RawDex;
            bc.Mana = bc.RawInt;

            bc.PhysicalResistanceSeed += (int)(bc.PhysicalResistanceSeed * (difficulty / 3));
            bc.FireResistSeed += (int)(bc.FireResistSeed * (difficulty / 3));
            bc.ColdResistSeed += (int)(bc.ColdResistSeed * (difficulty / 3));
            bc.PoisonResistSeed += (int)(bc.PoisonResistSeed * (difficulty / 3));
            bc.EnergyResistSeed += (int)(bc.EnergyResistSeed * (difficulty / 3));

            Ambushers[bc] = Core.Now + TimeSpan.FromMinutes(AmbusherDelete);

            bc.MoveToWorld(p, m.Map);

            Timer.StartTimer(() => bc.Combatant = m);
        }

        // *Your keen senses alert you to an incoming ambush of attackers!*
        m.LocalOverheadMessage(MessageType.Regular, 1150, 1155479);
        // You have been ambushed! Fight for your honor!!!
        m.SendLocalizedMessage(1049330, "", 0x22);
    }

    public static Type[] GetCreatureType(Mobile m, bool wet) => wet ? _seaTypes : _landTypes;

    [OnEvent(nameof(CreatureEvents.CreatureDeathEvent))]
    public static void OnCreatureDeath(BaseCreature bc)
    {
        if (bc == null || !Ambushers.TryGetValue(bc, out _))
        {
            return;
        }

        var damager = bc.LastKiller;

        if (damager != null && ActiveTrades.TryGetValue(damager, out var crate) && crate.Entry != null)
        {
            crate.Entry.Kills++;
        }

        Ambushers.Remove(bc);
    }

    public static void SerializeState(IGenericWriter writer)
    {
        writer.Write(0); // version

        writer.Write(ActiveTrades.Count);
        foreach (var (m, crate) in ActiveTrades)
        {
            writer.Write(m);
            writer.Write(crate);
        }

        writer.Write(Ambushers.Count);
        foreach (var (bc, dt) in Ambushers)
        {
            writer.Write(bc);
            writer.Write(dt);
        }

        writer.Write(Entries.Count);
        foreach (var (pm, entry) in Entries)
        {
            writer.Write(pm);
            writer.Write(entry.Points);
            writer.Write(entry.DistanceTraveled);
            writer.Write(entry.Completed);
            writer.Write(entry.CompletedSlim);
            writer.Write(entry.Canceled);
        }
    }

    public static void DeserializeState(IGenericReader reader)
    {
        reader.ReadInt(); // version

        ActiveTrades.Clear();

        var crateCount = reader.ReadInt();
        for (var i = 0; i < crateCount; i++)
        {
            var m = reader.ReadEntity<Mobile>();
            var crate = reader.ReadEntity<Item>() as TradeOrderCrate;

            if (m != null && crate != null)
            {
                ActiveTrades[m] = crate;
            }
        }

        Ambushers.Clear();

        var ambusherCount = reader.ReadInt();
        for (var i = 0; i < ambusherCount; i++)
        {
            var bc = reader.ReadEntity<Mobile>() as BaseCreature;
            var dt = reader.ReadDateTime();

            if (bc == null)
            {
                continue;
            }

            if (dt < Core.Now)
            {
                bc.Delete();
            }
            else
            {
                Ambushers[bc] = dt;
            }
        }

        Entries.Clear();

        var entryCount = reader.ReadInt();
        for (var i = 0; i < entryCount; i++)
        {
            var pm = reader.ReadEntity<Mobile>() as PlayerMobile;
            var points = reader.ReadInt();
            var distance = reader.ReadInt();
            var completed = reader.ReadInt();
            var completedSlim = reader.ReadInt();
            var canceled = reader.ReadInt();

            if (pm == null)
            {
                continue;
            }

            Entries[pm] = new CityTradeEntry(pm)
            {
                Points = points,
                DistanceTraveled = distance,
                Completed = completed,
                CompletedSlim = completedSlim,
                Canceled = canceled
            };
        }

        _nameBuffer = new Dictionary<Type, string>();
    }
}

[PropertyObject]
public class CityTradeEntry
{
    public CityTradeEntry(PlayerMobile pm) => Player = pm;

    public PlayerMobile Player { get; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Points { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Canceled { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int DistanceTraveled { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Completed { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int CompletedSlim { get; set; }

    public override string ToString() => $"{Player?.Name}: {Completed} orders";
}

using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Spells;
using Server.Spells.Third;
using Server.Network;
using Server.Targeting;

namespace Server.Systems.Bots;

public partial class BotBrain
{
    // After a run of failed decoding attempts the map is put aside until this tick.
    internal long NextDecodeTick;
}

/// <summary>
/// Treasure maps as a bot sees them. A map is worth keeping when the bot can read it, survive its
/// guardians and open its chest — by lockpick, or for the two lowest levels by the Unlock spell;
/// any other map it finds goes to market.
/// </summary>
public static class BotTreasure
{
    // The same thresholds the map and the chest use (TreasureMap.GetMinSkillLevel,
    // TreasureMapChest.Fill), which keep them private.
    public static double DecodeSkill(int level) => level switch
    {
        1     => -3.0,
        2     => 41.0,
        3     => 51.0,
        4     => 61.0,
        _     => 70.0
    };

    public static int LockSkill(int level) => level switch
    {
        1 => 36,
        2 => 76,
        3 => 84,
        4 => 92,
        _ => 100
    };

    // MaxLockLevel of a chest: the top of the lockpicking chance curve.
    private static int MaxLockLevel(int level) => LockSkill(level) + 40;

    // A pick at half the curve opens the lock in a couple of tries.
    public static bool PicksLock(Mobile bot, int level) => bot.Skills.Lockpicking.Value >= MaxLockLevel(level) / 2.0;

    // Unlock reaches treasure chests up to level 2, when Magery * 0.8 - 4 covers the lock.
    public static bool UnlocksBySpell(Mobile bot, int level) =>
        level <= 2 && (int)(bot.Skills.Magery.Value * 0.8) - 4 >= LockSkill(level);

    /// <summary>The toughest map whose guardians the bot can take.</summary>
    public static int MaxLevel(Mobile bot) => Math.Clamp(1 + (int)(BotCombatStyles.FightingSkill(bot) / 25), 1, 5);

    public static bool Keeps(Mobile bot, TreasureMap map) =>
        map is { Deleted: false, Completed: false } && map.Level is >= 1 && map.Level <= MaxLevel(bot) &&
        (PicksLock(bot, map.Level) || UnlocksBySpell(bot, map.Level)) &&
        (map.Decoder == bot || map.Decoder == null && bot.Skills.Cartography.Value >= DecodeSkill(map.Level));

    /// <summary>The map the bot would go after now, if any.</summary>
    public static TreasureMap Usable(Mobile bot)
    {
        if (bot.Backpack is not { } pack)
        {
            return null;
        }

        TreasureMap best = null;
        foreach (var map in pack.FindItemsByType<TreasureMap>())
        {
            // A decoded map first: the reading is already done.
            if (Keeps(bot, map) && (best == null || map.Decoder == bot && best.Decoder != bot))
            {
                best = map;
            }
        }

        return best;
    }

    /// <summary>Whether this bot hunts treasure with picks and so keeps a few at hand.</summary>
    public static bool WantsLockpicks(Mobile bot) =>
        Usable(bot) is { } map && !UnlocksBySpell(bot, map.Level) && bot.Backpack.GetAmount(typeof(Lockpick)) < 2;

    public static TreasureMapChest ChestOf(Mobile bot, TreasureMap map)
    {
        var chestMap = map.ChestMap;
        if (chestMap == null)
        {
            return null;
        }

        var loc = new Point3D(map.ChestLocation, chestMap.GetAverageZ(map.ChestLocation.X, map.ChestLocation.Y));
        foreach (var chest in chestMap.GetItemsInRange<TreasureMapChest>(loc, 2))
        {
            if (chest.Owner == bot && !chest.Deleted)
            {
                return chest;
            }
        }

        return null;
    }
}

/// <summary>
/// A treasure map the bot can use: read it, travel to the spot, dig the chest up, beat its
/// guardians, open the lock and carry off what fits.
/// </summary>
public sealed class TreasureHuntGoal : BotGoal
{
    public override string Name => "Поиски клада";

    public override string[] News => ["Откопал клад! Сундук, полный золота.", "Ходил по карте сокровищ, едва ноги унёс от стражей."];

    public override double Score(BotBrain brain)
    {
        var bot = brain.Bot;
        if (bot.Hits < bot.HitsMax * 0.8 || !TreasureMap.HasDiggingTool(bot) || BotTreasure.Usable(bot) is not { } map)
        {
            return 0;
        }

        if (map.Decoder == null && Core.TickCount - brain.NextDecodeTick < 0)
        {
            return 0;
        }

        // A pick-hunter waits for picks; supply buys them.
        if (!BotTreasure.UnlocksBySpell(bot, map.Level) && bot.Backpack.FindItemByType<Lockpick>() == null)
        {
            return 0;
        }

        return 0.5 + BotBrain.Trait(brain.Greed) * 0.3 + BotBrain.Trait((byte)(100 - brain.Caution)) * 0.1;
    }

    public override List<BotAction> Plan(BotBrain brain)
    {
        var bot = brain.Bot;
        if (BotTreasure.Usable(bot) is not { } map)
        {
            return null;
        }

        if (map.Decoder == null)
        {
            return [new DecodeMapAction(map)];
        }

        var chestMap = map.ChestMap;
        var spot = new Point3D(map.ChestLocation, chestMap.GetAverageZ(map.ChestLocation.X, map.ChestLocation.Y));
        return [new GoToAction(chestMap, spot, 2, "к месту клада"), new DigTreasureAction(map), new TreasureChestAction(map)];
    }
}

/// <summary>Studies the map (a double-click) until it gives up its secret or the bot gives up.</summary>
public sealed class DecodeMapAction : BotAction
{
    private const int MaxTries = 8;
    private const long GiveUpMs = 15 * 60_000;

    private readonly TreasureMap _map;
    private int _tries;

    public DecodeMapAction(TreasureMap map) => _map = map;

    public override BotActionResult Tick(BotBrain brain)
    {
        var bot = brain.Bot;
        if (_map.Deleted || !_map.IsChildOf(bot.Backpack))
        {
            return BotActionResult.Failed();
        }

        if (_map.Decoder == bot)
        {
            return BotActionResult.Done(1000);
        }

        if (++_tries > MaxTries)
        {
            brain.NextDecodeTick = Core.TickCount + GiveUpMs;
            return BotActionResult.Failed();
        }

        _map.OnDoubleClick(bot);
        return BotActionResult.Running(3000);
    }

    public override string Describe(BotBrain brain) => "Разбирает карту сокровищ";
}

/// <summary>Digs where the map points, through the map's own dig target, and stands still until
/// the chest is up.</summary>
public sealed class DigTreasureAction : BotAction
{
    private const long MaxMs = 60_000;

    private readonly TreasureMap _map;
    private long _until;
    private bool _digging;

    public DigTreasureAction(TreasureMap map) => _map = map;

    public override void Start(BotBrain brain) => _until = Core.TickCount + MaxMs;

    public override BotActionResult Tick(BotBrain brain)
    {
        var bot = brain.Bot;
        if (_map.Deleted || Core.TickCount - _until >= 0)
        {
            return BotActionResult.Failed();
        }

        if (_map.Completed)
        {
            return BotActionResult.Done(500);
        }

        if (_digging)
        {
            // The dig timer holds the action; once released without a chest, the dig was broken off.
            return bot.CanBeginAction<TreasureMap>() ? BotActionResult.Failed() : BotActionResult.Running(1000);
        }

        var x = _map.ChestLocation.X;
        var y = _map.ChestLocation.Y;

        // The chest can't come up under the digger's feet.
        if (bot.X == x && bot.Y == y)
        {
            bot.Move(bot.Direction);
            return BotActionResult.Running(500);
        }

        _map.OnBeginDig(bot);
        if (bot.Target is not { } target)
        {
            return BotActionResult.Failed();
        }

        var chestMap = _map.ChestMap;
        target.Invoke(bot, new LandTarget(new Point3D(x, y, chestMap.GetAverageZ(x, y)), chestMap));
        _digging = !bot.CanBeginAction<TreasureMap>();
        return _digging ? BotActionResult.Running(1000) : BotActionResult.Failed();
    }

    public override string Describe(BotBrain brain) => "Копает клад";
}

/// <summary>
/// The dug-up chest: fights off its guardians, opens the lock by pick or spell, opens the lid as a
/// player does (the trap goes off on whoever opens it) and takes what the pack can carry,
/// gold first.
/// </summary>
public sealed class TreasureChestAction : BotAction
{
    private const long MaxMs = 15 * 60_000;
    private const int GuardianSight = 15;

    private readonly TreasureMap _map;
    private TreasureMapChest _chest;
    private BotAction _step;
    private long _until;
    private long _nextAttempt;

    public TreasureChestAction(TreasureMap map) => _map = map;

    public override void Start(BotBrain brain) => _until = Core.TickCount + MaxMs;

    public override BotActionResult Tick(BotBrain brain)
    {
        var bot = brain.Bot;
        if (Core.TickCount - _until >= 0)
        {
            return BotActionResult.Failed();
        }

        if (_step != null)
        {
            var result = _step.Tick(brain);
            if (result.Status == BotActionStatus.Running)
            {
                return result;
            }

            _step.Stop(brain);
            _step = null;
        }

        _chest ??= BotTreasure.ChestOf(bot, _map);
        if (_chest == null || _chest.Deleted)
        {
            return BotActionResult.Failed();
        }

        if (brain.Combat.Opponent is { Alive: true, Deleted: false })
        {
            return BotActionResult.Running(1000);
        }

        if (Guardian(bot) is { } guardian)
        {
            BotCombat.Engage(brain, guardian);
            return BotActionResult.Running(1000);
        }

        if (!bot.InRange(_chest.GetWorldLocation(), 1))
        {
            _step = new GoToAction(_chest.Map, _chest.Location, 1, "к сундуку");
            _step.Start(brain);
            return BotActionResult.Running(250);
        }

        if (_chest.Locked)
        {
            return Unlock(brain);
        }

        Loot(brain);
        return BotActionResult.Done(1500);
    }

    private Mobile Guardian(PlayerMobile bot)
    {
        if (_chest.Guardians == null)
        {
            return null;
        }

        foreach (var m in _chest.Guardians)
        {
            if (m is { Deleted: false, Alive: true } && m.Map == bot.Map && bot.InRange(m, GuardianSight))
            {
                return m;
            }
        }

        return null;
    }

    private BotActionResult Unlock(BotBrain brain)
    {
        var bot = brain.Bot;

        // A cast spell or a pick at work: hand the chest to whatever cursor it raised, then wait.
        if (bot.Target is { } cursor)
        {
            cursor.Invoke(bot, _chest);
            return BotActionResult.Running(1500);
        }

        if (Core.TickCount - _nextAttempt < 0)
        {
            return BotActionResult.Running(1000);
        }

        if (BotTreasure.UnlocksBySpell(bot, _chest.Level))
        {
            var spell = new UnlockSpell(bot);
            if (bot.Mana >= spell.GetMana() && spell.Cast())
            {
                _nextAttempt = Core.TickCount + 4000;
                return BotActionResult.Running(1000);
            }
        }

        if (bot.Backpack.FindItemByType<Lockpick>() is not { } pick)
        {
            return BotActionResult.Failed();
        }

        // The pick's own timer keeps trying until the lock yields or the pick breaks.
        pick.OnDoubleClick(bot);
        _nextAttempt = Core.TickCount + 6000;
        return BotActionResult.Running(500);
    }

    private void Loot(BotBrain brain)
    {
        var bot = brain.Bot;
        var pack = bot.Backpack;

        _chest.OnDoubleClick(bot);
        if (_chest.Locked || _chest.Deleted)
        {
            return;
        }

        var items = new List<Item>(_chest.Items);
        items.Sort((a, b) => (b is Gold).CompareTo(a is Gold));

        foreach (var item in items)
        {
            var reject = LRReason.Inspecific;
            if (item.Deleted || !_chest.CheckLift(bot, item, ref reject) || !pack.TryDropItem(bot, item, false))
            {
                continue;
            }

            _chest.OnItemLifted(bot, item);
            if (item is not Gold)
            {
                brain.MarkLoot(item);
            }
        }

        MahaonBots.BotRumors.Spread($"Говорят, {bot.Name} откопал клад!");
    }

    public override void Stop(BotBrain brain) => _step?.Stop(brain);

    public override string Describe(BotBrain brain) => _chest?.Locked == false ? "Обирает сундук с кладом" : "Вскрывает сундук с кладом";
}

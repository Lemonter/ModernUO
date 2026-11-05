using System;
using System.Collections.Generic;
using Server.Multis;
using Server.Regions;
using Server.Systems.MahaonMapEdits;
using Server.Systems.MahaonWeather;

namespace Server.Systems.MahaonCombat;

/// <summary>
///     Горение — состояние, как яд: персонаж горит несколько секунд и теряет здоровье огнём
///     каждую секунду. Гаснет само по времени или сразу, если встать в воду (в том числе в
///     лужу) или если идёт дождь и ты не под крышей. В воде и под дождём не загорается вовсе.
///     Новый поджог продлевает горение и берёт больший урон, а не складывает огни.
///
///     Клиенту — пакет состояния (MahaonStatusEffects, подтип 9): Unity рисует огонь на
///     персонаже. Урон огнём идёт через AOS.Damage — его режет сопротивление огню и он
///     засчитывается поджигателю.
///
///     Сейчас поджигают огненные заклинания мага, открывшего перк «огонь поджигает» в Школе
///     огня (SchoolPerks); дальше сюда же можно подключить горящие поля, зажигательные стрелы.
/// </summary>
public static class BurningSystem
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(1);

    private sealed class Entry
    {
        public Mobile Attacker;
        public int PerTick;
        public int TicksLeft;
        public Timer Timer;
    }

    private static readonly Dictionary<Mobile, Entry> Active = new();

    public static bool IsBurning(Mobile m) => m != null && Active.ContainsKey(m);

    public static int SecondsLeft(Mobile m) => m != null && Active.TryGetValue(m, out var e) ? e.TicksLeft : 0;

    public static int DamagePerTick(Mobile m) => m != null && Active.TryGetValue(m, out var e) ? e.PerTick : 0;

    public static void Ignite(Mobile victim, Mobile attacker, int perTick, int ticks)
    {
        if (victim == null || victim.Deleted || !victim.Alive || perTick <= 0 || ticks <= 0)
        {
            return;
        }

        if (InWater(victim) || InRain(victim))
        {
            return; // в воде и под дождём не загоришься
        }

        if (Active.TryGetValue(victim, out var entry))
        {
            entry.TicksLeft = Math.Max(entry.TicksLeft, ticks);
            entry.PerTick = Math.Max(entry.PerTick, perTick);
            entry.Attacker = attacker ?? entry.Attacker;
        }
        else
        {
            entry = new Entry { Attacker = attacker, PerTick = perTick, TicksLeft = ticks };
            entry.Timer = Timer.DelayCall(TickInterval, TickInterval, () => Tick(victim));
            Active[victim] = entry;
            victim.SendMessage(0x22, "Ты горишь! Вода потушит огонь.");
        }

        MahaonStatusEffects.Broadcast(victim, MahaonStatus.Burning, true, entry.TicksLeft);
    }

    public static void Extinguish(Mobile victim, string message = null)
    {
        if (victim == null || !Active.Remove(victim, out var entry))
        {
            return;
        }

        entry.Timer?.Stop();

        if (message != null)
        {
            victim.SendMessage(0x59, message);
        }

        MahaonStatusEffects.Broadcast(victim, MahaonStatus.Burning, false, 0);
    }

    private static bool InWater(Mobile m) =>
        m.Map != null && m.Map != Map.Internal && MahaonWaterFlow.IsFlooded(m.Map, m.X, m.Y);

    /// <summary>Под дождём: погода общая на весь мир, но в доме и в подземелье не мочит.</summary>
    private static bool InRain(Mobile m) =>
        WeatherSystem.Current is MahaonWeather.MahaonWeather.Rain or MahaonWeather.MahaonWeather.Storm &&
        m.Region?.IsPartOf<DungeonRegion>() != true && BaseHouse.FindHouseAt(m) == null;

    private static void Tick(Mobile victim)
    {
        if (!Active.TryGetValue(victim, out var entry))
        {
            return;
        }

        // Мёртвый, удалённый или вышедший из игры — гасим без урона.
        if (victim.Deleted || !victim.Alive || victim.Map == null || victim.Map == Map.Internal)
        {
            Extinguish(victim);
            return;
        }

        if (InWater(victim))
        {
            Extinguish(victim, "Вода гасит огонь.");
            return;
        }

        if (InRain(victim))
        {
            Extinguish(victim, "Дождь гасит огонь.");
            return;
        }

        var attacker = entry.Attacker is { Deleted: false } ? entry.Attacker : null;
        // Без эффекта на тик: огонь на горящем рисует клиент по пакету состояния (Unity —
        // StatusVisuals). Столб Flame Strike (0x3709) каждую секунду ложился поверх него.
        AOS.Damage(victim, attacker, entry.PerTick, 0, 100, 0, 0, 0);

        if (--entry.TicksLeft <= 0)
        {
            Extinguish(victim, "Огонь погас.");
            return;
        }

        MahaonStatusEffects.Broadcast(victim, MahaonStatus.Burning, true, entry.TicksLeft);
    }
}

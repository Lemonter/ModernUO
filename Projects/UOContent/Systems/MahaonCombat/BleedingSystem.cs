using System;
using System.Collections.Generic;
using Server.Items;

namespace Server.Systems.MahaonCombat;

/// <summary>
///     Bleeding — 1-2% of CURRENT (not max) Hits lost per tick, once per second, for up to
///     5 ticks, cured early only by successfully using bandages (any other healing —
///     potions, spells — doesn't touch it, matching the "similar to poison, but bandage-
///     only" request). While active, drops a random blood decal at the victim's feet each
///     tick — the decal itself is separate scenery that lingers for a minute regardless of
///     whether the bleed that dropped it is still going.
/// </summary>
public static class BleedingSystem
{
    private const double MinPercentPerTick = 0.01;
    private const double MaxPercentPerTick = 0.02;
    private const int MaxTicks = 5;
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan DecalLifetime = TimeSpan.FromMinutes(1);

    private static readonly int[] BloodGraphics = { 0x122A, 0x122B, 0x122C, 0x122D, 0x122E };

    private class BleedEntry
    {
        public Timer Timer;
        public int TicksLeft;
        public Mobile Attacker;
    }

    private static readonly Dictionary<Mobile, BleedEntry> Active = new();

    public static bool IsBleeding(Mobile m) => Active.ContainsKey(m);

    /// <summary>Roughly how many seconds of bleeding are left — one tick is one second, so
    /// this doubles as a seconds estimate. Used by MahaonStatusGump. 0 if not bleeding.</summary>
    public static int TicksLeft(Mobile m) => Active.TryGetValue(m, out var entry) ? entry.TicksLeft : 0;

    /// <summary>Пустить кровь. Урон тиков засчитывается тому, кто её пустил (attacker).</summary>
    public static void ApplyBleed(Mobile victim, Mobile attacker = null)
    {
        if (victim == null || victim.Deleted || !victim.Alive)
        {
            return;
        }

        if (Active.TryGetValue(victim, out var existing))
        {
            existing.TicksLeft = MaxTicks; // refresh, don't stack multiple timers
            existing.Attacker = attacker ?? existing.Attacker;
            MahaonStatusEffects.Broadcast(victim, MahaonStatus.Bleeding, true, existing.TicksLeft);
            return;
        }

        var entry = new BleedEntry { TicksLeft = MaxTicks, Attacker = attacker };
        entry.Timer = Timer.DelayCall(TickInterval, TickInterval, () => Tick(victim));
        Active[victim] = entry;

        victim.SendMessage(0x22, "Ты истекаешь кровью!");
        attacker?.SendMessage(0x22, $"{victim.Name} истекает кровью.");
        MahaonStatusEffects.Broadcast(victim, MahaonStatus.Bleeding, true, entry.TicksLeft);
    }

    /// <summary>Called from bandage application — the only thing that stops this
    /// early.</summary>
    public static void CureBleed(Mobile victim)
    {
        if (!Active.TryGetValue(victim, out var entry))
        {
            return;
        }

        entry.Timer?.Stop();
        Active.Remove(victim);
        victim.SendMessage(0x59, "Кровотечение остановлено.");
        MahaonStatusEffects.Broadcast(victim, MahaonStatus.Bleeding, false, 0);
    }

    private static void Tick(Mobile victim)
    {
        if (!Active.TryGetValue(victim, out var entry))
        {
            return;
        }

        // Мёртвый, удалённый или вышедший из игры перестаёт кровоточить. Раньше вышедший
        // продолжал терять здоровье офлайн — и на 1 HP мог умереть, не будучи в игре.
        if (victim.Deleted || !victim.Alive || victim.Map == null || victim.Map == Map.Internal)
        {
            entry.Timer?.Stop();
            Active.Remove(victim);
            MahaonStatusEffects.Broadcast(victim, MahaonStatus.Bleeding, false, 0);
            return;
        }

        var percent = Utility.RandomDouble() * (MaxPercentPerTick - MinPercentPerTick) + MinPercentPerTick;
        var damage = Math.Max(1, (int)(victim.Hits * percent));

        // Урон — от того, кто пустил кровь: раньше он был ничей, и убийство кровотечением
        // никому не засчитывалось.
        victim.Damage(damage, entry.Attacker is { Deleted: false } ? entry.Attacker : null);

        DropBloodDecal(victim);

        entry.TicksLeft--;

        if (entry.TicksLeft <= 0)
        {
            entry.Timer?.Stop();
            Active.Remove(victim);
            MahaonStatusEffects.Broadcast(victim, MahaonStatus.Bleeding, false, 0);
            return;
        }

        MahaonStatusEffects.Broadcast(victim, MahaonStatus.Bleeding, true, entry.TicksLeft);
    }

    private static void DropBloodDecal(Mobile victim)
    {
        if (victim.Map == null || victim.Map == Map.Internal)
        {
            return;
        }

        var graphic = BloodGraphics[Utility.Random(BloodGraphics.Length)];
        var blood = new MahaonBleedBlood(graphic);
        blood.MoveToWorld(victim.Location, victim.Map);
    }
}

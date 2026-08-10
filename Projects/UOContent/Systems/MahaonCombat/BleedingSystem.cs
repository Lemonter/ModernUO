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
    }

    private static readonly Dictionary<Mobile, BleedEntry> Active = new();

    public static bool IsBleeding(Mobile m) => Active.ContainsKey(m);

    public static void ApplyBleed(Mobile victim)
    {
        if (victim.Deleted || !victim.Alive)
        {
            return;
        }

        if (Active.TryGetValue(victim, out var existing))
        {
            existing.TicksLeft = MaxTicks; // refresh, don't stack multiple timers
            return;
        }

        var entry = new BleedEntry { TicksLeft = MaxTicks };
        entry.Timer = Timer.DelayCall(TickInterval, TickInterval, () => Tick(victim));
        Active[victim] = entry;

        victim.SendMessage(0x22, "Ты истекаешь кровью!");
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
    }

    private static void Tick(Mobile victim)
    {
        if (!Active.TryGetValue(victim, out var entry))
        {
            return;
        }

        if (victim.Deleted || !victim.Alive)
        {
            entry.Timer?.Stop();
            Active.Remove(victim);
            return;
        }

        var percent = Utility.RandomDouble() * (MaxPercentPerTick - MinPercentPerTick) + MinPercentPerTick;
        var damage = Math.Max(1, (int)(victim.Hits * percent));
        victim.Damage(damage);

        DropBloodDecal(victim);

        entry.TicksLeft--;

        if (entry.TicksLeft <= 0)
        {
            entry.Timer?.Stop();
            Active.Remove(victim);
        }
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

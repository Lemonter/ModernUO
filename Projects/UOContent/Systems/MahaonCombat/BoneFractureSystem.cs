using System;
using System.Collections.Generic;

namespace Server.Systems.MahaonCombat;

public enum FractureLocation
{
    Arms,
    Legs,
    Hands
}

/// <summary>
///     Bone fractures — a chance to also break something on a successful called shot to
///     Arms/Legs/Hands (see HitLocationSystem.ConsumeHitBonus, this is checked right
///     alongside it). No splints or items involved at all, per the request — heals purely
///     by waiting it out, with Healing/Anatomy skill shortening the wait (a skilled
///     medic's own body mends faster, no external item needed). Three different effects
///     depending on which bone broke:
///
///     - Arms:  -30% weapon damage dealt (BaseWeapon.OnHit)
///     - Legs:  can't run, walking only (EventSink.Movement)
///     - Hands: +50% spell fizzle chance (Spell.Cast)
/// </summary>
public static class BoneFractureSystem
{
    public const double FractureChance = 0.15; // checked once per successful called-shot hit to a fracturable location

    /// <summary>Может ли это место сломаться — для чека удара.</summary>
    public static bool CanFracture(HitLocation location) => location is HitLocation.Arms or HitLocation.Legs or HitLocation.Hands;
    private static readonly TimeSpan BaseDuration = TimeSpan.FromSeconds(90);

    private class FractureEntry
    {
        public FractureLocation Location;
        public Timer Timer;
        public DateTime HealAt;
    }

    private static readonly Dictionary<Mobile, FractureEntry> Active = new();

    /// <summary>Call from wherever a called shot successfully lands (see
    /// HitLocationSystem.ConsumeHitBonus) — location must already be Arms/Legs/Hands;
    /// Chest/Neck never fracture.</summary>
    public static void RollFracture(Mobile victim, HitLocation location, Mobile attacker = null)
    {
        if (victim == null || !victim.Alive || !CanFracture(location))
        {
            return;
        }

        if (FractureChance < Utility.RandomDouble())
        {
            return;
        }

        var fractureLocation = location switch
        {
            HitLocation.Arms  => FractureLocation.Arms,
            HitLocation.Legs  => FractureLocation.Legs,
            _                 => FractureLocation.Hands
        };

        Apply(victim, fractureLocation);

        // Раньше о переломе узнавал только тот, кому сломали.
        attacker?.SendMessage(0x22, $"Ты слышишь хруст — {victim.Name}: {RuLocationName(fractureLocation)} сломана.");
    }

    private static void Apply(Mobile victim, FractureLocation location)
    {
        if (Active.TryGetValue(victim, out var existing))
        {
            existing.Timer?.Stop();
        }

        // Own Healing/Anatomy shortens the wait — up to half off at 100 skill in both,
        // matching how those skills already shorten normal bandage timers elsewhere.
        var healSkill = (victim.Skills[SkillName.Healing].Value + victim.Skills[SkillName.Anatomy].Value) / 2.0;
        var scalar = 1.0 - healSkill / 200.0; // 100/100 skill -> 0.5x duration
        var duration = TimeSpan.FromSeconds(BaseDuration.TotalSeconds * Math.Clamp(scalar, 0.5, 1.0));

        var entry = new FractureEntry { Location = location, HealAt = Core.Now + duration };
        entry.Timer = Timer.DelayCall(duration, () => Heal(victim));
        Active[victim] = entry;

        victim.SendMessage(0x22, LocationMessage(location));
        MahaonStatusEffects.Broadcast(victim, MahaonStatus.Fracture, true, (int)duration.TotalSeconds, (byte)location);
    }

    private static void Heal(Mobile victim)
    {
        if (!Active.Remove(victim, out var entry))
        {
            return;
        }

        victim.SendMessage(0x59, "Перелом наконец сросся.");
        MahaonStatusEffects.Broadcast(victim, MahaonStatus.Fracture, false, 0, (byte)entry.Location);
    }

    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Mobile, object> LastRunWarning = new();

    /// <summary>«Сломанная нога не даёт бежать» — не чаще раза в 5 секунд.</summary>
    public static void TellCannotRun(Mobile m)
    {
        if (LastRunWarning.TryGetValue(m, out var last) && last is DateTime when && Core.Now - when < TimeSpan.FromSeconds(5))
        {
            return;
        }

        LastRunWarning.AddOrUpdate(m, Core.Now);
        m.SendMessage(0x22, "Сломанная нога не даёт бежать — иди шагом.");
    }

    public static bool HasFracture(Mobile m, FractureLocation location) =>
        Active.TryGetValue(m, out var entry) && entry.Location == location;

    /// <summary>Which single location is currently fractured, if any — used by
    /// MahaonStatusGump. Only one fracture is ever tracked at a time per mobile (a new
    /// break replaces the old one, see Apply).</summary>
    public static FractureLocation? GetActiveFracture(Mobile m) =>
        Active.TryGetValue(m, out var entry) ? entry.Location : null;

    public static TimeSpan GetTimeLeft(Mobile m) =>
        Active.TryGetValue(m, out var entry) ? Utility.Max(entry.HealAt - Core.Now, TimeSpan.Zero) : TimeSpan.Zero;

    public static string RuLocationName(FractureLocation location) => location switch
    {
        FractureLocation.Arms  => "рука",
        FractureLocation.Legs  => "нога",
        FractureLocation.Hands => "кисть",
        _                       => "?"
    };

    private static string LocationMessage(FractureLocation location) => location switch
    {
        FractureLocation.Arms  => "Ты слышишь хруст — рука сломана! Удары стали слабее, пока не срастётся.",
        FractureLocation.Legs  => "Ты слышишь хруст — нога сломана! Бегать не выйдет, пока не срастётся.",
        FractureLocation.Hands => "Ты слышишь хруст — кисть сломана! Колдовать стало тяжелее, пока не срастётся.",
        _                       => "Что-то сломано."
    };
}

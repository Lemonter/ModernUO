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
    private const double FractureChance = 0.15; // checked once per successful called-shot hit to a fracturable location
    private static readonly TimeSpan BaseDuration = TimeSpan.FromSeconds(90);

    private class FractureEntry
    {
        public FractureLocation Location;
        public Timer Timer;
    }

    private static readonly Dictionary<Mobile, FractureEntry> Active = new();

    /// <summary>Call from wherever a called shot successfully lands (see
    /// HitLocationSystem.ConsumeHitBonus) — location must already be Arms/Legs/Hands;
    /// Chest/Neck never fracture.</summary>
    public static void RollFracture(Mobile victim, HitLocation location)
    {
        if (location is not (HitLocation.Arms or HitLocation.Legs or HitLocation.Hands))
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

        var entry = new FractureEntry { Location = location };
        entry.Timer = Timer.DelayCall(duration, () => Heal(victim));
        Active[victim] = entry;

        victim.SendMessage(0x22, LocationMessage(location));
    }

    private static void Heal(Mobile victim)
    {
        if (!Active.Remove(victim))
        {
            return;
        }

        victim.SendMessage(0x59, "Перелом наконец сросся.");
    }

    public static bool HasFracture(Mobile m, FractureLocation location) =>
        Active.TryGetValue(m, out var entry) && entry.Location == location;

    private static string LocationMessage(FractureLocation location) => location switch
    {
        FractureLocation.Arms  => "Ты слышишь хруст — рука сломана! Удары стали слабее, пока не срастётся.",
        FractureLocation.Legs  => "Ты слышишь хруст — нога сломана! Бегать не выйдет, пока не срастётся.",
        FractureLocation.Hands => "Ты слышишь хруст — кисть сломана! Колдовать стало тяжелее, пока не срастётся.",
        _                       => "Что-то сломано."
    };
}

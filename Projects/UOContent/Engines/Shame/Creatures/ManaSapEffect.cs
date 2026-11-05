using System;
using System.Collections.Generic;

namespace Server.Mobiles;

/// <summary>
///     The mana-diverting curse the three Shame mages share. ServUO repeats the same
///     <c>static Dictionary&lt;Mobile, Timer&gt;</c> plus repeating <c>Timer.DelayCall</c> block verbatim in
///     BurningMage, CrazedMage and CorruptedMage; here it is one effect per victim, ticking every 1.5s
///     until it ends.
///
///     The repeat is a single self-restarting one-shot <see cref="Timer.StartTimer(TimeSpan, Action, out TimerExecutionToken)" />
///     whose token is held by the effect, so it is never an unreferenced timer: it stops when the roll
///     ends the curse, when either party dies or is deleted, or when the mage that applied it is deleted.
///     The victim's own <c>Mana</c> is drained and part of it comes back as direct damage, as in ServUO.
/// </summary>
internal sealed class ManaSapEffect
{
    private static readonly Dictionary<Mobile, ManaSapEffect> _table = new();

    private readonly Mobile _victim;
    private readonly Mobile _source;

    private TimerExecutionToken _token;

    private ManaSapEffect(Mobile victim, Mobile source)
    {
        _victim = victim;
        _source = source;
    }

    public static bool IsUnderEffects(Mobile victim) => victim != null && _table.ContainsKey(victim);

    public static void Begin(Mobile victim, Mobile source)
    {
        if (victim == null || source == null || victim.Deleted || _table.ContainsKey(victim))
        {
            return;
        }

        var effect = new ManaSapEffect(victim, source);
        _table[victim] = effect;

        victim.SendLocalizedMessage(1151482); // Your mana has been tainted!
        victim.SendLocalizedMessage(1151485); // Your mana is being diverted.

        effect.Resume();
    }

    public static void End(Mobile victim)
    {
        if (victim == null || !_table.Remove(victim, out var effect))
        {
            return;
        }

        effect._token.Cancel();

        if (!victim.Deleted)
        {
            victim.SendLocalizedMessage(1151486); // Your mana is no longer being diverted.
            victim.SendLocalizedMessage(1151483); // Your mana is no longer corrupted.
        }
    }

    /// <summary>Cancels every curse this creature is feeding (it died or was deleted).</summary>
    public static void EndAllFrom(Mobile source)
    {
        if (source == null || _table.Count == 0)
        {
            return;
        }

        var victims = new List<Mobile>(_table.Count);

        foreach (var (victim, effect) in _table)
        {
            if (effect._source == source)
            {
                victims.Add(victim);
            }
        }

        foreach (var victim in victims)
        {
            End(victim);
        }
    }

    private void Resume() => Timer.StartTimer(TimeSpan.FromSeconds(1.5), Tick, out _token);

    private void Tick()
    {
        if (_victim.Deleted || _source.Deleted || !_victim.Alive || !_source.Alive ||
            !_table.TryGetValue(_victim, out var active) || active != this)
        {
            End(_victim);
            return;
        }

        _victim.SendLocalizedMessage(1151484); // You feel extra mana being drawn from you.
        _victim.SendLocalizedMessage(1151481); // Channeling the corrupted mana has damaged you!

        var toSap = Math.Min(_victim.Mana, Utility.RandomMinMax(30, 40));
        _victim.Mana -= toSap;

        AOS.Damage(_victim, _source, Math.Max(1, toSap / 10), false, 0, 0, 0, 0, 0, 0, 100);

        if (0.5 > Utility.RandomDouble())
        {
            End(_victim);
            return;
        }

        Resume();
    }
}

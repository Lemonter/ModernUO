using System;
using System.Collections.Concurrent;
using Server.Mobiles;

namespace Server.Systems.MahaonCombat;

/// <summary>
///     Mahaon: elementals and golems are all spellcasters, capped at 5th circle.
///
///     Half of them were already AI_Mage in vanilla (fire/air/water/poison/blood/ice/acid,
///     crystal), the other half — every ore elemental, earth, snow, enraged earth, volcano,
///     the golem and the flesh golem — were plain melee. Their constructors have been
///     switched to AI_Mage and given Magery/EvalInt individually; this file holds the one
///     rule they share, the circle cap, so a newly added elemental is covered without
///     anyone remembering to override anything.
///
///     The cap is applied in MageAI.GetRandomDamageSpellMage, which otherwise derives the
///     circle purely from Magery. Deriving the cap from a low Magery instead would have
///     capped the circle at the cost of making them fizzle constantly — the point is
///     casters that reliably throw 5th-circle spells, not bad casters.
/// </summary>
public static class ElementalMagerySystem
{
    /// <summary>5th circle: Magic Reflection / Mind Blast / Paralyze / Poison Field /
    /// Incognito / Blade Spirits / Dispel Field / Summon Creature. Lightning and Mind Blast
    /// are the damage spells MageAI will actually reach at this cap.</summary>
    public const int SpellCircleCap = 5;

    /// <summary>No cap. Matches MageAI's own upper clamp.</summary>
    public const int NoCap = 8;

    // Resolved per Type once, then remembered — GetSpellCircleCap is called on every
    // damage-spell pick, so this must not be doing string work each time.
    private static readonly ConcurrentDictionary<Type, int> _capByType = new();

    public static int GetSpellCircleCap(BaseCreature bc) =>
        bc == null ? NoCap : _capByType.GetOrAdd(bc.GetType(), static t => Resolve(t) ? SpellCircleCap : NoCap);

    /// <summary>
    ///     Matched on the class name rather than a hand-kept list, so anything named
    ///     "...Elemental" or "...Golem" is covered the day it is added. GolemController and
    ///     GolemCrafter are humans who make/command golems, not golems, and neither name
    ///     ends in "Golem", so the suffix test excludes them for free.
    /// </summary>
    private static bool Resolve(Type type)
    {
        for (var t = type; t != null && t != typeof(BaseCreature); t = t.BaseType)
        {
            if (t.Name.EndsWith("Elemental", StringComparison.Ordinal) ||
                t.Name.EndsWith("Golem", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}

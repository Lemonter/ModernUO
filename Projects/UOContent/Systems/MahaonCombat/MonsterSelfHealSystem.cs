using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.Systems.MahaonCombat;

/// <summary>
///     "Разумные" monsters (anything not a mindless animal — see IsMindless below) use
///     bandages or heal potions on themselves when hurt, same as the existing player-side
///     AutoBandageSystem but for BaseCreature. Checked from BaseAI.Think() every tick, so
///     this throttles its own real work internally instead of actually running every tick.
/// </summary>
public static class MonsterSelfHealSystem
{
    private const double HealthThreshold = 0.6; // start trying to heal below 60% HP
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(2);

    private static readonly Dictionary<Mobile, DateTime> NextCheck = new();

    public static void TryHeal(BaseCreature creature)
    {
        if (!creature.Alive || creature.HitsMax <= 0 || IsMindless(creature))
        {
            return;
        }

        if ((double)creature.Hits / creature.HitsMax >= HealthThreshold)
        {
            return;
        }

        if (NextCheck.TryGetValue(creature, out var next) && Core.Now < next)
        {
            return;
        }

        NextCheck[creature] = Core.Now + CheckInterval;

        if (BandageContext.GetContext(creature) != null)
        {
            return; // already bandaging
        }

        // Potion first if things are dire and one's on hand — faster than bandaging.
        if ((double)creature.Hits / creature.HitsMax < 0.3)
        {
            var potion = creature.Backpack?.FindItemByType<BaseHealPotion>();

            if (potion != null)
            {
                potion.Drink(creature);
                return;
            }
        }

        var bandage = creature.Backpack?.FindItemByType<Bandage>();

        if (bandage != null)
        {
            Bandage.BandageTargetRequest(creature, bandage, creature);
        }
    }

    // Pure animals (wolves, bears, etc.) don't rummage through a pack for supplies the way
    // a person or an undead spellcaster would — only non-animal, non-mindless bodies get
    // this. Anything with SpeechHue-worthy intelligence should qualify; kept broad on
    // purpose rather than an explicit type list, per the "разумные" (sentient) framing.
    private static bool IsMindless(BaseCreature creature) => creature.Body.IsAnimal && !creature.Body.IsMonster;
}

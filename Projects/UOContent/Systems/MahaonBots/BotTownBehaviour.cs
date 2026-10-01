using System;
using System.Collections.Generic;
using Server.Commands;
using Server.Engines.Craft;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Spells;
using Server.Spells.First;
using Server.Spells.Second;
using Server.Spells.Third;
using Server.Spells.Fourth;
using Server.Spells.Fifth;
using Server.Spells.Sixth;
using Server.Spells.Seventh;
using Server.Spells.Eighth;
using Server.Spells.Necromancy;
using Server.Spells.Chivalry;
using Server.Spells.Bushido;
using Server.Spells.Ninjitsu;
using Server.Spells.Spellweaving;
using Server.Systems.MahaonAuction;
using Server.Systems.MahaonCities;
using Server.Systems.MahaonCombat;
using Server.Systems.MahaonMining;
using Server.Systems.MahaonProfessions;
using Server.Systems.MahaonRecipes;
using Server.Systems.MahaonRaids;
using Server.Targeting;

namespace Server.Systems.MahaonBots;

/// <summary>
///     Городские повадки: попрошайничество и воровство.
///
///     Обе идут по формулам настоящих навыков (Skills/Begging.cs, Skills/Stealing.cs),
///     только без интерактивного выбора цели — бот выбирает жертву сам и решает всё за
///     один заход.
/// </summary>
public partial class BotController
{
    // -- Bard begging ----------------------------------------------------------------------
    //
    // Same formula as the real Begging skill handler (Skills/Begging.cs), just without the
    // interactive target-and-wait flow — bots pick a nearby vendor and resolve it in one go.

    private const int BegRange = 3;

    private static bool TryBegNearby(Mobile beggar)
    {
        if (beggar.Map == null)
        {
            return false;
        }

        foreach (var target in beggar.Map.GetMobilesInRange<Mobile>(beggar.Location, BegRange))
        {
            if (target.Player || !target.Body.IsHuman || target == beggar)
            {
                continue;
            }

            var theirPack = target.Backpack;
            if (theirPack == null)
            {
                continue;
            }

            if (!beggar.CheckSkill(SkillName.Begging, 0.0, 100.0))
            {
                continue;
            }

            var toConsume = theirPack.GetAmount(typeof(Gold)) / 10;
            var max = System.Math.Clamp(10 + beggar.Fame / 2500, 10, 14);

            if (ProfessionSystem.TouchesCategory(beggar, ProfessionCategory.Bard))
            {
                max = (int)(max * 1.75);
            }

            if (toConsume > max)
            {
                toConsume = max;
            }

            if (toConsume <= 0)
            {
                continue;
            }

            var consumed = theirPack.ConsumeUpTo(typeof(Gold), toConsume);
            if (consumed <= 0)
            {
                continue;
            }

            beggar.Backpack?.DropItem(new Gold(consumed));
            return true;
        }

        return false;
    }

    private const int StealRange = 8;

    private static bool TryStealFromNearby(Mobile thief)
    {
        if (thief.Map == null)
        {
            return false;
        }

        if (!thief.Hidden)
        {
            // Try to slip into hiding first — no minigame, just a skill-based roll, same
            // simplification used everywhere else for bot actions.
            var hidingSkill = thief.Skills[SkillName.Hiding].Value;
            if (Utility.RandomDouble() < System.Math.Clamp(hidingSkill / 100.0, 0.1, 0.95))
            {
                thief.Hidden = true;
            }

            return true; // spend this tick getting into position either way
        }

        foreach (var victim in thief.Map.GetMobilesInRange<Mobile>(thief.Location, StealRange))
        {
            if (victim == thief || !victim.Alive || victim.Deleted || victim.Backpack == null)
            {
                continue;
            }

            if (victim is PlayerMobile victimPm && Bots.TryGetValue(victimPm, out var victimProfile) &&
                Bots.TryGetValue((PlayerMobile)thief, out var thiefProfile) &&
                victimProfile.Party != null && victimProfile.Party == thiefProfile.Party)
            {
                continue; // don't steal from your own party
            }

            if (TryStealFrom(thief, victim))
            {
                thief.Hidden = false; // the act itself breaks stealth, same as real stealing
                thief.CriminalAction(false);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Money is always on the table; Snooping unlocks going after worn jewelry too.
    ///     Higher skill means more "slots" (item types) worth trying in a single pass, not
    ///     just a better success roll — a master thief has more options, not just better odds.
    /// </summary>
    private static readonly Layer[] JewelryLayers = { Layer.Ring, Layer.Bracelet, Layer.Earrings, Layer.Neck };

    private static bool TryStealFrom(Mobile thief, Mobile victim)
    {
        var stealingSkill = thief.Skills[SkillName.Stealing].Value;
        var snoopingSkill = thief.Skills[SkillName.Snooping].Value;

        var slots = new List<System.Func<Mobile, Mobile, bool>> { TryStealMoney };

        var jewelrySlotCount = (int)(snoopingSkill / 25); // 0 at <25 snooping, up to 4 at 100+
        for (var i = 0; i < jewelrySlotCount && i < JewelryLayers.Length; i++)
        {
            var layer = JewelryLayers[i];
            slots.Add((t, v) => TryStealJewelry(t, v, layer));
        }

        var chosen = slots[Utility.Random(slots.Count)];
        return chosen(thief, victim);
    }

    private static bool TryStealMoney(Mobile thief, Mobile victim)
    {
        Item stack = victim.Backpack.FindItemByType<Gold>();

        if (stack == null)
        {
            return false;
        }

        var stealingSkill = thief.Skills[SkillName.Stealing].Value;
        var successChance = System.Math.Clamp(stealingSkill / 120.0, 0.05, 0.9);

        if (!thief.CheckSkill(SkillName.Stealing, 0.0, 100.0) || Utility.RandomDouble() >= successChance)
        {
            return false;
        }

        // 100 Stealing = takes the whole stack; scales down linearly below that.
        var takeFraction = System.Math.Clamp(stealingSkill / 100.0, 0.0, 1.0);
        var takeAmount = System.Math.Max(1, (int)(stack.Amount * takeFraction));

        if (takeAmount >= stack.Amount)
        {
            return thief.Backpack?.TryDropItem(thief, stack, false) == true;
        }

        stack.Amount -= takeAmount;

        Item stolen = new Gold(takeAmount);

        if (thief.Backpack?.TryDropItem(thief, stolen, false) != true)
        {
            stolen.Delete();
            return false;
        }

        return true;
    }

    private static bool TryStealJewelry(Mobile thief, Mobile victim, Layer layer)
    {
        if (victim.FindItemOnLayer(layer) is not Item piece)
        {
            return false;
        }

        var stealingSkill = thief.Skills[SkillName.Stealing].Value;
        var successChance = System.Math.Clamp(stealingSkill / 120.0, 0.05, 0.9);

        if (!thief.CheckSkill(SkillName.Stealing, 0.0, 100.0) || Utility.RandomDouble() >= successChance)
        {
            return false;
        }

        return thief.Backpack?.TryDropItem(thief, piece, false) == true;
    }
}

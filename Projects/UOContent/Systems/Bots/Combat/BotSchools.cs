using System;
using Server.Items;
using Server.Spells;
using Server.Spells.Bushido;
using Server.Spells.Chivalry;
using Server.Spells.Mysticism;
using Server.Spells.Necromancy;
using Server.Spells.Ninjitsu;
using Server.Spells.Spellweaving;

namespace Server.Systems.Bots;

public enum SchoolUse : byte
{
    /// <summary>Aimed at the foe.</summary>
    Attack,

    /// <summary>On self while fighting; recast when it has worn off (the spell refuses a
    /// duplicate itself).</summary>
    Buff,

    /// <summary>Heals self.</summary>
    Heal,

    /// <summary>Cures self of poison.</summary>
    Cure
}

/// <summary>One spell a school offers a bot, the skill it takes, and when to use it.</summary>
public sealed record SchoolSpell(SkillName Skill, double MinSkill, SchoolUse Use, Func<Mobile, Spell> Make, double Chance = 1.0);

/// <summary>
/// The fighting schools beyond Magery — Chivalry, Necromancy, Bushido, Ninjitsu, Spellweaving,
/// Mysticism — as one table: what to cast, at what skill, for what purpose. A bot uses whatever its
/// own skills allow; nothing is assigned by class. Casting goes through the spell itself, so mana,
/// tithing points, reagents, skill checks and fizzles are the spell's business.
/// </summary>
public static class BotSchools
{
    // Ordered strongest first within each use: the first castable entry wins.
    private static readonly SchoolSpell[] Book =
    [
        // Heals and cures.
        new(SkillName.Chivalry, 0, SchoolUse.Heal, m => new CloseWoundsSpell(m)),
        new(SkillName.Bushido, 25, SchoolUse.Heal, m => new Confidence(m, null)),
        new(SkillName.Spellweaving, 0, SchoolUse.Heal, m => new GiftOfRenewalSpell(m)),
        new(SkillName.Mysticism, 58, SchoolUse.Heal, m => new CleansingWindsSpell(m, null)),
        new(SkillName.Chivalry, 5, SchoolUse.Cure, m => new CleanseByFireSpell(m)),
        new(SkillName.Mysticism, 58, SchoolUse.Cure, m => new CleansingWindsSpell(m, null)),

        // Buffs while fighting.
        new(SkillName.Chivalry, 45, SchoolUse.Buff, m => new EnemyOfOneSpell(m), 0.3),
        new(SkillName.Chivalry, 25, SchoolUse.Buff, m => new DivineFurySpell(m), 0.3),
        new(SkillName.Chivalry, 15, SchoolUse.Buff, m => new ConsecrateWeaponSpell(m), 0.4),
        new(SkillName.Bushido, 40, SchoolUse.Buff, m => new CounterAttack(m, null), 0.3),
        new(SkillName.Ninjitsu, 20, SchoolUse.Buff, m => new MirrorImage(m, null), 0.2),
        new(SkillName.Necromancy, 0, SchoolUse.Buff, m => new CurseWeaponSpell(m), 0.3),
        new(SkillName.Spellweaving, 10, SchoolUse.Buff, m => new ImmolatingWeaponSpell(m), 0.3),

        // Attacks.
        new(SkillName.Spellweaving, 80, SchoolUse.Attack, m => new WordOfDeathSpell(m), 0.5),
        new(SkillName.Necromancy, 65, SchoolUse.Attack, m => new StrangleSpell(m), 0.5),
        new(SkillName.Mysticism, 70, SchoolUse.Attack, m => new HailStormSpell(m), 0.3),
        new(SkillName.Necromancy, 50, SchoolUse.Attack, m => new PoisonStrikeSpell(m)),
        new(SkillName.Mysticism, 20, SchoolUse.Attack, m => new EagleStrikeSpell(m)),
        new(SkillName.Spellweaving, 10, SchoolUse.Attack, m => new ThunderstormSpell(m)),
        new(SkillName.Necromancy, 20, SchoolUse.Attack, m => new EvilOmenSpell(m), 0.2),
        new(SkillName.Necromancy, 20, SchoolUse.Attack, m => new PainSpikeSpell(m)),
        new(SkillName.Mysticism, 0, SchoolUse.Attack, m => new NetherBoltSpell(m))
    ];

    /// <summary>The best casting skill among the schools that fight at range.</summary>
    public static double CasterSkill(Mobile bot) =>
        Math.Max(bot.Skills.Necromancy.Value, Math.Max(bot.Skills.Mysticism.Value, bot.Skills.Spellweaving.Value));

    /// <summary>Casts the first fitting spell of the given use; true when one started.</summary>
    public static bool TryCast(BotBrain brain, SchoolUse use, Mobile foe)
    {
        var bot = brain.Bot;
        if (bot.Spell != null || bot.Target != null)
        {
            return false;
        }

        foreach (var entry in Book)
        {
            if (entry.Use != use || bot.Skills[entry.Skill].Value < entry.MinSkill ||
                entry.Chance < 1.0 && Utility.RandomDouble() >= entry.Chance)
            {
                continue;
            }

            var spell = entry.Make(bot);
            if (BotHealing.TryCast(brain, spell, use == SchoolUse.Attack ? foe : bot))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Arms the next melee swing with a special move or weapon ability when the skills allow:
    /// Lightning Strike and Momentum Strike (Bushido), Focus Attack and Death Strike (Ninjitsu),
    /// otherwise the weapon's own abilities. The engine validates mana and skill on activation.
    /// </summary>
    public static void TryArmSwing(Mobile bot)
    {
        if (SpecialMove.GetCurrentMove(bot) != null || WeaponAbility.GetCurrentAbility(bot) != null ||
            Utility.RandomDouble() > 0.35)
        {
            return;
        }

        if (bot.Skills.Ninjitsu.Value >= 85 && Arm(bot, typeof(DeathStrike)) ||
            bot.Skills.Bushido.Value >= 70 && Arm(bot, typeof(MomentumStrike)) ||
            bot.Skills.Bushido.Value >= 50 && Arm(bot, typeof(LightningStrike)) ||
            bot.Skills.Ninjitsu.Value >= 30 && Arm(bot, typeof(FocusAttack)))
        {
            return;
        }

        if (bot.Weapon is BaseWeapon weapon)
        {
            var ability = Utility.RandomBool() ? weapon.PrimaryAbility : weapon.SecondaryAbility;
            if (ability != null)
            {
                WeaponAbility.SetCurrentAbility(bot, ability);
            }
        }
    }

    private static bool Arm(Mobile bot, Type moveType)
    {
        var move = SpellRegistry.GetSpecialMove(SpellRegistry.GetRegistryNumber(moveType));
        return move != null && SpecialMove.SetCurrentMove(bot, move);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using ModernUO.CodeGeneratedEvents;
using Server.Engines.BuffIcons;
using Server.Items;
using Server.Mobiles;
using Server.Systems.MahaonMasteries;

namespace Server.Spells.SkillMasteries;

// Ported from real OSI/ServUO content (Scripts/Spells/Skill Masteries/Core/MasteryInfo.cs).
// Every `m.Skills[x].VolumeLearned`/`m.Skills.CurrentMastery`/`HasLearnedMastery()` call from
// the original reads through MasteryState instead (see that file for why — none of that API
// exists on this codebase's Skill/Skills classes). Login restoration uses this codebase's
// real hook (`[OnEvent(nameof(PlayerMobile.PlayerLoginEvent))]`, same pattern already used
// elsewhere this session), not ServUO's `EventSink.Login`.
public enum Volume
{
    None,
    One,
    Two,
    Three
}

public enum PassiveSpell
{
    None,
    EnchantedSummoning,
    Intuition,
    SavingThrow,
    Potency,
    Knockout,
    Boarding,
    AnticipateHit
}

public class MasteryInfo
{
    public const int MinSkillRequirement = 90;

    public static List<MasteryInfo> Infos { get; private set; }

    public static void Configure()
    {
        Infos = new List<MasteryInfo>
        {
            new(typeof(InspireSpell), 700, SkillName.Provocation),
            new(typeof(InvigorateSpell), 701, SkillName.Provocation),

            new(typeof(ResilienceSpell), 702, SkillName.Peacemaking),
            new(typeof(PerseveranceSpell), 703, SkillName.Peacemaking),

            new(typeof(TribulationSpell), 704, SkillName.Discordance),
            new(typeof(DespairSpell), 705, SkillName.Discordance),

            new(typeof(DeathRaySpell), 706, SkillName.Magery),
            new(typeof(EtherealBurstSpell), 707, SkillName.Magery),

            new(typeof(NetherBlastSpell), 708, SkillName.Mysticism),
            new(typeof(MysticWeaponSpell), 709, SkillName.Mysticism),

            new(typeof(CommandUndeadSpell), 710, SkillName.Necromancy),
            new(typeof(ConduitSpell), 711, SkillName.Necromancy),

            new(typeof(ManaShieldSpell), 712, SkillName.Spellweaving),
            new(typeof(SummonReaperSpell), 713, SkillName.Spellweaving),

            new(typeof(WarcrySpell), 716, SkillName.Bushido),

            new(typeof(RejuvenateSpell), 718, SkillName.Chivalry),
            new(typeof(HolyFistSpell), 719, SkillName.Chivalry),

            new(typeof(ShadowSpell), 720, SkillName.Ninjitsu),
            new(typeof(WhiteTigerFormSpell), 721, SkillName.Ninjitsu),

            new(typeof(FlamingShotSpell), 722, SkillName.Archery),
            new(typeof(PlayingTheOddsSpell), 723, SkillName.Archery),

            new(typeof(ThrustSpell), 724, SkillName.Fencing),
            new(typeof(PierceMove), 725, SkillName.Fencing),

            new(typeof(StaggerMove), 726, SkillName.Macing),
            new(typeof(ToughnessSpell), 727, SkillName.Macing),

            new(typeof(OnslaughtMove), 728, SkillName.Swords),
            new(typeof(FocusedEyeSpell), 729, SkillName.Swords),

            new(typeof(ElementalFurySpell), 730, SkillName.Throwing),
            new(typeof(CalledShotSpell), 731, SkillName.Throwing),

            new(typeof(ShieldBashSpell), 733, SkillName.Parry),
            new(typeof(BodyGuardSpell), 734, SkillName.Parry),
            new(typeof(HeightenedSensesSpell), 735, SkillName.Parry),

            new(typeof(ToleranceSpell), 736, SkillName.Poisoning),
            new(typeof(InjectedStrikeSpell), 737, SkillName.Poisoning),

            new(typeof(RampageSpell), 739, SkillName.Wrestling),
            new(typeof(FistsOfFuryMove), 740, SkillName.Wrestling),

            new(typeof(WhisperingSpell), 742, SkillName.AnimalTaming),
            new(typeof(CombatTrainingSpell), 743, SkillName.AnimalTaming),

            // Passive masteries — no spell type, just a per-skill flag consulted directly
            // by MasteryInfo's own bonus getters below.
            new(null, 714, SkillName.Magery, PassiveSpell.EnchantedSummoning),
            new(null, 714, SkillName.Necromancy, PassiveSpell.EnchantedSummoning),
            new(null, 714, SkillName.Spellweaving, PassiveSpell.EnchantedSummoning),
            new(null, 714, SkillName.Mysticism, PassiveSpell.EnchantedSummoning),

            new(null, 715, SkillName.Bushido, PassiveSpell.AnticipateHit),

            new(null, 717, SkillName.Bushido, PassiveSpell.Intuition),
            new(null, 717, SkillName.Ninjitsu, PassiveSpell.Intuition),
            new(null, 717, SkillName.Chivalry, PassiveSpell.Intuition),

            new(null, 732, SkillName.Archery, PassiveSpell.SavingThrow),
            new(null, 732, SkillName.Fencing, PassiveSpell.SavingThrow),
            new(null, 732, SkillName.Swords, PassiveSpell.SavingThrow),
            new(null, 732, SkillName.Macing, PassiveSpell.SavingThrow),
            new(null, 732, SkillName.Throwing, PassiveSpell.SavingThrow),

            new(null, 738, SkillName.Poisoning, PassiveSpell.Potency),
            new(null, 741, SkillName.Wrestling, PassiveSpell.Knockout),
            new(null, 744, SkillName.AnimalTaming, PassiveSpell.Boarding)
        };
    }

    public System.Type SpellType { get; }
    public int SpellID { get; }
    public SkillName MasterySkill { get; }
    public bool Passive => PassiveSpell != PassiveSpell.None;
    public PassiveSpell PassiveSpell { get; }

    public MasteryInfo(System.Type spellType, int spellID, SkillName masterySkill, PassiveSpell passive = PassiveSpell.None)
    {
        SpellType = spellType;
        SpellID = spellID;
        MasterySkill = masterySkill;
        PassiveSpell = passive;
    }

    public static MasteryInfo GetInfo(System.Type spell, SkillName skill) =>
        Infos.FirstOrDefault(info => info.SpellType == spell && info.MasterySkill == skill);

    public static MasteryInfo GetInfo(int spellId) => Infos.FirstOrDefault(info => info.SpellID == spellId);

    public static SkillName GetSkillForID(int spellId) => GetInfo(spellId)?.MasterySkill ?? SkillName.Archery;

    public static bool HasLearned(Mobile m, SkillName skill) => MasteryState.HasLearnedMastery(m, skill);

    public static bool HasLearned(Mobile m, SkillName skill, int volume) => MasteryState.HasLearnedVolume(m, skill, volume);

    public static bool HasLearned(Mobile m, System.Type spell, SkillName skill)
    {
        var info = GetInfo(spell, skill);
        return info != null && MasteryState.HasLearnedMastery(m, info.MasterySkill);
    }

    public static int GetMasteryLevel(Mobile m, SkillName name) => MasteryState.GetVolumeLearned(m, name);

    public static bool CanLearn(Mobile m, int spellId, SkillName skill)
    {
        var info = Infos.FirstOrDefault(i => i.SpellID == spellId && i.MasterySkill == skill);
        return info != null && m.Skills[info.MasterySkill].Value >= MinSkillRequirement;
    }

    public static bool LearnMastery(Mobile m, SkillName skill, int volume) => MasteryState.LearnMastery(m, skill, volume);

    public static bool IsPassiveMastery(int spellId) =>
        spellId is 714 or 715 or 716 or 732 or 738 or 741 or 744;

    /// <summary>Called whenever the active mastery changes (or on login, to restore
    /// whatever was already active) — expires every running active-mastery spell, clears
    /// any mastery-flavored SpecialMove, and reapplies the passive buff icon for the new
    /// mastery (if it has one).</summary>
    public static void OnMasteryChanged(Mobile m, SkillName oldMastery)
    {
        var newMastery = MasteryState.GetCurrentMastery(m);

        if (oldMastery != newMastery)
        {
            foreach (var spell in SkillMasterySpell.GetSpells(m))
            {
                spell.Expire();
            }

            if (m is PlayerMobile pm && oldMastery == SkillName.Necromancy)
            {
                // Command Undead's "borrowed" undead give up control when the caster
                // leaves the Necromancy mastery path — scoped to creatures this spell
                // itself commanded (WasCommandedByThisSpell), not every undead-eligible
                // follower the caster happens to have (a real Animate Dead pet, tamed
                // separately, would previously get force-released here too).
                foreach (var mob in pm.AllFollowers)
                {
                    if (mob is BaseCreature bc && CommandUndeadSpell.WasCommandedByThisSpell(bc))
                    {
                        bc.SetControlMaster(null);
                        CommandUndeadSpell.ClearCommanded(bc);
                    }
                }
            }

            if (SpecialMove.GetCurrentMove(m) is SkillMasteryMove)
            {
                SpecialMove.ClearCurrentMove(m);
            }

            m.RemoveStatMod("SavingThrow_Str");

            RemovePassiveBuffs(m);
        }

        var passive = GetActivePassive(m);

        if (passive != PassiveSpell.None && passive != PassiveSpell.AnticipateHit && m is PlayerMobile passivePm)
        {
            switch (passive)
            {
                case PassiveSpell.EnchantedSummoning:
                    passivePm.AddBuff(new BuffInfo(BuffIcon.EnchantedSummoning, 1155904, 1156090, TimeSpan.Zero, $"{EnchantedSummoningBonus(m)}\t{EnchantedSummoningBonus(m)}", true));
                    break;
                case PassiveSpell.Intuition:
                    passivePm.AddBuff(new BuffInfo(BuffIcon.Intuition, 1155907, 1156089, TimeSpan.Zero, IntuitionBonus(m).ToString(), true));
                    break;
                case PassiveSpell.SavingThrow:
                    var args = GetMasteryLevel(m, newMastery) switch
                    {
                        2 => "5\t5\t0\t0",
                        3 => "5\t5\t5\t5",
                        _ => "5\t0\t0\t0"
                    };

                    m.AddStatMod(new StatMod(StatType.Str, "SavingThrow_Str", 5, TimeSpan.Zero));
                    passivePm.AddBuff(new BuffInfo(BuffIcon.SavingThrow, 1156031, 1156032, TimeSpan.Zero, args, true));
                    break;
                case PassiveSpell.Potency:
                    passivePm.AddBuff(new BuffInfo(BuffIcon.Potency, 1155928, 1156195, TimeSpan.Zero, NonPoisonConsumeChance(m).ToString(), true));
                    break;
                case PassiveSpell.Knockout:
                    passivePm.AddBuff(new BuffInfo(BuffIcon.Knockout, 1155931, 1156030, TimeSpan.Zero, $"{GetKnockoutModifier(m)}\t{GetKnockoutModifier(m, true)}", true));
                    break;
                case PassiveSpell.Boarding:
                    passivePm.AddBuff(new BuffInfo(BuffIcon.Boarding, 1155934, 1156194, TimeSpan.Zero, BoardingSlotIncrease(m).ToString(), true));
                    break;
            }

            m.Delta(MobileDelta.WeaponDamage);
            m.UpdateResistances();

            if (m.Mana > m.ManaMax)
            {
                m.Mana = m.ManaMax;
            }
        }

        if (m.Backpack != null)
        {
            foreach (var book in m.Backpack.FindItemsByType<BookOfMasteries>())
            {
                book.InvalidateProperties();
            }
        }
    }

    public static PassiveSpell GetActivePassive(Mobile m)
    {
        if (m?.Skills == null || Infos == null)
        {
            return PassiveSpell.None;
        }

        var mastery = MasteryState.GetCurrentMastery(m);
        var info = Infos.FirstOrDefault(i => i.Passive && i.MasterySkill == mastery && i.PassiveSpell != PassiveSpell.AnticipateHit);

        return info?.PassiveSpell ?? PassiveSpell.None;
    }

    public static bool IsActivePassive(Mobile m, PassiveSpell spell)
    {
        if (spell == PassiveSpell.AnticipateHit)
        {
            return MasteryState.GetCurrentMastery(m) == SkillName.Bushido && HasLearnedMastery715(m);
        }

        return GetActivePassive(m) == spell;
    }

    private static bool HasLearnedMastery715(Mobile m) => MasteryState.HasLearnedMastery(m, SkillName.Bushido);

    public static void RemovePassiveBuffs(Mobile m)
    {
        if (m is not PlayerMobile pm)
        {
            return;
        }

        pm.RemoveBuff(BuffIcon.EnchantedSummoning);
        pm.RemoveBuff(BuffIcon.AnticipateHit);
        pm.RemoveBuff(BuffIcon.Intuition);
        pm.RemoveBuff(BuffIcon.SavingThrow);
        pm.RemoveBuff(BuffIcon.Potency);
        pm.RemoveBuff(BuffIcon.Knockout);
        pm.RemoveBuff(BuffIcon.Boarding);
    }

    [OnEvent(nameof(PlayerMobile.PlayerLoginEvent))]
    public static void OnLogin(PlayerMobile m)
    {
        var mastery = MasteryState.GetCurrentMastery(m);
        if (mastery != SkillName.Alchemy)
        {
            OnMasteryChanged(m, mastery);
        }
    }

    public static int GetSpellID(PassiveSpell spell) => spell switch
    {
        PassiveSpell.EnchantedSummoning => 714,
        PassiveSpell.AnticipateHit      => 715,
        PassiveSpell.Intuition          => 717,
        PassiveSpell.SavingThrow        => 732,
        PassiveSpell.Potency            => 738,
        PassiveSpell.Knockout           => 741,
        PassiveSpell.Boarding           => 744,
        _                               => -1
    };

    public static int GetLocalization(SkillName name) => name switch
    {
        SkillName.Discordance   => 1151945,
        SkillName.Provocation   => 1151946,
        SkillName.Peacemaking   => 1151947,
        SkillName.Magery        => 1155771,
        SkillName.Mysticism     => 1155772,
        SkillName.Necromancy    => 1155773,
        SkillName.Spellweaving  => 1155774,
        SkillName.Bushido       => 1155775,
        SkillName.Chivalry      => 1155776,
        SkillName.Ninjitsu      => 1155777,
        SkillName.Archery       => 1155786,
        SkillName.Fencing       => 1155778,
        SkillName.Macing        => 1155779,
        SkillName.Swords        => 1155780,
        SkillName.Throwing      => 1155781,
        SkillName.Parry         => 1155782,
        SkillName.Poisoning     => 1155783,
        SkillName.Wrestling     => 1155784,
        SkillName.AnimalTaming  => 1155785,
        _                       => 1151945
    };

    #region Passive Bonuses/Maluses

    public static int EnchantedSummoningBonus(BaseCreature bc) =>
        bc.Summoned && bc.SummonMaster != null ? EnchantedSummoningBonus(bc.SummonMaster) : 0;

    public static int EnchantedSummoningBonus(Mobile m)
    {
        if (!IsActivePassive(m, PassiveSpell.EnchantedSummoning))
        {
            return 0;
        }

        var sk = MasteryState.GetCurrentMastery(m);
        return (int)((m.Skills[sk].Value + GetMasteryLevel(m, sk) * 40) / 16);
    }

    public static int AnticipateHitBonus(Mobile m) =>
        IsActivePassive(m, PassiveSpell.AnticipateHit) ? (int)(m.Skills[SkillName.Bushido].Value * .67) : 0;

    public static int IntuitionBonus(Mobile m)
    {
        if (!IsActivePassive(m, PassiveSpell.Intuition))
        {
            return 0;
        }

        var sk = MasteryState.GetCurrentMastery(m);
        return GetMasteryLevel(m, sk) * 40 / 8;
    }

    public static int NonPoisonConsumeChance(Mobile m)
    {
        if (!IsActivePassive(m, PassiveSpell.Potency))
        {
            return 0;
        }

        var skill = m.Skills[SkillName.Poisoning].Value + m.Skills[SkillName.Anatomy].Value;
        skill += GetMasteryLevel(m, SkillName.Poisoning) * 20;

        return (int)(skill / 4.375);
    }

    public static int GetKnockoutModifier(Mobile m, bool pvp = false)
    {
        if (!IsActivePassive(m, PassiveSpell.Knockout))
        {
            return 0;
        }

        return GetMasteryLevel(m, SkillName.Wrestling) switch
        {
            3 => pvp ? 50 : 100,
            2 => pvp ? 25 : 50,
            1 => pvp ? 10 : 25,
            _ => 0
        };
    }

    public static int BoardingSlotIncrease(Mobile m) =>
        IsActivePassive(m, PassiveSpell.Boarding) ? GetMasteryLevel(m, SkillName.AnimalTaming) : 0;

    public static int SavingThrowChance(Mobile m, AosAttribute attr)
    {
        if (!IsActivePassive(m, PassiveSpell.SavingThrow))
        {
            return 0;
        }

        var level = GetMasteryLevel(m, MasteryState.GetCurrentMastery(m));

        if (level <= 0)
        {
            return 0;
        }

        return attr switch
        {
            AosAttribute.AttackChance => 5,
            AosAttribute.DefendChance => level > 1 ? 5 : 0,
            AosAttribute.BonusStr     => level > 2 ? 5 : 0,
            AosAttribute.WeaponDamage => level > 2 ? 5 : 0,
            _                         => 0
        };
    }

    #endregion

    public static readonly SkillName[] Skills =
    {
        SkillName.Peacemaking, SkillName.Provocation, SkillName.Discordance, SkillName.Magery,
        SkillName.Mysticism, SkillName.Necromancy, SkillName.Spellweaving, SkillName.Bushido,
        SkillName.Chivalry, SkillName.Ninjitsu, SkillName.Fencing, SkillName.Macing,
        SkillName.Swords, SkillName.Throwing, SkillName.Parry, SkillName.Poisoning,
        SkillName.Wrestling, SkillName.AnimalTaming, SkillName.Archery
    };
}

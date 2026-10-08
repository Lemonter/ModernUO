using System;
using System.Collections.Generic;

namespace Server.Systems.MahaonProfessions;

/// <summary>
///     Passive combat perks tied to profession category investment — the same
///     GetCategoryBonusScale (1.0 primary / 0.5 secondary / 0 neither) that GuardSystem/
///     RangerSystem/CourtMageSystem already use for HP/Stam/Mana rank bonuses, but for
///     Thief and Warrior specifically, per the shard owner's ask: "у любого воина когда хп
///     мало срабатывала стойкость... у вора 30% увороты".
///
///     Craft ("маг оружие") and Magic ("каст с занятыми руками") were also named, but both
///     already exist as unrestricted general mechanics — SpellChanneling/MageWeapon are
///     already imbuable by anyone (see ImbuingPropertyTable.cs) and nothing in Spell.cs
///     currently blocks or penalizes casting with a weapon equipped — so there's no bug to
///     fix there without first deciding whether the shard owner wants those two properties
///     restricted to Craft/Magic profession-holders specifically (a real nerf to everyone
///     else), which wasn't asked for. Left alone pending that answer.
///
///     "Thief hides mid-combat" is NOT here — that one already existed before today, wired
///     through Hiding.CombatOverride by SkillHandlers.MahaonHiding.cs, which flips it on for
///     anyone TouchesCategory(Thief) around each hide attempt. Don't re-add it here.
/// </summary>
public static class ProfessionPerkSystem
{
    // 30% at full (primary-category) Thief investment, per the shard owner's own number.
    // Checked once per incoming swing in BaseWeapon.CheckHit.
    private const double MaxDodgeChance = 0.30;

    // "Стойкость": работает всё время, пока ХП ниже порога — без кулдауна.
    private const double LowHpFraction = 0.30; // "когда хп мало"
    private const double MitigationFraction = 0.5; // that hit is cut in half when it procs



    /// <summary>«Уворот» — обычный перк Вора, только первичная категория. Сигнатурный у
    /// Вора другой — «Тень в бою», он живёт в SkillHandlers.MahaonHiding.</summary>
    public static bool TryDodge(Mobile defender) =>
        ProfessionSystem.HasFullKit(defender, ProfessionCategory.Thief) &&
        Utility.RandomDouble() < MaxDodgeChance;

    /// <summary>
    ///     Единственная «Стойкость». Раньше их было две — эта и вторая прямо в
    ///     PlayerMobile.Damage, с разными правилами, и срабатывали обе.
    ///
    ///     Правило по решению владельца шарда: срабатывает, пока ХП ниже 30% от максимума,
    ///     без кулдауна. То есть в затяжном бою на последней трети здоровья воин режет
    ///     входящий урон вдвое постоянно — это и есть «последний рубеж», а не разовый прок.
    /// </summary>
    public static int ApplyResilience(Mobile defender, int damage)
    {
        // Сигнатурный перк Воина — достаётся и тому, у кого Воин вторичной категорией,
        // и в полную силу.
        if (!ProfessionSystem.HasSignature(defender, ProfessionCategory.Warrior))
        {
            return damage;
        }

        if (defender.HitsMax <= 0 || damage <= 0 || defender.Hits >= defender.HitsMax * LowHpFraction)
        {
            return damage;
        }

        defender.PublicOverheadMessage(MessageType.Regular, 0x59, false, "Стойкость!");

        return (int)(damage * (1.0 - MitigationFraction));
    }
}

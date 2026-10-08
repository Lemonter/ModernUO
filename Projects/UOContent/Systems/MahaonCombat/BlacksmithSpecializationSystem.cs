using System;
using System.Collections.Generic;
using Server.Engines.Craft;
using Server.Items;

namespace Server.Systems.MahaonCombat;

public enum BlacksmithSpecialization : byte
{
    Armor = 0,   // Мастер бронник — trains crafting armor, boosts armor durability
    Blade = 1,   // Мастер клинков — trains crafting Swords/Fencing weapons, boosts their durability
    Blunt = 2,   // Мастер ударного оружия — trains crafting Macing weapons, boosts their durability
    Shield = 3   // Мастер щитов — trains crafting shields, boosts shield durability
}

/// <summary>
///     First of what's meant to become a matching system per craft skill (Tailoring,
///     Carpentry, etc. later — this is the pattern-setter, Blacksmith specifically, per
///     the person running this shard's own example). Real hook: BOTH live
///     CraftItem.CompleteCraft overloads, right after a successful item is produced —
///     not a per-item-type edit, same "one shared chokepoint" shape as combat/magic.
///
///     No "pick one active" step needed (same as magic schools) — whichever category the
///     freshly-crafted item falls into trains automatically, and the resulting item gets
///     a durability boost scaled to that specialization's trained value + a breadth bonus
///     from the others, applied directly to the item right after crafting.
/// </summary>
public static class BlacksmithSpecializationSystem
{
    private static readonly Dictionary<(Mobile, BlacksmithSpecialization), double> Value = new();

    public const double MaxValue = 100.0;
    private const double GainChance = 0.15;
    private const double BreadthUnitsPerPercent = 20.0;
    private const int BreadthCap = 10;

    private static Persistence _persistence;

    public static void Configure()
    {
        _persistence = new Persistence();
    }

    public static double GetValue(Mobile m, BlacksmithSpecialization spec) =>
        Value.GetValueOrDefault((m, spec), 0.0);

    private static void Train(Mobile m, BlacksmithSpecialization spec)
    {
        var key = (m, spec);
        var current = Value.GetValueOrDefault(key, 0.0);

        var cap = MahaonMasteryCapSystem.GetCap(m, RuSpecializationName(spec));
        if (current >= cap || Utility.RandomDouble() > GainChance)
        {
            return;
        }

        var gain = current < 50 ? 0.3 : current < 80 ? 0.15 : 0.05;
        var newValue = Math.Min(cap, current + gain);
        Value[key] = newValue;
        MahaonSkillTree.NotifyChanged(m);
        MahaonSkillTree.AnnounceGain(m, RuSpecializationName(spec), newValue - current, newValue);
    }

    private static double GetBreadthBonus(Mobile m, BlacksmithSpecialization active)
    {
        var total = 0.0;

        for (var s = 0; s < 4; s++)
        {
            var spec = (BlacksmithSpecialization)s;

            if (spec == active)
            {
                continue;
            }

            total += GetValue(m, spec);
        }

        return Math.Min(BreadthCap, total / BreadthUnitsPerPercent);
    }

    private static BlacksmithSpecialization? SpecializationOf(Item item)
    {
        // BaseShield derives from BaseArmor — must be checked first or shields would
        // register as generic armor and never get their own specialization credit.
        if (item is BaseShield)
        {
            return BlacksmithSpecialization.Shield;
        }

        if (item is BaseArmor)
        {
            return BlacksmithSpecialization.Armor;
        }

        if (item is BaseWeapon weapon)
        {
            var category = WeaponStyleSystem.GetCategory(weapon.DefSkill);

            return category switch
            {
                WeaponCategory.Swords or WeaponCategory.Fencing => BlacksmithSpecialization.Blade,
                WeaponCategory.Macing                           => BlacksmithSpecialization.Blunt,
                _                                                => null // e.g. Archery — not Blacksmith's craft
            };
        }

        return null;
    }

    /// <summary>Called from both live CraftItem.CompleteCraft overloads right after a
    /// successful item is produced. craftSystem filters out lookalike items made by a
    /// DIFFERENT skill — a cloth robe from Tailoring is also technically a BaseArmor, but
    /// shouldn't count toward Blacksmith specializations just because of its item type.
    /// Trains the matching specialization and, if the crafter has ANY of the 4 trained
    /// above zero, boosts the fresh item's durability — only items with a real
    /// MaxHitPoints (armor/weapons) apply, everything else no-ops via the null check.</summary>
    public static void OnItemCrafted(Mobile from, Item item, CraftSystem craftSystem)
    {
        if (craftSystem is not DefBlacksmithy)
        {
            return;
        }

        var spec = SpecializationOf(item);

        if (spec == null)
        {
            return;
        }

        Train(from, spec.Value);

        var value = GetValue(from, spec.Value);
        var breadth = GetBreadthBonus(from, spec.Value);
        var bonusPercent = value / 100.0 * 20.0 + breadth; // up to +20% at 100 trained, plus breadth

        if (bonusPercent <= 0)
        {
            return;
        }

        switch (item)
        {
            case BaseArmor armor when armor.MaxHitPoints > 0:
                var newArmorMax = (int)(armor.MaxHitPoints * (1.0 + bonusPercent / 100.0));
                armor.MaxHitPoints = newArmorMax;
                armor.HitPoints = newArmorMax;
                break;

            case BaseWeapon weapon when weapon.MaxHitPoints > 0:
                var newWeaponMax = (int)(weapon.MaxHitPoints * (1.0 + bonusPercent / 100.0));
                weapon.MaxHitPoints = newWeaponMax;
                weapon.HitPoints = newWeaponMax;
                break;
        }
    }

    public static string RuSpecializationName(BlacksmithSpecialization spec) => spec switch
    {
        BlacksmithSpecialization.Armor  => "Мастер-бронник",
        BlacksmithSpecialization.Blade  => "Мастер клинков",
        BlacksmithSpecialization.Blunt  => "Мастер ударного оружия",
        BlacksmithSpecialization.Shield => "Мастер щитов",
        _                                => spec.ToString()
    };

    private sealed class Persistence : GenericPersistence
    {
        public Persistence() : base("MahaonBlacksmithSpecialization", 1)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version
            MahaonSpecializationPersistenceHelper.Write(writer, Value, (w, key) => w.Write((byte)key));
        }

        public override void Deserialize(IGenericReader reader)
        {
            reader.ReadEncodedInt(); // version
            MahaonSpecializationPersistenceHelper.Read(reader, Value, r => (BlacksmithSpecialization)r.ReadByte());
        }
    }
}

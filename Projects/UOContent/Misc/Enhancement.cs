using System.Collections.Generic;

namespace Server;

/// <summary>Ported from ServUO (Scripts/Abilities/Enhancement.cs). A layer of temporary
/// attribute bonuses that sit on the *mobile* rather than on any item — so a spell can grant,
/// say, Hit Lightning for thirty seconds without ever touching the weapon it appears to come
/// from, and the weapon's own properties survive the effect untouched.
///
/// Each mobile holds a list of named entries; a system claims a title ("Enchant Spell",
/// "Immolating Weapon", …) and owns that entry. GetValue sums across every entry, so two
/// systems can stack on the same attribute without either clobbering the other, and removal
/// is per-title.
///
/// One gap against the original, deliberate and flagged rather than papered over:
/// EnhancementAttributes there carries five containers, and two of them —
/// SAAbsorptionAttributes and ExtendedWeaponAttributes — do not exist anywhere in this
/// codebase yet. They're separate Stygian Abyss subsystems with no readers here, so the entry
/// carries the three that do exist. Add the other two fields when those systems land; nothing
/// in this class needs restructuring for it.</summary>
public static class Enhancement
{
    private static readonly Dictionary<Mobile, List<EnhancementAttributes>> _table = new();

    public static bool AddMobile(Mobile m)
    {
        if (_table.ContainsKey(m))
        {
            return false;
        }

        _table[m] = new List<EnhancementAttributes>();
        return true;
    }

    /// <summary>Drops one titled entry, or every entry for the mobile when title is null.</summary>
    public static bool RemoveMobile(Mobile m, string title = null)
    {
        if (!_table.TryGetValue(m, out var list))
        {
            return false;
        }

        if (title == null)
        {
            _table.Remove(m);
        }
        else
        {
            for (var i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].Title == title)
                {
                    list.RemoveAt(i);
                }
            }

            if (list.Count == 0)
            {
                _table.Remove(m);
            }
        }

        Refresh(m);
        return true;
    }

    public static int GetValue(Mobile m, AosAttribute attribute)
    {
        if (!_table.TryGetValue(m, out var list))
        {
            return 0;
        }

        var value = 0;

        for (var i = 0; i < list.Count; i++)
        {
            value += list[i].Attributes[attribute];
        }

        return value;
    }

    public static int GetValue(Mobile m, AosWeaponAttribute attribute)
    {
        if (!_table.TryGetValue(m, out var list))
        {
            return 0;
        }

        var value = 0;

        for (var i = 0; i < list.Count; i++)
        {
            value += list[i].WeaponAttributes[attribute];
        }

        return value;
    }

    public static int GetValue(Mobile m, AosArmorAttribute attribute)
    {
        if (!_table.TryGetValue(m, out var list))
        {
            return 0;
        }

        var value = 0;

        for (var i = 0; i < list.Count; i++)
        {
            value += list[i].ArmorAttributes[attribute];
        }

        return value;
    }

    public static void SetValue(Mobile m, AosAttribute attribute, int value, string title)
    {
        GetOrCreate(m, title).Attributes[attribute] = value;
        Refresh(m);
    }

    public static void SetValue(Mobile m, AosWeaponAttribute attribute, int value, string title)
    {
        GetOrCreate(m, title).WeaponAttributes[attribute] = value;
        Refresh(m);
    }

    public static void SetValue(Mobile m, AosArmorAttribute attribute, int value, string title)
    {
        GetOrCreate(m, title).ArmorAttributes[attribute] = value;
        Refresh(m);
    }

    private static EnhancementAttributes GetOrCreate(Mobile m, string title)
    {
        if (!_table.TryGetValue(m, out var list))
        {
            list = new List<EnhancementAttributes>();
            _table[m] = list;
        }

        for (var i = 0; i < list.Count; i++)
        {
            if (list[i].Title == title)
            {
                return list[i];
            }
        }

        var entry = new EnhancementAttributes(title);
        list.Add(entry);
        return entry;
    }

    private static void Refresh(Mobile m)
    {
        m.UpdateResistances();
        m.Delta(MobileDelta.Stat | MobileDelta.WeaponDamage);
        m.CheckStatTimers();
    }
}

/// <summary>One named bundle of bonuses belonging to a single mobile. The owning system picks
/// the Title and is responsible for removing its own entry when the effect ends.</summary>
public class EnhancementAttributes
{
    public EnhancementAttributes(string title)
    {
        Title = title;

        Attributes = new AosAttributes(null);
        WeaponAttributes = new AosWeaponAttributes(null);
        ArmorAttributes = new AosArmorAttributes(null);
    }

    public string Title { get; }

    public AosAttributes Attributes { get; }
    public AosWeaponAttributes WeaponAttributes { get; }
    public AosArmorAttributes ArmorAttributes { get; }
}

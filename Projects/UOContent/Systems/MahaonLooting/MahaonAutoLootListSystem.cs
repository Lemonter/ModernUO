using System;
using System.Collections.Generic;

namespace Server.Systems.MahaonLooting;

/// <summary>One tracked item type in a player's autoloot list — icon (ItemID/Hue) comes
/// from the item that was targeted to add it, Enabled is the "лутать" checkbox (unchecking
/// keeps the entry in the list without deleting it, matching what was asked for).</summary>
public class MahaonAutoLootEntry
{
    public readonly Type ItemType;
    public readonly string DisplayName;
    public readonly int ItemID;
    public readonly int Hue;
    public bool Enabled;

    public MahaonAutoLootEntry(Type itemType, string displayName, int itemId, int hue, bool enabled = true)
    {
        ItemType = itemType;
        DisplayName = displayName;
        ItemID = itemId;
        Hue = hue;
        Enabled = enabled;
    }
}

/// <summary>
///     Per-player extensible autoloot list ("расширяемый список" — the shard owner's own
///     words) — on top of AutoLootSystem's fixed built-in categories (gold/reagents/
///     potions/bandages/ammo/gems), a player can target any item type once and have it
///     picked up automatically from then on, from any corpse or dungeon chest. Standalone
///     GenericPersistence store, same pattern as MasteryState/MahaonMetalTracker — this
///     data isn't itself a serialized Item/Mobile field.
/// </summary>
public class MahaonAutoLootListSystem : GenericPersistence
{
    private static readonly Dictionary<Mobile, List<MahaonAutoLootEntry>> Lists = new();

    public MahaonAutoLootListSystem() : base("MahaonAutoLootList", 1)
    {
    }

    public static void Configure() => new MahaonAutoLootListSystem();

    public static List<MahaonAutoLootEntry> GetList(Mobile m)
    {
        if (!Lists.TryGetValue(m, out var list))
        {
            list = new List<MahaonAutoLootEntry>();
            Lists[m] = list;
        }

        return list;
    }

    /// <summary>Used by AutoLootSystem to decide whether an item outside the built-in
    /// categories should still be swept up for this specific player.</summary>
    public static bool IsTracked(Mobile m, Type itemType)
    {
        if (!Lists.TryGetValue(m, out var list))
        {
            return false;
        }

        foreach (var entry in list)
        {
            if (entry.Enabled && entry.ItemType == itemType)
            {
                return true;
            }
        }

        return false;
    }

    /// <returns>false if this exact type is already tracked (button just re-opens the
    /// gump either way, no need for the caller to treat this as an error).</returns>
    public static bool AddEntry(Mobile m, Item item)
    {
        var list = GetList(m);
        var type = item.GetType();

        foreach (var entry in list)
        {
            if (entry.ItemType == type)
            {
                return false;
            }
        }

        list.Add(new MahaonAutoLootEntry(type, item.Name ?? type.Name, item.ItemID, item.Hue));
        return true;
    }

    public static void RemoveEntry(Mobile m, MahaonAutoLootEntry entry) => GetList(m).Remove(entry);

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version

        writer.WriteEncodedInt(Lists.Count);

        foreach (var (mobile, list) in Lists)
        {
            writer.Write(mobile);
            writer.WriteEncodedInt(list.Count);

            foreach (var entry in list)
            {
                writer.Write(entry.ItemType.FullName);
                writer.Write(entry.DisplayName);
                writer.Write(entry.ItemID);
                writer.Write(entry.Hue);
                writer.Write(entry.Enabled);
            }
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // version

        var mobileCount = reader.ReadEncodedInt();

        for (var i = 0; i < mobileCount; i++)
        {
            var mobile = reader.ReadEntity<Mobile>();
            var entryCount = reader.ReadEncodedInt();
            var list = new List<MahaonAutoLootEntry>();

            for (var j = 0; j < entryCount; j++)
            {
                var typeName = reader.ReadString();
                var displayName = reader.ReadString();
                var itemId = reader.ReadInt();
                var hue = reader.ReadInt();
                var enabled = reader.ReadBool();

                // A type can vanish across a content refactor — silently drop that one
                // stale entry rather than failing the whole player's list.
                var type = AssemblyHandler.FindTypeByFullName(typeName);
                if (type != null)
                {
                    list.Add(new MahaonAutoLootEntry(type, displayName, itemId, hue, enabled));
                }
            }

            if (mobile != null)
            {
                Lists[mobile] = list;
            }
        }
    }
}

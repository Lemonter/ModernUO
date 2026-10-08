using System;
using System.Collections.Generic;

namespace Server.Systems.MahaonMetals;

/// <summary>Какой металл у готового оружия/брони/замка — внешний словарь, а не поле на
/// самом предмете (тот же приём, что уже использовался в этой сессии для наложенных чар —
/// ImbuingSystem.ImbuedCount — не трогаем сериализацию базовых классов оружия/брони
/// напрямую). GenericPersistence-backed (тот же паттерн, что MasteryState/
/// CityControlSystem) — раньше был обычным static-словарём в памяти, и любое назначение
/// металла (перекованная вещь, замок сундука) слетало при рестарте сервера. Предметы
/// должны сохранять свой статус между рестартами, поэтому теперь реально пишется на диск.
///
/// Намеренно НЕ трогает Item.Name — у большинства ванильных предметов оно изначально
/// null (отображаемое имя берётся через LabelNumber/клило, не строку), перезапись
/// стёрла бы оригинальное имя предмета. Металл показывается через GetProperties-хук в
/// MahaonMetalPropertyHook.cs, не через переименование.</summary>
public sealed class MahaonMetalTracker : GenericPersistence
{
    private static MahaonMetalTracker _instance;

    private static readonly Dictionary<Item, MahaonMetal> Metals = new();

    public MahaonMetalTracker() : base("MahaonMetalTracker", 1)
    {
    }

    public static void Configure()
    {
        _instance = new MahaonMetalTracker();

        // ClearMetal exists but nothing calls it — locks/reforges add entries for the life
        // of the server, deleted items just leave a dead Item reference behind (harmless at
        // read time thanks to the null-check on load, but grows forever and gets rewritten
        // to disk on every save). Periodic sweep instead of hooking OnAfterDelete on every
        // possible container/weapon/armor base class.
        Timer.StartTimer(TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30), CleanupDeleted);
    }

    private static void CleanupDeleted()
    {
        List<Item> deleted = null;

        foreach (var item in Metals.Keys)
        {
            if (item.Deleted)
            {
                (deleted ??= new List<Item>()).Add(item);
            }
        }

        if (deleted == null)
        {
            return;
        }

        foreach (var item in deleted)
        {
            Metals.Remove(item);
        }
    }

    public static bool HasMetal(Item item) => Metals.ContainsKey(item);

    public static MahaonMetal? GetMetal(Item item) => Metals.TryGetValue(item, out var m) ? m : null;

    public static void SetMetal(Item item, MahaonMetal metal) => Metals[item] = metal;

    public static void ClearMetal(Item item) => Metals.Remove(item);

    /// <summary>
    ///     Marks a freshly created item as made of <paramref name="metal" /> and tints it to
    ///     that metal's colour, returning it so it can be written inline inside an
    ///     AddItem(...)/PackItem(...) call.
    ///
    ///     Exists because monster gear used to be given a bare Hue and nothing else, which
    ///     produced armour that was visibly coloured but reported no material at all —
    ///     BaseArmor.GetProperties has nothing to print unless either this tracker or a
    ///     vanilla CraftResource says what the thing is made of.
    /// </summary>
    public static T Forge<T>(T item, MahaonMetal metal) where T : Item
    {
        if (item == null)
        {
            return null;
        }

        SetMetal(item, metal);
        item.Hue = MahaonMetalTable.Get(metal).Hue;

        return item;
    }

    /// <summary>Сколько вещей данного металла сейчас надето на мобиле — используется
    /// эффектами, которым нужно "хоть одна вещь надета" (Doom-регенерация, иммунитет
    /// Махаона), а не привязка к конкретному предмету.</summary>
    public static int CountWorn(Mobile m, MahaonMetal metal)
    {
        var count = 0;

        foreach (var item in m.Items)
        {
            if (Metals.TryGetValue(item, out var itemMetal) && itemMetal == metal)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>Махаон — "полная защита от заклинаний до 4 круга включительно". Хотя бы
    /// одна надетая вещь достаточно, эффект не накопительный.</summary>
    public static bool HasMahaonSpellImmunity(Mobile m) => CountWorn(m, MahaonMetal.Mahaon) > 0;

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version

        writer.WriteEncodedInt(Metals.Count);

        foreach (var (item, metal) in Metals)
        {
            writer.Write(item);
            writer.Write((int)metal);
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // version

        var count = reader.ReadEncodedInt();

        for (var i = 0; i < count; i++)
        {
            var item = reader.ReadEntity<Item>();
            var metal = (MahaonMetal)reader.ReadInt();

            if (item != null)
            {
                Metals[item] = metal;
            }
        }
    }
}

using System;
using System.Collections.Generic;

namespace Server.Systems.MahaonCombat;

/// <summary>
///     Every specialization system in this folder (mining tiers, weapon styles, magic
///     schools, crafting specializations, etc.) kept its trained values in a plain runtime
///     Dictionary with no GenericPersistence at all — none of it survived a restart. Only
///     surfaced once a player asked "wait, doesn't style training save?" after training up
///     several specializations across a long session. Shared read/write helpers here so
///     each system's own small Persistence subclass (still one per system, each registered
///     under its own name — GenericPersistence itself doesn't support a shared instance
///     covering multiple unrelated dictionaries) doesn't have to hand-write the same
///     encode/decode loop two dozen times.
/// </summary>
internal static class MahaonSpecializationPersistenceHelper
{
    public static void Write(IGenericWriter writer, Dictionary<Mobile, double> dict)
    {
        writer.WriteEncodedInt(dict.Count);

        foreach (var (mobile, value) in dict)
        {
            writer.Write(mobile);
            writer.Write(value);
        }
    }

    public static void Read(IGenericReader reader, Dictionary<Mobile, double> dict)
    {
        var count = reader.ReadEncodedInt();

        for (var i = 0; i < count; i++)
        {
            var mobile = reader.ReadEntity<Mobile>();
            var value = reader.ReadDouble();

            if (mobile != null)
            {
                dict[mobile] = value;
            }
        }
    }

    public static void Write<TKey>(IGenericWriter writer, Dictionary<(Mobile, TKey), double> dict, Action<IGenericWriter, TKey> writeKey)
    {
        writer.WriteEncodedInt(dict.Count);

        foreach (var ((mobile, key), value) in dict)
        {
            writer.Write(mobile);
            writeKey(writer, key);
            writer.Write(value);
        }
    }

    public static void Read<TKey>(IGenericReader reader, Dictionary<(Mobile, TKey), double> dict, Func<IGenericReader, TKey> readKey)
    {
        var count = reader.ReadEncodedInt();

        for (var i = 0; i < count; i++)
        {
            var mobile = reader.ReadEntity<Mobile>();
            var key = readKey(reader);
            var value = reader.ReadDouble();

            if (mobile != null)
            {
                dict[(mobile, key)] = value;
            }
        }
    }

    public static void Write3<TKey1, TKey2>(
        IGenericWriter writer, Dictionary<(Mobile, TKey1, TKey2), double> dict,
        Action<IGenericWriter, TKey1> writeKey1, Action<IGenericWriter, TKey2> writeKey2
    )
    {
        writer.WriteEncodedInt(dict.Count);

        foreach (var ((mobile, key1, key2), value) in dict)
        {
            writer.Write(mobile);
            writeKey1(writer, key1);
            writeKey2(writer, key2);
            writer.Write(value);
        }
    }

    public static void Read3<TKey1, TKey2>(
        IGenericReader reader, Dictionary<(Mobile, TKey1, TKey2), double> dict,
        Func<IGenericReader, TKey1> readKey1, Func<IGenericReader, TKey2> readKey2
    )
    {
        var count = reader.ReadEncodedInt();

        for (var i = 0; i < count; i++)
        {
            var mobile = reader.ReadEntity<Mobile>();
            var key1 = readKey1(reader);
            var key2 = readKey2(reader);
            var value = reader.ReadDouble();

            if (mobile != null)
            {
                dict[(mobile, key1, key2)] = value;
            }
        }
    }
    // ---- Переезд в MahaonProgress ---------------------------------------------------------
    //
    // Те же самые вызовы, что делают классы сохранения всех веток, — только словарь у них
    // теперь заменён таблицей (ProgressTable), и перегрузки ниже выбираются сами, по типу.
    // Чтение разбирает ровно те же байты, что и раньше, и перекладывает значения в общее
    // хранилище. Запись пишет пустой список: данные теперь хранит MahaonProgress, а старый
    // файл остаётся только затем, чтобы его формат не сломался для того, кто прочтёт.

    public static void Write(IGenericWriter writer, ProgressTable table) => writer.WriteEncodedInt(0);

    public static void Read(IGenericReader reader, ProgressTable table)
    {
        var count = reader.ReadEncodedInt();

        for (var i = 0; i < count; i++)
        {
            var mobile = reader.ReadEntity<Mobile>();
            var value = reader.ReadDouble();

            if (mobile != null)
            {
                MahaonProgress.Migrated(mobile, table.Track, value);
            }
        }
    }

    public static void Write<TKey>(IGenericWriter writer, ProgressTable<TKey> table, Action<IGenericWriter, TKey> writeKey)
        where TKey : notnull => writer.WriteEncodedInt(0);

    public static void Read<TKey>(IGenericReader reader, ProgressTable<TKey> table, Func<IGenericReader, TKey> readKey)
        where TKey : notnull
    {
        var count = reader.ReadEncodedInt();

        for (var i = 0; i < count; i++)
        {
            var mobile = reader.ReadEntity<Mobile>();
            var key = readKey(reader);
            var value = reader.ReadDouble();

            if (mobile != null)
            {
                MahaonProgress.Migrated(mobile, table.TrackOf(key), value);
            }
        }
    }

    public static void Write3<TKey1, TKey2>(
        IGenericWriter writer, ProgressTable<TKey1, TKey2> table,
        Action<IGenericWriter, TKey1> writeKey1, Action<IGenericWriter, TKey2> writeKey2
    ) where TKey1 : notnull where TKey2 : notnull => writer.WriteEncodedInt(0);

    public static void Read3<TKey1, TKey2>(
        IGenericReader reader, ProgressTable<TKey1, TKey2> table,
        Func<IGenericReader, TKey1> readKey1, Func<IGenericReader, TKey2> readKey2
    ) where TKey1 : notnull where TKey2 : notnull
    {
        var count = reader.ReadEncodedInt();

        for (var i = 0; i < count; i++)
        {
            var mobile = reader.ReadEntity<Mobile>();
            var key1 = readKey1(reader);
            var key2 = readKey2(reader);
            var value = reader.ReadDouble();

            if (mobile != null)
            {
                MahaonProgress.Migrated(mobile, table.TrackOf(key1, key2), value);
            }
        }
    }
}

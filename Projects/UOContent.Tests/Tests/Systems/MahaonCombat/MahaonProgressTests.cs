using System;
using Server.Systems.MahaonCombat;
using Xunit;

namespace Server.Tests.Systems.MahaonCombat;

/// <summary>
///     Общее хранилище прокачки и переезд в него из двух с половиной десятков старых.
///
///     Главное здесь — не арифметика, а переезд: ветки, натренированные игроками за месяцы,
///     не должны потеряться. Поэтому старая запись собирается в тестах БАЙТ В БАЙТ так, как её
///     писал прежний MahaonSpecializationPersistenceHelper, и читается новыми перегрузками.
/// </summary>
[Collection("Sequential UOContent Tests")]
public class MahaonProgressTests
{
    private enum Fake : byte
    {
        Alpha = 0,
        Beta = 1
    }

    private static BufferReader Reader(BufferWriter writer) => new(Bytes(writer));

    private static byte[] Bytes(BufferWriter writer)
    {
        var bytes = new byte[writer.Position];
        writer.Buffer.AsSpan(0, (int)writer.Position).CopyTo(bytes);

        return bytes;
    }

    [Fact]
    public void Get_FallsBackWhenNothingStored()
    {
        var m = new Mobile();

        try
        {
            Assert.Equal(0.0, MahaonProgress.Get(m, "branch:test/none"));
            Assert.Equal(100.0, MahaonProgress.Get(m, "branch:test/none", 100.0));
            Assert.False(MahaonProgress.TryGet(m, "branch:test/none", out _));
        }
        finally
        {
            m.Delete();
        }
    }

    /// <summary>
    ///     Таблица обязана вести себя как словарь, который она заменила: системы веток
    ///     переписаны не были, у них поменялся только тип поля.
    /// </summary>
    [Fact]
    public void Table_BehavesLikeTheDictionaryItReplaced()
    {
        var m = new Mobile();
        var table = new ProgressTable<Fake>("branch:test");

        try
        {
            Assert.Equal(0.0, table.GetValueOrDefault((m, Fake.Alpha), 0.0));

            table[(m, Fake.Alpha)] = 42.5;

            Assert.Equal(42.5, table.GetValueOrDefault((m, Fake.Alpha), 0.0));
            Assert.Equal(42.5, table[(m, Fake.Alpha)]);
            Assert.Equal(0.0, table.GetValueOrDefault((m, Fake.Beta), 0.0));

            // И лежит под понятным именем дорожки.
            Assert.Equal(42.5, MahaonProgress.Get(m, "branch:test/Alpha"));
        }
        finally
        {
            m.Delete();
        }
    }

    [Fact]
    public void TwoKeyTable_NamesTracksByBothKeys()
    {
        var m = new Mobile();
        var table = new ProgressTable<WeaponCategory, byte>("branch:style");

        try
        {
            table[(m, WeaponCategory.Swords, (byte)3)] = 61.0;

            Assert.Equal("branch:style/Swords/3", table.TrackOf(WeaponCategory.Swords, 3));
            Assert.Equal(61.0, MahaonProgress.Get(m, "branch:style/Swords/3"));
        }
        finally
        {
            m.Delete();
        }
    }

    /// <summary>
    ///     Старая запись ветки — счётчик, затем (игрок, ключ, значение) — переезжает целиком.
    ///     Формат записи взят из прежнего помощника сохранения как есть.
    /// </summary>
    [Fact]
    public void OldTwoKeyRecord_MovesIntoTheSharedStore()
    {
        var m = new Mobile();
        var table = new ProgressTable<Fake>("branch:migrate");

        try
        {
            var writer = new BufferWriter(true);
            writer.WriteEncodedInt(2);
            writer.Write(m);
            writer.Write((byte)Fake.Alpha);
            writer.Write(37.5);
            writer.Write(m);
            writer.Write((byte)Fake.Beta);
            writer.Write(80.0);

            var bytes = Bytes(writer);
            var reader = new BufferReader(bytes);

            MahaonSpecializationPersistenceHelper.Read(reader, table, r => (Fake)r.ReadByte());

            Assert.Equal(37.5, table.GetValueOrDefault((m, Fake.Alpha), 0.0));
            Assert.Equal(80.0, table.GetValueOrDefault((m, Fake.Beta), 0.0));
            Assert.Equal(bytes.Length, reader.Position); // прочитано всё, ни байтом больше
        }
        finally
        {
            m.Delete();
        }
    }

    [Fact]
    public void OldSingleRecord_MovesIntoTheSharedStore()
    {
        var m = new Mobile();
        var table = new ProgressTable("branch:migrate-single");

        try
        {
            var writer = new BufferWriter(true);
            writer.WriteEncodedInt(1);
            writer.Write(m);
            writer.Write(55.0);

            MahaonSpecializationPersistenceHelper.Read(Reader(writer), table);

            Assert.Equal(55.0, table.GetValueOrDefault(m, 0.0));
        }
        finally
        {
            m.Delete();
        }
    }

    [Fact]
    public void OldThreeKeyRecord_MovesIntoTheSharedStore()
    {
        var m = new Mobile();
        var table = new ProgressTable<WeaponCategory, byte>("branch:migrate-style");

        try
        {
            var writer = new BufferWriter(true);
            writer.WriteEncodedInt(1);
            writer.Write(m);
            writer.Write((byte)WeaponCategory.Archery);
            writer.Write((byte)1);
            writer.Write(90.0);

            MahaonSpecializationPersistenceHelper.Read3(
                Reader(writer), table, r => (WeaponCategory)r.ReadByte(), r => r.ReadByte()
            );

            Assert.Equal(90.0, table.GetValueOrDefault((m, WeaponCategory.Archery, (byte)1), 0.0));
        }
        finally
        {
            m.Delete();
        }
    }

    /// <summary>
    ///     После переезда старый файл обязан писать пустой список — иначе при следующей
    ///     загрузке он перенёс бы устаревшие значения поверх свежих.
    /// </summary>
    [Fact]
    public void OldStore_WritesAnEmptyListAfterTheMove()
    {
        var table = new ProgressTable<Fake>("branch:empty");
        var writer = new BufferWriter(true);

        MahaonSpecializationPersistenceHelper.Write(writer, table, (w, k) => w.Write((byte)k));

        var bytes = Bytes(writer);
        IGenericReader reader = new BufferReader(bytes);

        Assert.Equal(0, reader.ReadEncodedInt());
        Assert.Equal(bytes.Length, ((BufferReader)reader).Position);
    }

    [Fact]
    public void SharedStore_SurvivesASaveAndLoad()
    {
        var m = new Mobile();

        try
        {
            MahaonProgress.Set(m, "branch:roundtrip/Alpha", 12.25);
            MahaonProgress.Set(m, "cap/Школа призыва", 110.0);

            var writer = new BufferWriter(true);
            MahaonProgress.WriteAll(writer);

            MahaonProgress.Set(m, "branch:roundtrip/Alpha", 0.0);
            MahaonProgress.Set(m, "cap/Школа призыва", 0.0);

            MahaonProgress.ReadAll(Reader(writer));

            Assert.Equal(12.25, MahaonProgress.Get(m, "branch:roundtrip/Alpha"));
            Assert.Equal(110.0, MahaonProgress.Get(m, "cap/Школа призыва"));
        }
        finally
        {
            m.Delete();
        }
    }
}

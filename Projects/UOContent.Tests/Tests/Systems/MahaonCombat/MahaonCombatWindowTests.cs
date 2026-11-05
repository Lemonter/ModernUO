using System;
using System.Text;
using Server.Mobiles;
using Server.Systems.MahaonCombat;
using Xunit;

namespace Server.Tests.Systems.MahaonCombat;

/// <summary>
///     Боевое окно и доска мастерства для Unity: выбор стиля отдельно от места удара и формат
///     подтипов 7 и 8.
///
///     Пакеты здесь разбираются ЦЕЛИКОМ, по описанию из Ultima-main/COMBAT-UI-TASK.md, и
///     проверяется, что разбор съел ровно весь пакет. Unity-клиент читает именно этот формат;
///     лишний или пропущенный байт на сервере сдвинул бы у него всё, что идёт дальше, — и
///     такая ошибка не видна ни в одном другом тесте.
/// </summary>
[Collection("Sequential UOContent Tests")]
public class MahaonCombatWindowTests
{
    /// <summary>Читатель формата 0xF9: big-endian, str8 и str16 — как описано в задаче.</summary>
    private ref struct Wire
    {
        private readonly ReadOnlySpan<byte> _data;

        public Wire(ReadOnlySpan<byte> data)
        {
            _data = data;
            Position = 0;
        }

        public int Position;

        public int Length => _data.Length;

        public byte U8() => _data[Position++];

        public sbyte S8() => (sbyte)_data[Position++];

        public ushort U16()
        {
            var v = (ushort)((_data[Position] << 8) | _data[Position + 1]);
            Position += 2;
            return v;
        }

        public string Str8()
        {
            var len = U8();
            var s = Encoding.UTF8.GetString(_data.Slice(Position, len));
            Position += len;
            return s;
        }

        public string Str16()
        {
            var len = U16();
            var s = Encoding.UTF8.GetString(_data.Slice(Position, len));
            Position += len;
            return s;
        }
    }

    // ---- Выбор стиля ---------------------------------------------------------------------

    [Fact]
    public void Style_IsChosenIndependentlyOfTheHitLocation()
    {
        var pm = new PlayerMobile();

        try
        {
            pm.Skills.Tactics.Base = 120.0;

            Assert.True(WeaponStyleSystem.SetActiveStyle(pm, WeaponCategory.Swords, 3));
            Assert.Equal(3, WeaponStyleSystem.GetActiveStyle(pm, WeaponCategory.Swords));

            // Смена места удара стиль больше не трогает.
            HitLocationSystem.SetLocation(pm, HitLocation.Neck);
            Assert.Equal(3, WeaponStyleSystem.GetActiveStyle(pm, WeaponCategory.Swords));

            HitLocationSystem.SetLocation(pm, HitLocation.Legs);
            Assert.Equal(3, WeaponStyleSystem.GetActiveStyle(pm, WeaponCategory.Swords));
        }
        finally
        {
            pm.Delete();
        }
    }

    [Fact]
    public void Style_OutOfRangeIsRefused()
    {
        var pm = new PlayerMobile();

        try
        {
            Assert.False(WeaponStyleSystem.SetActiveStyle(pm, WeaponCategory.Swords, WeaponStyleSystem.StyleCount));
        }
        finally
        {
            pm.Delete();
        }
    }

    /// <summary>
    ///     Переезд: у того, кто стиль ещё не выбирал, первым записывается тот стиль, что у него
    ///     получался от места удара, — обновление ничего не меняет молча.
    /// </summary>
    [Fact]
    public void Style_FirstReadKeepsWhatTheLocationUsedToGive()
    {
        var pm = new PlayerMobile();

        try
        {
            pm.Skills.Tactics.Base = 120.0;
            HitLocationSystem.SetLocation(pm, HitLocation.Neck);

            var first = WeaponStyleSystem.GetActiveStyle(pm, WeaponCategory.Macing);

            // А дальше место уже ни при чём.
            HitLocationSystem.SetLocation(pm, HitLocation.Chest);
            Assert.Equal(first, WeaponStyleSystem.GetActiveStyle(pm, WeaponCategory.Macing));
        }
        finally
        {
            pm.Delete();
        }
    }

    [Fact]
    public void FavoriteSpell_IsRemembered()
    {
        var pm = new PlayerMobile();

        try
        {
            MahaonCombatChoices.SetFavoriteSpell(pm, 42);
            Assert.Equal(42, MahaonCombatChoices.GetFavoriteSpell(pm));
        }
        finally
        {
            pm.Delete();
        }
    }

    // ---- Подтип 8 -------------------------------------------------------------------------

    [Fact]
    public void CombatWindow_ParsesExactlyByTheDocumentedFormat()
    {
        var pm = new PlayerMobile();

        try
        {
            pm.Skills.Tactics.Base = 60.0;

            var w = new Wire(MahaonCombatWindow.BuildCombatWindow(pm, 0));

            Assert.Equal(0xF9, w.U8());
            Assert.Equal(w.Length, w.U16());
            Assert.Equal(MahaonCombatWindow.SubtypeCombat, w.U8());

            Assert.Equal(0, w.U8());                     // вкладка
            Assert.Equal(0x000C, w.U16());               // тело: мужчина по умолчанию

            var locations = w.U8();
            Assert.Equal(7, locations);

            for (var i = 0; i < locations; i++)
            {
                w.U8();
                Assert.NotEmpty(w.Str8());
                Assert.InRange(w.U8(), 0, 2);
                w.U16();
                w.S8();
                w.Str16();
            }

            w.Str8();                                    // цель
            var weak = w.U8();
            for (var i = 0; i < weak; i++)
            {
                w.U8();
                w.U8();
            }

            var stances = w.U8();
            Assert.Equal(4, stances);
            for (var i = 0; i < stances; i++)
            {
                w.U8();
                w.Str8();
                w.U8();
                w.Str16();
            }

            var category = w.U8();
            var styles = w.U8();
            Assert.True(category == 255 ? styles == 0 : styles == WeaponStyleSystem.StyleCount);
            for (var i = 0; i < styles; i++)
            {
                w.U8();
                w.Str8();
                w.U8();
                w.U16();
                w.Str16();
            }

            var archery = w.U8();
            for (var i = 0; i < archery; i++)
            {
                w.U8();
                w.Str8();
                w.U8();
                w.Str16();
            }

            w.U8();                                      // стихия
            w.U16();                                     // любимое заклинание

            var channels = w.U8();
            Assert.Equal(3, channels);
            for (var i = 0; i < channels; i++)
            {
                w.U8();
                w.Str8();
                Assert.InRange(w.U8(), 0, 2);
                w.Str16();
            }

            string[] titles = { "ИТОГ УДАРА", "ИТОГ ВЫСТРЕЛА", "ИТОГ ЗАКЛИНАНИЯ" };

            foreach (var title in titles)
            {
                Assert.Equal(title, w.Str8());

                var rows = w.U8();
                for (var r = 0; r < rows; r++)
                {
                    w.Str8();
                    w.Str8();

                    var lines = w.U8();
                    for (var l = 0; l < lines; l++)
                    {
                        w.Str16();
                        Assert.InRange(w.S8(), -1, 1);
                    }
                }
            }

            Assert.InRange(w.U8(), 0, 4);                // стихия шара
            Assert.InRange(w.U8(), 0, 100);              // наполненность

            Assert.Equal(w.Length, w.Position);          // ни байтом больше, ни байтом меньше
        }
        finally
        {
            pm.Delete();
        }
    }

    // ---- Подтип 7 -------------------------------------------------------------------------

    [Fact]
    public void Board_ParsesExactlyAndEndsWithWeaponMastery()
    {
        var pm = new PlayerMobile();

        try
        {
            var w = new Wire(MahaonCombatWindow.BuildBoard(pm));

            Assert.Equal(0xF9, w.U8());
            Assert.Equal(w.Length, w.U16());
            Assert.Equal(MahaonCombatWindow.SubtypeBoard, w.U8());

            var categories = w.U8();
            string lastCategory = null;
            var masteryItems = 0;
            var masteryLines = 0;
            var spellItems = 0;

            for (var c = 0; c < categories; c++)
            {
                lastCategory = w.Str8();

                var skills = w.U8();
                for (var s = 0; s < skills; s++)
                {
                    w.U8();
                    w.Str8();
                    w.U16();
                    w.U16();

                    var branches = w.U8();
                    for (var b = 0; b < branches; b++)
                    {
                        Assert.StartsWith("name:", w.Str8());
                        w.Str8();
                        w.U16();
                        w.U16();
                        w.U8();

                        var perks = w.U8();
                        for (var p = 0; p < perks; p++)
                        {
                            w.U8();
                            w.U8();
                            w.Str16();
                        }
                    }

                    // Линейки: у «Владения оружием» — оружие (арт предмета), у магических
                    // навыков — школы с заклинаниями (иконка из гампов, этап 7).
                    var weaponCategory = lastCategory == "Владение оружием";
                    var lines = w.U8();
                    for (var l = 0; l < lines; l++)
                    {
                        Assert.StartsWith("line:", w.Str8());
                        w.Str8();

                        if (weaponCategory)
                        {
                            masteryLines++;
                        }

                        var items = w.U8();
                        for (var i = 0; i < items; i++)
                        {
                            Assert.StartsWith(weaponCategory ? "mastery:weapon/" : "mastery:spell/", w.Str8());
                            Assert.NotEmpty(w.Str8());
                            Assert.Equal(weaponCategory ? 0 : 1, w.U8());
                            w.U16();
                            Assert.InRange(w.U8(), 0, 100);
                            Assert.InRange(w.U8(), 0, 100);
                            w.U8();
                            w.Str16();

                            if (weaponCategory)
                            {
                                masteryItems++;
                            }
                            else
                            {
                                spellItems++;
                            }
                        }
                    }
                }
            }

            Assert.Equal(w.Length, w.Position);
            Assert.Equal("Владение оружием", lastCategory);
            Assert.Equal(WeaponMasterySystem.Lines.Count, masteryLines);

            var expected = 0;
            foreach (var line in WeaponMasterySystem.Lines.Keys)
            {
                foreach (var _ in WeaponMasterySystem.KindsInLine(line))
                {
                    expected++;
                }
            }

            Assert.Equal(expected, masteryItems);

            // Каждое заклинание из каталога школ — ровно одной строкой на доске.
            Assert.Equal(SpellSchools.Catalog.Count, spellItems);
            Assert.True(spellItems > 100, $"заклинаний на доске: {spellItems}");
        }
        finally
        {
            pm.Delete();
        }
    }

    [Fact]
    public void BodyGump_FollowsTheClientRule()
    {
        var pm = new PlayerMobile();

        try
        {
            pm.Body = 0x0190;
            Assert.Equal(0x000C, MahaonCombatWindow.BodyGump(pm));

            pm.Body = 0x0191;
            Assert.Equal(0x000D, MahaonCombatWindow.BodyGump(pm));

            pm.Body = 0x025D;
            Assert.Equal(0x000E, MahaonCombatWindow.BodyGump(pm));

            pm.Body = 0x029A;
            Assert.Equal(0x029A, MahaonCombatWindow.BodyGump(pm));

            pm.Body = 0x029B;
            Assert.Equal(0x0299, MahaonCombatWindow.BodyGump(pm));
        }
        finally
        {
            pm.Delete();
        }
    }
}

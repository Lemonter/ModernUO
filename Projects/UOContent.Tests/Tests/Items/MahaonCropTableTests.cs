using System;
using Server.Systems.MahaonFarming;
using Server.Systems.MahaonWorld;
using Xunit;

namespace Server.Tests.Items;

/// <summary>
///     Тесты, закрепляющие единственный опасный инвариант таблицы культур:
///     MahaonCropTable.Data индексируется значением MahaonCropType, то есть порядок строк
///     в таблице обязан совпадать с порядком членов enum'а.
///
///     Цена ошибки несоразмерна её заметности. Значение MahaonCropType лежит в сохранении
///     у каждой посаженной грядки (MahaonCropTile) и у каждого мешочка семян
///     (MahaonCropSeed). Переставь две строки местами — и после перезапуска у игроков
///     молча сменится культура на всех грядках сразу: посеянная дыня взойдёт канталупой.
///     Компилятор такого не видит, в логах ничего не будет, а откатить уже нечем.
///
///     Ровно это я и сделал, когда группировал строки по смыслу — поймано глазами на
///     ревью, а не тестом. Отсюда этот файл.
/// </summary>
/// <remarks>
///     Коллекция та же, что у остальных предметных тестов: создать Item можно только после
///     World.Load, иначе World.AddEntity бросает «Added Onion before world load».
/// </remarks>
[Collection("Sequential UOContent Tests")]
public class MahaonCropTableTests
{
    /// <summary>Как называется предмет урожая, если он назван не так, как культура.</summary>
    private static readonly (MahaonCropType Crop, string ItemType)[] Aliases =
    {
        (MahaonCropType.Wheat, "WheatSheaf"),
        (MahaonCropType.Corn, "EarOfCorn"),
        (MahaonCropType.Mandrake, "MandrakeRoot")
    };

    [Fact]
    public void Data_HasRowForEveryCropType()
    {
        Assert.Equal(Enum.GetValues<MahaonCropType>().Length, MahaonCropTable.Data.Length);
    }

    [Fact]
    public void Data_RowOrderMatchesEnumOrder()
    {
        foreach (var crop in Enum.GetValues<MahaonCropType>())
        {
            var produced = MahaonCropTable.Data[(int)crop].CreateHarvest();

            Assert.NotNull(produced);

            var expected = ExpectedItemTypeName(crop);
            var actual = produced.GetType().Name;

            Assert.True(
                actual.Equals(expected, StringComparison.Ordinal),
                $"{crop} (индекс {(int)crop}) отдаёт {actual}, а должен {expected}. " +
                "Похоже, строки в MahaonCropTable.Data переставлены относительно MahaonCropType — " +
                "это молча сменит культуру на всех уже посаженных грядках."
            );

            produced.Delete();
        }
    }

    /// <summary>
    ///     Зимой грядка прячется, а осенью её собирают — значит спелая графика должна
    ///     отличаться хотя бы от ростка, иначе стадии не видно вовсе.
    /// </summary>
    [Fact]
    public void Data_SproutAndRipeGraphicsDiffer()
    {
        foreach (var crop in Enum.GetValues<MahaonCropType>())
        {
            var def = MahaonCropTable.Data[(int)crop];

            Assert.True(def.SpringGraphic > 0, $"{crop}: нет графики ростка");
            Assert.True(def.SummerGraphic > 0, $"{crop}: нет графики созревания");
            Assert.True(def.AutumnGraphic > 0, $"{crop}: нет спелой графики");

            Assert.True(
                def.SpringGraphic != def.AutumnGraphic,
                $"{crop}: росток и спелая стадия — один и тот же тайл {def.AutumnGraphic:X4}"
            );
        }
    }

    /// <summary>
    ///     Засев полей (MahaonFieldSeeding) переводит вид тайла в сезонную культуру по
    ///     именам членов двух перечислений. Появится MahaonCropKind без пары в
    ///     MahaonCropType — и команда упадёт на этой культуре посреди мира.
    /// </summary>
    [Fact]
    public void EveryCropKindHasASeasonalType()
    {
        foreach (var kind in Enum.GetValues<MahaonCropKind>())
        {
            var type = MahaonCrops.SeasonalTypeFor(kind);

            Assert.Equal(kind.ToString(), type.ToString());
        }
    }

    private static string ExpectedItemTypeName(MahaonCropType crop)
    {
        foreach (var (aliasCrop, itemType) in Aliases)
        {
            if (aliasCrop == crop)
            {
                return itemType;
            }
        }

        return crop.ToString();
    }
}

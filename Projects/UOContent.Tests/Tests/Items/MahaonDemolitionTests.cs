using Server.Systems.MahaonMapEdits;
using Xunit;

namespace Server.Tests.Items;

/// <summary>
///     Тесты правил разрушения.
///
///     Здесь проверяется не механика, а политика: что именно игроку позволено стереть с
///     карты. Ошибка в эту сторону необратима по-настоящему — снесённую гору или дверь
///     подземелья откатить можно, но только если кто-то заметит, а заметят не сразу.
///
///     Правило одно: постройка помечена в tiledata флагами Wall или Roof, ландшафт — нет.
///     Оно выведено из самого tiledata (из 1217 тайлов со словом «wall» в названии флаг
///     стоит у всех, из 138 «rock/mountain» — ни у одного), и тесты закрепляют именно его,
///     а не список графики.
/// </summary>
public class MahaonDemolitionTests
{
    /// <summary>Флаги подменяем прямо в таблице: TileData в тестовом хосте пуст, и это
    /// единственный способ проверить правило, не поднимая клиентские файлы.</summary>
    private static void SetFlags(int graphic, TileFlag flags) =>
        TileData.ItemTable[graphic & TileData.MaxItemValue].Flags = flags;

    [Theory]
    [InlineData(TileFlag.Wall)]
    [InlineData(TileFlag.Roof)]
    [InlineData(TileFlag.Wall | TileFlag.Impassable)]
    [InlineData(TileFlag.Roof | TileFlag.Surface)]
    public void Structures_AreDestructible(TileFlag flags)
    {
        SetFlags(0x4000, flags);

        Assert.True(MahaonDemolition.IsDestructible(0x4000));
    }

    /// <summary>
    ///     Скалы и горы непроходимы, но не помечены как стена — и сносить их взрывом
    ///     нельзя, иначе первый же заряд проделает дыру в ландшафте.
    /// </summary>
    [Theory]
    [InlineData(TileFlag.Impassable)]
    [InlineData(TileFlag.Surface)]
    [InlineData(TileFlag.Impassable | TileFlag.Surface)]
    [InlineData(TileFlag.None)]
    public void Terrain_IsNotDestructible(TileFlag flags)
    {
        SetFlags(0x4001, flags);

        Assert.False(MahaonDemolition.IsDestructible(0x4001));
    }

    /// <summary>
    ///     Двери по флагам проходят как стены (секретные двери — Wall + Door), но в
    ///     подземельях это загадка, а не препятствие. Взрывать их — ломать чужой замысел.
    /// </summary>
    [Fact]
    public void Doors_SurviveEvenThoughTheyAreFlaggedAsWalls()
    {
        SetFlags(0x4002, TileFlag.Wall | TileFlag.Door | TileFlag.Impassable);

        Assert.False(MahaonDemolition.IsDestructible(0x4002));
    }

    /// <summary>Пометка события должна быть одна на весь подрыв — по ней идёт откат.</summary>
    [Fact]
    public void ReasonFor_IsStableWithinOneEvent()
    {
        var first = MahaonDemolition.ReasonFor(null, "подрыв");
        var second = MahaonDemolition.ReasonFor(null, "подрыв");

        Assert.Equal(first, second);
        Assert.Contains("подрыв", first);
    }

    /// <summary>Без карты рушить нечего — и падать тоже не на чем.</summary>
    [Fact]
    public void NullMap_IsNotDestructibleArea()
    {
        Assert.False(MahaonDemolition.IsDestructibleArea(null, Point3D.Zero));
        Assert.False(MahaonDemolition.IsDestructibleArea(Map.Internal, Point3D.Zero));
        Assert.Equal(0, MahaonDemolition.Demolish(Map.Internal, Point3D.Zero, 3, "тест"));
    }
}

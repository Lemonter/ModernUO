using System;
using Server.Multis;
using Server.Regions;

namespace Server.Systems.MahaonMapEdits;

/// <summary>
///     Земляные работы: ямы, рвы, пруды.
///
///     Единственное, чего нельзя изобразить предметами. Статик можно поставить поверх
///     земли, но опустить саму землю — только правкой карты, и до сих пор эта половина
///     системы не была задействована вовсе.
///
///     ---- Как это выглядит в игре ----
///
///     Копаешь тайл — он опускается на единицу и превращается в вскопанную землю. Копаешь
///     дальше — глубже. На MaxDepth тайл становится водой: яма набрала воду. Вода помечена
///     в tiledata флагом Wet, а рыбалка у нас принимает всё мокрое (см. HarvestTarget), так
///     что вырытый пруд оказывается рыбным сам собой, без единой строчки про рыбалку.
///
///     Засыпать можно обратно — землю поднимают той же лопатой, пока не вернётся исходная
///     высота. Это внутриигровая отмена, отдельная от админского [MapUndo: игрок правит
///     свою яму сам, не дёргая ГМ.
///
///     ---- Где нельзя ----
///
///     В охраняемых городах — по той же причине, что и подрыв. Под чужим домом — иначе
///     дом окажется висящим над ямой. И в скале: прокоп горы — это шахта, у неё свои
///     правила и свой навык, и смешивать их не надо.
/// </summary>
public static class MahaonExcavation
{
    /// <summary>На сколько единиц ниже исходной земли можно закопаться.</summary>
    public const int MaxDepth = 5;

    /// <summary>Вскопанная земля — вид ямы, пока она не набрала воду.</summary>
    private static readonly int[] DugGraphics = { 0x0009, 0x000A, 0x000B, 0x000C };

    /// <summary>Вода. Проверено по tiledata: ровно эти шесть тайлов земли помечены Wet.</summary>
    private static readonly int[] WaterGraphics = { 0x00A8, 0x00A9, 0x00AA, 0x00AB, 0x0136, 0x0137 };

    public enum DigResult
    {
        Dug,
        Flooded,
        TooDeep,
        Forbidden,
        Blocked
    }

    /// <summary>Исходная высота тайла — до всех наших правок.</summary>
    public static int OriginalZ(Map map, int x, int y)
    {
        var blockId = MahaonMapEdits.BlockIdFor(map, x, y);

        if (MahaonMapEdits.TryGetOriginalLand(map.MapID, blockId, out var land))
        {
            return land[(y & 7) * 8 + (x & 7)].Z;
        }

        return map.Tiles.GetLandTile(x, y).Z;
    }

    /// <summary>
    ///     Текущая высота тайла с учётом уже выкопанного.
    ///
    ///     Спрашиваем журнал, а не живую карту: живой слой можно выключить, и тогда
    ///     TileMatrix о наших правках не знает, а глубина ямы должна накапливаться всё
    ///     равно. Это же относится к IsWater ниже.
    /// </summary>
    public static int CurrentZ(Map map, int x, int y) => MahaonMapEdits.LandAt(map, x, y).Z;

    public static bool IsWater(Map map, int x, int y) =>
        Array.IndexOf(WaterGraphics, MahaonMapEdits.LandAt(map, x, y).Graphic) >= 0;

    /// <summary>Можно ли тут вообще копать — вопрос к месту, не к глубине.</summary>
    public static bool CanDigHere(Map map, int x, int y, out string why)
    {
        why = null;

        if (map == null || map == Map.Internal)
        {
            why = "Здесь не копают.";
            return false;
        }

        var z = CurrentZ(map, x, y);
        var point = new Point3D(x, y, z);

        if (Region.Find(point, map)?.GetRegion<GuardedRegion>() != null)
        {
            why = "В городе копать не дадут — стража рядом.";
            return false;
        }

        if (BaseHouse.FindHouseAt(point, map, 16) != null)
        {
            why = "Под домом копать нельзя.";
            return false;
        }

        var flags = TileData.LandTable[MahaonMapEdits.LandAt(map, x, y).Graphic & TileData.MaxLandValue].Flags;

        if ((flags & TileFlag.Impassable) != 0)
        {
            why = "Тут камень — это работа для шахтёрской кирки, а не для лопаты.";
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Копает один тайл. Пометка общая на всю яму, чтобы вся работа откатывалась разом.
    /// </summary>
    public static DigResult Dig(Map map, int x, int y, string reason)
    {
        if (!CanDigHere(map, x, y, out _))
        {
            return DigResult.Forbidden;
        }

        if (IsWater(map, x, y))
        {
            return DigResult.TooDeep; // глубже воды не копают
        }

        var original = OriginalZ(map, x, y);
        var current = CurrentZ(map, x, y);
        var depth = original - current;

        if (depth >= MaxDepth)
        {
            return DigResult.TooDeep;
        }

        var newZ = current - 1;
        var newDepth = depth + 1;

        if (newDepth >= MaxDepth)
        {
            // Яма набрала воду. Высоту при этом не опускаем ещё раз: поверхность воды
            // стоит там же, где было дно, иначе пруд получается утопленным в землю.
            MahaonMapEdits.SetLand(map, x, y, WaterGraphics[Utility.Random(WaterGraphics.Length)], newZ, reason);

            return DigResult.Flooded;
        }

        MahaonMapEdits.SetLand(map, x, y, DugGraphics[Utility.Random(DugGraphics.Length)], newZ, reason);

        return DigResult.Dug;
    }

    /// <summary>
    ///     Засыпает тайл обратно на единицу. Внутриигровая отмена: игрок правит свою яму
    ///     сам, не дёргая ГМ.
    /// </summary>
    public static bool Fill(Map map, int x, int y, string reason)
    {
        if (map == null || map == Map.Internal)
        {
            return false;
        }

        var original = OriginalZ(map, x, y);
        var current = CurrentZ(map, x, y);

        if (current >= original)
        {
            return false; // выше исходной земли не насыпаем: это уже не засыпка, а стройка
        }

        var blockId = MahaonMapEdits.BlockIdFor(map, x, y);
        var restored = MahaonMapEdits.TryGetOriginalLand(map.MapID, blockId, out var land)
            ? land[(y & 7) * 8 + (x & 7)].Graphic
            : (ushort)DugGraphics[0];

        var newZ = current + 1;

        // Вернулись на исходную высоту — возвращаем и исходный вид. Пока не вернулись,
        // тайл остаётся вскопанным, а не водой: засыпаемый пруд перестаёт быть прудом
        // с первой же лопаты.
        MahaonMapEdits.SetLand(
            map, x, y,
            newZ >= original ? restored : DugGraphics[Utility.Random(DugGraphics.Length)],
            newZ,
            reason
        );

        return true;
    }
}

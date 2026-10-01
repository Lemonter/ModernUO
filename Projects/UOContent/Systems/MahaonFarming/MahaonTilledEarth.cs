using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Вспаханная земля — след от плуга, в который стоит сеять.
///
///     Кладётся на тайл, где прошёлся плуг, но на единицу выше по Z: так грядка (её сажают
///     на саму землю) оказывается визуально поверх пашни, а не спорит с ней за одну высоту.
///     Полоть можно только чистую землю, поэтому двух пашен на одном тайле не бывает.
///
///     Смысл ровно один: посеянное в пашню даёт вдвое больше. Пашня не вечная — она
///     исчезает вместе со снятым урожаем, так что перед каждым посевом землю надо готовить
///     заново. Иначе один раз вспаханное поле кормило бы двойным урожаем вечно.
/// </summary>
[SerializationGenerator(0, false)]
public partial class MahaonTilledEarth : Item
{
    /// <summary>Насколько выше земли лежит пашня.</summary>
    public const int ZOffset = 1;

    /// <summary>Во сколько раз щедрее посев в подготовленную землю.</summary>
    public const int YieldMultiplier = 2;

    /// <summary>Графика вспаханной земли — «dirt patch» из клиента.</summary>
    private static readonly int[] Graphics = { 0x0911, 0x0912, 0x0913, 0x0914 };

    [Constructible]
    public MahaonTilledEarth() : base(Graphics[Utility.Random(Graphics.Length)])
    {
        Movable = false;
        Name = "вспаханная земля";
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add($"Посев здесь даёт урожай в {YieldMultiplier} раза больше");
    }

    /// <summary>Есть ли под этой точкой вспаханная земля. Ищем на тайле, не заботясь о
    /// высоте: пашня лежит на +1 от земли, грядка — на самой земле.</summary>
    public static MahaonTilledEarth Find(Point3D loc, Map map)
    {
        if (map == null)
        {
            return null;
        }

        foreach (var item in map.GetItemsInRange<MahaonTilledEarth>(loc, 0))
        {
            if (!item.Deleted && item.X == loc.X && item.Y == loc.Y)
            {
                return item;
            }
        }

        return null;
    }
}

using ModernUO.Serialization;
using Server.Systems.MahaonFarming;
using Server.Targeting;

namespace Server.Items;

/// <summary>
///     Плуг — орудие для работы в поле.
///
///     Вспашка ничего не сажает — она только кладёт на тайл вспаханную землю
///     (MahaonTilledEarth). Сеют по-прежнему семенами, и грядка остаётся сезонной; пашня
///     лишь удваивает то, что с неё соберут осенью, и исчезает вместе с урожаем.
/// </summary>
[SerializationGenerator(0, false)]
public partial class MahaonPlough : Item
{
    /// <summary>Сколько вспашек выдерживает, прежде чем развалиться.</summary>
    [SerializableField(0)]
    private int _usesRemaining;

    [Constructible]
    public MahaonPlough() : base(0x1500)
    {
        Weight = 10.0;
        Name = "плуг";
        _usesRemaining = 150;
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add($"Осталось использований: {_usesRemaining}");
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendMessage(0x22, "Плуг должен быть у тебя в рюкзаке.");
            return;
        }

        from.SendMessage(0x3B2, "Что вспахать?");
        from.Target = new PloughTarget(this);
    }

    /// <summary>Списывает одну вспашку. Возвращает false, если плуг развалился.</summary>
    public bool ConsumeUse(Mobile from)
    {
        UsesRemaining--;

        if (_usesRemaining > 0)
        {
            return true;
        }

        from.SendMessage(0x22, "Плуг разваливается у тебя в руках.");
        Delete();

        return false;
    }

    private class PloughTarget : Target
    {
        private readonly MahaonPlough _plough;

        public PloughTarget(MahaonPlough plough) : base(3, true, TargetFlags.None) => _plough = plough;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (_plough.Deleted || !_plough.IsChildOf(from.Backpack))
            {
                return;
            }

            // Обирать плугом больше нечего: поля с карты превратились в грядки
            // (MahaonFieldSeeding), а грядку собирают двойным кликом по ней самой. Плуг
            // теперь только пашет.
            if (targeted is not LandTarget land)
            {
                from.SendMessage(0x22, "Плугом пашут землю, а не это.");
                return;
            }

            var loc = new Point3D(land.X, land.Y, land.Z);
            var map = from.Map;

            if (map == null || !from.InRange(loc, 2))
            {
                from.SendMessage(0x22, "Слишком далеко.");
                return;
            }

            var flags = TileData.LandTable[land.TileID & TileData.MaxLandValue].Flags;

            if ((flags & TileFlag.Impassable) != 0 || (flags & TileFlag.Wet) != 0)
            {
                from.SendMessage(0x22, "Здесь не вспашешь — нужна ровная земля.");
                return;
            }

            if (MahaonTilledEarth.Find(loc, map) != null)
            {
                from.SendMessage(0x3B2, "Эта земля уже вспахана — можно сеять.");
                return;
            }

            foreach (var item in map.GetItemsInRange<Item>(loc, 0))
            {
                if (item.Deleted || item.X != loc.X || item.Y != loc.Y)
                {
                    continue;
                }

                // Грядку проверяем отдельно от видимости: зимой и после сбора она невидима,
                // но никуда не делась. Без этого поле можно было бы вспахать прямо поверх
                // неё и посеять вторую в тот же тайл.
                if (item is MahaonCropTile)
                {
                    from.SendMessage(0x22, "Тут уже грядка.");
                    return;
                }

                if (item.Visible)
                {
                    from.SendMessage(0x22, "Это место занято.");
                    return;
                }
            }

            if (!_plough.ConsumeUse(from))
            {
                return;
            }

            // Пашня ложится на единицу выше земли: грядку сажают на саму землю, и так они
            // не спорят за одну высоту, а пашня видна из-под всходов.
            var tilled = new MahaonTilledEarth();
            tilled.MoveToWorld(new Point3D(loc.X, loc.Y, loc.Z + MahaonTilledEarth.ZOffset), map);

            from.SendMessage(0x59, "Ты вспахиваешь землю. Посеянное здесь даст вдвое больше.");
            from.PlaySound(0x125);
        }
    }
}

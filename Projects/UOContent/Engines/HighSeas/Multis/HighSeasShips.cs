/// <summary>
///     Хранилище High Seas: списки доступа галер по серийнику корабля. В ServUO эта информация
///     лежала прямо в <c>BaseGalleon.SecurityEntry</c> и уезжала в общий сейв предметов; здесь она
///     вынесена в отдельный <see cref="GenericPersistence" /> (тот же приём, что
///     <c>Engines/NewMagincia/Bazaar/Core/BazaarPersistence.cs</c>), потому что на самом корабле
///     разрешены только <c>[SerializableField]</c>-поля, а словарь «мобил → уровень доступа» таким
///     полем не является.
///
///     Приоритет 10 (а не 1 или 2, как у Mobiles/Items) выбран осознанно: к моменту загрузки этого
///     файла предметы (2) и мобилы (1) уже прочитаны, поэтому <c>ReadEntity&lt;Mobile&gt;</c> внутри
///     <see cref="ShipAccessList" /> возвращает живые объекты и можно заодно выбросить записи давно
///     удалённых кораблей.
///
///     Сюда же переехал таймер выцветания краски (в ServUO он дёргался из <c>Serialize</c> корабля):
///     раз в минуту обходит <see cref="BaseBoat.Boats" /> и вызывает <c>CheckPaintDecay</c>.
/// </summary>
using System;
using System.Collections.Generic;
using Server.Multis;

namespace Server.Engines.HighSeas;

public sealed class HighSeasShips : GenericPersistence
{
    private static readonly Dictionary<Serial, ShipAccessList> _accessLists = new();

    private static HighSeasShips _persistence;

    public HighSeasShips() : base("HighSeasShips", 10)
    {
    }

    public static void Configure()
    {
        _persistence = new HighSeasShips();

        Timer.StartTimer(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), DecayPaint);
    }

    /// <summary>Список доступа корабля; создаётся при первом обращении.</summary>
    public static ShipAccessList GetAccessList(BaseGalleon galleon)
    {
        if (galleon?.Deleted != false)
        {
            return null;
        }

        if (!_accessLists.TryGetValue(galleon.Serial, out var list))
        {
            list = new ShipAccessList(galleon.Serial);
            list.SetToDefault();
            _accessLists[galleon.Serial] = list;
        }

        list.Galleon = galleon;

        return list;
    }

    public static void RemoveAccessList(BaseGalleon galleon)
    {
        if (galleon != null)
        {
            _accessLists.Remove(galleon.Serial);
        }
    }

    /// <summary>
    ///     Есть ли у корабля сохранённый список доступа. Нужен там, где самого корабля уже нет
    ///     (затопление), и в тестах: <see cref="GetAccessList" /> для удалённого корабля всегда null.
    /// </summary>
    public static bool HasAccessList(Serial serial) => _accessLists.ContainsKey(serial);

    private static void DecayPaint()
    {
        var boats = BaseBoat.Boats;

        // Обходим по индексу: CheckPaintDecay меняет только Hue корабля и не трогает список.
        for (var i = 0; i < boats.Count; i++)
        {
            if (boats[i] is BaseGalleon galleon)
            {
                galleon.CheckPaintDecay();
            }
        }
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version

        writer.WriteEncodedInt(_accessLists.Count);

        foreach (var (serial, list) in _accessLists)
        {
            writer.Write(serial);
            list.Serialize(writer);
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // version

        var count = reader.ReadEncodedInt();

        for (var i = 0; i < count; i++)
        {
            var boatSerial = reader.ReadSerial();
            var list = new ShipAccessList(boatSerial);
            list.Deserialize(reader);

            // Корабль могли удалить до сохранения — тогда запись не нужна.
            if (World.FindItem(boatSerial) is BaseGalleon)
            {
                _accessLists[boatSerial] = list;
            }
        }
    }
}

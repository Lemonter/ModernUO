using System;
using System.Collections.Generic;
using Server.Commands;
using Server.Engines.XmlSpawner2;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Despise;

/// <summary>Ported from ServUO's Despise Revamped dungeon (Scripts/Services/Dungeons/
/// DespiseRevamped/Setup.cs) — one-time in-game setup: run `[XmlLoad spawns/
/// despiserevamped.xml` first to populate the spawners, then `[SetupDespise` to place the
/// controller/ankhs/teleporters/wisp. `[DeleteDespise` tears it all back down via
/// WeakEntityCollection's "despise" tag.</summary>
public static class DespiseRevampedSetup
{
    public static void Initialize()
    {
        CommandSystem.Register("SetupDespise", AccessLevel.GameMaster, SetupDespiseOnCommand);
        CommandSystem.Register("DeleteDespise", AccessLevel.GameMaster, DeleteDespiseOnCommand);
    }

    private static void DeleteDespiseOnCommand(CommandEventArgs e)
    {
        WeakEntityCollection.Delete("despise");
        DespiseController.Instance = null;
    }

    /// <summary>
    ///     Установка подземелья.
    ///
    ///     Была устроена так, что одна ошибка посреди установки делала её неповторимой:
    ///     DespiseController в своём конструкторе сразу прописывается в Instance, поэтому
    ///     упавшая на полпути установка оставляла половину подземелья и заполненный
    ///     Instance — и следующий запуск отвечал «уже установлено», ничего не доделав.
    ///     Ровно это и происходило: ошибка в консоль, а потом «всё уже стоит».
    ///
    ///     Теперь при любой осечке всё поставленное сносится обратно (WeakEntityCollection
    ///     по метке «despise» — тем же способом, что и [DeleteDespise), Instance
    ///     очищается, и команду можно просто дать заново. Текст ошибки при этом уходит не
    ///     только в консоль, но и тому, кто команду дал.
    /// </summary>
    public static void SetupDespiseOnCommand(CommandEventArgs e)
    {
        try
        {
            SetupDespiseCore(e);
        }
        catch (System.Exception ex)
        {
            WeakEntityCollection.Delete("despise");
            DespiseController.Instance = null;

            e.Mobile.SendMessage(0x22, $"Установка Despise не удалась и откачена: {ex.GetType().Name}: {ex.Message}");
            e.Mobile.SendMessage(0x22, "Можно дать команду ещё раз — половины подземелья в мире не осталось.");

            Console.WriteLine($"[SetupDespise] {ex}");
        }
    }

    private static void SetupDespiseCore(CommandEventArgs e)
    {
        if (DespiseController.Instance == null)
        {
            DespiseController.RemoveAnkh();

            var controller = new DespiseController();
            WeakEntityCollection.Add("despise", controller);
            controller.MoveToWorld(new Point3D(5571, 626, 30), Map.Trammel);

            var goodAnkh = new DespiseAnkh(Alignment.Good);
            WeakEntityCollection.Add("despise", goodAnkh);
            goodAnkh.MoveToWorld(new Point3D(5474, 525, 79), Map.Trammel);

            var evilAnkh = new DespiseAnkh(Alignment.Evil);
            WeakEntityCollection.Add("despise", evilAnkh);
            evilAnkh.MoveToWorld(new Point3D(5472, 754, 10), Map.Trammel);

            SetupTeleporters();

            var wisp = new MysteriousWisp();
            WeakEntityCollection.Add("despise", wisp);
            wisp.MoveToWorld(new Point3D(1303, 1088, 0), Map.Trammel);

            // Сначала собираем список, потом подменяем.
            //
            // Внутри цикла происходило и то и другое разом: MoveToWorld кладёт новый
            // телепорт в тот же сектор, по которому мы идём, а Delete убирает оттуда
            // старый. Перечислитель этого не переживает — «Collection was modified after
            // the enumerator was instantiated», и установка обрывалась ровно здесь.
            var oldTeleporters = new List<Teleporter>();

            foreach (var item in Map.Trammel.GetItemsInRange(new Point3D(5588, 631, 30), 2))
            {
                if (item is Teleporter old)
                {
                    oldTeleporters.Add(old);
                }
            }

            foreach (var old in oldTeleporters)
            {
                if (old.Deleted)
                {
                    continue;
                }

                var tele = new DespiseTeleporter();
                WeakEntityCollection.Add("despise", tele);
                tele.PointDest = old.PointDest;
                tele.MapDest = old.MapDest;
                tele.MoveToWorld(old.Location, old.Map);

                old.Delete();
            }

            e.Mobile.SendMessage(0x59, "Despise установлен.");
        }
        else
        {
            e.Mobile.SendMessage(0x35, "Despise уже установлен — если нужно переставить, сначала [DeleteDespise.");
        }

        // Вызов стоял ЗА пределами if/else и без проверки: если создание контроллера
        // сорвалось, здесь прилетал второй NullReferenceException поверх первого.
        DespiseController.Instance?.CheckSpawnersVersion3();

        WarnIfSpawnersMissing(e.Mobile);
    }

    /// <summary>
    ///     Спаунеры подземелья приходят не отсюда, а из XML: «[XmlLoad spawns/
    ///     despiserevamped.xml». Про этот шаг легко забыть — команда отработает без единой
    ///     жалобы и оставит пустое подземелье. Пусть лучше скажет прямо.
    /// </summary>
    private static void WarnIfSpawnersMissing(Mobile from)
    {
        foreach (var item in World.Items.Values)
        {
            if (item is XmlSpawner { Deleted: false } spawner &&
                spawner.Name?.ToLowerInvariant().Contains("despiserevamped") == true)
            {
                return;
            }
        }

        from.SendMessage(
            0x35,
            "Спаунеров despiserevamped в мире нет — подземелье будет пустым. Сначала нужен [XmlLoad spawns/despiserevamped.xml."
        );
    }

    public static void SetupTeleporters()
    {
        // Gate1
        var gate1 = new GateTeleporter(3948, 1965, new Point3D(5458, 610, 50), Map.Trammel);
        var gate2 = new GateTeleporter(3948, 1960, new Point3D(5476, 737, 5), Map.Trammel);
        WeakEntityCollection.Add("despise", gate1);
        WeakEntityCollection.Add("despise", gate2);

        gate1.MoveToWorld(new Point3D(5476, 737, 5), Map.Trammel);
        gate2.MoveToWorld(new Point3D(5458, 610, 50), Map.Trammel);

        // Gate2
        gate1 = new GateTeleporter(3948, 1960, new Point3D(5460, 675, 20), Map.Trammel);
        gate2 = new GateTeleporter(3948, 1965, new Point3D(5460, 523, 60), Map.Trammel);
        WeakEntityCollection.Add("despise", gate1);
        WeakEntityCollection.Add("despise", gate2);

        gate1.MoveToWorld(new Point3D(5460, 523, 60), Map.Trammel);
        gate2.MoveToWorld(new Point3D(5460, 675, 20), Map.Trammel);

        // Gate3
        gate1 = new GateTeleporter(3948, 1965, new Point3D(5387, 628, 30), Map.Trammel);
        gate2 = new GateTeleporter(3948, 1960, new Point3D(5388, 753, 5), Map.Trammel);
        WeakEntityCollection.Add("despise", gate1);
        WeakEntityCollection.Add("despise", gate2);

        gate1.MoveToWorld(new Point3D(5388, 753, 5), Map.Trammel);
        gate2.MoveToWorld(new Point3D(5387, 628, 30), Map.Trammel);
    }
}

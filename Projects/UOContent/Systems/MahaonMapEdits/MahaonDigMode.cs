using System.Collections.Generic;

namespace Server.Systems.MahaonMapEdits;

/// <summary>
///     Числа заданы явно и менять их нельзя: режим лежит в сохранении одним байтом, и
///     перестановка значений молча переключила бы режим у всех, кто его выбирал.
///     Единица — бывший Harvest, нулём был Terraform, ныне Dig.
/// </summary>
public enum DigMode : byte
{
    /// <summary>Копать: земля опускается. Значение по умолчанию — то есть default(DigMode).</summary>
    Dig = 0,

    /// <summary>Добыча: песок. Глубина при этом не меняется.</summary>
    Harvest = 1,

    /// <summary>Закапывать: земля поднимается обратно, но не выше целины.</summary>
    Fill = 2
}

/// <summary>
///     Чем сейчас занят игрок с лопатой в руках: копает, закапывает или добывает песок.
///
///     ---- Зачем режим ----
///
///     Лопатой делают разные вещи, и по цели их не различить: один и тот же песчаный тайл
///     можно просеять на песок для стеклодува, опустить, роя канаву, или наоборот поднять,
///     засыпая её. Пока режима не было, выбор делался за игрока — и всегда оказывался не
///     тем, который нужен.
///
///     Направление раньше выводилось из журнала правок: посмотреть, чем тут занимались в
///     прошлый раз, и продолжить. Выглядело остроумно, а на деле означало, что лопата
///     решает за игрока и разворачивается сама — в том числе посреди работы, упёршись в
///     дно. Теперь направление спрашивают у игрока, а журнал остаётся тем, чем был:
///     историей правок, а не скрытым состоянием инструмента.
///
///     ---- Почему у игрока, а не у предмета ----
///
///     Поле в предмете означало бы смену версии сохранения и миграцию у каждой лопаты в
///     мире — ради одного бита. А главное, это ровно то, как в ультиме сделан выбор «руда
///     или руда с камнем» (PlayerMobile.ToggleMiningStone): выбор принадлежит игроку, а не
///     инструменту, и переключается из меню инструмента. Здесь то же самое, только хранится
///     отдельным сохранением, как и прочие наши настройки игрока — чтобы не трогать схему
///     PlayerMobile.
///
///     По умолчанию — земляные работы: песок требует навыка 100 и права SandMining, так что
///     для почти любого игрока режим добычи был бы просто отказом в ответ на каждый клик.
/// </summary>
public static class MahaonDigMode
{
    private static readonly Dictionary<Mobile, DigMode> Chosen = new();

    private static Persistence _persistence;

    public static void Configure()
    {
        _persistence = new Persistence();
    }

    public static DigMode Of(Mobile m) =>
        m != null && Chosen.TryGetValue(m, out var mode) ? mode : DigMode.Dig;

    public static void Set(Mobile m, DigMode mode)
    {
        if (m == null)
        {
            return;
        }

        // Значение по умолчанию не храним: сохранение остаётся размером с число тех, кто
        // режим вообще менял.
        if (mode == DigMode.Dig)
        {
            Chosen.Remove(m);
        }
        else
        {
            Chosen[m] = mode;
        }

        RefreshTools(m);
    }

    /// <summary>Название режима для тултипа лопаты и сообщений.</summary>
    public static string Name(DigMode mode) =>
        mode switch
        {
            DigMode.Harvest => "добыча песка",
            DigMode.Fill    => "закапывать",
            _               => "копать"
        };

    /// <summary>
    ///     Тултип лопаты показывает режим того, у кого она в руках или в сумке
    ///     (BaseHarvestTool.GetProperties). Режим хранится у игрока, а не в предмете, поэтому
    ///     сам предмет о смене не узнает — его тултип обновляется здесь, у всех лопат игрока
    ///     разом: в сумке (со вложенными сумками) и в руках.
    /// </summary>
    private static void RefreshTools(Mobile m)
    {
        if (m.Backpack != null)
        {
            foreach (var tool in m.Backpack.FindItemsByType<Items.BaseHarvestTool>())
            {
                if (tool.CanExcavate)
                {
                    tool.InvalidateProperties();
                }
            }
        }

        foreach (var item in m.Items)
        {
            if (item is Items.BaseHarvestTool { CanExcavate: true } tool)
            {
                tool.InvalidateProperties();
            }
        }
    }

    private sealed class Persistence : GenericPersistence
    {
        public Persistence() : base("MahaonDigMode", 1)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version
            writer.WriteEncodedInt(Chosen.Count);

            foreach (var (mobile, mode) in Chosen)
            {
                writer.Write(mobile);
                writer.Write((byte)mode);
            }
        }

        public override void Deserialize(IGenericReader reader)
        {
            reader.ReadEncodedInt(); // version

            var count = reader.ReadEncodedInt();

            for (var i = 0; i < count; i++)
            {
                var mobile = reader.ReadEntity<Mobile>();
                var mode = (DigMode)reader.ReadByte();

                if (mobile != null)
                {
                    Chosen[mobile] = mode;
                }
            }
        }
    }
}

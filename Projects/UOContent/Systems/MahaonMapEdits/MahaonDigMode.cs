using System.Collections.Generic;

namespace Server.Systems.MahaonMapEdits;

public enum DigMode
{
    /// <summary>Земляные работы: яма, ров, пруд. Значение по умолчанию — то есть default(DigMode).</summary>
    Terraform,

    /// <summary>Добыча: песок. Глубина при этом не меняется.</summary>
    Harvest
}

/// <summary>
///     Чем сейчас занят игрок с лопатой в руках: копает землю или добывает песок.
///
///     ---- Зачем режим ----
///
///     Лопатой делают две разные вещи, и по цели их не различить: один и тот же песчаный
///     тайл можно либо просеять на песок для стеклодува, либо опустить, роя канаву. Пока
///     режима не было, выбор делался за игрока — и всегда оказывался не тем, который нужен.
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
        m != null && Chosen.TryGetValue(m, out var mode) ? mode : DigMode.Terraform;

    public static void Set(Mobile m, DigMode mode)
    {
        if (m == null)
        {
            return;
        }

        // Значение по умолчанию не храним: сохранение остаётся размером с число тех, кто
        // режим вообще менял.
        if (mode == DigMode.Terraform)
        {
            Chosen.Remove(m);
            return;
        }

        Chosen[m] = mode;
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

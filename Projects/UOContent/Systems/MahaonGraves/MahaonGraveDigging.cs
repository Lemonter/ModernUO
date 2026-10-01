using System;
using Server.Engines.Harvest;
using Server.Items;

namespace Server.Systems.MahaonGraves;

/// <summary>
///     Копка могил лопатой.
///
///     Устроена как обычная система добычи движка — та же, что стоит за горным делом,
///     рыбалкой и лесозаготовкой. Это не прихоть: у HarvestDefinition уже есть ровно то,
///     чего не хватало самодельным версиям этой затеи на форумах ServUO, где автор сам
///     жаловался, что у него «нет предела на могилу и копать одну можно бесконечно». Тут
///     предел встроен: могила отдаёт MinTotal..MaxTotal и истощается, а HarvestBank сам
///     восстанавливает её через MinRespawn..MaxRespawn.
///
///     Разметку тайлов не пришлось выдумывать — она читается из самого клиента. В
///     tiledata.mul сорок девять статиков названы «grave» или «gravestone», тремя
///     сплошными кусками, и это ровно то, что стоит на всех кладбищах Британии.
/// </summary>
public class GraveDigging : HarvestSystem
{
    private static GraveDigging _system;

    public static GraveDigging System => _system ??= new GraveDigging();

    public HarvestDefinition Graves { get; }

    /// <summary>
    ///     Графика могил и надгробий — прямо из tiledata.mul клиента, а не по памяти.
    ///     Три сплошных куска: земляные холмики и старые плиты (0x0ED3..0x0EE2), одиночный
    ///     0x0EE8 и большой набор надгробий (0x1165..0x1184).
    /// </summary>
    public static readonly int[] GraveTiles = BuildGraveTiles();

    private static int[] BuildGraveTiles()
    {
        var tiles = new int[16 + 1 + 32];
        var i = 0;

        for (var id = 0x0ED3; id <= 0x0EE2; id++)
        {
            tiles[i++] = id;
        }

        tiles[i++] = 0x0EE8;

        for (var id = 0x1165; id <= 0x1184; id++)
        {
            tiles[i++] = id;
        }

        return tiles;
    }

    /// <summary>Шанс наткнуться на ящик Пандоры за одну удачную выкопку.</summary>
    private const double PandoraChance = 0.012;

    /// <summary>Шанс поднять из могилы её обитателя вместо добычи.</summary>
    private const double RestlessChance = 0.08;

    private GraveDigging()
    {
        Graves = new HarvestDefinition
        {
            // Могила — это одна могила, а не восьмиклеточная жила: копать соседнюю
            // придётся отдельно.
            BankWidth = 1,
            BankHeight = 1,

            MinTotal = 3,
            MaxTotal = 8,

            // Оно же «поля растут», только про могилы: истощённая могила приходит в себя
            // сама, без всяких спаунеров.
            MinRespawn = TimeSpan.FromMinutes(20.0),
            MaxRespawn = TimeSpan.FromMinutes(45.0),

            Skill = SkillName.Mining,
            LandTiles = Array.Empty<int>(),
            StaticTiles = GraveTiles,
            RangedTiles = false,
            MaxRange = 2,
            ConsumedPerHarvest = 1,
            ConsumedPerFeluccaHarvest = 1,

            EffectActions = new[] { 11 },
            EffectSounds = new[] { 0x125, 0x126 },
            EffectCounts = new[] { 1 },
            EffectDelay = TimeSpan.FromSeconds(1.6),
            EffectSoundDelay = TimeSpan.FromSeconds(0.9),

            NoResourcesMessage = "Эта могила уже разрыта — здесь ничего не осталось.",
            DoubleHarvestMessage = "Кто-то опередил тебя.",
            TimedOutOfRangeMessage = "Ты отошёл слишком далеко от могилы.",
            OutOfRangeMessage = 500446, // That is too far away.
            FailMessage = "Ты ворошишь землю, но не находишь ничего стоящего.",
            PackFullMessage = "В рюкзаке нет места, и находка остаётся в яме.",
            ToolBrokeMessage = 1044038 // You have worn out your tool!
        };

        HarvestResource[] res =
        {
            new(00.0, 00.0, 100.0, "кости", typeof(Bone)),
            new(45.0, 20.0, 100.0, "могильная пыль", typeof(GraveDust))
        };

        Graves.Resources = res;

        Graves.Veins = new[]
        {
            new HarvestVein(700, 0.0, res[0], null),
            new HarvestVein(300, 0.0, res[1], res[0])
        };

        Definitions = new[] { Graves };
    }

    /// <summary>
    ///     Тревожить мёртвых небезопасно, а изредка — прибыльно.
    ///
    ///     Проверка стоит здесь, а не в таблице добычи, потому что ящик Пандоры не ресурс:
    ///     он не должен ни складываться в стопку, ни зависеть от навыка, ни попадать в
    ///     жилу. Это отдельное событие поверх удачной выкопки.
    /// </summary>
    public override void OnHarvestFinished(
        Mobile from, Item tool, HarvestDefinition def, HarvestVein vein, HarvestBank bank, HarvestResource resource,
        object harvested
    )
    {
        base.OnHarvestFinished(from, tool, def, vein, bank, resource, harvested);

        if (from?.Map == null || from.Map == Map.Internal)
        {
            return;
        }

        if (Utility.RandomDouble() < PandoraChance)
        {
            var box = new MahaonPandorasBox(
                Utility.RandomList(
                    MahaonPandorasBoxColor.Red,
                    MahaonPandorasBoxColor.Green,
                    MahaonPandorasBoxColor.Blue
                )
            );

            if (from.Backpack?.TryDropItem(from, box, false) != true)
            {
                box.MoveToWorld(from.Location, from.Map);
            }

            from.SendMessage(0x59, "Лопата стукнула обо что-то твёрдое — в яме лежит запертый ящик.");
            from.PlaySound(0x1FA);
            from.FixedParticles(0x373A, 10, 15, 5018, EffectLayer.Waist);

            return;
        }

        if (Utility.RandomDouble() < RestlessChance)
        {
            MahaonGraveWakes.Wake(from);
        }
    }
}

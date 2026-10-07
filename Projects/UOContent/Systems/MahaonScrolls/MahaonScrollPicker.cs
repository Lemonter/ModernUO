using System.Collections.Generic;
using Server.Items;

namespace Server.Systems.MahaonScrolls;

/// <summary>
///     Выбор навыка для свитка силы — с оглядкой на того, кому свиток достаётся.
///
///     PowerScroll.CreateRandom берёт навык случайно из общего списка и о получателе не
///     знает вовсе. На чистой ультиме это терпимо: там у всех потолок 100, и любой свиток
///     что-то да даёт. У нас профессия сама поднимает потолок своих навыков до 120 — и
///     свиток на такой навык оказывается бумагой: он ничего не поднимает, потому что
///     поднимать уже некуда. Чем дальше игрок в своей профессии, тем чаще ему выпадала
///     именно эта бумага.
///
///     Здесь навык выбирается только среди тех, у кого потолок НИЖЕ значения свитка, и
///     среди них — сначала упёршиеся в потолок, затем те, которыми получатель занимается.
///     Если поднимать нечего совсем (всё выкачано до предела), отдаём случайный, как
///     раньше: лучше бесполезный свиток, чем отсутствие награды.
/// </summary>
public static class MahaonScrollPicker
{
    // В пределах стольких очков от потолка навык считается упёршимся в него.
    private const double CappedMargin = 5.0;

    // Навык, которым получатель занимается всерьёз.
    private const double TrainedSkill = 30.0;

    /// <summary>
    ///     Свиток силы, полезный именно этому игроку.
    /// </summary>
    /// <param name="m">Кому достанется свиток; null — обычный случайный выбор.</param>
    /// <param name="value">Значение свитка: 105, 110, 115 или 120.</param>
    /// <param name="noCraft">Исключить кузнечное и портняжное — так делают награды
    /// чемпионов, чтобы ремесленные свитки шли только с заказов.</param>
    public static PowerScroll CreatePowerScrollFor(Mobile m, int value, bool noCraft = false)
    {
        // Из тех, что свиток поднимет, сначала навыки, упёршиеся в свой потолок, затем те,
        // которыми получатель вообще занимается, и лишь потом любые: свиток на навык с
        // нулём очков — та же бумага, что и на выкачанный.
        var capped = new List<SkillName>();
        var trained = new List<SkillName>();
        var any = new List<SkillName>();

        if (m?.Skills != null)
        {
            foreach (var skill in PowerScroll.Skills)
            {
                if (noCraft && skill is SkillName.Blacksmith or SkillName.Tailoring)
                {
                    continue;
                }

                var owned = m.Skills[skill];

                if (owned == null || owned.Cap >= value)
                {
                    continue;
                }

                any.Add(skill);

                if (owned.Value >= owned.Cap - CappedMargin)
                {
                    capped.Add(skill);
                }
                else if (owned.Value >= TrainedSkill)
                {
                    trained.Add(skill);
                }
            }
        }

        var candidates = capped.Count > 0 ? capped : trained.Count > 0 ? trained : any;

        if (candidates.Count > 0)
        {
            return new PowerScroll(candidates[Utility.Random(candidates.Count)], value);
        }

        // Поднимать больше нечего — ведём себя как раньше.
        var increment = value - 100;

        return noCraft
            ? PowerScroll.CreateRandomNoCraft(increment, increment)
            : PowerScroll.CreateRandom(increment, increment);
    }
}

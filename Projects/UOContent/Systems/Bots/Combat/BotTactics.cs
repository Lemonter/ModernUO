using System;
using Server.Mobiles;

namespace Server.Systems.Bots;

/// <summary>
///     Расстановка боя одним взглядом: сколько против сколько, стоят ли враги кучей, тесно
///     ли вокруг.
///
///     Зачем это нужно отдельно. Бой у ботов был устроен как разговор один на один: взял
///     противника, подошёл, бьёт. Сколько врагов рядом и сколько своих — не спрашивалось
///     нигде, поэтому один против пяти бот вёл себя точно так же, как один на один, и так же
///     умирал; а впятером против двоих все пятеро били по разным целям, и ни одна не падала.
///     Для живого игрока это первое, на что он смотрит, ещё не выбрав заклинание.
///
///     Всё считается одним проходом по округе: расстановка нужна каждый тик боя, и три
///     отдельных перебора мобов на это тратить нельзя.
/// </summary>
public readonly struct BotSituation
{
    /// <summary>Враги, которых бот видит рядом (включая того, с кем бьётся).</summary>
    public readonly int Foes;

    /// <summary>Свои рядом, не считая самого бота: сгильдейцы, отряд, дружественные боты.</summary>
    public readonly int Allies;

    /// <summary>Сколько врагов стоит в одной куче вокруг самого плотного из них.</summary>
    public readonly int Clustered;

    /// <summary>Середина этой кучи — куда бить площадью.</summary>
    public readonly Mobile ClusterCenter;

    /// <summary>Тесно ли вокруг бота: коридор, проход, комната.</summary>
    public readonly bool Tight;

    public BotSituation(int foes, int allies, int clustered, Mobile clusterCenter, bool tight)
    {
        Foes = foes;
        Allies = allies;
        Clustered = clustered;
        ClusterCenter = clusterCenter;
        Tight = tight;
    }

    /// <summary>
    ///     Перевес: больше единицы — наша сторона сильнее по числу, меньше — слабее.
    ///     Сам бот считается за единицу, поэтому один против одного даёт ровно 1.
    /// </summary>
    public double Odds => Foes <= 0 ? double.MaxValue : (1.0 + Allies) / Foes;

    public bool Outnumbered => Odds < 0.6;

    public bool Outnumbering => Odds > 1.6;
}

public static class BotTactics
{
    /// <summary>Докуда бот считает участников драки.</summary>
    private const int Sight = 12;

    /// <summary>Насколько плотно должны стоять враги, чтобы их накрыло одним заклинанием.</summary>
    private const int ClusterRadius = 3;

    /// <summary>Сколько врагов в куче оправдывают массовое заклинание.</summary>
    public const int MassCastThreshold = 3;

    public static BotSituation Assess(BotBrain brain, Mobile foe)
    {
        var bot = brain.Bot;

        if (bot?.Map == null)
        {
            return new BotSituation(foe == null ? 0 : 1, 0, 0, foe, false);
        }

        // Враги собираются в список: по нему потом ищется куча, а второй проход по карте
        // обошёлся бы дороже небольшого массива.
        Span<Mobile> foes = new Mobile[16];
        var foeCount = 0;
        var allies = 0;

        foreach (var m in bot.Map.GetMobilesInRange<Mobile>(bot.Location, Sight))
        {
            if (m == bot || m.Deleted || !m.Alive || m.Hidden || m.IsDeadBondedPet)
            {
                continue;
            }

            if (IsAlly(bot, m))
            {
                allies++;
                continue;
            }

            // Враг — не всякий, кто не свой: мирный житель, лавочник и прохожий в драке не
            // участвуют. Признак тот же, которым пользуется сам бой: можно ли по нему
            // ударить и дерётся ли он с нами или с нашими.
            if (!bot.CanBeHarmful(m, false) || !IsHostile(bot, m, foe))
            {
                continue;
            }

            if (foeCount < foes.Length)
            {
                foes[foeCount] = m;
            }

            foeCount++;
        }

        var inList = Math.Min(foeCount, foes.Length);
        var clustered = FindCluster(foes[..inList], foe, out var center);

        return new BotSituation(Math.Max(foeCount, foe == null ? 0 : 1), allies, clustered, center ?? foe, IsTight(bot));
    }

    /// <summary>
    ///     Самая плотная кучка врагов и её середина.
    ///
    ///     Перебор парами, а не честная кластеризация: врагов в поле зрения единицы, а
    ///     кластеризация тут дала бы ту же середину за куда большие деньги.
    /// </summary>
    private static int FindCluster(ReadOnlySpan<Mobile> foes, Mobile fallback, out Mobile center)
    {
        center = fallback;

        if (foes.Length == 0)
        {
            return 0;
        }

        var best = 0;

        for (var i = 0; i < foes.Length; i++)
        {
            var around = 1;

            for (var j = 0; j < foes.Length; j++)
            {
                if (i != j && foes[i].InRange(foes[j], ClusterRadius))
                {
                    around++;
                }
            }

            if (around > best)
            {
                best = around;
                center = foes[i];
            }
        }

        return best;
    }

    private static bool IsAlly(Mobile bot, Mobile m) =>
        m is PlayerMobile && BotSocialRules.IsFriend(bot, m) ||
        m is BaseCreature { Controlled: true } pet && pet.ControlMaster is { } master &&
        (master == bot || BotSocialRules.IsFriend(bot, master));

    /// <summary>
    ///     Участвует ли этот в НАШЕЙ драке. Иначе бот считал бы врагами всех окрестных
    ///     монстров, включая тех, кто дрался в другом конце поля с кем-то третьим, и
    ///     шарахался бы от расстановки, которой нет.
    /// </summary>
    private static bool IsHostile(Mobile bot, Mobile m, Mobile foe)
    {
        if (m == foe)
        {
            return true;
        }

        if (m.Combatant == bot)
        {
            return true;
        }

        return m.Combatant is { } other && IsAlly(bot, other);
    }

    /// <summary>
    ///     Тесно ли вокруг. Считается по непроходимым соседям в кольце: в коридоре их
    ///     большинство, в поле нет вовсе.
    ///
    ///     Нужно для площадных заклинаний: стена огня в проходе запирает всех, а в чистом
    ///     поле её просто обойдут, и мана уйдёт впустую.
    /// </summary>
    private static bool IsTight(Mobile bot)
    {
        var map = bot.Map;
        var blocked = 0;

        for (var dx = -2; dx <= 2; dx++)
        {
            for (var dy = -2; dy <= 2; dy++)
            {
                if (dx == 0 && dy == 0)
                {
                    continue;
                }

                var land = map.Tiles.GetLandTile(bot.X + dx, bot.Y + dy);

                if ((TileData.LandTable[land.ID & TileData.MaxLandValue].Flags & TileFlag.Impassable) != 0)
                {
                    blocked++;
                }
            }
        }

        // Больше трети кольца — уже не открытое место.
        return blocked * 3 > 24;
    }
}

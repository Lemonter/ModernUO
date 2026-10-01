using System;
using System.Collections.Generic;
using ModernUO.CodeGeneratedEvents;
using Server.Items;
using Server.Mobiles;

namespace Server.Systems.MahaonSlayer;

/// <summary>
///     Звания охотника: «убийца орков», «гроза драконов» и прочие, зарабатываемые убийствами
///     и дающие прибавку к урону против той же породы.
///
///     Породы не выдуманы — берём готовую таксономию ультимы (SlayerGroup/SlayerEntry): те
///     же двадцать семь видов, по которым уже работают слееровые клинки. Заводить свой
///     список «кто есть орк» значило бы развести два разных ответа на один вопрос, и второй
///     бы отстал при первом же новом монстре.
///
///     Считаем не последнее попадание, а всех, у кого есть права на добычу: иначе в группе
///     звание доставалось бы тому, кто добил, а остальные били впустую.
///
///     Прибавка намеренно скромная (5% за ступень, максимум 15%): это признание заслуг, а не
///     замена слееровому оружию, у которого бонус +100%.
/// </summary>
public sealed class MahaonSlayerTitles : GenericPersistence
{
    private static MahaonSlayerTitles _instance;

    public MahaonSlayerTitles() : base("MahaonSlayerTitles", 1)
    {
    }

    /// <summary>Сколько надо убить на каждую ступень.</summary>
    public static readonly int[] Thresholds = { 100, 500, 2000 };

    /// <summary>Прибавка к урону за ступень, в процентах.</summary>
    public const int BonusPerTier = 5;

    private static readonly string[] TierNames = { "убийца", "истребитель", "гроза" };

    private static readonly Dictionary<Mobile, Dictionary<SlayerName, int>> Kills = new();

    public static void Configure() => _instance = new MahaonSlayerTitles();

    /// <summary>
    ///     Ступень (0 — нет звания, 1..3). Отдельно от названия, потому что по ней же
    ///     считается прибавка к урону.
    /// </summary>
    public static int TierFor(int kills)
    {
        var tier = 0;

        for (var i = 0; i < Thresholds.Length; i++)
        {
            if (kills >= Thresholds[i])
            {
                tier = i + 1;
            }
        }

        return tier;
    }

    public static int KillsOf(Mobile m, SlayerName name) =>
        Kills.TryGetValue(m, out var byName) && byName.TryGetValue(name, out var n) ? n : 0;

    /// <summary>Название звания по-русски, или null, если ещё не заслужено.</summary>
    public static string TitleFor(Mobile m, SlayerName name)
    {
        var tier = TierFor(KillsOf(m, name));

        return tier == 0 ? null : $"{TierNames[tier - 1]} {SlayerRu.GenitiveFor(name)}";
    }

    /// <summary>
    ///     Прибавка к урону этого бойца против этой цели, в процентах.
    ///
    ///     Званий может подойти сразу несколько — орк попадает и под «убийцу орков», и под
    ///     «убийцу человекоподобных» (надгруппа Repond). Берём лучшее, а не сумму: иначе
    ///     надгруппы складывались бы с породами и прибавка тихо удваивалась.
    /// </summary>
    public static int DamageBonus(Mobile attacker, Mobile defender)
    {
        if (attacker is not PlayerMobile || defender == null || !Kills.TryGetValue(attacker, out var byName))
        {
            return 0;
        }

        // Идём от цели, а не от списка званий: пород у существа обычно две, а званий у
        // бойца со временем накапливается два десятка. Плюс ответ по типу запомнен, так что
        // на замах приходится пара обращений к словарю вместо сотен IsAssignableFrom.
        var best = 0;

        foreach (var name in SlayerRu.CachedEntriesFor(defender.GetType()))
        {
            if (!byName.TryGetValue(name, out var kills))
            {
                continue;
            }

            var bonus = TierFor(kills) * BonusPerTier;

            if (bonus > best)
            {
                best = bonus;
            }
        }

        return best;
    }

    /// <summary>Следующий порог после такого числа убийств, или 0, если ступени кончились.</summary>
    public static int NextThreshold(int kills)
    {
        for (var i = 0; i < Thresholds.Length; i++)
        {
            if (kills < Thresholds[i])
            {
                return Thresholds[i];
            }
        }

        return 0;
    }

    /// <summary>
    ///     Заслуженные звания — для свитка в пеперделе. У незавершённых показываем, сколько
    ///     осталось до следующей ступени: иначе непонятно, докуда вообще растёт.
    /// </summary>
    public static List<string> TitlesOf(Mobile m)
    {
        var titles = new List<string>();

        if (m == null || !Kills.TryGetValue(m, out var byName))
        {
            return titles;
        }

        // По числу убийств, а не по алфавиту: сверху то, чем человек занимался всерьёз.
        // Подвал свитка скроллится вместе с телом, так что длинный список гамп не ломает —
        // обрезать незачем, достаточно разумного порядка.
        var earned = new List<(SlayerName Name, int Kills)>();

        foreach (var (name, kills) in byName)
        {
            if (TierFor(kills) > 0)
            {
                earned.Add((name, kills));
            }
        }

        earned.Sort((a, b) => b.Kills.CompareTo(a.Kills));

        foreach (var (name, kills) in earned)
        {
            var next = NextThreshold(kills);

            titles.Add(
                next == 0
                    ? $"{TitleFor(m, name)} ({kills})"
                    : $"{TitleFor(m, name)} ({kills} / {next})"
            );
        }

        return titles;
    }

    /// <summary>
    ///     Породы, которых бьют, но звания ещё не выслужили.
    ///
    ///     Без этого охота невидима: до сотни убийств игрок вообще не знает, что звания
    ///     существуют, и первое сообщение о них приходит как гром среди ясного неба. Показ
    ///     ограничен самыми добытыми — иначе после недели игры сюда попадут все двадцать
    ///     семь пород по паре убийств и утопят полезное.
    /// </summary>
    public static List<string> ProgressOf(Mobile m, int limit = 3)
    {
        var lines = new List<string>();

        if (m == null || !Kills.TryGetValue(m, out var byName))
        {
            return lines;
        }

        var pending = new List<(SlayerName Name, int Kills)>();

        foreach (var (name, kills) in byName)
        {
            if (TierFor(kills) == 0)
            {
                pending.Add((name, kills));
            }
        }

        pending.Sort((a, b) => b.Kills.CompareTo(a.Kills));

        for (var i = 0; i < pending.Count && i < limit; i++)
        {
            var (name, kills) = pending[i];
            lines.Add($"{SlayerRu.GenitiveFor(name)}: {kills} / {NextThreshold(kills)}");
        }

        return lines;
    }

    [OnEvent(nameof(BaseCreature.CreatureDeathEvent))]
    public static void OnCreatureDeath(BaseCreature bc)
    {
        // Призванные и подневольные не в счёт: иначе «убийцу демонов» фармили бы, вызывая
        // и убивая собственных демонов.
        if (bc == null || bc.Summoned || bc.Controlled || bc.NoKillAwards)
        {
            return;
        }

        var matched = SlayerRu.EntriesFor(bc);

        if (matched.Count == 0)
        {
            return;
        }

        var rights = BaseCreature.GetLootingRights(bc.DamageEntries, bc.HitsMax);

        for (var i = 0; i < rights.Count; i++)
        {
            var ds = rights[i];

            if (!ds.m_HasRight || ds.m_Mobile is not PlayerMobile { Deleted: false } player)
            {
                continue;
            }

            foreach (var name in matched)
            {
                Credit(player, name);
            }
        }
    }

    private static void Credit(PlayerMobile player, SlayerName name)
    {
        if (!Kills.TryGetValue(player, out var byName))
        {
            Kills[player] = byName = new Dictionary<SlayerName, int>();
        }

        byName.TryGetValue(name, out var before);
        var after = before + 1;
        byName[name] = after;

        var tierBefore = TierFor(before);
        var tierAfter = TierFor(after);

        if (tierAfter > tierBefore)
        {
            player.SendMessage(
                0x59,
                $"Ты заслужил звание «{TitleFor(player, name)}». " +
                $"Урон по ним теперь выше на {tierAfter * BonusPerTier}%."
            );
        }
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version

        var live = 0;

        foreach (var (player, byName) in Kills)
        {
            if (player?.Deleted == false && byName.Count > 0)
            {
                live++;
            }
        }

        writer.WriteEncodedInt(live);

        foreach (var (player, byName) in Kills)
        {
            if (player?.Deleted != false || byName.Count == 0)
            {
                continue;
            }

            writer.Write(player);
            writer.WriteEncodedInt(byName.Count);

            foreach (var (name, kills) in byName)
            {
                writer.WriteEncodedInt((int)name);
                writer.WriteEncodedInt(kills);
            }
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // version

        var count = reader.ReadEncodedInt();

        for (var i = 0; i < count; i++)
        {
            var player = reader.ReadEntity<Mobile>();
            var entries = reader.ReadEncodedInt();
            var byName = new Dictionary<SlayerName, int>();

            for (var j = 0; j < entries; j++)
            {
                var name = (SlayerName)reader.ReadEncodedInt();
                var kills = reader.ReadEncodedInt();

                // Порода могла исчезнуть из SlayerName между версиями — счётчик по ней
                // роняем молча, но остальные звания игрока не теряем.
                if (Enum.IsDefined(name))
                {
                    byName[name] = kills;
                }
            }

            if (player?.Deleted == false && byName.Count > 0)
            {
                Kills[player] = byName;
            }
        }
    }
}

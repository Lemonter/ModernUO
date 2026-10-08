using System;
using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Systems.MahaonProfessions;

public class ProfessionSystem : GenericPersistence
{
    private static ProfessionSystem _instance;

    private const double BaseSkillCap = 100.0;

    /// <summary>С чего начинает характеристики новый персонаж.</summary>
    private const int StartingStat = 25;
    private const double PrimarySkillCap = 120.0;

    private static readonly Dictionary<Mobile, MahaonProfession> Chosen = new();

    /// <summary>
    ///     Третий путь — категория, которую игрок добирает сам поверх двух, заданных
    ///     профессией. Живёт отдельным словарём, а не полем ProfessionInfo, именно потому,
    ///     что он у каждого свой: два Паладина с разным третьим путём — разные персонажи,
    ///     чего с жёсткой таблицей не выйдет.
    /// </summary>
    private static readonly Dictionary<Mobile, ProfessionCategory> Third = new();

    public ProfessionSystem() : base("MahaonProfessions", 1)
    {
    }

    public static void Configure()
    {
        _instance = new ProfessionSystem();
    }

    public static MahaonProfession? GetProfession(Mobile m) =>
        Chosen.TryGetValue(m, out var profession) ? profession : null;

    /// <summary>Третий путь игрока, если он его уже выбрал.</summary>
    public static ProfessionCategory? GetThirdCategory(Mobile m) =>
        m != null && Third.TryGetValue(m, out var category) ? category : null;

    /// <summary>
    ///     Можно ли взять эту категорию третьим путём: только когда профессия уже выбрана,
    ///     третий путь ещё не взят и категория не совпадает с двумя своими.
    /// </summary>
    public static bool CanTakeThird(Mobile m, ProfessionCategory category)
    {
        var profession = GetProfession(m);

        if (profession == null || GetThirdCategory(m) != null)
        {
            return false;
        }

        var info = ProfessionData.All[profession.Value];

        return info.Category != category && info.SecondaryCategory != category;
    }

    public static bool SetThirdCategory(Mobile m, ProfessionCategory category)
    {
        if (!CanTakeThird(m, category))
        {
            return false;
        }

        Third[m] = category;
        return true;
    }

    public static void SetProfession(Mobile m, MahaonProfession profession)
    {
        Chosen[m] = profession;

        var info = ProfessionData.All[profession];

        // Every skill starts at the flat 100 cap...
        foreach (var skillName in System.Enum.GetValues<SkillName>())
        {
            if ((int)skillName >= m.Skills.Length)
            {
                continue;
            }

            var skill = m.Skills[skillName];
            if (skill == null)
            {
                continue; // not available in this era/ruleset
            }

            skill.Cap = BaseSkillCap;
        }

        // ...except the primary category's full class kit, which can go to 120.
        if (ProfessionData.CategorySkills.TryGetValue(info.Category, out var primarySkills))
        {
            foreach (var skillName in primarySkills)
            {
                if ((int)skillName >= m.Skills.Length)
                {
                    continue;
                }

                var skill = m.Skills[skillName];
                if (skill != null)
                {
                    skill.Cap = PrimarySkillCap;
                }
            }
        }

        // Начальный задел новичку, чтобы он не выходил в мир с десяткой в каждой
        // характеристике. Плоское число, а не доля потолка профессии: потолки теперь у
        // всех одинаковые (300) и ничего осмысленного не задают, а доля от них дала бы
        // задел в сто двадцать единиц на ровном месте.
        //
        // Уже наработанное не трогается: смена профессии не должна ничего отбирать.
        m.RawStr = Math.Max(m.RawStr, StartingStat);
        m.RawDex = Math.Max(m.RawDex, StartingStat);
        m.RawInt = Math.Max(m.RawInt, StartingStat);

        // StatCap is the vanilla "sum of all three" ceiling — set it generously above this
        // profession's own individual caps so it never becomes the binding constraint
        // instead of the profession's real per-stat numbers.
        m.StatCap = info.StrCap + info.DexCap + info.IntCap;

        // Stand-in for "icon above the name" — a real floating icon needs client-side
        // graphic work we can't do here, so the profession shows as the character's title
        // instead (visible on single-click / in the paperdoll), same idea, simpler to ship.
        var name = ProfessionData.GetName(profession, m.Female);
        if (m is PlayerMobile pm)
        {
            pm.DisplayChampionTitle = false; // avoid the title fighting with champion titles
        }

        m.Title = name;
    }

    /// <summary>Reset — per the profession stone's new 1000-gold reset option. Drops the
    /// profession identity, title, and the primary-category skill cap bonus (back to the
    /// flat 100 everyone else has), and widens StatCap back to the vanilla default so it
    /// isn't left stuck at whatever the old profession's tighter ceiling was. Doesn't
    /// reduce stats/skills already trained — a respec, not a punishment.</summary>
    public static void ClearProfession(Mobile m)
    {
        if (!Chosen.Remove(m))
        {
            return;
        }

        foreach (var skillName in System.Enum.GetValues<SkillName>())
        {
            if ((int)skillName >= m.Skills.Length)
            {
                continue;
            }

            var skill = m.Skills[skillName];
            if (skill != null)
            {
                skill.Cap = BaseSkillCap;
            }
        }

        // Третий путь выбран поверх этой профессии — со сбросом уходит и он, иначе новая
        // профессия унаследовала бы чужой выбор, а то и собственную же категорию третьим
        // путём.
        Third.Remove(m);

        m.StatCap = 225; // vanilla default
        m.Title = null;
    }

    public static bool TouchesCategory(Mobile m, ProfessionCategory category)
    {
        var profession = GetProfession(m);
        if (profession == null)
        {
            return false;
        }

        var info = ProfessionData.All[profession.Value];

        return info.Category == category || info.SecondaryCategory == category ||
               GetThirdCategory(m) == category;
    }

    /// <summary>1.0 if the category is this player's PRIMARY profession category, 0.5 if
    /// it's only their secondary, 0.0 if neither (or no profession chosen at all) — the
    /// shared "полный бонус от первой профы, половина от второй" rule used by both
    /// GuardSystem (Warrior -> HP) and the ranger bounty-quest track (Ranger -> Stamina).</summary>
    public static double GetCategoryBonusScale(Mobile m, ProfessionCategory category)
    {
        var profession = GetProfession(m);
        if (profession == null)
        {
            return 0.0;
        }

        var info = ProfessionData.All[profession.Value];

        if (info.Category == category)
        {
            return 1.0;
        }

        if (info.SecondaryCategory == category)
        {
            return 0.5;
        }

        return 0.0;
    }

    /// <summary>
    ///     Полный ли это набор категории — то есть первичная ли она у игрока.
    ///
    ///     Первичная категория даёт все три своих перка, вторичная — только сигнатурный
    ///     (см. HasSignature). Прежнее правило «вторичка = половина от той же силы» из
    ///     GetCategoryBonusScale осталось только там, где перк по своей природе
    ///     числовой и делится: ранги гвардии, ранжира и придворного мага.
    /// </summary>
    public static bool HasFullKit(Mobile m, ProfessionCategory category)
    {
        var profession = GetProfession(m);

        return profession != null && ProfessionData.All[profession.Value].Category == category;
    }

    /// <summary>
    ///     Достаётся ли игроку сигнатурный перк категории — да, если она первичная ИЛИ
    ///     вторичная, и в обоих случаях в полную силу. Именно это делает Паладина
    ///     (Воин+Вера) и Храмовника (Вера+Воин) разными персонажами: у одного три
    ///     воинских перка плюс сигнатурный веры, у другого наоборот.
    /// </summary>
    public static bool HasSignature(Mobile m, ProfessionCategory category) => TouchesCategory(m, category);

    /// <summary>The profession's own per-stat ceiling — separate from the aggregate
    /// StatCap, which is set wide enough that this is the real binding limit.</summary>
    public static int GetStatCap(Mobile m, StatType stat)
    {
        var profession = GetProfession(m);
        if (profession == null)
        {
            return 100;
        }

        var info = ProfessionData.All[profession.Value];
        return stat switch
        {
            StatType.Str => info.StrCap,
            StatType.Dex => info.DexCap,
            StatType.Int => info.IntCap,
            _ => 100
        };
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(1); // version
        writer.WriteEncodedInt(Chosen.Count);

        foreach (var (mobile, profession) in Chosen)
        {
            writer.Write(mobile);
            writer.WriteEncodedInt((int)profession);
        }

        // Версия 1 — третий путь.
        writer.WriteEncodedInt(Third.Count);

        foreach (var (mobile, category) in Third)
        {
            writer.Write(mobile);
            writer.WriteEncodedInt((int)category);
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        var version = reader.ReadEncodedInt();

        var count = reader.ReadEncodedInt();
        for (var i = 0; i < count; i++)
        {
            var mobile = reader.ReadEntity<Mobile>();
            var profession = (MahaonProfession)reader.ReadEncodedInt();

            if (mobile != null)
            {
                Chosen[mobile] = profession;
            }
        }

        if (version < 1)
        {
            return; // сохранение до третьего пути — у всех он просто не выбран
        }

        var thirdCount = reader.ReadEncodedInt();
        for (var i = 0; i < thirdCount; i++)
        {
            var mobile = reader.ReadEntity<Mobile>();
            var category = (ProfessionCategory)reader.ReadEncodedInt();

            if (mobile != null)
            {
                Third[mobile] = category;
            }
        }
    }
}

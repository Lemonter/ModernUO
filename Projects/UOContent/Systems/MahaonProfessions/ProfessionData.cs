using System.Collections.Generic;

namespace Server.Systems.MahaonProfessions;

public enum ProfessionCategory
{
    Magic,
    Craft,
    Warrior,
    Thief,
    Bard,
    Ranger
}

public enum MahaonProfession
{
    // Magic
    Wizard, BattleMage, Illusionist, Trickster, Witch,
    // Craft
    Grandmaster, Sutler, Artisan, Craftsman, Merchant,
    // Warrior
    Paladin, Warrior, Mercenary, Scout, Knight,
    // Thief
    Saboteur, Safecracker, Assassin, Cardsharp, Robber,
    // Bard
    Fakir, Musician, Troubadour, ConArtist, WanderingActor,
    // Ranger
    Gamekeeper, Hunter, Marksman, ForestBandit, Traveler
}

/// <summary>
///     Every profession is a hybrid of a primary category (which grants the full 5/4/3-skill
///     class kit at up to 120) and a secondary category (which grants that category's
///     signature passive ability, not its skills). Stat numbers are caps to grow toward, not
///     starting values — same idea as skills.
///
///     Reconstructed from the archived site's actual per-profession pages
///     (kingsofmahaon.narod.ru/prof1.htm through prof6.htm), not guessed from the category
///     table alone. Two professions (Трубадур, Егерь) had no surviving page text on the
///     source site itself — those stat/secondary values are the user's own best estimate,
///     not sourced. Everything else here is read directly off the real page text.
/// </summary>
public readonly struct ProfessionInfo
{
    public readonly ProfessionCategory Category;
    public readonly ProfessionCategory SecondaryCategory;
    public readonly string MaleName;
    public readonly string FemaleName;
    public readonly int StrCap;
    public readonly int DexCap;
    public readonly int IntCap;

    public ProfessionInfo(
        ProfessionCategory category, ProfessionCategory secondaryCategory, string maleName, string femaleName,
        int strCap, int dexCap, int intCap
    )
    {
        Category = category;
        SecondaryCategory = secondaryCategory;
        MaleName = maleName;
        FemaleName = femaleName;
        StrCap = strCap;
        DexCap = dexCap;
        IntCap = intCap;
    }
}

public static class ProfessionData
{
    /// <summary>The full class skill kit per category, each capped at 120 for anyone whose
    /// primary category this is. Everyone else caps at the standard 100.</summary>
    public static readonly Dictionary<ProfessionCategory, SkillName[]> CategorySkills = new()
    {
        [ProfessionCategory.Magic] = new[] { SkillName.Magery, SkillName.EvalInt, SkillName.Meditation },
        [ProfessionCategory.Craft] = new[]
        {
            SkillName.ArmsLore, SkillName.Blacksmith, SkillName.Carpentry, SkillName.Tailoring, SkillName.Tinkering
        },
        [ProfessionCategory.Warrior] = new[]
        {
            SkillName.Fencing, SkillName.Macing, SkillName.Swords, SkillName.Wrestling, SkillName.Tactics
        },
        [ProfessionCategory.Thief] = new[]
        {
            SkillName.Hiding, SkillName.Stealth, SkillName.Lockpicking, SkillName.Stealing
        },
        [ProfessionCategory.Bard] = new[]
        {
            SkillName.Provocation, SkillName.Peacemaking, SkillName.Musicianship, SkillName.Discordance
        },
        [ProfessionCategory.Ranger] = new[]
        {
            SkillName.AnimalTaming, SkillName.Archery, SkillName.Tracking, SkillName.Veterinary
        }
    };

    public static readonly Dictionary<MahaonProfession, ProfessionInfo> All = new()
    {
        // -- Magic (primary) --------------------------------------------------------------
        [MahaonProfession.Wizard] = new(ProfessionCategory.Magic, ProfessionCategory.Craft,
            "Волшебник", "Волшебница", 60, 100, 120),
        [MahaonProfession.BattleMage] = new(ProfessionCategory.Magic, ProfessionCategory.Warrior,
            "Боевой маг", "Чародейка", 80, 100, 120),
        [MahaonProfession.Illusionist] = new(ProfessionCategory.Magic, ProfessionCategory.Thief,
            "Иллюзионист", "Иллюзионистка", 60, 100, 140),
        [MahaonProfession.Trickster] = new(ProfessionCategory.Magic, ProfessionCategory.Bard,
            "Фокусник", "Фокусница", 60, 100, 140),
        [MahaonProfession.Witch] = new(ProfessionCategory.Magic, ProfessionCategory.Ranger,
            "Ведьмак", "Ведьма", 40, 100, 140),

        // -- Craft (primary) ---------------------------------------------------------------
        [MahaonProfession.Grandmaster] = new(ProfessionCategory.Craft, ProfessionCategory.Magic,
            "Великиймастер", "Рукодельница", 80, 100, 60),
        [MahaonProfession.Sutler] = new(ProfessionCategory.Craft, ProfessionCategory.Warrior,
            "Маркитант", "Маркитантка", 120, 100, 40),
        [MahaonProfession.Artisan] = new(ProfessionCategory.Craft, ProfessionCategory.Thief,
            "Мастеровой", "Мастерица", 100, 100, 60),
        [MahaonProfession.Craftsman] = new(ProfessionCategory.Craft, ProfessionCategory.Bard,
            "Искусник", "Искусница", 100, 100, 60),
        [MahaonProfession.Merchant] = new(ProfessionCategory.Craft, ProfessionCategory.Ranger,
            "Купец", "Купчиха", 80, 100, 60),

        // -- Warrior (primary) --------------------------------------------------------------
        [MahaonProfession.Paladin] = new(ProfessionCategory.Warrior, ProfessionCategory.Magic,
            "Паладин", "Воительница веры", 120, 100, 60),
        [MahaonProfession.Warrior] = new(ProfessionCategory.Warrior, ProfessionCategory.Craft,
            "Гридень", "Амазонка", 140, 100, 40),
        [MahaonProfession.Mercenary] = new(ProfessionCategory.Warrior, ProfessionCategory.Thief,
            "Наемник", "Наемница", 140, 100, 60),
        [MahaonProfession.Scout] = new(ProfessionCategory.Warrior, ProfessionCategory.Bard,
            "Разведчик", "Разведчица", 140, 100, 60),
        [MahaonProfession.Knight] = new(ProfessionCategory.Warrior, ProfessionCategory.Ranger,
            "Рыцарь", "Странствующая дева", 120, 100, 60),

        // -- Thief (primary) ----------------------------------------------------------------
        [MahaonProfession.Saboteur] = new(ProfessionCategory.Thief, ProfessionCategory.Magic,
            "Диверсант", "Лазутчица", 60, 140, 80),
        [MahaonProfession.Safecracker] = new(ProfessionCategory.Thief, ProfessionCategory.Craft,
            "Медвежатник", "Взломщица", 80, 140, 60),
        [MahaonProfession.Assassin] = new(ProfessionCategory.Thief, ProfessionCategory.Warrior,
            "Наемный убийца", "Убийца", 100, 140, 60),
        [MahaonProfession.Cardsharp] = new(ProfessionCategory.Thief, ProfessionCategory.Bard,
            "Шулер", "Картежница", 80, 140, 80),
        [MahaonProfession.Robber] = new(ProfessionCategory.Thief, ProfessionCategory.Ranger,
            "Грабитель", "Грабительница", 60, 140, 80),

        // -- Bard (primary) -----------------------------------------------------------------
        // Fakir shows no allowed spells and no other-category ability text on the source
        // page at all — the one profession in the whole list with no clear secondary, so it
        // stays that way here too rather than inventing one.
        [MahaonProfession.Fakir] = new(ProfessionCategory.Bard, ProfessionCategory.Bard,
            "Факир", "Байдера", 40, 140, 100),
        [MahaonProfession.Musician] = new(ProfessionCategory.Bard, ProfessionCategory.Craft,
            "Менестрель", "Певица", 60, 140, 80),
        // No surviving page text for Трубадур — stats and secondary are the user's own
        // estimate, not sourced like the rest of this table.
        [MahaonProfession.Troubadour] = new(ProfessionCategory.Bard, ProfessionCategory.Warrior,
            "Трубадур", "Танцовщица", 60, 140, 80),
        [MahaonProfession.ConArtist] = new(ProfessionCategory.Bard, ProfessionCategory.Thief,
            "Аферист", "Аферистка", 60, 140, 100),
        [MahaonProfession.WanderingActor] = new(ProfessionCategory.Bard, ProfessionCategory.Craft,
            "Бродячий актер", "Актриса", 40, 140, 100),

        // -- Ranger (primary) ---------------------------------------------------------------
        // No surviving page text for Егерь — stats are the user's own estimate; secondary
        // follows the same row-pattern (row 1 -> Magic) seen consistently across the other
        // four fully-documented categories.
        [MahaonProfession.Gamekeeper] = new(ProfessionCategory.Ranger, ProfessionCategory.Magic,
            "Егерь", "Смотрительница леса", 60, 140, 80),
        [MahaonProfession.Hunter] = new(ProfessionCategory.Ranger, ProfessionCategory.Craft,
            "Охотник", "Охотница", 80, 140, 60),
        // Стрелок is confirmed Ranger+Warrior directly by the user, overriding the source
        // page's own unclear "Magic Resistance to 200" text.
        [MahaonProfession.Marksman] = new(ProfessionCategory.Ranger, ProfessionCategory.Warrior,
            "Стрелок", "Лучница", 100, 140, 60),
        [MahaonProfession.ForestBandit] = new(ProfessionCategory.Ranger, ProfessionCategory.Thief,
            "Лесной разбойник", "Лесная разбойница", 80, 140, 80),
        [MahaonProfession.Traveler] = new(ProfessionCategory.Ranger, ProfessionCategory.Bard,
            "Путешественник", "Путешественница", 80, 140, 80)
    };

    public static string GetName(MahaonProfession profession, bool female) =>
        female ? All[profession].FemaleName : All[profession].MaleName;

    public static List<MahaonProfession> InCategory(ProfessionCategory category)
    {
        var result = new List<MahaonProfession>();

        foreach (var (profession, info) in All)
        {
            if (info.Category == category)
            {
                result.Add(profession);
            }
        }

        return result;
    }
}

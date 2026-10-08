using System.Collections.Generic;

namespace Server.Systems.MahaonProfessions;

public enum ProfessionCategory
{
    Magic,
    Craft,
    Warrior,
    Thief,
    Bard,
    Ranger,

    // Mahaon: добавлены строго в конец - значение enum лежит в сейве через
    // ProfessionData.All, вставка в середину переименовала бы всем профессию.
    // Категории под то, чего на исходном шарде не существовало: он застыл на UO конца
    // девяностых, где не было ни некромантии, ни рыцарства, ни мистицизма, ни плетения,
    // ни восточных школ.
    Necromancy,
    Faith,
    Mysticism,
    Weaving,
    Bushido,
    Ninjitsu
}

public enum MahaonProfession
{
    // Магия
    Wizard,
    BattleMage,
    Illusionist,
    Trickster,
    Witch,
    // Ремесло
    Grandmaster,
    Sutler,
    Artisan,
    Craftsman,
    Merchant,
    // Воин
    Paladin,
    Warrior,
    Mercenary,
    Scout,
    Knight,
    // Вор
    Saboteur,
    Safecracker,
    Assassin,
    Cardsharp,
    Robber,
    // Бард
    Fakir,
    Musician,
    Troubadour,
    ConArtist,
    WanderingActor,
    // Рейнджер
    Gamekeeper,
    Hunter,
    Marksman,
    ForestBandit,
    Traveler,

    // --- новые категории, дописаны в конец ради совместимости сейва ---
    // Некромантия
    Warlock,
    DeathKnight,
    Defiler,
    Mourner,
    Embalmer,
    // Вера
    Templar,
    Exorcist,
    Preacher,
    Inquisitor,
    PilgrimBrother,
    // Мистицизм
    Seer,
    Imbuer,
    ElementalFighter,
    ElementalCaller,
    Shaman,
    // Плетение
    Weaver,
    Druid,
    GroveSinger,
    GroveKeeper,
    DuskWeaver,
    // Бусидо
    Swordmaster,
    WarriorMonk,
    HorseArcher,
    Stargazer,
    HouseGuardian,
    // Ниндзюцу
    Infiltrator,
    MountainWanderer,
    ShadowBinder,
    Trapper,
    Puppeteer
}

/// <summary>
///     Профессия - это пара категорий. Первичная даёт полный набор навыков до 120 и все
///     три перка своей категории; вторичная - только сигнатурный перк своей, но в полную
///     силу. Поэтому Паладин (Воин+Магия) и Боевой маг (Магия+Воин) - разные персонажи,
///     а не зеркало.
///
///     Первые тридцать восстановлены с архива настоящего шарда
///     (kingsofmahaon.narod.ru/prof1.htm ... prof6.htm) и не менялись: имена, категории,
///     статкапы как есть. Единственная правка - вторички Факира и Бродячего актёра:
///     в архиве у первого её не было вовсе, у второго стояло Ремесло, дублируя
///     Менестреля. Свободными оставались Магия и Рейнджер, их и проставил.
///
///     Остальные тридцать придуманы с нуля под шесть новых категорий.
/// </summary>
public readonly struct ProfessionInfo
{
    public readonly ProfessionCategory Category;
    public readonly ProfessionCategory SecondaryCategory;
    public readonly string MaleName;
    public readonly string FemaleName;
    // Пределы характеристик по профессиям больше не различаются: у всех по сотне на
    // каждую, то есть СУММА 300 — именно она и работает.
    //
    // Различаться им было бессмысленно: отдельную характеристику игрока ограничивает
    // общешардовая настройка stats.statMax (SkillCheck.CanRaise, у нас 125), а профессия
    // задавала только сумму (Mobile.StatCap) — числа вроде «Сила 140» не значили ровным
    // счётом ничего, потому что до 125 рос кто угодно, а выше не рос никто.
    //
    // Поля оставлены: из них складывается StatCap и на них смотрит обучение ботов.
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
    /// <summary>
    ///     Полный набор навыков категории - до 120 у того, чья это первичная категория,
    ///     и до 100 у всех остальных.
    ///
    ///     Раньше шесть категорий покрывали 25 навыков из 58. Без профессии оставались
    ///     не только школы, появившиеся после 2000 года, но и Mining/Lumberjacking/Fishing
    ///     (при живой категории "Ремесло", которая из этого сырья всё и делает), Anatomy,
    ///     Healing, Parry, MagicResist, Alchemy, Inscribe и ещё десяток вещей возрастом
    ///     старше самого шарда. Теперь покрыты все 58.
    ///
    ///     Навык намеренно может входить в несколько категорий: Anatomy честно
    ///     принадлежит и Воину, и Вере, и Бусидо.
    /// </summary>
    public static readonly Dictionary<ProfessionCategory, SkillName[]> CategorySkills = new()
    {
        [ProfessionCategory.Magic] = new[]
        {
            SkillName.Magery, SkillName.EvalInt, SkillName.Meditation, SkillName.Inscribe, SkillName.ItemID
        },
        [ProfessionCategory.Craft] = new[]
        {
            SkillName.Blacksmith, SkillName.Carpentry, SkillName.Tailoring, SkillName.Tinkering,
            SkillName.ArmsLore, SkillName.Fletching, SkillName.Alchemy, SkillName.Cooking, SkillName.Mining,
            SkillName.Lumberjacking
        },
        [ProfessionCategory.Warrior] = new[]
        {
            SkillName.Swords, SkillName.Fencing, SkillName.Macing, SkillName.Wrestling, SkillName.Tactics,
            SkillName.Anatomy, SkillName.Parry, SkillName.Healing
        },
        [ProfessionCategory.Thief] = new[]
        {
            SkillName.Hiding, SkillName.Stealth, SkillName.Stealing, SkillName.Lockpicking, SkillName.Snooping,
            SkillName.RemoveTrap, SkillName.DetectHidden, SkillName.Poisoning
        },
        [ProfessionCategory.Bard] = new[]
        {
            SkillName.Provocation, SkillName.Peacemaking, SkillName.Musicianship, SkillName.Discordance,
            SkillName.Begging, SkillName.TasteID
        },
        [ProfessionCategory.Ranger] = new[]
        {
            SkillName.Archery, SkillName.Tracking, SkillName.AnimalTaming, SkillName.AnimalLore,
            SkillName.Veterinary, SkillName.Herding, SkillName.Camping, SkillName.Cartography, SkillName.Fishing
        },
        [ProfessionCategory.Necromancy] = new[]
        {
            SkillName.Necromancy, SkillName.SpiritSpeak, SkillName.Forensics, SkillName.MagicResist
        },
        [ProfessionCategory.Faith] = new[]
        {
            SkillName.Chivalry, SkillName.Focus, SkillName.Healing, SkillName.Anatomy
        },
        [ProfessionCategory.Mysticism] = new[]
        {
            SkillName.Mysticism, SkillName.Focus, SkillName.Imbuing, SkillName.MagicResist
        },
        [ProfessionCategory.Weaving] = new[] { SkillName.Spellweaving, SkillName.Meditation, SkillName.AnimalLore },
        [ProfessionCategory.Bushido] = new[]
        {
            SkillName.Bushido, SkillName.Parry, SkillName.Tactics, SkillName.Anatomy
        },
        [ProfessionCategory.Ninjitsu] = new[]
        {
            SkillName.Ninjitsu, SkillName.Throwing, SkillName.Hiding, SkillName.Stealth, SkillName.Poisoning
        }
    };

    public static readonly Dictionary<MahaonProfession, ProfessionInfo> All = new()
    {
        // -- Магия --
        [MahaonProfession.Wizard] = new(ProfessionCategory.Magic, ProfessionCategory.Craft,
            "Волшебник", "Волшебница", 100, 100, 100),
        [MahaonProfession.BattleMage] = new(ProfessionCategory.Magic, ProfessionCategory.Warrior,
            "Боевой маг", "Чародейка", 100, 100, 100),
        [MahaonProfession.Illusionist] = new(ProfessionCategory.Magic, ProfessionCategory.Thief,
            "Иллюзионист", "Иллюзионистка", 100, 100, 100),
        [MahaonProfession.Trickster] = new(ProfessionCategory.Magic, ProfessionCategory.Bard,
            "Фокусник", "Фокусница", 100, 100, 100),
        [MahaonProfession.Witch] = new(ProfessionCategory.Magic, ProfessionCategory.Ranger,
            "Ведьмак", "Ведьма", 100, 100, 100),

        // -- Ремесло --
        [MahaonProfession.Grandmaster] = new(ProfessionCategory.Craft, ProfessionCategory.Magic,
            "Великий мастер", "Рукодельница", 100, 100, 100),
        [MahaonProfession.Sutler] = new(ProfessionCategory.Craft, ProfessionCategory.Warrior,
            "Маркитант", "Маркитантка", 100, 100, 100),
        [MahaonProfession.Artisan] = new(ProfessionCategory.Craft, ProfessionCategory.Thief,
            "Мастеровой", "Мастерица", 100, 100, 100),
        [MahaonProfession.Craftsman] = new(ProfessionCategory.Craft, ProfessionCategory.Bard,
            "Искусник", "Искусница", 100, 100, 100),
        [MahaonProfession.Merchant] = new(ProfessionCategory.Craft, ProfessionCategory.Ranger,
            "Купец", "Купчиха", 100, 100, 100),

        // -- Воин --
        [MahaonProfession.Paladin] = new(ProfessionCategory.Warrior, ProfessionCategory.Magic,
            "Паладин", "Воительница веры", 100, 100, 100),
        [MahaonProfession.Warrior] = new(ProfessionCategory.Warrior, ProfessionCategory.Craft,
            "Гридень", "Амазонка", 100, 100, 100),
        [MahaonProfession.Mercenary] = new(ProfessionCategory.Warrior, ProfessionCategory.Thief,
            "Наемник", "Наемница", 100, 100, 100),
        [MahaonProfession.Scout] = new(ProfessionCategory.Warrior, ProfessionCategory.Bard,
            "Разведчик", "Разведчица", 100, 100, 100),
        [MahaonProfession.Knight] = new(ProfessionCategory.Warrior, ProfessionCategory.Ranger,
            "Рыцарь", "Странствующая дева", 100, 100, 100),

        // -- Вор --
        [MahaonProfession.Saboteur] = new(ProfessionCategory.Thief, ProfessionCategory.Magic,
            "Диверсант", "Лазутчица", 100, 100, 100),
        [MahaonProfession.Safecracker] = new(ProfessionCategory.Thief, ProfessionCategory.Craft,
            "Медвежатник", "Взломщица", 100, 100, 100),
        [MahaonProfession.Assassin] = new(ProfessionCategory.Thief, ProfessionCategory.Warrior,
            "Наемный убийца", "Убийца", 100, 100, 100),
        [MahaonProfession.Cardsharp] = new(ProfessionCategory.Thief, ProfessionCategory.Bard,
            "Шулер", "Картежница", 100, 100, 100),
        [MahaonProfession.Robber] = new(ProfessionCategory.Thief, ProfessionCategory.Ranger,
            "Грабитель", "Грабительница", 100, 100, 100),

        // -- Бард --
        [MahaonProfession.Fakir] = new(ProfessionCategory.Bard, ProfessionCategory.Magic,
            "Факир", "Байдера", 100, 100, 100),
        [MahaonProfession.Musician] = new(ProfessionCategory.Bard, ProfessionCategory.Craft,
            "Менестрель", "Певица", 100, 100, 100),
        [MahaonProfession.Troubadour] = new(ProfessionCategory.Bard, ProfessionCategory.Warrior,
            "Трубадур", "Танцовщица", 100, 100, 100),
        [MahaonProfession.ConArtist] = new(ProfessionCategory.Bard, ProfessionCategory.Thief,
            "Аферист", "Аферистка", 100, 100, 100),
        [MahaonProfession.WanderingActor] = new(ProfessionCategory.Bard, ProfessionCategory.Ranger,
            "Бродячий актер", "Актриса", 100, 100, 100),

        // -- Рейнджер --
        [MahaonProfession.Gamekeeper] = new(ProfessionCategory.Ranger, ProfessionCategory.Magic,
            "Егерь", "Смотрительница леса", 100, 100, 100),
        [MahaonProfession.Hunter] = new(ProfessionCategory.Ranger, ProfessionCategory.Craft,
            "Охотник", "Охотница", 100, 100, 100),
        [MahaonProfession.Marksman] = new(ProfessionCategory.Ranger, ProfessionCategory.Warrior,
            "Стрелок", "Лучница", 100, 100, 100),
        [MahaonProfession.ForestBandit] = new(ProfessionCategory.Ranger, ProfessionCategory.Thief,
            "Лесной разбойник", "Лесная разбойница", 100, 100, 100),
        [MahaonProfession.Traveler] = new(ProfessionCategory.Ranger, ProfessionCategory.Bard,
            "Путешественник", "Путешественница", 100, 100, 100),

        // -- Некромантия --
        [MahaonProfession.Warlock] = new(ProfessionCategory.Necromancy, ProfessionCategory.Magic,
            "Чернокнижник", "Чернокнижница", 100, 100, 100),
        [MahaonProfession.DeathKnight] = new(ProfessionCategory.Necromancy, ProfessionCategory.Warrior,
            "Рыцарь смерти", "Дева смерти", 100, 100, 100),
        [MahaonProfession.Defiler] = new(ProfessionCategory.Necromancy, ProfessionCategory.Thief,
            "Осквернитель", "Осквернительница", 100, 100, 100),
        [MahaonProfession.Mourner] = new(ProfessionCategory.Necromancy, ProfessionCategory.Bard,
            "Плакальщик", "Плакальщица", 100, 100, 100),
        [MahaonProfession.Embalmer] = new(ProfessionCategory.Necromancy, ProfessionCategory.Craft,
            "Бальзамировщик", "Бальзамировщица", 100, 100, 100),

        // -- Вера --
        [MahaonProfession.Templar] = new(ProfessionCategory.Faith, ProfessionCategory.Warrior,
            "Храмовник", "Храмовница", 100, 100, 100),
        [MahaonProfession.Exorcist] = new(ProfessionCategory.Faith, ProfessionCategory.Magic,
            "Экзорцист", "Экзорцистка", 100, 100, 100),
        [MahaonProfession.Preacher] = new(ProfessionCategory.Faith, ProfessionCategory.Bard,
            "Проповедник", "Проповедница", 100, 100, 100),
        [MahaonProfession.Inquisitor] = new(ProfessionCategory.Faith, ProfessionCategory.Thief,
            "Инквизитор", "Инквизиторша", 100, 100, 100),
        [MahaonProfession.PilgrimBrother] = new(ProfessionCategory.Faith, ProfessionCategory.Ranger,
            "Странствующий брат", "Странствующая сестра", 100, 100, 100),

        // -- Мистицизм --
        [MahaonProfession.Seer] = new(ProfessionCategory.Mysticism, ProfessionCategory.Magic,
            "Прорицатель", "Прорицательница", 100, 100, 100),
        [MahaonProfession.Imbuer] = new(ProfessionCategory.Mysticism, ProfessionCategory.Craft,
            "Наделяющий", "Наделяющая", 100, 100, 100),
        [MahaonProfession.ElementalFighter] = new(ProfessionCategory.Mysticism, ProfessionCategory.Warrior,
            "Ратник стихий", "Дева стихий", 100, 100, 100),
        [MahaonProfession.ElementalCaller] = new(ProfessionCategory.Mysticism, ProfessionCategory.Faith,
            "Заклинатель стихий", "Заклинательница стихий", 100, 100, 100),
        [MahaonProfession.Shaman] = new(ProfessionCategory.Mysticism, ProfessionCategory.Ranger,
            "Шаман", "Шаманка", 100, 100, 100),

        // -- Плетение --
        [MahaonProfession.Weaver] = new(ProfessionCategory.Weaving, ProfessionCategory.Magic,
            "Плетельщик", "Плетельщица", 100, 100, 100),
        [MahaonProfession.Druid] = new(ProfessionCategory.Weaving, ProfessionCategory.Ranger,
            "Друид", "Друидесса", 100, 100, 100),
        [MahaonProfession.GroveSinger] = new(ProfessionCategory.Weaving, ProfessionCategory.Bard,
            "Певец рощи", "Певица рощи", 100, 100, 100),
        [MahaonProfession.GroveKeeper] = new(ProfessionCategory.Weaving, ProfessionCategory.Faith,
            "Хранитель рощи", "Хранительница рощи", 100, 100, 100),
        [MahaonProfession.DuskWeaver] = new(ProfessionCategory.Weaving, ProfessionCategory.Thief,
            "Сумеречный плетельщик", "Сумеречная плетельщица", 100, 100, 100),

        // -- Бусидо --
        [MahaonProfession.Swordmaster] = new(ProfessionCategory.Bushido, ProfessionCategory.Warrior,
            "Мечник", "Мечница", 100, 100, 100),
        [MahaonProfession.WarriorMonk] = new(ProfessionCategory.Bushido, ProfessionCategory.Faith,
            "Воин-монах", "Воительница-монахиня", 100, 100, 100),
        [MahaonProfession.HorseArcher] = new(ProfessionCategory.Bushido, ProfessionCategory.Ranger,
            "Конный лучник", "Конная лучница", 100, 100, 100),
        [MahaonProfession.Stargazer] = new(ProfessionCategory.Bushido, ProfessionCategory.Magic,
            "Звездочёт", "Звездочётка", 100, 100, 100),
        [MahaonProfession.HouseGuardian] = new(ProfessionCategory.Bushido, ProfessionCategory.Craft,
            "Хранитель дома", "Хранительница дома", 100, 100, 100),

        // -- Ниндзюцу --
        [MahaonProfession.Infiltrator] = new(ProfessionCategory.Ninjitsu, ProfessionCategory.Thief,
            "Лазутчик", "Лазутчица", 100, 100, 100),
        [MahaonProfession.MountainWanderer] = new(ProfessionCategory.Ninjitsu, ProfessionCategory.Warrior,
            "Горный странник", "Горная странница", 100, 100, 100),
        [MahaonProfession.ShadowBinder] = new(ProfessionCategory.Ninjitsu, ProfessionCategory.Magic,
            "Заклинатель теней", "Заклинательница теней", 100, 100, 100),
        [MahaonProfession.Trapper] = new(ProfessionCategory.Ninjitsu, ProfessionCategory.Ranger,
            "Ловчий", "Ловчая", 100, 100, 100),
        [MahaonProfession.Puppeteer] = new(ProfessionCategory.Ninjitsu, ProfessionCategory.Bard,
            "Кукловод", "Кукловодша", 100, 100, 100)
    };

    public static string RuCategoryName(ProfessionCategory category) => category switch
    {
        ProfessionCategory.Magic       => "Магия",
        ProfessionCategory.Craft       => "Ремесло",
        ProfessionCategory.Warrior     => "Воин",
        ProfessionCategory.Thief       => "Вор",
        ProfessionCategory.Bard        => "Бард",
        ProfessionCategory.Ranger      => "Рейнджер",
        ProfessionCategory.Necromancy  => "Некромантия",
        ProfessionCategory.Faith       => "Вера",
        ProfessionCategory.Mysticism   => "Мистицизм",
        ProfessionCategory.Weaving     => "Плетение",
        ProfessionCategory.Bushido     => "Бусидо",
        ProfessionCategory.Ninjitsu    => "Ниндзюцу",
        _ => category.ToString()
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

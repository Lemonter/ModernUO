using System;
using System.Collections.Generic;
using Server.Commands;
using Server.Items;
using Server.Mobiles;

namespace Server.Systems.MahaonCombat;

/// <summary>
///     Мастерство владения оружием — как в Albion Online.
///
///     ---- Что качается ----
///
///     Не навык, а ТИП ОРУЖИЯ: бьёшься алебардой — растёт «алебарда». Навыков оружия в
///     ультиме пять, и алебарда в них — это мечи; мастерство по навыку значило бы, что катана
///     качает алебарду. Поэтому ключ — класс оружия, а артефакт идёт как своё базовое оружие:
///     Calm наследует Halberd и качает алебарду.
///
///     ---- Линейки ----
///
///     Семейство берётся из базового класса оружия — BaseSword, BaseAxe, BasePoleArm и так
///     далее, они уже есть в коде и ровно по смыслу. Мастерство одного оружия на четверть
///     течёт в соседей по линейке: мастер длинного меча берёт катану не с нуля.
///
///     ---- Что даёт ----
///
///     Мелко и ровно, каждое очко, без ступеней: шанс попасть, шанс крита и совсем немного
///     урона. Отдаётся вкладом в профиль удара (StrikeProfile), то есть видно в чеке и в
///     [StrikeInfo с подписью «Мастерство: алебарда».
///
///     ---- Как растёт ----
///
///     От попаданий по живой цели — та же кривая, что у всех веток (0.3 / 0.15 / 0.05 по полосам
///     0–50 / 50–80 / 80–100), чтобы прокачка ощущалась одинаково. Игрок видит только целые
///     уровни, и каждый новый — тёмно-золотым сообщением.
/// </summary>
public static class WeaponMasterySystem
{
    public const int MaxLevel = 100;

    /// <summary>Какая доля лучшего соседа по линейке идёт в зачёт.</summary>
    public const double LineSpill = 0.25;

    /// <summary>Тёмное золото: светлый конец рампы — (222, 156, 32), подобрано по hues.mul.
    /// Нарочно не фиолетовый оттенок прироста веток: это другой слой.</summary>
    public const int GainHue = 0x06B9;

    private const double GainChance = 0.15;

    public const double HitPerLevel = 0.10;
    public const double CritPerLevel = 0.08;
    public const double DamagePerLevel = 0.05;

    private const string TrackPrefix = "mastery:weapon/";

    /// <summary>Вид оружия: ключ дорожки, линейка, имя для доски, имя для «владение …» и
    /// номер графики предмета — иконка на доске мастерства. У кулаков ноль: предмета нет.</summary>
    public sealed record Kind(string Key, string Line, string Name, string Instrumental, int Icon)
    {
        public string Track => TrackPrefix + Key;
    }

    /// <summary>Линейки: ключ → имя для доски.</summary>
    public static readonly IReadOnlyDictionary<string, string> Lines = new Dictionary<string, string>
    {
        ["swords"] = "Мечи",
        ["axes"] = "Топоры",
        ["polearms"] = "Древковое",
        ["bashing"] = "Дробящее",
        ["staves"] = "Посохи",
        ["spears"] = "Копья",
        ["knives"] = "Ножи и кинжалы",
        ["bows"] = "Луки",
        ["crossbows"] = "Арбалеты",
        ["thrown"] = "Метательное",
        ["wrestling"] = "Рукопашная"
    };

    // Имена — из русского клилока шарда, чтобы на доске было то же, что в подсказке к
    // предмету. Творительный падеж — руками: «владение алебардой» правилами не склонить, а
    // ошибка в каждом сообщении о приросте видна сразу.
    //
    // Отступления от простого «базовый класс = линейка»:
    //   Lance наследует BaseSword, но это копьё для верховых — в копья;
    //   луки и арбалеты в коде одно семейство (BaseRanged) — здесь две линейки;
    //   TribalSpear — то же копьё, отдельной строкой не идёт;
    //   PaladinSword и ThinLongsword в клилоке совпали с мечом викингов и длинным мечом —
    //   им свои имена, иначе на доске два одинаковых пункта.
    private static readonly (Type Type, string Line, string Name, string Instrumental, int Icon)[] Table =
    {
        (typeof(Axe), "axes", "топор", "топором", 0x0F49),
        (typeof(BattleAxe), "axes", "боевой топор", "боевым топором", 0x0F47),
        (typeof(DoubleAxe), "axes", "двойной топор", "двойным топором", 0x0F4B),
        (typeof(DualShortAxes), "axes", "парные короткие топоры", "парными короткими топорами", 0x08FD),
        (typeof(ExecutionersAxe), "axes", "топор палача", "топором палача", 0x0F45),
        (typeof(Hatchet), "axes", "топорик", "топориком", 0x0F43),
        (typeof(LargeBattleAxe), "axes", "большой боевой топор", "большим боевым топором", 0x13FB),
        (typeof(OrnateAxe), "axes", "украшенный топор", "украшенным топором", 0x2D28),
        (typeof(Pickaxe), "axes", "кирка", "киркой", 0x0E86),
        (typeof(TwoHandedAxe), "axes", "двуручный топор", "двуручным топором", 0x1443),
        (typeof(WarAxe), "axes", "военный топор", "военным топором", 0x13B0),

        (typeof(Club), "bashing", "дубина", "дубиной", 0x13B4),
        (typeof(DiamondMace), "bashing", "алмазная булава", "алмазной булавой", 0x2D24),
        (typeof(DiscMace), "bashing", "дисковая булава", "дисковой булавой", 0x0903),
        (typeof(HammerPick), "bashing", "кирка-молот", "киркой-молотом", 0x143D),
        (typeof(Mace), "bashing", "булава", "булавой", 0x0F5C),
        (typeof(MagicWand), "bashing", "жезл", "жезлом", 0x0DF2),
        (typeof(Maul), "bashing", "кувалда", "кувалдой", 0x143B),
        (typeof(Nunchaku), "bashing", "нунчаки", "нунчаками", 0x27AE),
        (typeof(Scepter), "bashing", "скипетр", "скипетром", 0x26BC),
        (typeof(Tessen), "bashing", "тэссэн", "тэссэном", 0x27A3),
        (typeof(Tetsubo), "bashing", "тэцубо", "тэцубо", 0x27A6),
        (typeof(WarHammer), "bashing", "боевой молот", "боевым молотом", 0x1439),
        (typeof(WarMace), "bashing", "боевая булава", "боевой булавой", 0x1407),

        (typeof(AssassinSpike), "knives", "шип убийцы", "шипом убийцы", 0x2D21),
        (typeof(ButcherKnife), "knives", "разделочный нож", "разделочным ножом", 0x13F6),
        (typeof(Cleaver), "knives", "тесак", "тесаком", 0x0EC3),
        (typeof(Dagger), "knives", "кинжал", "кинжалом", 0x0F52),
        (typeof(ElvenSpellblade), "knives", "эльфийский заклинательный клинок", "эльфийским заклинательным клинком", 0x2D20),
        (typeof(Kama), "knives", "кама", "камой", 0x27AD),
        (typeof(Lajatang), "knives", "лайцзятан", "лайцзятаном", 0x27A7),
        (typeof(Leafblade), "knives", "листовой клинок", "листовым клинком", 0x2D22),
        (typeof(Sai), "knives", "сай", "саем", 0x27AF),
        (typeof(SkinningKnife), "knives", "нож для свежевания", "ножом для свежевания", 0x0EC4),
        (typeof(Tekagi), "knives", "текаги", "текаги", 0x27AB),
        (typeof(WarCleaver), "knives", "боевой тесак", "боевым тесаком", 0x2D2F),

        (typeof(Bardiche), "polearms", "бердыш", "бердышом", 0x0F4D),
        (typeof(Halberd), "polearms", "алебарда", "алебардой", 0x143E),
        (typeof(Scythe), "polearms", "коса", "косой", 0x26BA),

        (typeof(Bow), "bows", "лук", "луком", 0x13B2),
        (typeof(CompositeBow), "bows", "составной лук", "составным луком", 0x26C2),
        (typeof(ElvenCompositeLongbow), "bows", "эльфийский составной длинный лук", "эльфийским составным длинным луком", 0x2D1E),
        (typeof(MagicalShortbow), "bows", "магический короткий лук", "магическим коротким луком", 0x2D2B),
        (typeof(Yumi), "bows", "юми", "юми", 0x27A5),

        (typeof(Crossbow), "crossbows", "арбалет", "арбалетом", 0x0F50),
        (typeof(HeavyCrossbow), "crossbows", "тяжёлый арбалет", "тяжёлым арбалетом", 0x13FD),
        (typeof(RepeatingCrossbow), "crossbows", "многозарядный арбалет", "многозарядным арбалетом", 0x26C3),

        (typeof(BladedStaff), "spears", "клинковый посох", "клинковым посохом", 0x26BD),
        (typeof(DoubleBladedStaff), "spears", "двухклинковый посох", "двухклинковым посохом", 0x26BF),
        (typeof(DualPointedSpear), "spears", "парное остроконечное копьё", "парным остроконечным копьём", 0x0904),
        (typeof(GargishWarFork), "spears", "гаргулья боевая вилка", "гаргульей боевой вилкой", 0x48BE),
        (typeof(Lance), "spears", "копьё для верховых", "копьём для верховых", 0x26C0),
        (typeof(Pike), "spears", "пика", "пикой", 0x26BE),
        (typeof(Pitchfork), "spears", "вилы", "вилами", 0x0E87),
        (typeof(ShortSpear), "spears", "короткое копьё", "коротким копьём", 0x1403),
        (typeof(Spear), "spears", "копьё", "копьём", 0x0F62),
        (typeof(WarFork), "spears", "боевые вилы", "боевыми вилами", 0x1405),

        (typeof(BlackStaff), "staves", "чёрный посох", "чёрным посохом", 0x0DF0),
        (typeof(GlassStaff), "staves", "стеклянный посох", "стеклянным посохом", 0x0905),
        (typeof(GnarledStaff), "staves", "узловатый посох", "узловатым посохом", 0x13F8),
        (typeof(QuarterStaff), "staves", "боевой шест", "боевым шестом", 0x0E89),
        (typeof(SerpentstoneStaff), "staves", "посох змеиного камня", "посохом змеиного камня", 0x0906),
        (typeof(ShepherdsCrook), "staves", "пастуший посох", "пастушьим посохом", 0x0E81),
        (typeof(WildStaff), "staves", "дикий посох", "диким посохом", 0x2D25),

        (typeof(BloodBlade), "swords", "кровавый клинок", "кровавым клинком", 0x08FE),
        (typeof(Bokuto), "swords", "бокуто", "бокуто", 0x27A8),
        (typeof(BoneHarvester), "swords", "костяной жнец", "костяным жнецом", 0x26BB),
        (typeof(Broadsword), "swords", "широкий меч", "широким мечом", 0x0F5E),
        (typeof(CrescentBlade), "swords", "полумесяцевый клинок", "полумесяцевым клинком", 0x26C1),
        (typeof(Cutlass), "swords", "кортик", "кортиком", 0x1441),
        (typeof(Daisho), "swords", "дайсё", "дайсё", 0x27A9),
        (typeof(DreadSword), "swords", "ужасающий меч", "ужасающим мечом", 0x090B),
        (typeof(ElvenMachete), "swords", "эльфийское мачете", "эльфийским мачете", 0x2D35),
        (typeof(GargishTalwar), "swords", "гаргулийский тальвар", "гаргулийским тальваром", 0x0908),
        (typeof(GlassSword), "swords", "стеклянный меч", "стеклянным мечом", 0x090C),
        (typeof(Katana), "swords", "катана", "катаной", 0x13FF),
        (typeof(Kryss), "swords", "крис", "крисом", 0x1401),
        (typeof(Longsword), "swords", "длинный меч", "длинным мечом", 0x0F61),
        (typeof(NoDachi), "swords", "но-дати", "но-дати", 0x27A2),
        (typeof(PaladinSword), "swords", "меч паладина", "мечом паладина", 0x13B9),
        (typeof(RadiantScimitar), "swords", "сияющий ятаган", "сияющим ятаганом", 0x2D33),
        (typeof(RuneBlade), "swords", "рунный клинок", "рунным клинком", 0x2D32),
        (typeof(Scimitar), "swords", "ятаган", "ятаганом", 0x13B6),
        (typeof(ThinLongsword), "swords", "тонкий длинный меч", "тонким длинным мечом", 0x13B8),
        (typeof(VikingSword), "swords", "меч викингов", "мечом викингов", 0x13B9),
        (typeof(Wakizashi), "swords", "вакидзаси", "вакидзаси", 0x27A4),

        (typeof(Boomerang), "thrown", "бумеранг", "бумерангом", 0x08FF),
        (typeof(Cyclone), "thrown", "циклон", "циклоном", 0x0901),
        (typeof(SoulGlaive), "thrown", "глефа душ", "глефой душ", 0x090A),

        (typeof(Fists), "wrestling", "кулаки", "кулаками", 0x0000)
    };

    private static readonly Dictionary<Type, Kind> ByType = new();
    private static readonly Dictionary<string, List<Kind>> ByLine = new();

    /// <summary>Что уже выяснили для классов, которых нет в таблице напрямую (артефакты).</summary>
    private static readonly Dictionary<Type, Kind> Resolved = new();

    static WeaponMasterySystem()
    {
        foreach (var (type, line, name, instrumental, icon) in Table)
        {
            var kind = new Kind(type.Name, line, name, instrumental, icon);
            ByType[type] = kind;

            if (!ByLine.TryGetValue(line, out var list))
            {
                ByLine[line] = list = new List<Kind>();
            }

            list.Add(kind);
        }

        // Племенное копьё — то же копьё: своей строки на доске не получает.
        ByType[typeof(TribalSpear)] = ByType[typeof(Spear)];
    }

    public static IEnumerable<Kind> KindsInLine(string line) =>
        ByLine.TryGetValue(line, out var list) ? list : Array.Empty<Kind>();

    /// <summary>
    ///     Вид оружия по предмету. Идём вверх по наследованию до первого класса из таблицы —
    ///     так артефакт считается своим базовым оружием.
    /// </summary>
    public static bool TryGetKind(BaseWeapon weapon, out Kind kind)
    {
        kind = null;

        if (weapon == null)
        {
            return false;
        }

        var type = weapon.GetType();

        if (Resolved.TryGetValue(type, out kind))
        {
            return kind != null;
        }

        for (var t = type; t != null && t != typeof(BaseWeapon); t = t.BaseType)
        {
            if (ByType.TryGetValue(t, out kind))
            {
                break;
            }
        }

        Resolved[type] = kind; // null тоже запоминаем: это «оружие без мастерства»

        return kind != null;
    }

    /// <summary>Своё мастерство, с дробью.</summary>
    public static double Raw(Mobile m, Kind kind) => MahaonProgress.Get(m, kind.Track);

    /// <summary>Своё мастерство, целым уровнем — так его видит игрок.</summary>
    public static int Level(Mobile m, Kind kind) => (int)Math.Floor(Raw(m, kind));

    /// <summary>Действующее: своё плюс четверть лучшего соседа по линейке, не выше потолка.</summary>
    public static double Effective(Mobile m, Kind kind)
    {
        var own = Raw(m, kind);
        var best = 0.0;

        foreach (var other in KindsInLine(kind.Line))
        {
            if (other != kind)
            {
                best = Math.Max(best, Raw(m, other));
            }
        }

        return Math.Min(MaxLevel, own + best * LineSpill);
    }

    // ---- Тренировка ---------------------------------------------------------------------------

    /// <summary>
    ///     Попадание по живой цели — шанс прироста. Зовётся из BaseWeapon.OnHit рядом с прочими
    ///     «после удара».
    /// </summary>
    public static void OnHit(Mobile attacker, Mobile defender, BaseWeapon weapon)
    {
        if (attacker is not PlayerMobile || defender == null || defender == attacker || !defender.Alive)
        {
            return;
        }

        if (!TryGetKind(weapon, out var kind))
        {
            return;
        }

        var raw = Raw(attacker, kind);

        if (raw >= MaxLevel || Utility.RandomDouble() > GainChance)
        {
            return;
        }

        Advance(attacker, kind, raw, GainFor(raw));
    }

    /// <summary>Сколько прибавляет один удачный удар на этом уровне.</summary>
    public static double GainFor(double raw) => raw < 50 ? 0.3 : raw < 80 ? 0.15 : 0.05;

    /// <summary>Двигает мастерство и, если перешли целый уровень, сообщает об этом.</summary>
    public static void Advance(Mobile m, Kind kind, double raw, double gain)
    {
        var after = Math.Min(MaxLevel, raw + gain);

        MahaonProgress.Set(m, kind.Track, after);

        var was = (int)Math.Floor(raw);
        var now = (int)Math.Floor(after);

        if (now > was)
        {
            m.SendMessage(GainHue, $"Твоё владение {kind.Instrumental} выросло на {now - was}. Теперь — {now}.");

            // Доска мастерства — через то же склеенное обновление, что и дерево веток.
            MahaonSkillTree.NotifyChanged(m);
        }
    }

    // ---- Вклад в удар -------------------------------------------------------------------------

    private sealed class Contributor : IStrikeContributor
    {
        public void Contribute(in StrikeContext context, StrikeProfile profile)
        {
            if (context.Attacker is not PlayerMobile || !TryGetKind(context.Weapon, out var kind))
            {
                return;
            }

            var level = Effective(context.Attacker, kind);

            if (level <= 0)
            {
                return;
            }

            var source = $"Мастерство: {kind.Name} ({(int)level})";

            profile.Add(source, StrikeField.HitChance, level * HitPerLevel);
            profile.Add(source, StrikeField.CritChance, level * CritPerLevel);
            profile.Add(source, StrikeField.Damage, level * DamagePerLevel);
        }
    }

    private static Contributor _contributor;

    public static void Configure()
    {
        _contributor ??= new Contributor();
        StrikeProfiles.Register(_contributor);

        CommandSystem.Register("WeaponMastery", AccessLevel.GameMaster, WeaponMastery_OnCommand);
    }

    // ---- Команда ------------------------------------------------------------------------------

    [Usage("WeaponMastery [уровень]")]
    [Description("Мастерство оружия в твоих руках: показать, а с числом — выставить (для проверки).")]
    private static void WeaponMastery_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;

        if (from.Weapon is not BaseWeapon weapon || !TryGetKind(weapon, out var kind))
        {
            from.SendMessage(0x22, "У этого оружия нет мастерства.");
            return;
        }

        if (e.Length >= 1)
        {
            var level = Math.Clamp(e.GetInt32(0), 0, MaxLevel);
            MahaonProgress.Set(from, kind.Track, level);
        }

        var effective = Effective(from, kind);

        from.SendMessage(GainHue, $"Владение: {kind.Name} — {Level(from, kind)}, с линейкой «{Lines[kind.Line]}» — {(int)effective}.");
        from.SendMessage(
            0x3B2,
            $"  к попаданию +{effective * HitPerLevel:0.#}, крит {effective * CritPerLevel:0.#}%, урон +{effective * DamagePerLevel:0.#}%"
        );
    }
}

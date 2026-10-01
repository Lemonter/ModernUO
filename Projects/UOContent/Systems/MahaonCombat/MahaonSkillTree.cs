using System;
using System.Collections.Generic;
using Server.Network;
using Server.Systems.MahaonMetals;

namespace Server.Systems.MahaonCombat;

/// <summary>
///     Foundation for a fully custom, non-skills.mul skill tree — per the person running
///     this shard, eventually EVERY skill (combat and non-combat) migrates to this same
///     shape (Albion-style categories with sub-skills), not just combat. Real, trainable
///     0-100 values gained through use, synced to the client via a dedicated packet, shown
///     in a dedicated gump (see ClassicUO's MahaonSkillTreeGump) instead of the vanilla
///     Skills window.
///
///     Style value tracking itself lives in WeaponStyleSystem now (covers all 5 weapon
///     categories uniformly, not just martial arts) — this class is purely the tree
///     SHAPE builder, pulling numbers from WeaponStyleSystem for the 5 selectable
///     categories and straight from vanilla Skills[] for everything else.
///
///     Shard owner's own vision: every Mahaon specialization/school/technique should sit
///     as a SUBSECTION directly under the real vanilla skill it belongs to (e.g. Magery's
///     6 schools nested under Magery itself), not as a separate parallel subcategory a
///     player has to guess the connection to by name alone. BuildSkillWithSpecsSubCategory
///     is the shared shape for that — entry 0 is always the real vanilla skill's own
///     value, entries 1+ are its specializations. A handful of specializations map to
///     SEVERAL different real skills individually rather than one skill owning many (the
///     5 Thieving actions, the 4 "minor" skills, the 3 Bard actions, Bushido/Ninjitsu
///     techniques) — those get one small merged subcategory per real skill too, just with
///     fewer entries. Any vanilla skill with nothing Mahaon-specific attached stays in a
///     flat leftover list per category, same as before.
///
///     In-memory only for now, same convention as most Mahaon systems this session —
///     revisit with real serialization once the shape of the FULL multi-category tree is
///     settled (premature to build persistence for a structure that's still this early).
/// </summary>
public static class MahaonSkillTree
{
    /// <summary>Одна мастерка в дереве. Cap — её персональный потолок у ЭТОГО игрока
    /// (свитки мастерства и профессия его двигают, см. MahaonMasteryCapSystem); он
    /// проставляется одним махом в GetTreeSnapshot, а не в каждом Build*-методе.</summary>
    public readonly record struct SkillEntry(
        byte EntryId, string Name, double Value, string Description = "", double Cap = 0
    );

    public readonly record struct SkillSubCategory(
        string Name, bool IsSelectable, byte CategoryId, byte SelectedId, SkillEntry[] Entries, bool IsUsableSkillList
    );

    public readonly record struct SkillCategory(string Name, SkillSubCategory[] SubCategories);

    // Vanilla skills auto-push a delta packet to the client the instant their Value
    // changes (Skills.cs, Owner.NetState.SendSkillChange) — every Mahaon custom system
    // below (weapon styles, magic schools, craft/gathering specializations, ...) had no
    // equivalent, so the tree only ever reached the client once, at gump-open, and just
    // sat there going stale while the player kept training. Every one of those systems'
    // "value changed" call sites now calls this instead of silently returning.
    //
    // Coalesced via a self-clearing "pending" set rather than a persistent per-player
    // last-sent timestamp — a burst of hits/crafts/casts within the same 250ms collapses
    // into one packet, and the tracking entry never outlives that window, so nothing
    // accumulates for players who log off (unlike a permanent Dictionary<Mobile, DateTime>
    // would).
    private static readonly HashSet<Mobile> PendingPush = new();
    private static readonly TimeSpan PushCoalesceWindow = TimeSpan.FromMilliseconds(250);

    public static void NotifyChanged(Mobile player)
    {
        if (player is not Mobiles.PlayerMobile pm || pm.NetState == null)
        {
            return;
        }

        if (!PendingPush.Add(pm))
        {
            return; // already scheduled this window
        }

        Timer.DelayCall(PushCoalesceWindow, () =>
        {
            PendingPush.Remove(pm);

            if (pm.NetState != null)
            {
                pm.NetState.SendSkillTree(GetTreeSnapshot(pm));
            }
        });
    }

    // Vanilla skills show a "Твой навык в X вырос на Y. Теперь он составляет Z." system
    // message on every real gain (client-side, ResGeneral.YourSkillIn0Has1By2ItIsNow3) —
    // none of the Mahaon custom systems below had an equivalent, so training a weapon
    // style/school/specialization gave no feedback at all versus a vanilla skill training
    // right next to it. Mirrors the vanilla wording.
    //
    // Deliberately NOT the vanilla skill-gain hue (used by the plain Str/Dex/Int/skill
    // messages in PacketHandlers.cs, which pass hue 0/natural) — this hue doubles as a
    // signal the client's MessageManager.HandleMessage watches for specifically, to also
    // pop a purple Marck Script toast (see client MahaonSkillGainToastGump) distinguishing
    // "mastery/style" gains from ordinary skill gains at a glance, not just by hue in the
    // scrollback. Must stay in sync with the client's MahaonSkillTree.MasteryGainHue check.
    //
    // 0x486 is a verified real purple (the same hue used by the OSI VioletCouragePurple
    // Treasures of Tokuno pigment, GreaterArtifacts.cs) — this replaces the previous 0x0095,
    // which was mislabeled "deep purple" (copied from an unrelated MahaonMetalTable.cs
    // comment) but actually falls in the hue table's orange/tan band, not purple.
    public const int MasteryGainHue = 0x486;

    public static void AnnounceGain(Mobile player, string skillName, double gain, double newValue)
    {
        if (player is not Mobiles.PlayerMobile pm || pm.NetState == null)
        {
            return;
        }

        pm.SendMessage(MasteryGainHue, $"Твой навык в {skillName} вырос на {gain:0.0}. Теперь он составляет {newValue:0.0}.");
    }

    /// <summary>Full tree snapshot sent to the client on every skills-query — see
    /// MahaonSkillTreePackets.SendSkillTree and MahaonSkillTreeGump on the client.</summary>
    public static SkillCategory[] GetTreeSnapshot(Mobile player)
    {
        var combatCategory = new SkillCategory(
            "Боевые навыки",
            new[]
            {
                BuildStyleSubCategory(player, WeaponCategory.Wrestling),
                BuildStyleSubCategory(player, WeaponCategory.Swords),
                BuildStyleSubCategory(player, WeaponCategory.Macing),
                BuildStyleSubCategory(player, WeaponCategory.Fencing),
                BuildStyleSubCategory(player, WeaponCategory.Archery),
                BuildStyleSubCategory(player, WeaponCategory.Throwing)
            }
        );

        var magicCategory = new SkillCategory(
            "Магия",
            new[]
            {
                BuildMagerySubCategory(player),
                BuildNecromancySubCategory(player),
                BuildChivalrySubCategory(player),
                BuildSpellweavingSubCategory(player),
                BuildMysticismSubCategory(player),
                BuildBushidoSubCategory(player),
                BuildNinjitsuSubCategory(player)
            }
        );

        var thievingCategory = new SkillCategory(
            "Воровство",
            new[]
            {
                BuildStealingSubCategory(player),
                BuildLockpickingSubCategory(player),
                BuildStealthSubCategory(player),
                BuildPoisoningSubCategory(player),
                BuildSnoopingSubCategory(player)
            }
        );

        var craftCategory = new SkillCategory(
            "Ремёсла",
            new[]
            {
                BuildBlacksmithSubCategory(player),
                BuildTailoringSubCategory(player),
                BuildCarpentrySubCategory(player),
                BuildTinkeringSubCategory(player),
                BuildCookingSubCategory(player),
                BuildAlchemySubCategory(player),
                BuildInscriptionSubCategory(player)
            }
        );

        var wildernessCategory = new SkillCategory(
            "Дикая природа",
            new[]
            {
                BuildTamingSubCategory(player),
                BuildFishingSubCategory(player),
                BuildMiningSubCategory(player),
                BuildLumberjackingSubCategory(player),
                BuildVeterinarySubCategory(player),
                BuildHerdingSubCategory(player),
                BuildTrackingSubCategory(player),
                BuildForensicsSubCategory(player),
                BuildTasteIDSubCategory(player),
                BuildCampingSubCategory(player),
                BuildItemIDSubCategory(player)
            }
        );

        var bardCategory = new SkillCategory(
            "Бардовское дело",
            new[]
            {
                BuildDiscordanceSubCategory(player),
                BuildPeacemakingSubCategory(player),
                BuildProvocationSubCategory(player)
            }
        );

        var miscCategory = new SkillCategory("Прочее", new[] { BuildHealingSubCategory(player) });

        return WithCaps(
            player,
            new[]
            {
                combatCategory, magicCategory, thievingCategory, craftCategory, wildernessCategory, bardCategory,
                miscCategory
            }
        );
    }

    /// <summary>
    ///     Проставляет каждой записи её персональный потолок.
    ///
    ///     Раньше клиент получал только текущее значение мастерки и показывал голое число:
    ///     «43» — а много это или почти предел, игроку взять было неоткуда, потому что
    ///     потолок у каждого свой (свитки мастерства, профессия — см.
    ///     ProfessionMasteryRules). Проще всего дописать потолки здесь, одним проходом по
    ///     готовому снимку, чем таскать игрока в полтора десятка Build*-методов.
    /// </summary>
    private static SkillCategory[] WithCaps(Mobile player, SkillCategory[] categories)
    {
        foreach (var category in categories)
        {
            foreach (var sub in category.SubCategories)
            {
                if (sub.IsUsableSkillList)
                {
                    continue; // обычный список навыков, потолки у него свои и не отсюда
                }

                for (var i = 0; i < sub.Entries.Length; i++)
                {
                    var entry = sub.Entries[i];
                    sub.Entries[i] = entry with { Cap = MahaonMasteryCapSystem.GetCap(player, entry.Name) };
                }
            }
        }

        return categories;
    }

    /// <summary>One of the 5 weapon-category subcategories (Рукопашный бой / Мечи /
    /// Дробящее / Колющее / Лук) — 5 selectable style entries each, values and active
    /// choice both pulled from WeaponStyleSystem.</summary>
    private const string BaselineStyleDescription =
        "Базовый («классический») стиль — совпадает с обычным навыком владения этим видом оружия, растёт как любой ванильный навык.";

    private const string NamedStyleDescription =
        "Стиль боя, привязанный к месту удара (выбирается в боевом меню, вкладка «Целиться в»). " +
        "Активный сейчас стиль даёт бонус к урону в этой категории оружия, растущий с его тренированностью; " +
        "тренируется от успешных ударов, пока выбран активным (т.е. пока выбрано его место удара). Тренировка ДРУГИХ " +
        "стилей этой же категории тоже немного добавляет к бонусу («широта» владения), даже когда они не выбраны.";

    private static SkillSubCategory BuildStyleSubCategory(Mobile player, WeaponCategory category)
    {
        var entries = new SkillEntry[7];

        for (byte style = 0; style < 7; style++)
        {
            entries[style] = new SkillEntry(
                style, WeaponStyleSystem.StyleName(category, style), WeaponStyleSystem.GetValue(player, category, style),
                style == 0 ? BaselineStyleDescription : NamedStyleDescription
            );
        }

        var activeStyle = WeaponStyleSystem.GetActiveStyle(player, category);
        return new SkillSubCategory(WeaponStyleSystem.RuCategoryName(category), true, (byte)category, activeStyle, entries, false);
    }

    /// <summary>Shared shape for a real vanilla skill's Mahaon specialization(s), grouped
    /// under a subcategory named after the real skill they belong to (so the connection is
    /// still obvious by name) — but the vanilla skill's OWN value/entry is NOT included
    /// here anymore. This tree is specializations-and-styles only now; the real skill list
    /// (with real Russian names) lives in the vanilla Standard/Advanced Skills gump
    /// instead, reached separately — this tree used to duplicate that with its own entry
    /// 0 per subcategory, which is exactly what the shard owner asked to remove.</summary>
    private static SkillSubCategory BuildSkillWithSpecsSubCategory(
        Mobile player, SkillName skill, (string name, double value, string description)[] specs
    )
    {
        var entries = new SkillEntry[specs.Length];

        for (var i = 0; i < specs.Length; i++)
        {
            entries[i] = new SkillEntry((byte)i, specs[i].name, specs[i].value, specs[i].description);
        }

        return new SkillSubCategory(RuSkillName(skill), false, 0, 0, entries, false);
    }

    private const string MagicSchoolDescription =
        "Школа магии. Растёт пассивно от каста заклинаний именно этой школы (не нужно ничего выбирать активным) — " +
        "даёт бонус к урону заклинаниями этой школы.";

    private static SkillSubCategory BuildMagerySubCategory(Mobile player)
    {
        var specs = new (string, double, string)[6];

        for (byte school = 0; school < 6; school++)
        {
            var s = (MagerySchool)school;
            specs[school] = (MagerySchoolSystem.RuSchoolName(s), MagerySchoolSystem.GetValue(player, s), MagicSchoolDescription);
        }

        return BuildSkillWithSpecsSubCategory(player, SkillName.Magery, specs);
    }

    private static SkillSubCategory BuildNecromancySubCategory(Mobile player)
    {
        var specs = new (string, double, string)[5];

        for (byte school = 0; school < 5; school++)
        {
            var s = (NecromancySchool)school;
            specs[school] = (NecromancySchoolSystem.RuSchoolName(s), NecromancySchoolSystem.GetValue(player, s), MagicSchoolDescription);
        }

        return BuildSkillWithSpecsSubCategory(player, SkillName.Necromancy, specs);
    }

    private static SkillSubCategory BuildChivalrySubCategory(Mobile player)
    {
        var specs = new (string, double, string)[3];

        for (byte school = 0; school < 3; school++)
        {
            var s = (ChivalrySchool)school;
            specs[school] = (ChivalrySchoolSystem.RuSchoolName(s), ChivalrySchoolSystem.GetValue(player, s), MagicSchoolDescription);
        }

        return BuildSkillWithSpecsSubCategory(player, SkillName.Chivalry, specs);
    }

    private static SkillSubCategory BuildSpellweavingSubCategory(Mobile player)
    {
        var specs = new (string, double, string)[5];

        for (byte school = 0; school < 5; school++)
        {
            var s = (SpellweavingSchool)school;
            specs[school] = (SpellweavingSchoolSystem.RuSchoolName(s), SpellweavingSchoolSystem.GetValue(player, s), MagicSchoolDescription);
        }

        return BuildSkillWithSpecsSubCategory(player, SkillName.Spellweaving, specs);
    }

    private static SkillSubCategory BuildMysticismSubCategory(Mobile player)
    {
        var specs = new (string, double, string)[3];

        for (byte school = 0; school < 3; school++)
        {
            var s = (MysticismSchool)school;
            specs[school] = (MysticismSchoolSystem.RuSchoolName(s), MysticismSchoolSystem.GetValue(player, s), MagicSchoolDescription);
        }

        return BuildSkillWithSpecsSubCategory(player, SkillName.Mysticism, specs);
    }

    private const string MartialTechniqueDescription =
        "Приём Бусидо/Ниндзюцу. Растёт от успешного применения этого конкретного приёма в бою — даёт бонус к урону при " +
        "использовании, плюс небольшую добавку от тренированности ДРУГИХ приёмов того же боевого пути.";

    // MartialTechnique's 8 values split across two real skills (Bushido owns the first 3,
    // Ninjitsu the last 5 — see MartialTechniqueSystem's own BushidoGroup/NinjitsuGroup).
    private static SkillSubCategory BuildBushidoSubCategory(Mobile player)
    {
        var specs = new (string, double, string)[3];

        for (byte t = 0; t < 3; t++)
        {
            var tech = (MartialTechnique)t;
            specs[t] = (MartialTechniqueSystem.RuTechniqueName(tech), MartialTechniqueSystem.GetValue(player, tech), MartialTechniqueDescription);
        }

        return BuildSkillWithSpecsSubCategory(player, SkillName.Bushido, specs);
    }

    private static SkillSubCategory BuildNinjitsuSubCategory(Mobile player)
    {
        var specs = new (string, double, string)[5];

        for (byte t = 0; t < 5; t++)
        {
            var tech = (MartialTechnique)(t + 3);
            specs[t] = (MartialTechniqueSystem.RuTechniqueName(tech), MartialTechniqueSystem.GetValue(player, tech), MartialTechniqueDescription);
        }

        return BuildSkillWithSpecsSubCategory(player, SkillName.Ninjitsu, specs);
    }

    // ThievingSpecialization's 5 values each belong to a DIFFERENT real skill individually
    // (Pickpocket->Stealing, Locksmith->Lockpicking, Shadow->Stealth, Poisoner->Poisoning,
    // Snoop->Snooping) rather than one skill owning all 5 — one single-spec subcategory
    // per real skill, same shared shape either way.
    private static SkillSubCategory BuildStealingSubCategory(Mobile player) =>
        BuildSkillWithSpecsSubCategory(
            player, SkillName.Stealing,
            new[]
            {
                (
                    ThievingSpecializationSystem.RuSpecializationName(ThievingSpecialization.Pickpocket),
                    ThievingSpecializationSystem.GetValue(player, ThievingSpecialization.Pickpocket),
                    "Растёт от успешной кражи из рук/сумки живой цели (не из сундуков и не с трупов). Повышает шанс успеха карманных краж."
                )
            }
        );

    private static SkillSubCategory BuildLockpickingSubCategory(Mobile player) =>
        BuildSkillWithSpecsSubCategory(
            player, SkillName.Lockpicking,
            new[]
            {
                (
                    ThievingSpecializationSystem.RuSpecializationName(ThievingSpecialization.Locksmith),
                    ThievingSpecializationSystem.GetValue(player, ThievingSpecialization.Locksmith),
                    "Растёт от каждого успешного взлома замка. Повышает шанс успеха при следующих взломах."
                )
            }
        );

    private static SkillSubCategory BuildStealthSubCategory(Mobile player) =>
        BuildSkillWithSpecsSubCategory(
            player, SkillName.Stealth,
            new[]
            {
                (
                    ThievingSpecializationSystem.RuSpecializationName(ThievingSpecialization.Shadow),
                    ThievingSpecializationSystem.GetValue(player, ThievingSpecialization.Shadow),
                    "Растёт от успешного скрытного перемещения. Повышает шанс не быть замеченным при движении в скрытом состоянии."
                )
            }
        );

    private static SkillSubCategory BuildPoisoningSubCategory(Mobile player) =>
        BuildSkillWithSpecsSubCategory(
            player, SkillName.Poisoning,
            new[]
            {
                (
                    ThievingSpecializationSystem.RuSpecializationName(ThievingSpecialization.Poisoner),
                    ThievingSpecializationSystem.GetValue(player, ThievingSpecialization.Poisoner),
                    "Растёт от успешного нанесения яда на оружие. Повышает шанс успешного отравления."
                )
            }
        );

    private static SkillSubCategory BuildSnoopingSubCategory(Mobile player) =>
        BuildSkillWithSpecsSubCategory(
            player, SkillName.Snooping,
            new[]
            {
                (
                    ThievingSpecializationSystem.RuSpecializationName(ThievingSpecialization.Snoop),
                    ThievingSpecializationSystem.GetValue(player, ThievingSpecialization.Snoop),
                    "Растёт от успешного обыска чужого рюкзака. Повышает шанс успеха при обыске."
                )
            }
        );

    private static SkillSubCategory BuildBlacksmithSubCategory(Mobile player)
    {
        var specs = new (string, double, string)[4];

        for (byte spec = 0; spec < 4; spec++)
        {
            var s = (BlacksmithSpecialization)spec;
            specs[spec] = (
                BlacksmithSpecializationSystem.RuSpecializationName(s), BlacksmithSpecializationSystem.GetValue(player, s),
                "Растёт от успешной ковки соответствующего типа предметов. Даёт бонус к качеству/шансу успеха именно в этом направлении кузнечного дела."
            );
        }

        return BuildSkillWithSpecsSubCategory(player, SkillName.Blacksmith, specs);
    }

    private static SkillSubCategory BuildTailoringSubCategory(Mobile player)
    {
        var specs = new (string, double, string)[2];

        for (byte spec = 0; spec < 2; spec++)
        {
            var s = (TailoringSpecialization)spec;
            specs[spec] = (
                TailoringSpecializationSystem.RuSpecializationName(s), TailoringSpecializationSystem.GetValue(player, s),
                "Растёт от успешного пошива соответствующего типа вещей. Даёт бонус к качеству/шансу успеха именно в этом направлении шитья."
            );
        }

        return BuildSkillWithSpecsSubCategory(player, SkillName.Tailoring, specs);
    }

    private static SkillSubCategory BuildCarpentrySubCategory(Mobile player) =>
        BuildSkillWithSpecsSubCategory(
            player, SkillName.Carpentry,
            new[]
            {
                (
                    CarpentrySpecializationSystem.RuSpecializationName, CarpentrySpecializationSystem.GetValue(player),
                    "Растёт от успешной работы плотника. Даёт бонус к качеству/шансу успеха при изготовлении."
                )
            }
        );

    private static SkillSubCategory BuildTinkeringSubCategory(Mobile player) =>
        BuildSkillWithSpecsSubCategory(
            player, SkillName.Tinkering,
            new[]
            {
                (
                    TinkeringSpecializationSystem.RuSpecializationName, TinkeringSpecializationSystem.GetValue(player),
                    "Растёт от успешной работы механика. Даёт бонус к качеству/шансу успеха при изготовлении."
                )
            }
        );

    private static SkillSubCategory BuildCookingSubCategory(Mobile player) =>
        BuildSkillWithSpecsSubCategory(
            player, SkillName.Cooking,
            new[]
            {
                (
                    CookingSpecializationSystem.RuSpecializationName, CookingSpecializationSystem.GetValue(player),
                    "Растёт от успешного приготовления еды. Даёт бонус к качеству/шансу успеха при готовке."
                )
            }
        );

    private static SkillSubCategory BuildAlchemySubCategory(Mobile player) =>
        BuildSkillWithSpecsSubCategory(
            player, SkillName.Alchemy,
            new[]
            {
                (
                    AlchemySpecializationSystem.RuSpecializationName, AlchemySpecializationSystem.GetValue(player),
                    "Растёт от успешного изготовления зелий. Даёт бонус к качеству/шансу успеха у алхимика."
                )
            }
        );

    private static SkillSubCategory BuildInscriptionSubCategory(Mobile player) =>
        BuildSkillWithSpecsSubCategory(
            player, SkillName.Inscribe,
            new[]
            {
                (
                    InscriptionSpecializationSystem.RuSpecializationName, InscriptionSpecializationSystem.GetValue(player),
                    "Растёт от успешного письма свитков/книг. Даёт бонус к качеству/шансу успеха у писца."
                )
            }
        );

    private static SkillSubCategory BuildTamingSubCategory(Mobile player)
    {
        var specs = new (string, double, string)[2];

        for (byte spec = 0; spec < 2; spec++)
        {
            var s = (TamingSpecialization)spec;
            specs[spec] = (
                TamingSpecializationSystem.RuSpecializationName(s), TamingSpecializationSystem.GetValue(player, s),
                "Растёт от успешного приручения соответствующего типа существ. Повышает шанс успеха именно с этим типом."
            );
        }

        return BuildSkillWithSpecsSubCategory(player, SkillName.AnimalTaming, specs);
    }

    private static SkillSubCategory BuildFishingSubCategory(Mobile player)
    {
        var specs = new (string, double, string)[2];

        for (byte spec = 0; spec < 2; spec++)
        {
            var s = (FishingSpecialization)spec;
            specs[spec] = (
                FishingSpecializationSystem.RuSpecializationName(s), FishingSpecializationSystem.GetValue(player, s),
                "Растёт от рыбалки соответствующим способом. Повышает шансы/качество улова именно в этом направлении."
            );
        }

        return BuildSkillWithSpecsSubCategory(player, SkillName.Fishing, specs);
    }

    private static readonly MahaonMetalTier[] MiningTiers =
    {
        MahaonMetalTier.Обычный, MahaonMetalTier.Уникальный, MahaonMetalTier.Раритетный, MahaonMetalTier.Мифический
    };

    private static SkillSubCategory BuildMiningSubCategory(Mobile player)
    {
        var specs = new (string, double, string)[MiningTiers.Length];

        for (var i = 0; i < MiningTiers.Length; i++)
        {
            var tier = MiningTiers[i];
            specs[i] = (
                GatheringSpecializationSystem.RuMiningName(tier), GatheringSpecializationSystem.GetMiningValue(player, tier),
                "Растёт от добычи металла соответствующего тира. Повышает шансы/выход именно в этом направлении."
            );
        }

        return BuildSkillWithSpecsSubCategory(player, SkillName.Mining, specs);
    }

    private static SkillSubCategory BuildLumberjackingSubCategory(Mobile player)
    {
        var specs = new (string, double, string)[5];

        for (byte spec = 0; spec < 5; spec++)
        {
            var s = (LumberjackingSpecialization)spec;
            specs[spec] = (
                GatheringSpecializationSystem.RuLumberName(s), GatheringSpecializationSystem.GetLumberValue(player, s),
                "Растёт от рубки леса соответствующей редкости. Повышает шансы/выход именно в этом направлении."
            );
        }

        return BuildSkillWithSpecsSubCategory(player, SkillName.Lumberjacking, specs);
    }

    private static SkillSubCategory BuildVeterinarySubCategory(Mobile player)
    {
        var specs = new (string, double, string)[2];

        for (byte spec = 0; spec < 2; spec++)
        {
            var s = (VeterinarySpecialization)spec;
            specs[spec] = (
                VeterinarySpecializationSystem.RuSpecializationName(s), VeterinarySpecializationSystem.GetValue(player, s),
                "Растёт от успешного лечения соответствующего типа существ. Повышает шанс успеха именно с этим типом."
            );
        }

        return BuildSkillWithSpecsSubCategory(player, SkillName.Veterinary, specs);
    }

    private static SkillSubCategory BuildHerdingSubCategory(Mobile player)
    {
        var specs = new (string, double, string)[4];

        for (byte s = 0; s < 4; s++)
        {
            var spec = (HerdingSpecialization)s;
            specs[s] = (
                AnimalTrainingSystem.RuSpecializationName(spec), AnimalTrainingSystem.GetSpecValue(player, spec),
                "Растёт от соответствующей работы с животными (выпас/дрессировка). Повышает эффективность именно в этом направлении."
            );
        }

        return BuildSkillWithSpecsSubCategory(player, SkillName.Herding, specs);
    }

    private static SkillSubCategory BuildTrackingSubCategory(Mobile player)
    {
        var specs = new (string, double, string)[4];

        for (byte s = 0; s < 4; s++)
        {
            var spec = (TrackingSpecialization)s;
            specs[s] = (
                TrackingSpecializationSystem.RuSpecializationName(spec), TrackingSpecializationSystem.GetValue(player, spec),
                "Растёт от выслеживания соответствующего типа целей. Повышает шанс успеха именно с этим типом."
            );
        }

        return BuildSkillWithSpecsSubCategory(player, SkillName.Tracking, specs);
    }

    // MinorSkill's 4 values each belong to a DIFFERENT real skill individually
    // (Forensics/TasteID/Camping/ItemID) — same "several skills, one spec each" shape as
    // the Thieving actions above.
    private static SkillSubCategory BuildForensicsSubCategory(Mobile player) =>
        BuildSkillWithSpecsSubCategory(
            player, SkillName.Forensics,
            new[]
            {
                (
                    MinorSkillSpecializationSystem.RuSkillName(MinorSkill.Forensics),
                    MinorSkillSpecializationSystem.GetValue(player, MinorSkill.Forensics),
                    "Растёт от успешного осмотра трупов. Повышает шанс успеха при опознании причины смерти/личности."
                )
            }
        );

    private static SkillSubCategory BuildTasteIDSubCategory(Mobile player) =>
        BuildSkillWithSpecsSubCategory(
            player, SkillName.TasteID,
            new[]
            {
                (
                    MinorSkillSpecializationSystem.RuSkillName(MinorSkill.TasteID),
                    MinorSkillSpecializationSystem.GetValue(player, MinorSkill.TasteID),
                    "Растёт от успешного распознавания еды/зелий на вкус. Повышает шанс успеха проверки."
                )
            }
        );

    private static SkillSubCategory BuildCampingSubCategory(Mobile player) =>
        BuildSkillWithSpecsSubCategory(
            player, SkillName.Camping,
            new[]
            {
                (
                    MinorSkillSpecializationSystem.RuSkillName(MinorSkill.Camping),
                    MinorSkillSpecializationSystem.GetValue(player, MinorSkill.Camping),
                    "Растёт от успешной установки лагеря/костра. Повышает шанс успеха и качество отдыха."
                )
            }
        );

    private static SkillSubCategory BuildItemIDSubCategory(Mobile player) =>
        BuildSkillWithSpecsSubCategory(
            player, SkillName.ItemID,
            new[]
            {
                (
                    MinorSkillSpecializationSystem.RuSkillName(MinorSkill.ItemID),
                    MinorSkillSpecializationSystem.GetValue(player, MinorSkill.ItemID),
                    "Растёт от успешного опознания магических предметов. Повышает шанс успеха проверки."
                )
            }
        );

    // BardSpecialization's 3 values map 1:1 by name to Discordance/Peacemaking/Provocation.
    private static SkillSubCategory BuildDiscordanceSubCategory(Mobile player) =>
        BuildSkillWithSpecsSubCategory(
            player, SkillName.Discordance,
            new[]
            {
                (
                    BardSpecializationSystem.RuSpecializationName(BardSpecialization.Discordance),
                    BardSpecializationSystem.GetValue(player, BardSpecialization.Discordance),
                    "Растёт от успешного применения диссонанса на цели. Повышает эффективность ослабления цели."
                )
            }
        );

    private static SkillSubCategory BuildPeacemakingSubCategory(Mobile player) =>
        BuildSkillWithSpecsSubCategory(
            player, SkillName.Peacemaking,
            new[]
            {
                (
                    BardSpecializationSystem.RuSpecializationName(BardSpecialization.Peacemaking),
                    BardSpecializationSystem.GetValue(player, BardSpecialization.Peacemaking),
                    "Растёт от успешного усмирения цели. Повышает шанс успеха и длительность усмирения."
                )
            }
        );

    private static SkillSubCategory BuildProvocationSubCategory(Mobile player) =>
        BuildSkillWithSpecsSubCategory(
            player, SkillName.Provocation,
            new[]
            {
                (
                    BardSpecializationSystem.RuSpecializationName(BardSpecialization.Provocation),
                    BardSpecializationSystem.GetValue(player, BardSpecialization.Provocation),
                    "Растёт от успешной провокации. Повышает шанс успеха стравливания целей друг с другом."
                )
            }
        );

    private static SkillSubCategory BuildHealingSubCategory(Mobile player) =>
        BuildSkillWithSpecsSubCategory(
            player, SkillName.Healing,
            new[]
            {
                (
                    HealingSpecializationSystem.RuSpecializationName, HealingSpecializationSystem.GetValue(player),
                    "Растёт от успешного лечения (бинтами и заклинаниями). Повышает эффективность/шанс успеха лечения."
                )
            }
        );

    // Public: also reused by GemSocketingSystem/SoulStoneSocketing to show a human-readable
    // skill name in socketed-item tooltips, rather than duplicating this switch there.
    public static string RuSkillName(SkillName skill) => skill switch
    {
        SkillName.Alchemy       => "Алхимия",
        SkillName.Anatomy       => "Анатомия",
        SkillName.AnimalLore    => "Знание животных",
        SkillName.ItemID        => "Оценка предметов",
        SkillName.ArmsLore      => "Оружиеведение",
        SkillName.Parry         => "Парирование",
        SkillName.Begging       => "Попрошайничество",
        SkillName.Blacksmith    => "Кузнечное дело",
        SkillName.Fletching     => "Изготовление луков",
        SkillName.Peacemaking   => "Миротворчество",
        SkillName.Camping       => "Кемпинг",
        SkillName.Carpentry     => "Плотницкое дело",
        SkillName.Cartography   => "Картография",
        SkillName.Cooking       => "Кулинария",
        SkillName.DetectHidden  => "Обнаружение",
        SkillName.Discordance   => "Разлад",
        SkillName.EvalInt       => "Оценка магии",
        SkillName.Healing       => "Лечение",
        SkillName.Fishing       => "Рыбалка",
        SkillName.Forensics     => "Судмедэкспертиза",
        SkillName.Herding       => "Пастушество",
        SkillName.Hiding        => "Скрытность (прятаться)",
        SkillName.Provocation   => "Провокация",
        SkillName.Inscribe      => "Писарское дело",
        SkillName.Lockpicking   => "Взлом замков",
        SkillName.Magery        => "Магия",
        SkillName.MagicResist   => "Сопротивление магии",
        SkillName.Tactics       => "Тактика",
        SkillName.Snooping      => "Обыск",
        SkillName.Musicianship  => "Музицирование",
        SkillName.Poisoning     => "Отравление",
        SkillName.SpiritSpeak   => "Общение с духами",
        SkillName.Stealing      => "Воровство",
        SkillName.Tailoring     => "Портняжное дело",
        SkillName.AnimalTaming  => "Приручение животных",
        SkillName.TasteID       => "Дегустация",
        SkillName.Tinkering     => "Механика",
        SkillName.Tracking      => "Выслеживание",
        SkillName.Veterinary    => "Ветеринария",
        SkillName.Lumberjacking => "Лесозаготовка",
        SkillName.Mining        => "Горное дело",
        SkillName.Meditation    => "Медитация",
        SkillName.Stealth       => "Скрытное перемещение",
        SkillName.RemoveTrap    => "Обезвреживание ловушек",
        SkillName.Necromancy    => "Некромантия",
        SkillName.Focus         => "Концентрация",
        SkillName.Chivalry      => "Рыцарство",
        SkillName.Bushido       => "Бусидо",
        SkillName.Ninjitsu      => "Ниндзюцу",
        SkillName.Spellweaving  => "Плетение заклинаний",
        SkillName.Mysticism     => "Мистицизм",
        SkillName.Imbuing       => "Наложение свойств",
        SkillName.Throwing      => "Метание",
        // Were missing entirely (handled by BuildStyleSubCategory for the tree's own UI,
        // which never needed to name them here) — fell through to skill.ToString(),
        // showing English in anything ELSE that calls RuSkillName, like a Diamond/Star
        // Sapphire gem socket landing on a Combat-category weapon skill (GemSocketingSystem
        // .AddPropertyLines) — looked like the roll ignored the chosen category entirely,
        // it just displayed in English.
        SkillName.Wrestling     => "Борьба",
        SkillName.Swords        => "Владение мечами",
        SkillName.Macing        => "Владение дробящим",
        SkillName.Fencing       => "Фехтование",
        SkillName.Archery       => "Стрельба из лука",
        _                       => skill.ToString()
    };
}

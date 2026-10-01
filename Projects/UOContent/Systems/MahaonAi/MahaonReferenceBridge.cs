using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Server.Engines.Craft;
using Server.Items;
using Server.Logging;
using Server.Mobiles;
using Server.Multis;
using Server.Systems.MahaonGems;
using Server.Systems.MahaonImbuing;
using Server.Systems.MahaonMetals;
using Server.Systems.MahaonMining;
using Server.Spells;
using Server.Spells.SkillMasteries;
using Server.Systems.MahaonProfessions;

namespace Server.Systems.MahaonAi;

/// <summary>
///     Pushes the shard's *reference tables* to the website — metals, wood, leather,
///     houses. Third of the site bridges, same shape as <see cref="MahaonForumBridge"/>
///     and <see cref="MahaonWorldSnapshotBridge"/>: one-way, disabled cleanly when
///     unconfigured, fire-and-forget.
///
///     The point is that the website never hardcodes any of this. Hues, skill thresholds,
///     effect texts and house costs all come from the same tables the game itself reads,
///     so retuning a hue in MahaonMetalTable.cs changes the colour of the swatch on the
///     site without anyone touching the site. The site only supplies the pictures, which
///     it decodes from the client's own art files and tints with the hue numbers below.
/// </summary>
public static class MahaonReferenceBridge
{
    private static readonly ILogger _logger = LogFactory.GetLogger(typeof(MahaonReferenceBridge));
    private static HttpClient _httpClient;
    private static string _postUrl;

    // Item art the site tints per row. Ingot/log/leather are the same graphic for every
    // grade — only the hue tells them apart, exactly as in game.
    private const int IngotArtId = 0x1BF2;
    private const int LogArtId = 0x1BDD;
    private const int LeatherArtId = 0x1081;

    private const int SchemaVersion = 1;

    private static readonly TimeSpan FirstPushDelay = TimeSpan.FromSeconds(35);

    public static void Configure()
    {
        _postUrl = ServerConfiguration.GetOrUpdateSetting("mahaonReference.postUrl", "");

        if (IsEnabled)
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

            // Console command rather than a timer: these tables only change when someone
            // edits the shard's code, which no schedule can predict. The website's control
            // panel types this into the server's stdin when its "refresh" button is
            // pressed — the same channel "save" and "shutdown" already travel on, so the
            // game gains no new inbound surface.
            //
            // The handler runs on the console reader thread, so the actual build is posted
            // to the game loop: it reads World/craft state and must not touch any of that
            // off-thread.
            ConsoleInputHandler.RegisterCommand(
                ["tables", "reftables"],
                "Rebuilds the website's reference tables and pushes them.",
                _ => Core.LoopContext.Post(Push)
            );

            _logger.Information($"MahaonReferenceBridge enabled — posting to {_postUrl}.");
        }
        else
        {
            _logger.Information("MahaonReferenceBridge disabled (no mahaonReference.postUrl configured).");
        }
    }

    public static void Initialize()
    {
        if (!IsEnabled)
        {
            return;
        }

        // One push once the world is up, and that is the whole schedule. The payload
        // cannot change while the server runs, so repeating it would only ever resend
        // identical bytes; if the site missed this one, the panel's refresh button asks
        // for another.
        Timer.DelayCall(FirstPushDelay, Push);
    }

    public static bool IsEnabled => !string.IsNullOrEmpty(_postUrl);

    private static void Push()
    {
        // Built on the game loop, sent off it — see MahaonWorldSnapshotBridge for why.
        var reference = BuildReference();
        _ = SendAsync(reference);
    }

    private static ReferenceTables BuildReference() =>
        new()
        {
            SchemaVersion = SchemaVersion,
            BuiltAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Metals = BuildMetals(),
            Wood = BuildWood(),
            Leather = BuildLeather(),
            Houses = BuildHouses(),
            Crafts = BuildAllCrafts(),
            Professions = BuildProfessions(),
            Imbuing = BuildImbuing(),
            Gems = BuildGems(),
            GemMaxSockets = GemSocketingSystem.MaxSocketsPerItem,
            Skills = BuildSkills(),
            Masteries = BuildMasteries(),
            MasteryMinSkill = MasteryInfo.MinSkillRequirement,
            Bestiary = BuildBestiary(),
            LootSamples = LootSampleCount,
            Spells = BuildSpells()
        };

    private static List<MetalRow> BuildMetals()
    {
        var rows = new List<MetalRow>(MahaonMetalTable.Data.Count);

        foreach (var (metal, info) in MahaonMetalTable.Data)
        {
            rows.Add(
                new MetalRow
                {
                    Key = metal.ToString(),
                    Name = info.RuName,
                    Skill = info.MiningSkillRequired,
                    Tier = info.Tier.ToString(),
                    Effect = info.EffectDescription,
                    Magic = info.UsableForMagicItems,
                    Hue = info.Hue,
                    ArtId = IngotArtId
                }
            );
        }

        rows.Sort((a, b) => a.Skill.CompareTo(b.Skill));
        return rows;
    }

    private static List<ResourceRow> BuildWood()
    {
        var rows = new List<ResourceRow>();

        // Rarity here is not a label someone wrote down — it is the weight each species
        // carries in the drop table (MahaonResourceTiers.PickWeighted normalises the
        // weights of everything the current skill has unlocked). Reporting the share at
        // full skill, where every species is eligible, is the one figure that means the
        // same thing for every row.
        double totalWeight = 0;
        foreach (var (_, _, weight) in MahaonResourceTiers.WoodTiers)
        {
            totalWeight += weight;
        }

        foreach (var (type, minSkill, weight) in MahaonResourceTiers.WoodTiers)
        {
            var resource = CraftResources.GetFromType(type);
            var info = CraftResources.GetInfo(resource);

            rows.Add(
                new ResourceRow
                {
                    Key = resource.ToString(),
                    Name = info?.Name ?? type.Name,
                    Skill = (int)minSkill,
                    Hue = info?.Hue ?? 0,
                    ArtId = LogArtId,
                    Weight = weight,
                    ChanceAtMaxSkill = totalWeight > 0 ? weight / totalWeight * 100.0 : 0,
                    Effects = BuildEffects(info?.AttributeInfo)
                }
            );
        }

        return rows;
    }

    /// <summary>
    ///     Turns a resource's CraftAttributeInfo into a plain list of the bonuses that are
    ///     actually set. Zero fields are dropped rather than listed as zeroes: a grade with
    ///     nothing set gives no bonus at all, and an empty list says that honestly instead
    ///     of padding the row with twenty "0" columns.
    /// </summary>
    private static List<EffectRow> BuildEffects(CraftAttributeInfo attributes)
    {
        var effects = new List<EffectRow>();

        if (attributes == null)
        {
            return effects;
        }

        void Add(string label, int value, string suffix = null)
        {
            if (value != 0)
            {
                effects.Add(new EffectRow { Label = label, Value = value, Suffix = suffix });
            }
        }

        Add("Физическая защита", attributes.ArmorPhysicalResist);
        Add("Защита от огня", attributes.ArmorFireResist);
        Add("Защита от холода", attributes.ArmorColdResist);
        Add("Защита от яда", attributes.ArmorPoisonResist);
        Add("Защита от энергии", attributes.ArmorEnergyResist);
        Add("Прочность брони", attributes.ArmorDurability, "%");
        Add("Удача от брони", attributes.ArmorLuck);
        Add("Золото от брони", attributes.ArmorGoldIncrease, "%");
        Add("Требования брони", attributes.ArmorLowerRequirements, "% ниже");

        Add("Урон огнём", attributes.WeaponFireDamage, "%");
        Add("Урон холодом", attributes.WeaponColdDamage, "%");
        Add("Урон ядом", attributes.WeaponPoisonDamage, "%");
        Add("Урон энергией", attributes.WeaponEnergyDamage, "%");
        Add("Урон хаосом", attributes.WeaponChaosDamage, "%");
        Add("Прямой урон", attributes.WeaponDirectDamage, "%");
        Add("Прочность оружия", attributes.WeaponDurability, "%");
        Add("Удача от оружия", attributes.WeaponLuck);
        Add("Золото от оружия", attributes.WeaponGoldIncrease, "%");
        Add("Требования оружия", attributes.WeaponLowerRequirements, "% ниже");

        // Runic values come in min/max pairs and only make sense together.
        if (attributes.RunicMinAttributes != 0 || attributes.RunicMaxAttributes != 0)
        {
            effects.Add(
                new EffectRow
                {
                    Label = "Руник: свойств",
                    Value = attributes.RunicMinAttributes,
                    ValueMax = attributes.RunicMaxAttributes
                }
            );
        }

        if (attributes.RunicMinIntensity != 0 || attributes.RunicMaxIntensity != 0)
        {
            effects.Add(
                new EffectRow
                {
                    Label = "Руник: сила",
                    Value = attributes.RunicMinIntensity,
                    ValueMax = attributes.RunicMaxIntensity,
                    Suffix = "%"
                }
            );
        }

        return effects;
    }

    private static List<ResourceRow> BuildLeather()
    {
        // Leather has no skill gate of its own — the grade comes from what you skinned —
        // so Skill stays null here rather than being filled with a meaningless zero.
        var resources = new[]
        {
            CraftResource.RegularLeather,
            CraftResource.SpinedLeather,
            CraftResource.HornedLeather,
            CraftResource.BarbedLeather
        };

        var rows = new List<ResourceRow>(resources.Length);

        foreach (var resource in resources)
        {
            var info = CraftResources.GetInfo(resource);

            rows.Add(
                new ResourceRow
                {
                    Key = resource.ToString(),
                    Name = info?.Name ?? resource.ToString(),
                    Skill = null,
                    Hue = info?.Hue ?? 0,
                    ArtId = LeatherArtId,
                    Effects = BuildEffects(info?.AttributeInfo)
                }
            );
        }

        return rows;
    }

    // Building a craft list means constructing one of every craftable item to read its
    // real stats, so each system is built once and kept: the payload is static for the
    // lifetime of the server, and there is no reason to churn hundreds of items through
    // the world on every push.
    private static readonly Dictionary<CraftSystem, List<CraftRow>> _craftCache = new();

    /// <summary>
    ///     Every craft system the shard has, in the order the site shows them: the big
    ///     ones people actually plan around first, the odds and ends last. Labels live
    ///     here rather than on the site so adding a twelfth system stays a one-line change
    ///     on this side only.
    /// </summary>
    private static List<CraftSystemRow> BuildAllCrafts()
    {
        var systems = new (string Id, string Label, CraftSystem System)[]
        {
            ("blacksmithy", "Кузнечное", DefBlacksmithy.CraftSystem),
            ("tailoring", "Портняжное", DefTailoring.CraftSystem),
            ("carpentry", "Столярное", DefCarpentry.CraftSystem),
            ("bowfletching", "Лучное", DefBowFletching.CraftSystem),
            ("tinkering", "Инженерное", DefTinkering.CraftSystem),
            ("alchemy", "Алхимия", DefAlchemy.CraftSystem),
            ("inscription", "Каллиграфия", DefInscription.CraftSystem),
            ("cooking", "Кулинария", DefCooking.CraftSystem),
            ("masonry", "Каменное", DefMasonry.CraftSystem),
            ("glassblowing", "Стеклодувное", DefGlassblowing.CraftSystem),
            ("cartography", "Картография", DefCartography.CraftSystem)
        };

        var rows = new List<CraftSystemRow>(systems.Length);

        foreach (var (id, label, system) in systems)
        {
            // A system that never initialised (disabled by era, say) is skipped rather
            // than shipped as an empty tab.
            if (system == null)
            {
                continue;
            }

            rows.Add(
                new CraftSystemRow
                {
                    Id = id,
                    Label = label,
                    Skill = system.MainSkill.ToString(),
                    Items = BuildCrafts(system)
                }
            );
        }

        return rows;
    }

    /// <summary>
    ///     Every recipe in a craft system, with the skill window, what it consumes, and —
    ///     where the result is a weapon or a piece of armour — the stats it comes out with.
    /// </summary>
    private static List<CraftRow> BuildCrafts(CraftSystem system)
    {
        if (system == null)
        {
            return new List<CraftRow>();
        }

        if (_craftCache.TryGetValue(system, out var cached))
        {
            return cached;
        }

        var rows = new List<CraftRow>(system.CraftItems.Count);

        foreach (var craft in system.CraftItems)
        {
            var resources = new List<CraftResourceRow>(craft.Resources.Count);

            foreach (var res in craft.Resources)
            {
                resources.Add(
                    new CraftResourceRow
                    {
                        Name = ResolveText(res.Name),
                        NameNumber = res.Name?.Number ?? 0,
                        Amount = res.Amount
                    }
                );
            }

            var row = new CraftRow
            {
                Name = ResolveText(craft.NameNumber, craft.NameString),
                NameNumber = craft.NameNumber,
                Group = ResolveText(craft.GroupNameNumber, craft.GroupNameString),
                GroupNumber = craft.GroupNameNumber,
                Resources = resources
            };

            if (craft.Skills.Count > 0)
            {
                var skill = craft.Skills[0];
                row.Skill = skill.SkillToMake.ToString();
                row.MinSkill = skill.MinSkill;
                row.MaxSkill = skill.MaxSkill;
            }

            FillItemStats(row, craft.ItemType);
            rows.Add(row);
        }

        rows.Sort(
            (a, b) =>
            {
                var byGroup = string.CompareOrdinal(a.Group, b.Group);
                return byGroup != 0 ? byGroup : a.MinSkill.CompareTo(b.MinSkill);
            }
        );

        _craftCache[system] = rows;
        return rows;
    }

    /// <summary>
    ///     Reads a craftable's own numbers off a throwaway instance. There is no static
    ///     table for these — damage, speed and armour rating are instance properties with
    ///     era-dependent overrides — so the only answer guaranteed to match what a player
    ///     actually gets is the one the item itself reports.
    ///
    ///     Runs on the game loop (the caller is a Timer callback), and the sample is
    ///     deleted immediately, so nothing is left in the world.
    /// </summary>
    private static void FillItemStats(CraftRow row, Type itemType)
    {
        if (itemType == null)
        {
            return;
        }

        // The craft system builds the result with ItemType.CreateInstance<Item>(), which
        // finds a constructor and fills in its optional arguments — so using the same call
        // yields exactly the item a player would receive. Activator.CreateInstance(type)
        // only ever matches a truly parameterless constructor, which most craftables do
        // not have; that was leaving a quarter of the recipes without stats or a picture.
        //
        // The pre-check keeps the engine's helper from logging "no constructor" for the
        // handful that genuinely need arguments (spell scrolls want a spell id): those are
        // skipped quietly and still get their row.
        if (!HasConstructibleWithoutArguments(itemType))
        {
            return;
        }

        Item sample = null;

        try
        {
            sample = itemType.CreateInstance<Item>();

            if (sample == null)
            {
                return;
            }

            row.ArtId = sample.ItemID;

            switch (sample)
            {
                case BaseWeapon weapon:
                    {
                        row.Kind = "weapon";
                        row.MinDamage = weapon.MinDamage;
                        row.MaxDamage = weapon.MaxDamage;
                        row.Speed = weapon.Speed;
                        row.StrRequirement = weapon.StrRequirement;
                        row.UsesSkill = weapon.Skill.ToString();
                        row.TwoHanded = weapon.Layer == Layer.TwoHanded;
                        row.MaxRange = weapon.MaxRange;
                        break;
                    }

                case BaseArmor armor:
                    {
                        row.Kind = "armor";
                        row.ArmorRating = armor.ArmorBase;
                        row.StrRequirement = armor.StrRequirement;

                        // Base values, before the resource bonus: the resource half is
                        // already its own table on the site, and folding iron's zeroes in
                        // here would just double-count once someone reads both.
                        row.PhysicalResist = armor.BasePhysicalResistance;
                        row.FireResist = armor.BaseFireResistance;
                        row.ColdResist = armor.BaseColdResistance;
                        row.PoisonResist = armor.BasePoisonResistance;
                        row.EnergyResist = armor.BaseEnergyResistance;
                        break;
                    }

                case BasePotion potion:
                    {
                        row.Kind = "potion";
                        row.PotionEffect = potion.PotionEffect.ToString();
                        FillPotionStats(row, potion);
                        break;
                    }

                default:
                    {
                        row.Kind = "item";
                        break;
                    }
            }
        }
        catch
        {
            // Not every craftable has a parameterless constructor, and a recipe we cannot
            // sample is still worth listing with its skill and resources — just without
            // stats. Better a row with a gap than a missing row.
        }
        finally
        {
            sample?.Delete();
        }
    }

    /// <summary>
    ///     Whether the type can be built with no arguments supplied — either a
    ///     parameterless constructor or one whose every parameter is optional.
    /// </summary>
    private static bool HasConstructibleWithoutArguments(Type type)
    {
        foreach (var ctor in type.GetConstructors())
        {
            var usable = true;

            foreach (var parameter in ctor.GetParameters())
            {
                if (!parameter.IsOptional)
                {
                    usable = false;
                    break;
                }
            }

            if (usable)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     What a potion actually does. Every family keeps its numbers on its own base
    ///     class rather than in one shared table, so this reads whichever one applies and
    ///     leaves the rest null — a potion with no recognised family still gets its row,
    ///     just without a effect line.
    /// </summary>
    private static void FillPotionStats(CraftRow row, BasePotion potion)
    {
        switch (potion)
        {
            case BaseHealPotion heal:
                {
                    row.MinEffect = heal.MinHeal;
                    row.MaxEffect = heal.MaxHeal;
                    row.EffectLabel = "лечит";
                    row.DelaySeconds = heal.Delay;
                    break;
                }

            case BaseRefreshPotion refresh:
                {
                    // Refresh is a fraction of max stamina, not a flat number.
                    row.MinEffect = (int)Math.Round(refresh.Refresh * 100);
                    row.EffectLabel = "восстанавливает выносливость";
                    row.EffectSuffix = "%";
                    break;
                }

            case BaseAgilityPotion agility:
                {
                    row.MinEffect = agility.DexOffset;
                    row.EffectLabel = "ловкость";
                    row.DurationSeconds = agility.Duration.TotalSeconds;
                    break;
                }

            case BaseStrengthPotion strength:
                {
                    row.MinEffect = strength.StrOffset;
                    row.EffectLabel = "сила";
                    row.DurationSeconds = strength.Duration.TotalSeconds;
                    break;
                }

            case BaseExplosionPotion explosion:
                {
                    row.MinEffect = explosion.MinDamage;
                    row.MaxEffect = explosion.MaxDamage;
                    row.EffectLabel = "урон";
                    break;
                }

            case BaseConflagrationPotion conflagration:
                {
                    row.MinEffect = conflagration.MinDamage;
                    row.MaxEffect = conflagration.MaxDamage;
                    row.EffectLabel = "урон";
                    break;
                }

            case BaseConfusionBlastPotion confusion:
                {
                    // Confusion blast has no damage of its own — the radius is the whole
                    // point of the thing.
                    row.MinEffect = confusion.Radius;
                    row.EffectLabel = "радиус";
                    break;
                }

            case BasePoisonPotion poison:
                {
                    row.EffectLabel = "отравляет";
                    row.EffectText = poison.Poison?.Name;
                    break;
                }

            case BaseCurePotion cure:
                {
                    // A cure potion is a per-poison-level chance table, not one number.
                    var levels = cure.LevelInfo;

                    if (levels is { Length: > 0 })
                    {
                        var parts = new List<string>(levels.Length);

                        foreach (var level in levels)
                        {
                            parts.Add($"{level.Poison?.Name}: {level.Chance * 100:0}%");
                        }

                        row.EffectLabel = "снимает яд";
                        row.EffectText = string.Join(", ", parts);
                    }

                    break;
                }
        }
    }

    private static string ResolveText(TextDefinition text) =>
        text == null ? null : ResolveText(text.Number, text.String);

    private static string ResolveText(int number, string fallback) =>
        number > 0 ? Localization.GetText(number) : fallback;

    /// <summary>
    ///     What a category grants as its signature passive, in the shard's own terms. Only
    ///     Thief and Warrior have one implemented (ProfessionPerkSystem); Craft and Magic
    ///     were named but left alone because both effects already exist as unrestricted
    ///     mechanics, and the remaining two have nothing. Saying so is more use than an
    ///     empty column.
    /// </summary>
    private static readonly Dictionary<ProfessionCategory, string> CategoryPerks = new()
    {
        [ProfessionCategory.Thief] = "30% шанс увернуться от удара",
        [ProfessionCategory.Warrior] = "«Стойкость»: пока здоровья меньше 30%, входящий урон режется вдвое"
    };

    private static readonly Dictionary<ProfessionCategory, string> CategoryNames = new()
    {
        [ProfessionCategory.Magic] = "Магия",
        [ProfessionCategory.Craft] = "Ремесло",
        [ProfessionCategory.Warrior] = "Воин",
        [ProfessionCategory.Thief] = "Вор",
        [ProfessionCategory.Bard] = "Бард",
        [ProfessionCategory.Ranger] = "Следопыт",

        // Категории, дописанные под школы, которых на исходном шарде не было.
        [ProfessionCategory.Necromancy] = "Некромантия",
        [ProfessionCategory.Faith] = "Вера",
        [ProfessionCategory.Mysticism] = "Мистицизм",
        [ProfessionCategory.Weaving] = "Плетение чар",
        [ProfessionCategory.Bushido] = "Бусидо",
        [ProfessionCategory.Ninjitsu] = "Ниндзюцу"
    };

    /// <summary>
    ///     The shard's professions. Stat figures are caps to grow toward, not starting
    ///     values, and the skill list is the primary category's kit — the one that caps at
    ///     120 instead of the usual 100. The secondary category grants its signature
    ///     passive rather than its skills, so its skills are deliberately not listed here.
    /// </summary>
    private static List<ProfessionRow> BuildProfessions()
    {
        var rows = new List<ProfessionRow>(ProfessionData.All.Count);

        foreach (var (profession, info) in ProfessionData.All)
        {
            var skills = new List<string>();

            if (ProfessionData.CategorySkills.TryGetValue(info.Category, out var kit))
            {
                foreach (var skill in kit)
                {
                    skills.Add(skill.ToString());
                }
            }

            rows.Add(
                new ProfessionRow
                {
                    Key = profession.ToString(),
                    Name = info.MaleName,
                    FemaleName = info.FemaleName,
                    Category = CategoryNames.GetValueOrDefault(info.Category, info.Category.ToString()),
                    SecondaryCategory =
                        CategoryNames.GetValueOrDefault(info.SecondaryCategory, info.SecondaryCategory.ToString()),
                    Perk = CategoryPerks.GetValueOrDefault(info.SecondaryCategory),
                    PrimaryPerk = CategoryPerks.GetValueOrDefault(info.Category),
                    Str = info.StrCap,
                    Dex = info.DexCap,
                    Int = info.IntCap,
                    Skills = skills
                }
            );
        }

        return rows;
    }

    private static readonly string[] ImbuingTierNames = { "низкий", "средний", "высокий" };

    /// <summary>
    ///     Imbuable properties with all three intensity tiers side by side: the skill each
    ///     needs, what it actually grants, and what it costs in gold and materials.
    /// </summary>
    private static List<ImbuingRow> BuildImbuing()
    {
        var rows = new List<ImbuingRow>(ImbuingPropertyTable.All.Length);

        foreach (var property in ImbuingPropertyTable.All)
        {
            var appliesTo = new List<string>(property.AppliesTo.Length);

            foreach (var type in property.AppliesTo)
            {
                appliesTo.Add(
                    type switch
                    {
                        ImbuingItemType.Weapon  => "оружие",
                        ImbuingItemType.Armor   => "броня",
                        ImbuingItemType.Jewelry => "украшения",
                        _                       => type.ToString()
                    }
                );
            }

            var tiers = new List<ImbuingTierRow>(ImbuingTierNames.Length);

            for (var i = 0; i < ImbuingTierNames.Length; i++)
            {
                var materials = new List<CraftResourceRow>();

                if (property.MaterialsPerTier is { Length: > 0 } perTier && i < perTier.Length && perTier[i] != null)
                {
                    foreach (var (material, amount) in perTier[i])
                    {
                        materials.Add(
                            new CraftResourceRow
                            {
                                Name = ResolveText(CraftItem.LabelNumber(material)),
                                NameNumber = CraftItem.LabelNumber(material),
                                Amount = amount
                            }
                        );
                    }
                }

                tiers.Add(
                    new ImbuingTierRow
                    {
                        Tier = ImbuingTierNames[i],
                        Skill = ValueAt(property.MinSkillPerTier, i),
                        Magnitude = ValueAt(property.MagnitudePerTier, i),
                        Gold = ValueAt(property.GoldPerTier, i),
                        Materials = materials
                    }
                );
            }

            rows.Add(
                new ImbuingRow
                {
                    Id = property.Id,
                    Name = property.RuName,
                    AppliesTo = appliesTo,
                    Tiers = tiers
                }
            );
        }

        return rows;
    }

    private static int ValueAt(int[] values, int index) =>
        values != null && index < values.Length ? values[index] : 0;

    /// <summary>
    ///     Socketable gems: which bonus each grants and how much. The two that roll a skill
    ///     instead of granting a set amount are marked as such rather than reported as
    ///     zero.
    /// </summary>
    private static List<GemRow> BuildGems()
    {
        var rows = new List<GemRow>(GemSocketingSystem.SocketableGems.Length);

        foreach (var gemType in GemSocketingSystem.SocketableGems)
        {
            var bonus = GemSocketingSystem.BonusTypeFor(gemType);

            if (bonus == null)
            {
                continue;
            }

            var rolls = bonus is GemBonusType.RandomSkill or GemBonusType.RandomSkillGreater;
            var magnitude = GemSocketingSystem.MagnitudeFor(bonus.Value);

            // Regen bonuses are stored as a fraction of the tick rate, everything else as
            // flat points — showing 0.15 next to a 5 would read as the smaller bonus.
            var percent = bonus is GemBonusType.HitsRegen or GemBonusType.StamRegen or GemBonusType.ManaRegen;

            var row = new GemRow
            {
                Name = ResolveText(CraftItem.LabelNumber(gemType)) ?? gemType.Name,
                NameNumber = CraftItem.LabelNumber(gemType),
                Bonus = GemSocketingSystem.RuBonusName(bonus.Value),
                Magnitude = percent ? magnitude * 100 : magnitude,
                Percent = percent,
                Rolls = rolls
            };

            FillGemArt(row, gemType);
            rows.Add(row);
        }

        return rows;
    }

    private static void FillGemArt(GemRow row, Type gemType)
    {
        if (!HasConstructibleWithoutArguments(gemType))
        {
            return;
        }

        Item sample = null;

        try
        {
            sample = gemType.CreateInstance<Item>();

            if (sample != null)
            {
                row.ArtId = sample.ItemID;
                row.Hue = sample.Hue;
            }
        }
        catch
        {
            // No art is a cosmetic loss; the row still carries the numbers.
        }
        finally
        {
            sample?.Delete();
        }
    }

    /// <summary>
    ///     Every skill, with what training it grows and how fast. The gain figures are the
    ///     engine's own: StrGain/DexGain/IntGain are the relative chances that a successful
    ///     use raises that stat, and GainFactor scales how readily the skill itself rises.
    ///     All of it comes from Data/skills.json through SkillInfo — nothing is restated
    ///     here, so retuning that file moves the website too.
    /// </summary>
    private static List<SkillRow> BuildSkills()
    {
        var masterySkills = new HashSet<SkillName>();

        foreach (var info in MasteryInfo.Infos)
        {
            masterySkills.Add(info.MasterySkill);
        }

        var rows = new List<SkillRow>(SkillInfo.Table.Length);

        for (var i = 0; i < SkillInfo.Table.Length; i++)
        {
            var info = SkillInfo.Table[i];

            if (info == null)
            {
                continue;
            }

            rows.Add(
                new SkillRow
                {
                    Id = info.SkillID,
                    Name = info.Name,
                    NameNumber = SkillNameCliloc(info.SkillID),
                    Title = info.Title,
                    PrimaryStat = info.PrimaryStat.ToString(),
                    SecondaryStat = info.SecondaryStat.ToString(),

                    // SkillInfo divides the scales by 100 on construction but keeps the
                    // gains as given, so these are the raw numbers from the json.
                    StrGain = info.StrGain,
                    DexGain = info.DexGain,
                    IntGain = info.IntGain,
                    GainFactor = info.GainFactor,
                    HasMastery = masterySkills.Contains((SkillName)info.SkillID)
                }
            );
        }

        rows.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        return rows;
    }

    // The client's skill names live in one contiguous cliloc run — the same mapping AOS.cs
    // uses when it labels a skill.
    private static int SkillNameCliloc(int skillId) => 1044060 + skillId;

    /// <summary>
    ///     Skill masteries: which skill unlocks what, and whether the entry is an active
    ///     spell or a passive that simply applies once the mastery is chosen.
    /// </summary>
    private static List<MasteryRow> BuildMasteries()
    {
        var rows = new List<MasteryRow>(MasteryInfo.Infos.Count);

        foreach (var info in MasteryInfo.Infos)
        {
            var row = new MasteryRow
            {
                Skill = info.MasterySkill.ToString(),
                SkillNumber = SkillNameCliloc((int)info.MasterySkill),
                SpellId = info.SpellID,
                Passive = info.Passive,
                PassiveName = info.Passive ? info.PassiveSpell.ToString() : null
            };

            // A spell's display name lives on an instance (Spell.Info), and building one
            // needs a caster — not worth constructing spells just to read a label. The
            // class name with its suffix trimmed is the same word in every case here
            // (InspireSpell -> Inspire, PierceMove -> Pierce). A passive has no spell type
            // at all, so its enum name is the only handle there is.
            row.Name = info.SpellType != null
                ? TrimSpellSuffix(info.SpellType.Name)
                : info.PassiveSpell.ToString();

            rows.Add(row);
        }

        rows.Sort(
            (a, b) =>
            {
                var bySkill = string.CompareOrdinal(a.Skill, b.Skill);
                return bySkill != 0 ? bySkill : a.SpellId.CompareTo(b.SpellId);
            }
        );

        return rows;
    }

    private static string TrimSpellSuffix(string typeName)
    {
        if (typeName.EndsWith("Spell", StringComparison.Ordinal))
        {
            return typeName[..^5];
        }

        return typeName.EndsWith("Move", StringComparison.Ordinal) ? typeName[..^4] : typeName;
    }

    // How many times each creature's loot is rolled to work out what it drops. There is no
    // static drop table to read — GenerateLoot rolls at the moment of death — so the only
    // honest answer is a measured one, and the site says plainly that it is measured over
    // this many rolls rather than dressing it up as the true probability.
    private const int LootSampleCount = 20;

    private static List<CreatureRow> _bestiaryCache;

    /// <summary>
    ///     Every creature the shard can spawn, with the numbers it actually carries and
    ///     what came out of its corpse across a fixed number of loot rolls.
    /// </summary>
    private static List<CreatureRow> BuildBestiary()
    {
        if (_bestiaryCache != null)
        {
            return _bestiaryCache;
        }

        var rows = new List<CreatureRow>();
        var creatureType = typeof(BaseCreature);

        foreach (var type in creatureType.Assembly.GetTypes())
        {
            if (type.IsAbstract || !creatureType.IsAssignableFrom(type) || !HasConstructibleWithoutArguments(type))
            {
                continue;
            }

            var row = BuildCreature(type);

            if (row != null)
            {
                rows.Add(row);
            }
        }

        rows.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        return _bestiaryCache = rows;
    }

    private static CreatureRow BuildCreature(Type type)
    {
        BaseCreature sample = null;

        try
        {
            sample = type.CreateInstance<BaseCreature>();

            if (sample == null)
            {
                return null;
            }

            var row = new CreatureRow
            {
                Type = type.Name,
                Name = string.IsNullOrEmpty(sample.Name) ? type.Name : sample.Name,
                Body = sample.Body.BodyID,
                Hue = sample.Hue,
                Hits = sample.HitsMax,
                Str = sample.RawStr,
                Dex = sample.RawDex,
                Int = sample.RawInt,
                DamageMin = sample.DamageMin,
                DamageMax = sample.DamageMax,
                PhysicalResist = sample.PhysicalResistanceSeed,
                FireResist = sample.FireResistSeed,
                ColdResist = sample.ColdResistSeed,
                PoisonResist = sample.PoisonResistSeed,
                EnergyResist = sample.EnergyResistSeed,
                Fame = sample.Fame,
                Karma = sample.Karma,
                Tamable = sample.Tamable,
                TameSkill = sample.Tamable ? sample.MinTameSkill : 0
            };

            SampleLoot(sample, row);
            return row;
        }
        catch
        {
            // A creature whose constructor needs more than we can supply simply does not
            // make it into the list; nothing else in the payload depends on it.
            return null;
        }
        finally
        {
            sample?.Delete();
        }
    }

    /// <summary>
    ///     Rolls death loot a fixed number of times and counts how often each kind of item
    ///     turned up. Gold is tracked as a range instead of a count — "gold appeared 20
    ///     times out of 20" says nothing, how much of it did is the useful part.
    /// </summary>
    private static void SampleLoot(BaseCreature sample, CreatureRow row)
    {
        var seen = new Dictionary<int, (string Name, int Count)>();
        var goldMin = int.MaxValue;
        var goldMax = 0;

        for (var roll = 0; roll < LootSampleCount; roll++)
        {
            try
            {
                // A corpse holds both halves: LootPackEntry refuses to build an entry
                // whose AtSpawnTime flag disagrees with the phase, and gold is declared as
                // spawn-time — it goes into the pack when the creature appears and simply
                // stays there. Rolling only the death phase is why gold first came out
                // empty for all but a handful of creatures.
                sample.GenerateLoot(true);
                sample.GenerateLoot(false);
            }
            catch
            {
                break; // a creature that cannot roll loot still keeps its stats
            }

            var backpack = sample.Backpack;

            if (backpack == null)
            {
                continue;
            }

            var gold = 0;
            var countedThisRoll = new HashSet<int>();

            foreach (var item in backpack.Items)
            {
                if (item is Gold goldPile)
                {
                    gold += goldPile.Amount;
                    continue;
                }

                var label = CraftItem.LabelNumber(item.GetType());

                // Counted once per roll, not once per item: two daggers in one corpse is
                // still "this roll produced daggers".
                if (!countedThisRoll.Add(label))
                {
                    continue;
                }

                var name = ResolveText(label) ?? item.GetType().Name;

                if (seen.TryGetValue(label, out var current))
                {
                    seen[label] = (current.Name, current.Count + 1);
                }
                else
                {
                    seen[label] = (name, 1);
                }
            }

            if (gold > 0)
            {
                goldMin = Math.Min(goldMin, gold);
                goldMax = Math.Max(goldMax, gold);
            }

            // Empty the pack for the next roll by deleting its contents, not the pack
            // itself: AddLoot reuses whatever Backpack it finds, and a deleted container
            // silently swallows everything dropped into it — which is why every creature
            // first reported zero gold.
            for (var i = backpack.Items.Count - 1; i >= 0; i--)
            {
                backpack.Items[i].Delete();
            }
        }

        var loot = new List<LootRow>(seen.Count);

        foreach (var (label, entry) in seen)
        {
            loot.Add(new LootRow { Name = entry.Name, NameNumber = label, Seen = entry.Count });
        }

        loot.Sort((a, b) => b.Seen.CompareTo(a.Seen));

        row.Loot = loot;
        row.GoldMin = goldMin == int.MaxValue ? 0 : goldMin;
        row.GoldMax = goldMax;
    }

    // Scroll art is one contiguous run of item graphics indexed by the spell's own id —
    // the same base MagicFocusGump uses to draw a spell icon.
    private const int ScrollArtBase = 0x1F2E;

    /// <summary>
    ///     Where each school's names live in the client's string table, and which registry
    ///     ids belong to it. Verified against the shard's own registration order in
    ///     Spells/Initializer.cs rather than assumed: Ninjitsu in particular does not start
    ///     where the neighbouring runs would suggest.
    ///
    ///     Only Magery has a matching run of prose descriptions (1061290 onward). The other
    ///     books show requirements rather than a description in-client, so nothing is
    ///     invented for them — DescNumber simply stays 0.
    /// </summary>
    private static readonly (string Id, string Label, int First, int Last, int NameBase, int DescBase)[] SpellSchools =
    {
        ("magery", "Магия", 0, 63, 3002011, 1061290),
        ("necromancy", "Некромантия", 100, 116, 1060509, 0),
        ("chivalry", "Рыцарство", 200, 209, 1060585, 0),
        ("bushido", "Бусидо", 400, 405, 1060595, 0),
        ("ninjitsu", "Ниндзюцу", 500, 507, 1060610, 0),
        ("spellweaving", "Плетение чар", 600, 615, 1071026, 0),
        ("mysticism", "Мистицизм", 677, 692, 1031678, 0)
    };

    /// <summary>
    ///     Every registered spell, whatever school it belongs to. Schools whose abilities
    ///     have no scroll at all (chivalry, bushido, ninjitsu, spellweaving) sit in the same
    ///     table as the ones that do, marked as uncraftable — a separate page for them
    ///     would only repeat this content with the scroll column missing.
    /// </summary>
    private static List<SpellRow> BuildSpells()
    {
        var scrollTypes = new HashSet<int>();

        // Which spells actually have a scroll: taken from the inscription recipes rather
        // than from a list of our own, so the two can never disagree.
        foreach (var craft in DefInscription.CraftSystem?.CraftItems ?? new List<CraftItem>())
        {
            if (craft.ItemType != null && typeof(SpellScroll).IsAssignableFrom(craft.ItemType))
            {
                var scroll = TryMakeScroll(craft.ItemType);

                if (scroll != null)
                {
                    scrollTypes.Add(scroll.SpellID);
                    scroll.Delete();
                }
            }
        }

        var rows = new List<SpellRow>();

        foreach (var (schoolId, label, first, last, nameBase, descBase) in SpellSchools)
        {
            for (var id = first; id <= last; id++)
            {
                var spell = SpellRegistry.NewSpell(id, null, null);
                var move = spell == null ? SpellRegistry.GetSpecialMove(id) : null;

                // Bushido and ninjitsu register several of their abilities as SpecialMove
                // rather than Spell — they are toggles, not casts. Leaving them out would
                // have shown those two books as three entries each instead of six and
                // eight.
                if (spell?.Info == null && move == null)
                {
                    continue;
                }

                var offset = id - first;

                var row = new SpellRow
                {
                    Id = id,
                    School = schoolId,
                    SchoolLabel = label,
                    Name = spell?.Info.Name ?? TrimSpellSuffix(move.GetType().Name),
                    NameNumber = nameBase + offset,
                    DescNumber = descBase > 0 ? descBase + offset : 0,
                    Mantra = spell?.Info.Mantra,
                    ScrollArtId = ScrollArtBase + id,
                    Scroll = scrollTypes.Contains(id),
                    Reagents = BuildReagents(spell?.Info.Reagents)
                };

                if (spell is MagerySpell magery)
                {
                    // Circle is one-based on the page; the enum starts at zero.
                    row.Circle = (int)magery.Circle + 1;
                }

                if (move != null)
                {
                    row.Mana = move.BaseMana;
                    row.MinSkill = move.RequiredSkill;
                }
                else
                {
                    try
                    {
                        row.Mana = spell.GetMana();
                        spell.GetCastSkills(out var min, out _);
                        row.MinSkill = min;
                    }
                    catch
                    {
                        // A few abilities compute their cost from the caster; without one
                        // they simply have no number to show.
                    }
                }

                rows.Add(row);
            }
        }

        return rows;
    }

    private static SpellScroll TryMakeScroll(Type type)
    {
        if (!HasConstructibleWithoutArguments(type))
        {
            return null;
        }

        try
        {
            return type.CreateInstance<SpellScroll>();
        }
        catch
        {
            return null;
        }
    }

    private static List<CraftResourceRow> BuildReagents(Type[] reagents)
    {
        var rows = new List<CraftResourceRow>();

        if (reagents == null)
        {
            return rows;
        }

        foreach (var reagent in reagents)
        {
            if (reagent == null)
            {
                continue;
            }

            var label = CraftItem.LabelNumber(reagent);
            rows.Add(
                new CraftResourceRow
                {
                    Name = ResolveText(label) ?? reagent.Name,
                    NameNumber = label,
                    Amount = 1
                }
            );
        }

        return rows;
    }

    private static List<HouseRow> BuildHouses()
    {
        var art = BuildMiniHouseArtLookup();
        var entries = Core.EJ ? HousePlacementEntry.HousesEJ : HousePlacementEntry.ClassicHouses;
        var rows = new List<HouseRow>(entries.Length);

        foreach (var entry in entries)
        {
            rows.Add(
                new HouseRow
                {
                    Name = Localization.GetText(entry.Description),
                    NameNumber = entry.Description,
                    Cost = entry.Cost,
                    Storage = entry.Storage,
                    Lockdowns = entry.Lockdowns,
                    Vendors = entry.Vendors,
                    MultiId = entry.MultiID,
                    ArtId = art.GetValueOrDefault(entry.Description, 0)
                }
            );
        }

        rows.Sort((a, b) => a.Cost.CompareTo(b.Cost));
        return rows;
    }

    /// <summary>
    ///     Fallback picture for a house, used only when the site cannot draw the real
    ///     multi (an old client with no MultiCollection.uop). The miniature-house rewards
    ///     are little models of real house designs, keyed by the same cliloc the placement
    ///     entry uses, so they map across for free.
    ///
    ///     Only the single-graphic ones are offered. A big design's model is assembled
    ///     from 4 or 16 separate tiles laid out in a grid (see MiniHouseAddon.Construct),
    ///     and handing over just the first one shows a corner of a house rather than a
    ///     house — worse than showing nothing.
    /// </summary>
    private static Dictionary<int, int> BuildMiniHouseArtLookup()
    {
        var lookup = new Dictionary<int, int>();

        foreach (MiniHouseType type in Enum.GetValues<MiniHouseType>())
        {
            var info = MiniHouseInfo.GetInfo(type);

            if (info?.Graphics is { Length: 1 } graphics && !lookup.ContainsKey(info.LabelNumber))
            {
                lookup[info.LabelNumber] = graphics[0];
            }
        }

        return lookup;
    }

    private static async Task SendAsync(ReferenceTables reference)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(_postUrl, reference).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _logger.Information("MahaonReferenceBridge: reference tables pushed.");
            }
            else
            {
                _logger.Warning($"MahaonReferenceBridge: site returned {(int)response.StatusCode}.");
            }
        }
        catch (Exception ex)
        {
            _logger.Warning($"MahaonReferenceBridge: failed to push reference tables — {ex.Message}");
        }
    }

    private sealed class ReferenceTables
    {
        [JsonPropertyName("schemaVersion")] public int SchemaVersion { get; set; }
        [JsonPropertyName("builtAt")] public long BuiltAt { get; set; }
        [JsonPropertyName("metals")] public List<MetalRow> Metals { get; set; }
        [JsonPropertyName("wood")] public List<ResourceRow> Wood { get; set; }
        [JsonPropertyName("leather")] public List<ResourceRow> Leather { get; set; }
        [JsonPropertyName("houses")] public List<HouseRow> Houses { get; set; }
        [JsonPropertyName("crafts")] public List<CraftSystemRow> Crafts { get; set; }
        [JsonPropertyName("professions")] public List<ProfessionRow> Professions { get; set; }
        [JsonPropertyName("imbuing")] public List<ImbuingRow> Imbuing { get; set; }
        [JsonPropertyName("gems")] public List<GemRow> Gems { get; set; }
        [JsonPropertyName("gemMaxSockets")] public int GemMaxSockets { get; set; }
        [JsonPropertyName("skills")] public List<SkillRow> Skills { get; set; }
        [JsonPropertyName("masteries")] public List<MasteryRow> Masteries { get; set; }
        [JsonPropertyName("masteryMinSkill")] public int MasteryMinSkill { get; set; }
        [JsonPropertyName("bestiary")] public List<CreatureRow> Bestiary { get; set; }
        [JsonPropertyName("lootSamples")] public int LootSamples { get; set; }
        [JsonPropertyName("spells")] public List<SpellRow> Spells { get; set; }
    }

    private sealed class SpellRow
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("school")] public string School { get; set; }
        [JsonPropertyName("schoolLabel")] public string SchoolLabel { get; set; }
        [JsonPropertyName("circle")] public int? Circle { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("nameNumber")] public int NameNumber { get; set; }
        [JsonPropertyName("descNumber")] public int DescNumber { get; set; }
        [JsonPropertyName("mantra")] public string Mantra { get; set; }
        [JsonPropertyName("mana")] public int Mana { get; set; }
        [JsonPropertyName("minSkill")] public double MinSkill { get; set; }
        [JsonPropertyName("reagents")] public List<CraftResourceRow> Reagents { get; set; }
        [JsonPropertyName("scrollArtId")] public int ScrollArtId { get; set; }
        [JsonPropertyName("scroll")] public bool Scroll { get; set; }
    }

    private sealed class CreatureRow
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("body")] public int Body { get; set; }
        [JsonPropertyName("hue")] public int Hue { get; set; }
        [JsonPropertyName("hits")] public int Hits { get; set; }
        [JsonPropertyName("str")] public int Str { get; set; }
        [JsonPropertyName("dex")] public int Dex { get; set; }
        [JsonPropertyName("int")] public int Int { get; set; }
        [JsonPropertyName("damageMin")] public int DamageMin { get; set; }
        [JsonPropertyName("damageMax")] public int DamageMax { get; set; }
        [JsonPropertyName("physicalResist")] public int PhysicalResist { get; set; }
        [JsonPropertyName("fireResist")] public int FireResist { get; set; }
        [JsonPropertyName("coldResist")] public int ColdResist { get; set; }
        [JsonPropertyName("poisonResist")] public int PoisonResist { get; set; }
        [JsonPropertyName("energyResist")] public int EnergyResist { get; set; }
        [JsonPropertyName("fame")] public int Fame { get; set; }
        [JsonPropertyName("karma")] public int Karma { get; set; }
        [JsonPropertyName("tamable")] public bool Tamable { get; set; }
        [JsonPropertyName("tameSkill")] public double TameSkill { get; set; }
        [JsonPropertyName("loot")] public List<LootRow> Loot { get; set; }
        [JsonPropertyName("goldMin")] public int GoldMin { get; set; }
        [JsonPropertyName("goldMax")] public int GoldMax { get; set; }
    }

    private sealed class LootRow
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("nameNumber")] public int NameNumber { get; set; }
        [JsonPropertyName("seen")] public int Seen { get; set; }
    }

    private sealed class SkillRow
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("nameNumber")] public int NameNumber { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("primaryStat")] public string PrimaryStat { get; set; }
        [JsonPropertyName("secondaryStat")] public string SecondaryStat { get; set; }
        [JsonPropertyName("strGain")] public double StrGain { get; set; }
        [JsonPropertyName("dexGain")] public double DexGain { get; set; }
        [JsonPropertyName("intGain")] public double IntGain { get; set; }
        [JsonPropertyName("gainFactor")] public double GainFactor { get; set; }
        [JsonPropertyName("hasMastery")] public bool HasMastery { get; set; }
    }

    private sealed class MasteryRow
    {
        [JsonPropertyName("skill")] public string Skill { get; set; }
        [JsonPropertyName("skillNumber")] public int SkillNumber { get; set; }
        [JsonPropertyName("spellId")] public int SpellId { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("nameNumber")] public int NameNumber { get; set; }
        [JsonPropertyName("passive")] public bool Passive { get; set; }
        [JsonPropertyName("passiveName")] public string PassiveName { get; set; }
    }

    private sealed class ProfessionRow
    {
        [JsonPropertyName("key")] public string Key { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("femaleName")] public string FemaleName { get; set; }
        [JsonPropertyName("category")] public string Category { get; set; }
        [JsonPropertyName("secondaryCategory")] public string SecondaryCategory { get; set; }
        [JsonPropertyName("perk")] public string Perk { get; set; }
        [JsonPropertyName("primaryPerk")] public string PrimaryPerk { get; set; }
        [JsonPropertyName("str")] public int Str { get; set; }
        [JsonPropertyName("dex")] public int Dex { get; set; }
        [JsonPropertyName("int")] public int Int { get; set; }
        [JsonPropertyName("skills")] public List<string> Skills { get; set; }
    }

    private sealed class ImbuingRow
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("appliesTo")] public List<string> AppliesTo { get; set; }
        [JsonPropertyName("tiers")] public List<ImbuingTierRow> Tiers { get; set; }
    }

    private sealed class ImbuingTierRow
    {
        [JsonPropertyName("tier")] public string Tier { get; set; }
        [JsonPropertyName("skill")] public int Skill { get; set; }
        [JsonPropertyName("magnitude")] public int Magnitude { get; set; }
        [JsonPropertyName("gold")] public int Gold { get; set; }
        [JsonPropertyName("materials")] public List<CraftResourceRow> Materials { get; set; }
    }

    private sealed class GemRow
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("nameNumber")] public int NameNumber { get; set; }
        [JsonPropertyName("bonus")] public string Bonus { get; set; }
        [JsonPropertyName("magnitude")] public double Magnitude { get; set; }
        [JsonPropertyName("percent")] public bool Percent { get; set; }
        [JsonPropertyName("rolls")] public bool Rolls { get; set; }
        [JsonPropertyName("artId")] public int ArtId { get; set; }
        [JsonPropertyName("hue")] public int Hue { get; set; }
    }

    private sealed class CraftSystemRow
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("label")] public string Label { get; set; }
        [JsonPropertyName("skill")] public string Skill { get; set; }
        [JsonPropertyName("items")] public List<CraftRow> Items { get; set; }
    }

    private sealed class MetalRow
    {
        [JsonPropertyName("key")] public string Key { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("skill")] public int Skill { get; set; }
        [JsonPropertyName("tier")] public string Tier { get; set; }
        [JsonPropertyName("effect")] public string Effect { get; set; }
        [JsonPropertyName("magic")] public bool Magic { get; set; }
        [JsonPropertyName("hue")] public int Hue { get; set; }
        [JsonPropertyName("artId")] public int ArtId { get; set; }
    }

    private sealed class ResourceRow
    {
        [JsonPropertyName("key")] public string Key { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("skill")] public int? Skill { get; set; }
        [JsonPropertyName("hue")] public int Hue { get; set; }
        [JsonPropertyName("artId")] public int ArtId { get; set; }
        [JsonPropertyName("weight")] public double? Weight { get; set; }
        [JsonPropertyName("chanceAtMaxSkill")] public double? ChanceAtMaxSkill { get; set; }
        [JsonPropertyName("effects")] public List<EffectRow> Effects { get; set; }
    }

    private sealed class EffectRow
    {
        [JsonPropertyName("label")] public string Label { get; set; }
        [JsonPropertyName("value")] public int Value { get; set; }
        [JsonPropertyName("valueMax")] public int? ValueMax { get; set; }
        [JsonPropertyName("suffix")] public string Suffix { get; set; }
    }

    private sealed class CraftRow
    {
        [JsonPropertyName("name")] public string Name { get; set; }

        // The site keeps its own Russian cliloc table and prefers it over the English text
        // above; sending the number is what lets it do the swap without the game having to
        // change what it reads itself.
        [JsonPropertyName("nameNumber")] public int NameNumber { get; set; }
        [JsonPropertyName("group")] public string Group { get; set; }
        [JsonPropertyName("groupNumber")] public int GroupNumber { get; set; }
        [JsonPropertyName("skill")] public string Skill { get; set; }
        [JsonPropertyName("minSkill")] public double MinSkill { get; set; }
        [JsonPropertyName("maxSkill")] public double MaxSkill { get; set; }
        [JsonPropertyName("resources")] public List<CraftResourceRow> Resources { get; set; }
        [JsonPropertyName("artId")] public int ArtId { get; set; }
        [JsonPropertyName("kind")] public string Kind { get; set; }

        [JsonPropertyName("minDamage")] public int? MinDamage { get; set; }
        [JsonPropertyName("maxDamage")] public int? MaxDamage { get; set; }
        [JsonPropertyName("speed")] public float? Speed { get; set; }
        [JsonPropertyName("usesSkill")] public string UsesSkill { get; set; }
        [JsonPropertyName("twoHanded")] public bool? TwoHanded { get; set; }
        [JsonPropertyName("maxRange")] public int? MaxRange { get; set; }

        [JsonPropertyName("armorRating")] public int? ArmorRating { get; set; }
        [JsonPropertyName("physicalResist")] public int? PhysicalResist { get; set; }
        [JsonPropertyName("fireResist")] public int? FireResist { get; set; }
        [JsonPropertyName("coldResist")] public int? ColdResist { get; set; }
        [JsonPropertyName("poisonResist")] public int? PoisonResist { get; set; }
        [JsonPropertyName("energyResist")] public int? EnergyResist { get; set; }

        [JsonPropertyName("strRequirement")] public int? StrRequirement { get; set; }

        [JsonPropertyName("potionEffect")] public string PotionEffect { get; set; }
        [JsonPropertyName("effectLabel")] public string EffectLabel { get; set; }
        [JsonPropertyName("effectText")] public string EffectText { get; set; }
        [JsonPropertyName("effectSuffix")] public string EffectSuffix { get; set; }
        [JsonPropertyName("minEffect")] public int? MinEffect { get; set; }
        [JsonPropertyName("maxEffect")] public int? MaxEffect { get; set; }
        [JsonPropertyName("delaySeconds")] public double? DelaySeconds { get; set; }
        [JsonPropertyName("durationSeconds")] public double? DurationSeconds { get; set; }
    }

    private sealed class CraftResourceRow
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("nameNumber")] public int NameNumber { get; set; }
        [JsonPropertyName("amount")] public int Amount { get; set; }
    }

    private sealed class HouseRow
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("nameNumber")] public int NameNumber { get; set; }
        [JsonPropertyName("cost")] public int Cost { get; set; }
        [JsonPropertyName("storage")] public int Storage { get; set; }
        [JsonPropertyName("lockdowns")] public int Lockdowns { get; set; }
        [JsonPropertyName("vendors")] public int Vendors { get; set; }
        [JsonPropertyName("multiId")] public int MultiId { get; set; }
        [JsonPropertyName("artId")] public int ArtId { get; set; }
    }
}

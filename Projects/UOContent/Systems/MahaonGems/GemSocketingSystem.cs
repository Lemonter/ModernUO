using System;
using System.Collections.Generic;
using Server.Gumps;
using Server.Items;

namespace Server.Systems.MahaonGems;

public enum GemBonusType
{
    HitsRegen,
    StamRegen,
    ManaRegen,
    FireResist,
    ColdResist,
    PoisonResist,
    EnergyResist, // no gem grants this anymore — StarSapphire moved to RandomSkillGreater below
    PoisonDamage,
    RandomSkill,
    RandomSkillGreater // Star Sapphire — "Diamond+": same mechanic, bigger roll
}

/// <summary>Broad groupings a Diamond/StarSapphire socketer can steer their roll toward
/// once skilled enough (see GemSocketingSystem.CanChooseCategory) — same conceptual split
/// as MahaonSkillTree's top-level categories, kept independent here rather than sharing
/// those exact arrays since this only needs a coarse "which corner of the skill list", not
/// the tree's own category shape.</summary>
public enum GemSkillCategory
{
    Combat,
    Magic,
    Thieving,
    Craft,
    Wilderness,
    Misc
}

public readonly struct GemSocket
{
    public readonly GemBonusType Type;
    public readonly double Magnitude;
    public readonly SkillName? Skill; // only meaningful for RandomSkill (diamond)

    public GemSocket(GemBonusType type, double magnitude, SkillName? skill = null)
    {
        Type = type;
        Magnitude = magnitude;
        Skill = skill;
    }
}

/// <summary>
///     Side table for socketed gem bonuses — same reasoning as SoulStoneSocketing, can't
///     add fields to every stock weapon/armor/jewelry class. Regular gems give a small
///     fixed bonus of one kind; diamonds roll a random skill bonus whose size depends on
///     the socketer's combined Tinkering + Item Identification + Arms Lore (max 360 = 3×120
///     = the biggest possible roll).
/// </summary>
public class GemSocketingSystem : GenericPersistence
{
    private static GemSocketingSystem _instance;

    // Public so the website's reference page can state the limit instead of repeating the
    // number in a second place that would quietly go stale.
    public const int MaxSocketsPerItem = 3;
    private const double MaxCombinedSkillForDiamond = 360.0; // Tinkering + ItemID + ArmsLore, all at 120

    private static readonly Dictionary<Item, List<GemSocket>> Sockets = new();
    private static readonly Dictionary<Item, List<SkillMod>> ActiveSkillMods = new();

    public GemSocketingSystem() : base("MahaonGemSockets", 1)
    {
    }

    public static void Configure()
    {
        _instance = new GemSocketingSystem();

        // Deserialize only restores the raw Sockets data — the engine sets Item.Parent
        // directly while loading equipped items, bypassing OnItemAdded/OnItemRemoved (the
        // only place OnEquipChanged is normally called from), so an already-equipped
        // diamond's EquippedSkillMod would otherwise never get recreated after a restart.
        // WorldLoad fires after every persistence (Mobiles/Items included) has finished, so
        // Parent is reliable here.
        EventSink.WorldLoad += ReapplyEquippedBonuses;
    }

    private static void ReapplyEquippedBonuses()
    {
        foreach (var (item, sockets) in Sockets)
        {
            if (item.Parent is Mobile owner)
            {
                foreach (var socket in sockets)
                {
                    ApplyOne(owner, item, socket);
                }
            }
        }
    }

    public static int SocketCount(Item item) => Sockets.TryGetValue(item, out var list) ? list.Count : 0;

    /// <summary>
    ///     Во что вообще можно вставить камень: во всё, что надевается.
    ///
    ///     Раньше проверка была по типу — «BaseWeapon, BaseArmor или BaseJewel», — и мимо
    ///     неё проходило всё, что не наследует эти три базы: роба, плащ, шляпа, сапоги,
    ///     штаны, пояс, колчан, талисман, книга заклинаний. Половина надетого на персонаже
    ///     инкрустации не принимала вовсе, хотя бонус от неё считается по надетому и
    ///     никакой разницы, из какого класса предмет, для этого нет.
    ///
    ///     Признак теперь один и по существу: у предмета есть слой экипировки. Волосы,
    ///     борода, рюкзак и всё, что за пределами пользовательских слоёв (верховое
    ///     животное, банк), сюда не попадают.
    /// </summary>
    public static bool IsSocketable(Item item) =>
        item is { Deleted: false } &&
        item.Layer >= Layer.FirstValid &&
        item.Layer <= Layer.LastUserValid &&
        item.Layer is not (Layer.Hair or Layer.FacialHair or Layer.Backpack);

    /// <summary>Read-only view of what's actually socketed — GetProperties on
    /// BaseWeapon/BaseArmor/BaseJewel calls this to show it in the item's tooltip (there was
    /// previously no way for a player to see what a gem gave without checking dev notes).</summary>
    public static IReadOnlyList<GemSocket> GetSockets(Item item) =>
        Sockets.TryGetValue(item, out var list) ? list : System.Array.Empty<GemSocket>();

    /// <summary>Called from BaseWeapon/BaseArmor/BaseJewel.GetProperties — was previously
    /// no way for a player to see what a socketed gem actually gave without checking dev
    /// notes.</summary>
    public static void AddPropertyLines(Item item, IPropertyList list)
    {
        foreach (var socket in GetSockets(item))
        {
            var isSkillGem = socket.Type is GemBonusType.RandomSkill or GemBonusType.RandomSkillGreater;

            var desc = isSkillGem && socket.Skill != null
                ? $"{Server.Systems.MahaonCombat.MahaonSkillTree.RuSkillName(socket.Skill.Value)} +{socket.Magnitude:0.#}"
                : $"{RuBonusName(socket.Type)} +{socket.Magnitude:0.#}";

            list.Add($"Камень: {desc}");
        }
    }

    public static string RuBonusName(GemBonusType type) => type switch
    {
        GemBonusType.HitsRegen         => "реген здоровья",
        GemBonusType.StamRegen         => "реген выносливости",
        GemBonusType.ManaRegen         => "реген маны",
        GemBonusType.FireResist        => "защита от огня",
        GemBonusType.ColdResist        => "защита от холода",
        GemBonusType.PoisonResist      => "защита от яда",
        GemBonusType.EnergyResist      => "защита от энергии",
        GemBonusType.PoisonDamage      => "урон ядом",
        GemBonusType.RandomSkill       => "навык",
        GemBonusType.RandomSkillGreater => "навык",
        _                               => type.ToString()
    };

    /// <summary>Fixed bonus magnitude per gem type — every gem of a kind gives the same
    /// amount; there's nothing to roll except for the diamond's skill bonus.</summary>
    public static GemBonusType? BonusTypeFor(Type gemType)
    {
        if (gemType == typeof(Tourmaline))
        {
            return GemBonusType.HitsRegen;
        }

        if (gemType == typeof(Amber))
        {
            return GemBonusType.StamRegen;
        }

        if (gemType == typeof(Amethyst))
        {
            return GemBonusType.ManaRegen;
        }

        if (gemType == typeof(Ruby))
        {
            return GemBonusType.FireResist;
        }

        if (gemType == typeof(Sapphire))
        {
            return GemBonusType.ColdResist;
        }

        if (gemType == typeof(Emerald))
        {
            return GemBonusType.PoisonResist;
        }

        if (gemType == typeof(StarSapphire))
        {
            return GemBonusType.RandomSkillGreater;
        }

        if (gemType == typeof(Citrine))
        {
            return GemBonusType.PoisonDamage;
        }

        if (gemType == typeof(Diamond))
        {
            return GemBonusType.RandomSkill;
        }

        return null;
    }

    /// <summary>Every gem kind the socketing system accepts, in the order the website
    /// lists them: regen first, then resists, then the two that roll a skill.</summary>
    public static readonly Type[] SocketableGems =
    {
        typeof(Tourmaline), typeof(Amber), typeof(Amethyst),
        typeof(Ruby), typeof(Sapphire), typeof(Emerald),
        typeof(Citrine), typeof(Diamond), typeof(StarSapphire)
    };

    /// <summary>The fixed amount a gem of this bonus type grants. Zero for the two that
    /// roll instead of granting a set value.</summary>
    public static double MagnitudeFor(GemBonusType type) => FixedMagnitude(type);

    private static double FixedMagnitude(GemBonusType type) => type switch
    {
        GemBonusType.HitsRegen    => 0.15, // 15% faster regen tick per gem, stacks additively
        GemBonusType.StamRegen    => 0.15,
        GemBonusType.ManaRegen    => 0.15,
        GemBonusType.FireResist   => 5,
        GemBonusType.ColdResist   => 5,
        GemBonusType.PoisonResist => 5,
        GemBonusType.EnergyResist => 5,
        GemBonusType.PoisonDamage => 2,
        _                         => 0
    };

    /// <summary>
    ///     Tries to socket a gem into an item. For everything but a diamond/star sapphire,
    ///     the bonus is fixed. For those two, rolls a random skill (optionally narrowed to
    ///     one category — see CanChooseCategory) and a bonus size scaled by the socketer's
    ///     own Tinkering + Item Identification + Arms Lore; star sapphire's roll runs
    ///     noticeably higher than diamond's ("Diamond+").
    /// </summary>
    public static bool TrySocket(Mobile socketer, Item target, Item gem, GemSkillCategory? category = null)
    {
        var bonusType = BonusTypeFor(gem.GetType());
        if (bonusType == null)
        {
            return false;
        }

        if (!Sockets.TryGetValue(target, out var list))
        {
            Sockets[target] = list = new List<GemSocket>();
        }

        if (list.Count >= MaxSocketsPerItem)
        {
            return false;
        }

        GemSocket socket;

        if (bonusType is GemBonusType.RandomSkill or GemBonusType.RandomSkillGreater)
        {
            var combined = socketer.Skills[SkillName.Tinkering].Value +
                           socketer.Skills[SkillName.ItemID].Value +
                           socketer.Skills[SkillName.ArmsLore].Value;

            var scale = Math.Clamp(combined / MaxCombinedSkillForDiamond, 0.0, 1.0);

            var pool = category != null && CategorySkills.TryGetValue(category.Value, out var categorySkills)
                ? categorySkills
                : AllSkillNames;

            var skill = Utility.RandomElement(pool);

            // A weak roll might only be a fraction of a point; a maxed-out jeweler can hit
            // as much as +5 (diamond) or +8 (star sapphire) to a skill in one go.
            var magnitude = bonusType == GemBonusType.RandomSkillGreater
                ? Math.Round(0.3 + scale * 7.7, 1)
                : Math.Round(0.2 + scale * 4.8, 1);

            socket = new GemSocket(bonusType.Value, magnitude, skill);
        }
        else
        {
            socket = new GemSocket(bonusType.Value, FixedMagnitude(bonusType.Value));
        }

        list.Add(socket);

        if (target.Parent is Mobile owner)
        {
            ApplyOne(owner, target, socket);
        }

        return true;
    }

    /// <summary>Shared entry point for both socketing flows (GemEncrustingTool's 3-step
    /// target chain and a gem's own direct-insert double-click) — the skill-check/crack-on-
    /// fail roll, then either an immediate TrySocket or, for a diamond/star sapphire
    /// socketer skilled enough, a category-choice gump first.</summary>
    public static void BeginSocket(Mobile from, Item item, Item gem)
    {
        if (!from.CheckSkill(SkillName.Tinkering, 0.0, 100.0))
        {
            from.SendMessage(0x22, "Не получилось — камень треснул при вставке.");
            gem.Consume(); // was Delete() — destroyed the WHOLE stack instead of just the one gem being socketed
            return;
        }

        var bonusType = BonusTypeFor(gem.GetType());

        var isSkillGem = bonusType is GemBonusType.RandomSkill or GemBonusType.RandomSkillGreater;

        if (isSkillGem && CanChooseCategory(from))
        {
            from.SendGump(new GemCategoryChoiceGump(from, item, gem));
            return;
        }

        FinishSocket(from, item, gem, null);
    }

    public static void FinishSocket(Mobile from, Item item, Item gem, GemSkillCategory? category)
    {
        if (gem.Deleted || item.Deleted)
        {
            return;
        }

        if (TrySocket(from, item, gem, category))
        {
            from.SendMessage(0x59, $"Ты вставляешь {gem.Name ?? gem.GetType().Name} в {item.Name ?? "предмет"}.");
            gem.Consume(); // was Delete() — destroyed the WHOLE stack instead of just the one gem being socketed
        }
        else
        {
            from.SendMessage("Не получилось.");
        }
    }

    // 100 Tinkering specifically (not the combined 3-skill roll formula above) — shard
    // owner's own call: a master jeweler can steer WHERE the roll lands, not just how big.
    public static bool CanChooseCategory(Mobile socketer) => socketer.Skills[SkillName.Tinkering].Value >= 100.0;

    public static string RuCategoryName(GemSkillCategory category) => category switch
    {
        GemSkillCategory.Combat     => "Бой",
        GemSkillCategory.Magic      => "Магия",
        GemSkillCategory.Thieving   => "Воровство",
        GemSkillCategory.Craft      => "Ремёсла",
        GemSkillCategory.Wilderness => "Дикая природа",
        GemSkillCategory.Misc       => "Прочее",
        _                           => category.ToString()
    };

    private static readonly SkillName[] AllSkillNames = BuildAllSkillNames();

    private static SkillName[] BuildAllSkillNames()
    {
        var values = Enum.GetValues<SkillName>();
        var list = new List<SkillName>(values.Length);
        list.AddRange(values);
        return list.ToArray();
    }

    private static readonly Dictionary<GemSkillCategory, SkillName[]> CategorySkills = new()
    {
        [GemSkillCategory.Combat] = new[]
        {
            SkillName.Wrestling, SkillName.Swords, SkillName.Macing, SkillName.Fencing, SkillName.Archery,
            SkillName.Throwing, SkillName.Parry, SkillName.Tactics, SkillName.Anatomy, SkillName.ArmsLore,
            SkillName.Focus, SkillName.Bushido, SkillName.Ninjitsu
        },
        [GemSkillCategory.Magic] = new[]
        {
            SkillName.Magery, SkillName.EvalInt, SkillName.MagicResist, SkillName.Meditation, SkillName.Necromancy,
            SkillName.Chivalry, SkillName.Spellweaving, SkillName.Mysticism, SkillName.SpiritSpeak
        },
        [GemSkillCategory.Thieving] = new[]
        {
            SkillName.Snooping, SkillName.Stealing, SkillName.Stealth, SkillName.Lockpicking, SkillName.Poisoning,
            SkillName.Begging, SkillName.DetectHidden, SkillName.Hiding, SkillName.RemoveTrap
        },
        [GemSkillCategory.Craft] = new[]
        {
            SkillName.Blacksmith, SkillName.Tailoring, SkillName.Carpentry, SkillName.Tinkering, SkillName.Fletching,
            SkillName.Cooking, SkillName.Alchemy, SkillName.Inscribe, SkillName.Cartography, SkillName.Imbuing
        },
        [GemSkillCategory.Wilderness] = new[]
        {
            SkillName.AnimalLore, SkillName.AnimalTaming, SkillName.Herding, SkillName.Veterinary, SkillName.Fishing,
            SkillName.Lumberjacking, SkillName.Mining, SkillName.Tracking, SkillName.Forensics, SkillName.Camping,
            SkillName.ItemID, SkillName.TasteID
        },
        [GemSkillCategory.Misc] = new[]
        {
            SkillName.Musicianship, SkillName.Discordance, SkillName.Peacemaking, SkillName.Provocation, SkillName.Healing
        }
    };

    /// <summary>Call from PlayerMobile.OnItemAdded/OnItemRemoved, same as soul stones.</summary>
    public static void OnEquipChanged(Mobile owner, Item item, bool equipped)
    {
        if (!Sockets.TryGetValue(item, out var sockets))
        {
            return;
        }

        if (equipped)
        {
            foreach (var socket in sockets)
            {
                ApplyOne(owner, item, socket);
            }
        }
        else
        {
            RemoveAll(owner, item);
        }
    }

    private static void ApplyOne(Mobile owner, Item item, GemSocket socket)
    {
        // Was `!= GemBonusType.RandomSkill` only — Star Sapphire rolls RandomSkillGreater
        // (see BonusTypeFor), a DIFFERENT enum value, so every Star Sapphire socket hit
        // this early-return and never got an EquippedSkillMod at all. The tooltip
        // (AddPropertyLines above) already correctly handled both types, so the bonus
        // looked real ("написано на предмете") while doing nothing when worn.
        var isSkillGem = socket.Type is GemBonusType.RandomSkill or GemBonusType.RandomSkillGreater;

        if (!isSkillGem || socket.Skill == null)
        {
            return; // everything else is read live off the socket list, no mod object needed
        }

        var mod = new EquippedSkillMod(
            socket.Skill.Value, $"MahaonGem-{item.Serial}-{socket.Skill}", true, socket.Magnitude, item, owner
        );

        owner.AddSkillMod(mod);

        if (!ActiveSkillMods.TryGetValue(item, out var mods))
        {
            ActiveSkillMods[item] = mods = new List<SkillMod>();
        }

        mods.Add(mod);
    }

    private static void RemoveAll(Mobile owner, Item item)
    {
        if (!ActiveSkillMods.TryGetValue(item, out var mods))
        {
            return;
        }

        foreach (var mod in mods)
        {
            owner.RemoveSkillMod(mod);
        }

        ActiveSkillMods.Remove(item);
    }

    // -- Live bonus readers — sum every matching socket across everything worn -----------

    private static double SumBonus(Mobile m, GemBonusType type)
    {
        double total = 0;

        foreach (var item in m.Items)
        {
            if (!Sockets.TryGetValue(item, out var sockets))
            {
                continue;
            }

            foreach (var socket in sockets)
            {
                if (socket.Type == type)
                {
                    total += socket.Magnitude;
                }
            }
        }

        return total;
    }

    public static int GetRegenBonus(Mobile m, GemBonusType regenType) => (int)SumBonus(m, regenType);

    /// <summary>Multiply a regen TimeSpan interval by this to get the sped-up interval —
    /// capped so stacking gems can't collapse the tick down to nothing.</summary>
    public static double GetRegenSpeedMultiplier(Mobile m, GemBonusType regenType)
    {
        var bonus = SumBonus(m, regenType);
        return 1.0 - System.Math.Clamp(bonus, 0.0, 0.75);
    }

    public static int GetResistanceBonus(Mobile m, GemBonusType resistType) => (int)SumBonus(m, resistType);

    public static int GetPoisonDamageBonus(Mobile m) => (int)SumBonus(m, GemBonusType.PoisonDamage);

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version
        writer.WriteEncodedInt(Sockets.Count);

        foreach (var (item, list) in Sockets)
        {
            writer.Write(item);
            writer.WriteEncodedInt(list.Count);

            foreach (var socket in list)
            {
                writer.WriteEncodedInt((int)socket.Type);
                writer.Write(socket.Magnitude);
                writer.Write(socket.Skill != null);
                if (socket.Skill != null)
                {
                    writer.WriteEncodedInt((int)socket.Skill.Value);
                }
            }
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // version

        var itemCount = reader.ReadEncodedInt();
        for (var i = 0; i < itemCount; i++)
        {
            var item = reader.ReadEntity<Item>();
            var socketCount = reader.ReadEncodedInt();

            var list = new List<GemSocket>();
            for (var j = 0; j < socketCount; j++)
            {
                var type = (GemBonusType)reader.ReadEncodedInt();
                var magnitude = reader.ReadDouble();
                SkillName? skill = reader.ReadBool() ? (SkillName)reader.ReadEncodedInt() : null;

                list.Add(new GemSocket(type, magnitude, skill));
            }

            if (item != null)
            {
                Sockets[item] = list;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using Server.Items;
using Server.Targeting;

namespace Server.Systems.MahaonSoulStones;

/// <summary>
///     Side table for socketed soul-stone bonuses, since we can't add fields to every stock
///     weapon/armor/jewelry class. Call <see cref="OnEquipChanged" /> from
///     PlayerMobile.OnItemAdded/OnItemRemoved to keep the actual EquippedSkillMods in sync
///     with what's equipped.
/// </summary>
public class SoulStoneSocketing : GenericPersistence
{
    private static SoulStoneSocketing _instance;

    private const int MaxSocketsPerItem = 3;

    private static readonly Dictionary<Item, List<(SkillName? skill, StatType? stat, double bonus)>> Sockets = new();

    // One soul stone now grants a whole BUNDLE of bonuses at once (3 fixed skills + 1-3
    // stats, per MahaonSoulStone.ColorToSkills/ColorToStats) rather than a single skill-or-
    // stat pick — Sockets above stays a flat per-bonus list (ApplyOne/RemoveAll don't care
    // about grouping, they're all-or-nothing per item either way), but MaxSocketsPerItem is
    // about how many STONES have been inserted, not how many individual bonuses exist, so
    // that has to be tracked separately.
    private static readonly Dictionary<Item, int> SocketSlots = new();

    private static readonly Dictionary<Item, List<SkillMod>> ActiveSkillMods = new();
    private static readonly Dictionary<Item, List<StatMod>> ActiveStatMods = new();

    public SoulStoneSocketing() : base("MahaonSoulStoneSockets", 1)
    {
    }

    public static void Configure()
    {
        _instance = new SoulStoneSocketing();

        // Same root cause as GemSocketingSystem: the engine sets Item.Parent directly while
        // loading equipped items, bypassing OnItemAdded/OnItemRemoved (the only place
        // OnEquipChanged is normally called from), so an already-equipped item's
        // EquippedSkillMod/StatMod would otherwise never get recreated after a restart.
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
                foreach (var (skill, stat, bonus) in sockets)
                {
                    ApplyOne(owner, item, skill, stat, bonus);
                }
            }
        }
    }

    public static int SocketCount(Item item) => SocketSlots.GetValueOrDefault(item, 0);

    /// <summary>Read-only view of what's actually socketed — see
    /// GemSocketingSystem.GetSockets for the same reasoning on the gem side.</summary>
    public static IReadOnlyList<(SkillName? skill, StatType? stat, double bonus)> GetSockets(Item item) =>
        Sockets.TryGetValue(item, out var list) ? list : System.Array.Empty<(SkillName?, StatType?, double)>();

    /// <summary>Called from BaseWeapon/BaseArmor/BaseJewel.GetProperties, alongside
    /// GemSocketingSystem.AddPropertyLines.</summary>
    public static void AddPropertyLines(Item item, IPropertyList list)
    {
        foreach (var (skill, stat, bonus) in GetSockets(item))
        {
            var desc = skill != null
                ? $"{Server.Systems.MahaonCombat.MahaonSkillTree.RuSkillName(skill.Value)} +{bonus:0.#}"
                : $"{RuStatName(stat!.Value)} +{(int)bonus}";

            list.Add($"Камень души: {desc}");
        }
    }

    private static string RuStatName(StatType stat) => stat switch
    {
        StatType.Str => "Сила",
        StatType.Dex => "Ловкость",
        StatType.Int => "Интеллект",
        _            => stat.ToString()
    };

    /// <summary>Sockets one soul stone's whole bundle of bonuses (see
    /// MahaonSoulStone.ColorToSkills/ColorToStats) as a single slot — counts once against
    /// MaxSocketsPerItem regardless of how many individual skill/stat entries it contains.</summary>
    public static bool TrySocket(Item target, IReadOnlyList<(SkillName? skill, StatType? stat, double bonus)> bonuses)
    {
        var slotsUsed = SocketSlots.GetValueOrDefault(target, 0);

        if (slotsUsed >= MaxSocketsPerItem || bonuses.Count == 0)
        {
            return false;
        }

        if (!Sockets.TryGetValue(target, out var list))
        {
            Sockets[target] = list = new List<(SkillName?, StatType?, double)>();
        }

        list.AddRange(bonuses);
        SocketSlots[target] = slotsUsed + 1;

        // If it's already being worn, apply immediately rather than waiting for the next
        // equip/unequip cycle.
        if (target.Parent is Mobile owner)
        {
            foreach (var (skill, stat, bonus) in bonuses)
            {
                ApplyOne(owner, target, skill, stat, bonus);
            }
        }

        return true;
    }

    /// <summary>
    ///     Call from PlayerMobile.OnItemAdded/OnItemRemoved. Adds mods when a socketed item
    ///     gets equipped, removes them when it's taken off.
    /// </summary>
    public static void OnEquipChanged(Mobile owner, Item item, bool equipped)
    {
        if (!Sockets.TryGetValue(item, out var sockets))
        {
            return;
        }

        if (equipped)
        {
            foreach (var (skill, stat, bonus) in sockets)
            {
                ApplyOne(owner, item, skill, stat, bonus);
            }
        }
        else
        {
            RemoveAll(owner, item);
        }
    }

    private static void ApplyOne(Mobile owner, Item item, SkillName? skill, StatType? stat, double bonus)
    {
        if (skill != null)
        {
            var mod = new EquippedSkillMod(skill.Value, $"MahaonSoulStone-{item.Serial}-{skill}", true, bonus, item, owner);
            owner.AddSkillMod(mod);

            if (!ActiveSkillMods.TryGetValue(item, out var mods))
            {
                ActiveSkillMods[item] = mods = new List<SkillMod>();
            }

            mods.Add(mod);
        }
        else if (stat != null)
        {
            var mod = new StatMod(stat.Value, $"MahaonSoulStone-{item.Serial}-{stat}", (int)bonus, TimeSpan.Zero);
            owner.AddStatMod(mod);

            if (!ActiveStatMods.TryGetValue(item, out var mods))
            {
                ActiveStatMods[item] = mods = new List<StatMod>();
            }

            mods.Add(mod);
        }
    }

    private static void RemoveAll(Mobile owner, Item item)
    {
        if (ActiveSkillMods.TryGetValue(item, out var skillMods))
        {
            foreach (var mod in skillMods)
            {
                owner.RemoveSkillMod(mod);
            }

            ActiveSkillMods.Remove(item);
        }

        if (ActiveStatMods.TryGetValue(item, out var statMods))
        {
            foreach (var mod in statMods)
            {
                owner.RemoveStatMod(mod.Name);
            }

            ActiveStatMods.Remove(item);
        }
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(1); // version — 1 adds SocketSlots (stone count, not bonus count)
        writer.WriteEncodedInt(Sockets.Count);

        foreach (var (item, list) in Sockets)
        {
            writer.Write(item);
            writer.WriteEncodedInt(SocketSlots.GetValueOrDefault(item, 0));
            writer.WriteEncodedInt(list.Count);

            foreach (var (skill, stat, bonus) in list)
            {
                writer.Write(skill != null);
                if (skill != null)
                {
                    writer.WriteEncodedInt((int)skill.Value);
                }

                writer.Write(stat != null);
                if (stat != null)
                {
                    writer.WriteEncodedInt((int)stat.Value);
                }

                writer.Write(bonus);
            }
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        var version = reader.ReadEncodedInt();

        var itemCount = reader.ReadEncodedInt();
        for (var i = 0; i < itemCount; i++)
        {
            var item = reader.ReadEntity<Item>();
            var slots = version >= 1 ? reader.ReadEncodedInt() : 0;
            var socketCount = reader.ReadEncodedInt();

            var list = new List<(SkillName?, StatType?, double)>();
            for (var j = 0; j < socketCount; j++)
            {
                SkillName? skill = reader.ReadBool() ? (SkillName)reader.ReadEncodedInt() : null;
                StatType? stat = reader.ReadBool() ? (StatType)reader.ReadEncodedInt() : null;
                var bonus = reader.ReadDouble();

                list.Add((skill, stat, bonus));
            }

            if (item != null)
            {
                Sockets[item] = list;
                // Pre-version-1 saves (one bonus == one slot, the old model) fall back to
                // the flat bonus count so existing sockets don't silently become "free"
                // extra slots after this upgrade.
                SocketSlots[item] = version >= 1 ? slots : list.Count;
            }
        }
    }
}

/// <summary>
///     Targeting cursor used by MahaonSoulStone.OnDoubleClick to pick which gear piece to socket.
/// </summary>
public class SoulStoneSocketTarget : Target
{
    private readonly MahaonSoulStone _stone;

    public SoulStoneSocketTarget(MahaonSoulStone stone) : base(2, false, TargetFlags.None) => _stone = stone;

    protected override void OnTarget(Mobile from, object targeted)
    {
        if (_stone.Deleted)
        {
            return;
        }

        if (targeted is not Item item || !Systems.MahaonGems.GemSocketingSystem.IsSocketable(item))
        {
            from.SendMessage("Сюда нельзя вставить — только то, что надевают.");
            return;
        }

        if (!item.IsChildOf(from.Backpack) && item.Parent != from)
        {
            from.SendMessage("Предмет должен быть у тебя в руках или надет.");
            return;
        }

        if (SoulStoneSocketing.SocketCount(item) >= 3)
        {
            from.SendMessage("В этот предмет больше не вставить камней души — все слоты заняты.");
            return;
        }

        var bonus = MahaonSoulStone.SizeToBonus(_stone.Size);
        var skills = MahaonSoulStone.ColorToSkills(_stone.Color);
        var stats = MahaonSoulStone.ColorToStats(_stone.Color);

        var bundle = new List<(SkillName? skill, StatType? stat, double bonus)>(skills.Length + stats.Length);

        foreach (var skill in skills)
        {
            bundle.Add((skill, null, bonus));
        }

        foreach (var stat in stats)
        {
            bundle.Add((null, stat, bonus));
        }

        // Only the FIRST soul stone dyes the item — SocketCount before TrySocket below is
        // still the pre-insertion count, so 0 here means this is the first one.
        var isFirstStone = SoulStoneSocketing.SocketCount(item) == 0;

        if (SoulStoneSocketing.TrySocket(item, bundle))
        {
            if (isFirstStone)
            {
                item.Hue = MahaonSoulStone.ColorToHue(_stone.Color);
            }

            from.SendMessage(0x59, $"Ты вставляешь {SizeRu(_stone.Size)} {ColorRu(_stone.Color)} камень души в {item.Name ?? "предмет"}.");
            _stone.Delete();
        }
        else
        {
            from.SendMessage("Не получилось.");
        }
    }
    private static string SizeRu(SoulStoneSize size) => size switch
    {
        SoulStoneSize.Small  => "малый",
        SoulStoneSize.Medium => "средний",
        SoulStoneSize.Large  => "большой",
        SoulStoneSize.Giant  => "гигантский",
        _                    => size.ToString()
    };

    private static string ColorRu(SoulStoneColor color) => color switch
    {
        SoulStoneColor.Black => "чёрный",
        SoulStoneColor.Red   => "красный",
        SoulStoneColor.Blue  => "синий",
        SoulStoneColor.Green => "зелёный",
        SoulStoneColor.Gold  => "золотой",
        _                    => color.ToString()
    };
}

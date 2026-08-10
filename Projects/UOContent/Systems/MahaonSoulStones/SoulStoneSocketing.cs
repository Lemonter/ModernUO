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
    private static readonly Dictionary<Item, List<SkillMod>> ActiveSkillMods = new();
    private static readonly Dictionary<Item, List<StatMod>> ActiveStatMods = new();

    public SoulStoneSocketing() : base("MahaonSoulStoneSockets", 1)
    {
    }

    public static void Configure()
    {
        _instance = new SoulStoneSocketing();
    }

    public static int SocketCount(Item item) => Sockets.TryGetValue(item, out var list) ? list.Count : 0;

    public static bool TrySocket(Item target, SkillName? skill, StatType? stat, double bonus)
    {
        if (!Sockets.TryGetValue(target, out var list))
        {
            Sockets[target] = list = new List<(SkillName?, StatType?, double)>();
        }

        if (list.Count >= MaxSocketsPerItem)
        {
            return false;
        }

        list.Add((skill, stat, bonus));

        // If it's already being worn, apply immediately rather than waiting for the next
        // equip/unequip cycle.
        if (target.Parent is Mobile owner)
        {
            ApplyOne(owner, target, skill, stat, bonus);
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
        writer.WriteEncodedInt(0); // version
        writer.WriteEncodedInt(Sockets.Count);

        foreach (var (item, list) in Sockets)
        {
            writer.Write(item);
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
        reader.ReadEncodedInt(); // version

        var itemCount = reader.ReadEncodedInt();
        for (var i = 0; i < itemCount; i++)
        {
            var item = reader.ReadEntity<Item>();
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

        if (targeted is not Item item || item is not (BaseWeapon or BaseArmor or BaseJewel))
        {
            from.SendMessage("Сюда нельзя вставить — только оружие, броня и украшения.");
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
        var skill = MahaonSoulStone.ColorToSkill(_stone.Color);

        StatType? stat = null;
        if (skill == null)
        {
            stat = Utility.RandomList(StatType.Str, StatType.Dex, StatType.Int);
        }

        if (SoulStoneSocketing.TrySocket(item, skill, stat, bonus))
        {
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

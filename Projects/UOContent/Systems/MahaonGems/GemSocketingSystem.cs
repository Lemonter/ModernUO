using System;
using System.Collections.Generic;
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
    EnergyResist,
    PoisonDamage,
    RandomSkill
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

    private const int MaxSocketsPerItem = 3;
    private const double MaxCombinedSkillForDiamond = 360.0; // Tinkering + ItemID + ArmsLore, all at 120

    private static readonly Dictionary<Item, List<GemSocket>> Sockets = new();
    private static readonly Dictionary<Item, List<SkillMod>> ActiveSkillMods = new();

    public GemSocketingSystem() : base("MahaonGemSockets", 1)
    {
    }

    public static void Configure()
    {
        _instance = new GemSocketingSystem();
    }

    public static int SocketCount(Item item) => Sockets.TryGetValue(item, out var list) ? list.Count : 0;

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
            return GemBonusType.EnergyResist;
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
    ///     Tries to socket a gem into an item. For everything but a diamond, the bonus is
    ///     fixed. For a diamond, rolls a random skill and a bonus size scaled by the
    ///     socketer's own Tinkering + Item Identification + Arms Lore.
    /// </summary>
    public static bool TrySocket(Mobile socketer, Item target, Item gem)
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

        if (bonusType == GemBonusType.RandomSkill)
        {
            var combined = socketer.Skills[SkillName.Tinkering].Value +
                           socketer.Skills[SkillName.ItemID].Value +
                           socketer.Skills[SkillName.ArmsLore].Value;

            var scale = Math.Clamp(combined / MaxCombinedSkillForDiamond, 0.0, 1.0);
            var skill = Utility.RandomElement(AllSkillNames);

            // A weak roll might only be a fraction of a point; a maxed-out jeweler can hit
            // as much as +5 to a skill in one go.
            var magnitude = Math.Round(0.2 + scale * 4.8, 1);

            socket = new GemSocket(GemBonusType.RandomSkill, magnitude, skill);
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

    private static readonly SkillName[] AllSkillNames = BuildAllSkillNames();

    private static SkillName[] BuildAllSkillNames()
    {
        var values = Enum.GetValues<SkillName>();
        var list = new List<SkillName>(values.Length);
        list.AddRange(values);
        return list.ToArray();
    }

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
        if (socket.Type != GemBonusType.RandomSkill || socket.Skill == null)
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

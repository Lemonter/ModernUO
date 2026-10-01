using System;
using System.Collections.Generic;
using Server.Items;

namespace Server.Systems.MahaonImbuing;

/// <summary>
///     Ядро наложения чар — как в ванильной UO: выбираешь предмет → выбираешь свойство →
///     выбираешь ярус интенсивности (открыт по навыку) → тратишь материалы+золото →
///     шанс успеха от навыка против сложности яруса → применяется РЕАЛЬНОЕ свойство
///     предмета (не отдельная параллельная система, см. ImbuingPropertyTable). Без
///     Порошка закрепления предмет теряет часть максимальной прочности за наложение —
///     настоящий компромисс, не косметика.
///
///     Бюджет свойств — простой встроенный счётчик (не пытается воспроизвести весовую
///     систему интенсивности реальной ванили), максимум 5 наложенных свойств на предмет
///     одновременно, независимо от яруса каждого.
/// </summary>
public sealed class ImbuingSystem : GenericPersistence
{
    private static ImbuingSystem _instance;

    private const int MaxPropertiesPerItem = 5;
    private static readonly Dictionary<Item, int> ImbuedCount = new();

    public ImbuingSystem() : base("MahaonImbuing", 1)
    {
    }

    public static void Configure() => _instance = new ImbuingSystem();

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version

        writer.WriteEncodedInt(ImbuedCount.Count);

        foreach (var (item, count) in ImbuedCount)
        {
            writer.Write(item);
            writer.WriteEncodedInt(count);
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // version

        var entries = reader.ReadEncodedInt();

        for (var i = 0; i < entries; i++)
        {
            var item = reader.ReadEntity<Item>();
            var count = reader.ReadEncodedInt();

            if (item != null)
            {
                ImbuedCount[item] = count;
            }
        }
    }

    public static bool CanImbue(Item item, out string reason)
    {
        if (item is not (BaseWeapon or BaseArmor or BaseJewel))
        {
            reason = "Чары можно наложить только на оружие, броню или украшение.";
            return false;
        }

        var hasDurability = item switch
        {
            IDurability d => d.MaxHitPoints > 0,
            BaseJewel j   => j.MaxHitPoints > 0,
            _              => false
        };

        if (!hasDurability)
        {
            reason = "У этого предмета нет прочности — на него нельзя наложить чары.";
            return false;
        }

        var count = ImbuedCount.GetValueOrDefault(item, 0);

        if (count >= MaxPropertiesPerItem)
        {
            reason = $"На этом предмете уже максимум наложенных свойств ({MaxPropertiesPerItem}).";
            return false;
        }

        reason = null;
        return true;
    }

    public static ImbuingItemType GetItemType(Item item) => item switch
    {
        BaseWeapon => ImbuingItemType.Weapon,
        BaseArmor  => ImbuingItemType.Armor,
        BaseJewel  => ImbuingItemType.Jewelry,
        _          => ImbuingItemType.Weapon // CanImbue already filtered anything else out
    };

    /// <summary>«Печать мастера» — сигнатурный перк Ремесла. Достаётся и тому, у кого
    /// Ремесло вторичной категорией.</summary>
    public static bool CanImbueProperty(Mobile from, ImbuingProperty property) =>
        property.ExclusiveTo == null ||
        Systems.MahaonProfessions.ProfessionSystem.HasSignature(from, property.ExclusiveTo.Value);

    public static int GetAvailableTiers(Mobile from, ImbuingProperty property)
    {
        if (!CanImbueProperty(from, property))
        {
            return 0; // не его ремесло — ни один ярус не откроется, сколько навык ни качай
        }

        var skill = from.Skills[SkillName.Imbuing].Value;
        var unlocked = 0;

        for (var i = 0; i < property.MinSkillPerTier.Length; i++)
        {
            if (skill >= property.MinSkillPerTier[i])
            {
                unlocked = i + 1;
            }
        }

        return unlocked; // 0 = ни один ярус не открыт
    }

    public static bool HasMaterials(Mobile from, ImbuingProperty property, int tier)
    {
        foreach (var (material, amount) in property.MaterialsPerTier[tier])
        {
            if (from.Backpack?.GetAmount(material) < amount)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Шанс успеха: навык против сложности яруса, тот же принцип, что и у
    /// обычных CheckSkill — выше навык над требуемым порогом яруса, выше шанс, никогда
    /// не гарантирован полностью даже с большим запасом.</summary>
    public static double GetSuccessChance(Mobile from, ImbuingProperty property, int tier)
    {
        var skill = from.Skills[SkillName.Imbuing].Value;
        var required = property.MinSkillPerTier[tier];
        var over = skill - required;

        return Math.Clamp(0.5 + over / 100.0, 0.05, 0.95);
    }

    public readonly record struct ImbuingResult(bool Success, string Message);

    public static ImbuingResult TryImbue(Mobile from, Item item, ImbuingProperty property, int tier, bool usePowder)
    {
        if (!CanImbue(item, out var reason))
        {
            return new ImbuingResult(false, reason);
        }

        if (!CanImbueProperty(from, property))
        {
            return new ImbuingResult(
                false, "Такую печать ставит только рука ремесленника — ищи мастера своего дела."
            );
        }

        if (GetAvailableTiers(from, property) <= tier)
        {
            return new ImbuingResult(false, "Навыка не хватает для этого яруса.");
        }

        if (!HasMaterials(from, property, tier))
        {
            return new ImbuingResult(false, "Не хватает материалов.");
        }

        var gold = property.GoldPerTier[tier];

        if (from.Backpack?.GetAmount(typeof(Gold)) < gold)
        {
            return new ImbuingResult(false, "Не хватает золота.");
        }

        if (usePowder && from.Backpack?.GetAmount(typeof(PowderOfFortifying)) < 1)
        {
            return new ImbuingResult(false, "Не хватает Порошка закрепления.");
        }

        var success = Utility.RandomDouble() < GetSuccessChance(from, property, tier);

        // Материалы и золото тратятся независимо от результата — как и в ванильной
        // системе, попытка стоит ресурсов, а не только успех.
        foreach (var (material, amount) in property.MaterialsPerTier[tier])
        {
            from.Backpack.ConsumeTotal(material, amount);
        }

        from.Backpack.ConsumeTotal(typeof(Gold), gold);

        if (usePowder)
        {
            from.Backpack.ConsumeTotal(typeof(PowderOfFortifying), 1);
        }

        if (!success)
        {
            return new ImbuingResult(false, "Наложение не удалось. Материалы и золото потрачены впустую.");
        }

        property.Apply(item, property.MagnitudePerTier[tier]);
        ImbuedCount[item] = ImbuedCount.GetValueOrDefault(item, 0) + 1;

        if (!usePowder)
        {
            // BaseJewel doesn't implement IDurability (only BaseWeapon/BaseArmor do) even
            // though it has its own real MaxHitPoints/HitPoints fields — handled as a
            // separate branch rather than silently skipping durability loss for jewelry.
            if (item is IDurability durability)
            {
                var loss = Math.Max(1, durability.MaxHitPoints / 10);
                var newMax = Math.Max(1, durability.MaxHitPoints - loss);
                durability.MaxHitPoints = newMax;

                if (durability.HitPoints > newMax)
                {
                    durability.HitPoints = newMax;
                }
            }
            else if (item is BaseJewel jewel && jewel.MaxHitPoints > 0)
            {
                var loss = Math.Max(1, jewel.MaxHitPoints / 10);
                var newMax = Math.Max(1, jewel.MaxHitPoints - loss);
                jewel.MaxHitPoints = newMax;

                if (jewel.HitPoints > newMax)
                {
                    jewel.HitPoints = newMax;
                }
            }
        }

        return new ImbuingResult(true, $"Чары наложены: {property.RuName}.");
    }

    /// <summary>Расколдовывание — разбирает магическую вещь обратно на материалы.
    /// Упрощённо относительно ванили: фиксированный возврат по количеству реально
    /// наложенных этой же системой свойств, не пытается угадать состав чужого лута.</summary>
    public static ImbuingResult Unravel(Mobile from, Item item)
    {
        if (item is not (BaseWeapon or BaseArmor or BaseJewel))
        {
            return new ImbuingResult(false, "Расколдовать можно только оружие, броню или украшение.");
        }

        var count = ImbuedCount.GetValueOrDefault(item, 0);

        if (count <= 0)
        {
            return new ImbuingResult(false, "На этом предмете нет наших наложенных чар — расколдовывать нечего.");
        }

        ImbuedCount.Remove(item);
        item.Delete();

        var essence = new EnchantedEssence(count * 2);
        var residue = new MagicalResidue(count * 3);

        from.Backpack?.TryDropItem(from, essence, false);
        from.Backpack?.TryDropItem(from, residue, false);

        return new ImbuingResult(true, $"Предмет расколдован: {essence.Amount} эссенции, {residue.Amount} остатка.");
    }
}

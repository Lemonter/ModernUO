using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.Systems.Bots;

/// <summary>
/// Keeps a bot dressed for its trade: a weapon for its fighting skill and a piece of armour on
/// each slot, worn from the pack when it has one or bought from a town vendor. This is how a bot
/// recovers from a corpse it couldn't get back to: nothing is handed out for free.
/// </summary>
public sealed class OutfitGoal : BotGoal
{
    public override string Name => "Снаряжение";

    public override bool IsUpkeep => true;

    private const int MinWeaponSkill = 30;

    // Heaviest first: the bot takes the best piece its strength can wear.
    private static readonly (Layer Layer, Type[] Heavy, Type[] Light)[] _armor =
    [
        (Layer.InnerTorso,
            [typeof(PlateChest), typeof(ChainChest), typeof(RingmailChest), typeof(StuddedChest), typeof(LeatherChest)],
            [typeof(StuddedChest), typeof(LeatherChest)]),
        (Layer.Pants,
            [typeof(PlateLegs), typeof(ChainLegs), typeof(RingmailLegs), typeof(StuddedLegs), typeof(LeatherLegs)],
            [typeof(StuddedLegs), typeof(LeatherLegs)]),
        (Layer.Arms,
            [typeof(PlateArms), typeof(RingmailArms), typeof(StuddedArms), typeof(LeatherArms)],
            [typeof(StuddedArms), typeof(LeatherArms)]),
        (Layer.Gloves,
            [typeof(PlateGloves), typeof(RingmailGloves), typeof(StuddedGloves), typeof(LeatherGloves)],
            [typeof(StuddedGloves), typeof(LeatherGloves)]),
        (Layer.Neck,
            [typeof(PlateGorget), typeof(StuddedGorget), typeof(LeatherGorget)],
            [typeof(StuddedGorget), typeof(LeatherGorget)]),
        (Layer.Helm,
            [typeof(PlateHelm), typeof(CloseHelm), typeof(NorseHelm), typeof(Helmet), typeof(Bascinet), typeof(LeatherCap)],
            [typeof(LeatherCap)])
    ];

    // Casters keep to plain leather: studded and heavier stop them meditating.
    private static readonly Type[][] _casterArmor = Array.ConvertAll(
        _armor,
        slot => Array.FindAll(slot.Light, t => t.Name.StartsWith("Leather", StringComparison.Ordinal))
    );

    private static readonly (SkillName Skill, Type[] Weapons)[] _weapons =
    [
        (SkillName.Swords, [typeof(Katana), typeof(Broadsword), typeof(Longsword), typeof(VikingSword), typeof(Scimitar)]),
        (SkillName.Macing, [typeof(WarHammer), typeof(Maul), typeof(WarMace), typeof(HammerPick), typeof(Mace)]),
        (SkillName.Fencing, [typeof(Kryss), typeof(WarFork), typeof(Spear), typeof(ShortSpear)]),
        (SkillName.Archery, [typeof(HeavyCrossbow), typeof(Crossbow), typeof(Bow)])
    ];

    /// <summary>What the bot is missing, best candidates first; null when it is fully dressed.</summary>
    internal static Type[] Missing(Mobile bot, out bool weapon)
    {
        weapon = false;

        if (!HoldsWeapon(bot) && BestWeaponSkill(bot) is { } skill)
        {
            foreach (var (s, types) in _weapons)
            {
                if (s == skill)
                {
                    weapon = true;
                    return types;
                }
            }
        }

        var style = BotCombatStyles.Of(bot);
        for (var i = 0; i < _armor.Length; i++)
        {
            var (layer, heavy, light) = _armor[i];
            if (bot.FindItemOnLayer(layer) == null)
            {
                return style switch
                {
                    BotCombatStyle.Melee => heavy,
                    BotCombatStyle.Mage  => _casterArmor[i],
                    _                    => light
                };
            }
        }

        return null;
    }

    private static bool HoldsWeapon(Mobile bot) =>
        bot.FindItemOnLayer(Layer.OneHanded) is BaseWeapon || bot.FindItemOnLayer(Layer.TwoHanded) is BaseWeapon;

    private static SkillName? BestWeaponSkill(Mobile bot)
    {
        SkillName? best = null;
        var bestValue = (double)MinWeaponSkill;

        foreach (var (skill, _) in _weapons)
        {
            var value = bot.Skills[skill].Value;
            if (value >= bestValue)
            {
                bestValue = value;
                best = skill;
            }
        }

        // A caster who dabbles in a weapon keeps its hands for the spellbook.
        return best != null && BotCombatStyles.Of(bot) == BotCombatStyle.Mage ? null : best;
    }

    // The stat checks of CanEquip, without its messages and stat bonuses.
    internal static bool CanWear(Mobile bot, Item item) =>
        item switch
        {
            BaseArmor a  => a.StrRequirement <= bot.Str && a.DexRequirement <= bot.Dex && a.IntRequirement <= bot.Int,
            BaseWeapon w => w.StrRequirement <= bot.Str && w.DexRequirement <= bot.Dex && w.IntRequirement <= bot.Int,
            _            => true
        };

    internal static Item FindInPack(Mobile bot, Type[] types)
    {
        var pack = bot.Backpack;
        if (pack == null)
        {
            return null;
        }

        foreach (var type in types)
        {
            foreach (var item in pack.Items)
            {
                if (item.GetType() == type && CanWear(bot, item))
                {
                    return item;
                }
            }
        }

        return null;
    }

    public override double Score(BotBrain brain)
    {
        var bot = brain.Bot;
        if (Missing(bot, out var weapon) is not { } types)
        {
            return 0;
        }

        if (FindInPack(bot, types) != null)
        {
            return 1.0;
        }

        var funds = (bot.Backpack?.GetAmount(typeof(Gold)) ?? 0) + Banker.GetBalance(bot);
        if (weapon)
        {
            return funds >= 100 ? 0.95 : 0;
        }

        return funds >= 300 ? 0.45 + BotBrain.Trait(brain.Caution) * 0.2 : 0;
    }

    public override List<BotAction> Plan(BotBrain brain)
    {
        var bot = brain.Bot;
        if (Missing(bot, out _) is not { } types)
        {
            return null;
        }

        if (FindInPack(bot, types) != null)
        {
            return [new EquipAction(types)];
        }

        var city = BotSocialRules.TownFor(bot);
        if (city == null)
        {
            return null;
        }

        BaseVendor seller = null;
        Type type = null;
        var price = 0;

        foreach (var candidate in types)
        {
            foreach (var vendor in WorldCatalog.GetVendors(city))
            {
                if (BuyFromVendorAction.FindStock(vendor, candidate) is { } stock && stock.Type == candidate &&
                    stock.GetDisplayEntity() is Item display && CanWear(bot, display))
                {
                    (seller, type, price) = (vendor, candidate, stock.Price);
                    break;
                }
            }

            if (seller != null)
            {
                break;
            }
        }

        if (seller == null)
        {
            return null;
        }

        var steps = new List<BotAction>();
        var purse = bot.Backpack?.GetAmount(typeof(Gold)) ?? 0;

        if (purse < price)
        {
            var banker = WorldCatalog.GetBanker(city);
            if (banker == null || Banker.GetBalance(bot) < price - purse)
            {
                return null;
            }

            steps.Add(new GoToAction(banker, 3, "в банк"));
            steps.Add(new WithdrawGoldAction(banker, price - purse));
        }

        steps.Add(new GoToAction(seller, 2, $"к торговцу {seller.Name}"));
        steps.Add(new BuyFromVendorAction(seller, type, 1));
        steps.Add(new EquipAction([type]));
        return steps;
    }
}

/// <summary>Puts on the first item of the given types the bot carries and can wear.</summary>
public sealed class EquipAction : BotAction
{
    private readonly Type[] _types;

    public EquipAction(Type[] types) => _types = types;

    public override BotActionResult Tick(BotBrain brain)
    {
        var bot = brain.Bot;
        var item = OutfitGoal.FindInPack(bot, _types);

        return item != null && bot.EquipItem(item) ? BotActionResult.Done(800) : BotActionResult.Failed();
    }

    public override string Describe(BotBrain brain) => "Надевает снаряжение";
}

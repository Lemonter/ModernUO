using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.Systems.Bots;

/// <summary>
/// Takes what is worth taking from a corpse the bot may loot without a crime: everything from its
/// own corpse, and from a kill the gold and whatever a vendor or the auction will pay for. Items go
/// through the corpse's own lift handler, so looting rights and criminal flags behave as for a
/// player.
/// </summary>
public sealed class LootCorpseAction : BotAction
{
    private const int Reach = 2;

    private readonly Corpse _corpse;

    public LootCorpseAction(Corpse corpse) => _corpse = corpse;

    /// <summary>
    /// Its own corpse, any kill it may loot without a crime, and — for a player killer — the
    /// corpses of its victims, crime or not.
    /// </summary>
    public static bool MayLoot(Mobile bot, Corpse corpse) =>
        corpse is { Deleted: false } && corpse.Map == bot.Map &&
        (corpse.Owner == bot || !corpse.IsCriminalAction(bot) && corpse.Owner is not { Player: true } ||
         bot is BotMobile { IsPk: true } && corpse.Owner is { Player: true } && corpse.CanLoot(bot, null));

    public override BotActionResult Tick(BotBrain brain)
    {
        var bot = brain.Bot;
        var pack = bot.Backpack;

        if (pack == null || !MayLoot(bot, _corpse) || !bot.InRange(_corpse.GetWorldLocation(), Reach))
        {
            return BotActionResult.Failed();
        }

        var own = _corpse.Owner == bot;
        var take = new List<Item>();

        // Skin the kill first, as a hunter with a blade does: the hides fall into the corpse.
        if (!own && !_corpse.Carved && _corpse.Owner is BaseCreature { Hides: > 0 } && Blade(bot) is { } blade)
        {
            _corpse.Carve(bot, blade);
        }

        foreach (var item in _corpse.Items)
        {
            if (own || item is Gold || IsWorthTaking(item) || BotQuests.WantsTrophy(bot, item))
            {
                take.Add(item);
            }
        }

        foreach (var item in take)
        {
            if (!pack.TryDropItem(bot, item, false))
            {
                break;
            }

            _corpse.OnItemLifted(bot, item);

            // Trophies go to the quest giver, not the market.
            if (!own && item is not Gold and not MahaonAnimalHead)
            {
                brain.MarkLoot(item);
            }
        }

        brain.MarkLooted(_corpse);

        if (own)
        {
            brain.OwnCorpse = null;
        }
        return BotActionResult.Done(800);
    }

    /// <summary>Anything with resale value that isn't trash: arms, armour, jewellery, gems,
    /// reagents, scrolls, hides.</summary>
    private static bool IsWorthTaking(Item item) =>
        item.Movable && item.LootType != LootType.Blessed &&
        (item is BaseWeapon or BaseArmor or BaseJewel or BaseReagent or SpellScroll or BaseHides || BotCrafting.IsRawMeat(item) ||
         Gems.Contains(item.GetType()));

    /// <summary>Something to skin with: the blade in hand, or a knife in the pack.</summary>
    private static Item Blade(Mobile bot) =>
        bot.Weapon is BaseSword or BaseKnife or BaseAxe ? (Item)bot.Weapon : bot.Backpack?.FindItemByType<BaseKnife>();

    private static readonly HashSet<System.Type> Gems =
    [
        typeof(Amber), typeof(Amethyst), typeof(Citrine), typeof(Diamond), typeof(Emerald), typeof(Ruby),
        typeof(Sapphire), typeof(StarSapphire), typeof(Tourmaline)
    ];

    public override string Describe(BotBrain brain) => "Обыскивает труп";
}

using System;
using System.Collections.Generic;
using Server.Engines.Craft;
using Server.Items;

namespace Server.Systems.Bots;

/// <summary>
/// The scribe's side of crafting. Inscription only writes spells the scribe has in its own book,
/// so a scribe grows its range the way a player does: buys the scroll at the mage's shop and
/// drops it into the book.
/// </summary>
public static class BotScribe
{
    // Scroll type -> spell id. The id lives on the instance, so each type is built once to read it.
    private static readonly Dictionary<Type, int> _spellIds = new();

    public static bool IsScribe(Mobile bot) => DefInscription.CraftSystem is { } system && BotCrafting.IsCrafter(bot, system);

    public static int SpellIdOf(Type scrollType)
    {
        if (_spellIds.TryGetValue(scrollType, out var id))
        {
            return id;
        }

        var scroll = scrollType.CreateEntityInstance<SpellScroll>();
        id = scroll?.SpellID ?? -1;
        scroll?.Delete();
        _spellIds[scrollType] = id;
        return id;
    }

    /// <summary>Whether the bot's book holds the spell this scroll type writes.</summary>
    public static bool KnowsSpell(Mobile bot, Type scrollType)
    {
        var id = SpellIdOf(scrollType);
        return id >= 0 && Spellbook.Find(bot, id)?.HasSpell(id) == true;
    }

    /// <summary>
    /// The next magery scroll to buy for the book: the lowest circle missing from it that the
    /// scribe's skill can write with a fair chance. Null without a book, or when nothing is missing.
    /// </summary>
    public static Type NextScrollToLearn(Mobile bot)
    {
        var system = DefInscription.CraftSystem;
        var book = Spellbook.FindRegular(bot);
        if (system == null || book == null)
        {
            return null;
        }

        var skill = bot.Skills.Inscribe.Value;

        foreach (var craftItem in system.CraftItems)
        {
            var type = craftItem.ItemType;
            if (!typeof(SpellScroll).IsAssignableFrom(type) || craftItem.Skills.Count == 0)
            {
                continue;
            }

            var id = SpellIdOf(type);
            if (id < 0 || Spellbook.GetTypeForSpell(id) != SpellbookType.Regular || book.HasSpell(id))
            {
                continue;
            }

            // Midway between the craft's min and max skill the chance is about one in two.
            var skillReq = craftItem.Skills[0];
            if (skill < (skillReq.MinSkill + skillReq.MaxSkill) / 2)
            {
                continue;
            }

            if (bot.Backpack?.FindItemByType(type) == null)
            {
                return type;
            }
        }

        return null;
    }

    /// <summary>A scroll bought to go into the book, not to be sold.</summary>
    public static bool WantsForBook(Mobile bot, Item item) =>
        item is SpellScroll scroll && IsScribe(bot) && Spellbook.GetTypeForSpell(scroll.SpellID) == SpellbookType.Regular &&
        Spellbook.FindRegular(bot) is { } book && !book.HasSpell(scroll.SpellID);

    /// <summary>Drops every scroll the book lacks onto it, as a player adds a spell.</summary>
    public static int FillBook(Mobile bot)
    {
        var pack = bot.Backpack;
        if (pack == null || Spellbook.FindRegular(bot) is not { } book)
        {
            return 0;
        }

        var added = 0;
        foreach (var scroll in pack.EnumerateItemsByType<SpellScroll>())
        {
            if (WantsForBook(bot, scroll) && book.OnDragDrop(bot, scroll))
            {
                added++;
            }
        }

        return added;
    }
}

/// <summary>Puts newly bought spell scrolls into the scribe's book.</summary>
public sealed class FillSpellbookAction : BotAction
{
    public override BotActionResult Tick(BotBrain brain) => BotActionResult.Done(BotScribe.FillBook(brain.Bot) > 0 ? 1000 : 0);

    public override string Describe(BotBrain brain) => "Вписывает заклинание в книгу";
}

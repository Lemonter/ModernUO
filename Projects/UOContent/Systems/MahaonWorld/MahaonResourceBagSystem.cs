using Server.Items;

namespace Server.Systems.MahaonWorld;

/// <summary>
///     Routes a freshly-gathered resource into the gatherer's matching MahaonResourceBag
///     (Systems.MahaonWorld — the bag classes themselves live in Server.Items, same split
///     as everywhere else in Mahaon between "the item" and "the system that uses it"), if
///     they're carrying one. Falls through to the normal backpack when they aren't, or when
///     the bag is present but full/can't otherwise accept the item (BaseContainer.DropItem
///     already returns false in either case).
/// </summary>
public static class MahaonResourceBagSystem
{
    /// <summary>Best-effort category guess from the item's real C# type — works for ore
    /// (BaseOre — plus gems, via the same GemSocketingSystem.BonusTypeFor check GuildBank
    /// already uses to recognize them, since gems share no common base class of their own),
    /// wood (Log and its subclasses), fish (Fish/BigFish), reagents (BaseReagent, including
    /// necromancer ingredients — they're BaseReagent-derived too), and the hunter's bag's
    /// two unrelated halves: skinning resources (BaseHides, Wool, Feather — no single
    /// shared base across all three, so each is listed) and combat consumables
    /// (BasePotion, Bandage, SpellScroll, Arrow, Bolt). Crops don't share anything either
    /// (Onion/Garlic/etc.), so callers harvesting crops should use the TryGive overload
    /// that takes an explicit category instead of relying on this.</summary>
    public static MahaonResourceCategory? DetectCategory(Item item) => item switch
    {
        BaseOre                                                   => MahaonResourceCategory.Ore,
        Log                                                        => MahaonResourceCategory.Wood,
        Fish or BigFish                                            => MahaonResourceCategory.Fish,
        BaseReagent                                                => MahaonResourceCategory.Reagent,
        BaseHides or Wool or Feather                               => MahaonResourceCategory.Hunter,
        BasePotion or Bandage or SpellScroll or Arrow or Bolt      => MahaonResourceCategory.Hunter,
        _ when Systems.MahaonGems.GemSocketingSystem.BonusTypeFor(item.GetType()) != null => MahaonResourceCategory.Ore,
        _                                                          => null
    };

    /// <summary>Tries to place the item in a matching bag in the gatherer's backpack.
    /// Returns false (does nothing to the item) if there's no such bag, or the category
    /// can't be determined and none was supplied — caller should fall back to its normal
    /// placement logic either way.</summary>
    public static bool TryGive(Mobile from, Item item, MahaonResourceCategory? category = null)
    {
        var resolvedCategory = category ?? DetectCategory(item);

        if (resolvedCategory == null || from?.Backpack == null)
        {
            return false;
        }

        foreach (var child in from.Backpack.Items)
        {
            if (child is MahaonResourceBag bag && bag.Category == resolvedCategory)
            {
                return bag.TryDropItem(from, item, false);
            }
        }

        return false;
    }
}

using Server.Items;
using System;
using Server.Engines.MLQuests;
using Server.Engines.MLQuests.Definitions;
using Server.Mobiles;

namespace Server.Engines.ExploringTheDeep;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/ExploringTheDeepQuestChain.cs).
///
///     ServUO tracked this quest line in a single <c>ExploringTheDeepQuestChain</c> enum stored on
///     <c>PlayerMobile</c>, and every NPC, creature and prop compared against it. This codebase tracks ML
///     quests through <see cref="MLQuestContext" /> instead, so the enum is gone and its comparisons are
///     expressed as "is this quest in progress" / "has it been completed". The mapping is:
///
///     <code>
///     None                        -> no quest started
///     HeplerPaulson               -> SalvageMaster in progress
///     HeplerPaulsonComplete       -> SalvageMaster done
///     CusteauPerronHouse          -> SalvageMaster done (the Master Thinker props are flavour now)
///     CusteauPerron               -> TheMasterTinker in progress
///     Sorcerers                   -> TheSorcerersSchematics in progress
///     CollectTheComponent         -> TheSorcerersSchematics done
///     CollectTheComponentComplete -> TheSalvagersSuit done
///     </code>
///
///     <c>ExploringTheDeepBaseChain</c> is dropped: it held a single "current quest" Type, which is what
///     <see cref="MLQuest.NextQuest" /> already does.
/// </summary>
public static class ExploringDeepChain
{
    /// <summary>The Ice Wyrm scale errand Cousteau Perron starts.</summary>
    public static Type MasterTinkerQuest => typeof(TheMasterTinker);

    /// <summary>The turn-in of the salvager suit plans, after which the four craftsmen will deal.</summary>
    public static Type ComponentQuest => typeof(TheSorcerersSchematics);

    public static bool IsDoing(PlayerMobile pm, Type questType) =>
        pm != null && MLQuestSystem.GetContext(pm)?.IsDoingQuest(questType) == true;

    public static bool HasDone(PlayerMobile pm, Type questType) =>
        pm != null && MLQuestSystem.GetContext(pm)?.HasDoneQuest(questType) == true;

    /// <summary>Has the Salvage Master's first errand been run (the old "HeplerPaulsonComplete")?</summary>
    public static bool SalvageMasterDone(PlayerMobile pm) => HasDone(pm, typeof(SalvageMaster));

    /// <summary>
    ///     Cousteau Perron has read the plans, so the four craftsmen will now deal with the player (the old
    ///     "CollectTheComponent").
    /// </summary>
    public static bool ComponentStage(PlayerMobile pm) => HasDone(pm, typeof(TheSorcerersSchematics));

    /// <summary>The suit is assembled and the Salvager's Suit quest was turned in.</summary>
    public static bool SuitComplete(PlayerMobile pm) => HasDone(pm, typeof(TheSalvagersSuit));

    /// <summary>
    ///     Whether a mobile is wearing all four pieces of the Salvager's Suit, which is what every
    ///     underwater check ultimately boils down to.
    /// </summary>
    public static bool WearingSuit(Mobile m)
    {
        if (m?.Items == null)
        {
            return false;
        }

        var pieces = 0;

        foreach (var item in m.Items)
        {
            if (item is CanvassRobe or BootsOfBallast or NictitatingLens or AquaPendant or GargishNictitatingLens &&
                item.Parent is Mobile owner && owner.FindItemOnLayer(item.Layer) == item)
            {
                ++pieces;
            }
        }

        return pieces >= 4;
    }
}

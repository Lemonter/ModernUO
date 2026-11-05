using System;
using System.Collections.Generic;
using Server.Engines.MLQuests.Objectives;
using Server.Engines.MLQuests.Rewards;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.MLQuests.Definitions;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Questers/*.cs).
///
///     ServUO ran this content on the old <c>Server.Engines.Quests</c> chain: an
///     <c>ExploringTheDeepQuestChain</c> enum on <see cref="PlayerMobile" />, hand-written story gumps and
///     <c>OnDragDrop</c> hand-ins. It is rebuilt here on the ML quest framework, which is what this
///     codebase uses for quest chains: the same beats, the same NPCs and the same items, expressed as
///     quests with collect objectives and rewards, gated on their predecessors. The old story gumps are
///     replaced by the framework's offer/progress/reward gumps; the original page text survives as the
///     quest descriptions (the clilocs are unchanged).
///
///     One structural note: the Salvage Master's final turn-in used to check "are all four suit parts in
///     the pack" and hand over the map without taking anything. Under the ML framework a collect objective
///     consumes what it collects, so the reward for that last quest hands the four parts back along with
///     the map. The player ends up holding exactly what they had plus the map, which is what ServUO's
///     version produced.
/// </summary>
public abstract class ExploringDeepQuest : MLQuest
{
    /// <summary>The quest that must be completed before this one may be offered, if any.</summary>
    protected virtual Type Prerequisite => null;

    public override bool CanOffer(IQuestGiver quester, PlayerMobile pm, MLQuestContext context, bool message)
    {
        if (!base.CanOffer(quester, pm, context, message))
        {
            return false;
        }

        if (Prerequisite != null && context?.HasDoneQuest(Prerequisite) != true)
        {
            if (message)
            {
                // You feel as though by doing this you are missing out on an important part of your journey...
                MLQuestSystem.Tell(quester, pm, 1154325);
            }

            return false;
        }

        return true;
    }
}

/// <summary>Hepler Paulson's first errand: fish the wreckage out of Gravewater Lake.</summary>
public class SalvageMaster : ExploringDeepQuest
{
    public SalvageMaster()
    {
        Activated = true;
        OneTimeOnly = true;

        Title = "Спасение на дне";
        Description = 1154279;
        RefusalMessage = 1154282;
        InProgressMessage = 1154283;
        CompletionMessage = 1154284;
        CompletionNotice = CompletionNoticeShort;

        Objectives.Add(new CollectObjective(1, typeof(BrokenShipwreckRemains), "обломки кораблекрушения"));

        Rewards.Add(new DummyReward(1154286));
    }
}

/// <summary>Cousteau Perron's price for the suit: a scale from the Ice Wyrm.</summary>
public class TheMasterTinker : ExploringDeepQuest
{
    public TheMasterTinker()
    {
        Activated = true;
        OneTimeOnly = true;

        Title = "Мастер-механик";
        Description = 1154287;
        RefusalMessage = 1154290;
        InProgressMessage = 1154291;
        CompletionMessage = 1154292;
        CompletionNotice = CompletionNoticeShort;

        Objectives.Add(new CollectObjective(1, typeof(IceWyrmScale), "чешуя ледяного вирма"));

        Rewards.Add(new DummyReward(1154292));
    }

    protected override Type Prerequisite => typeof(SalvageMaster);
}

/// <summary>The plans from the Sorcerer's Dungeon strongbox, carried back to Cousteau Perron.</summary>
public class TheSorcerersSchematics : ExploringDeepQuest
{
    public TheSorcerersSchematics()
    {
        Activated = true;
        OneTimeOnly = true;

        Title = "Чертежи чародеев";
        Description = 1154292;
        RefusalMessage = 1154290;
        InProgressMessage = 1154293;
        CompletionMessage = 1154293;
        CompletionNotice = CompletionNoticeShort;

        Objectives.Add(new CollectObjective(1, typeof(SalvagerSuitPlans), "чертежи костюма спасателя"));

        Rewards.Add(new DummyReward(1154293));
    }

    protected override Type Prerequisite => typeof(TheMasterTinker);
}

/// <summary>Champ Huthwait's boots, in exchange for Mercutio's cutlass.</summary>
public class BallastBoots : ExploringDeepQuest
{
    public BallastBoots()
    {
        Activated = true;

        Title = "Ботинки балласта";
        Description = 1154303;
        RefusalMessage = 1154304;
        InProgressMessage = 1154307;
        CompletionMessage = 1154310;
        CompletionNotice = CompletionNoticeShort;

        Objectives.Add(new CollectObjective(1, typeof(MercutiosCutlass), "сабля Меркуцио"));

        Rewards.Add(new ItemReward(1154242, typeof(BootsOfBallast)));
    }

    protected override Type Prerequisite => typeof(TheSorcerersSchematics);
}

/// <summary>Madeline Harte's robe, in exchange for her son's hat.</summary>
public class CanvassRobeQuest : ExploringDeepQuest
{
    public CanvassRobeQuest()
    {
        Activated = true;

        Title = "Парусиновый халат";
        Description = 1154300;
        RefusalMessage = 1154300;
        InProgressMessage = 1154300;
        CompletionMessage = 1154302;
        CompletionNotice = CompletionNoticeShort;

        Objectives.Add(new CollectObjective(1, typeof(WillemHartesHat), "шляпа Уильяма Харта"));

        Rewards.Add(new ItemReward(1154238, typeof(CanvassRobe)));
    }

    protected override Type Prerequisite => typeof(TheSorcerersSchematics);
}

/// <summary>Zalia's pendant, in exchange for the Aqua Gem.</summary>
public class AquaPendantQuest : ExploringDeepQuest
{
    public AquaPendantQuest()
    {
        Activated = true;

        Title = "Аква-подвеска";
        Description = 1154311;
        RefusalMessage = 1154312;
        InProgressMessage = 1154315;
        CompletionMessage = 1154318;
        CompletionNotice = CompletionNoticeShort;

        Objectives.Add(new CollectObjective(1, typeof(AquaGem), "аква-самоцвет"));

        Rewards.Add(new ItemReward(1154246, typeof(AquaPendant)));
    }

    protected override Type Prerequisite => typeof(TheSorcerersSchematics);
}

/// <summary>Josef Skimmons' lenses, in exchange for the orc drilling machine schematics.</summary>
public class NictitatingLenses : ExploringDeepQuest
{
    public NictitatingLenses()
    {
        Activated = true;

        Title = "Мигательные линзы";
        Description = 1154294;
        RefusalMessage = 1154294;
        InProgressMessage = 1154296;
        CompletionMessage = 1154298;
        CompletionNotice = CompletionNoticeShort;

        Objectives.Add(new CollectObjective(1, typeof(OrcishSchematics), "схемы орков"));

        Rewards.Add(new RaceLensReward());
    }

    protected override Type Prerequisite => typeof(TheSorcerersSchematics);

    /// <summary>Gargoyles get the gargish lenses, everyone else the elven ones.</summary>
    private class RaceLensReward : ItemReward
    {
        public override Item CreateItem() => null;

        public override void AddRewardItems(PlayerMobile pm, List<Item> rewards) =>
            rewards.Add(pm.Race == Race.Gargoyle ? new GargishNictitatingLens() : new NictitatingLens());
    }
}

/// <summary>
///     Hepler Paulson assembles the suit: the four parts come in, the map to the Gravewater Lake wreck goes
///     out, and the parts are handed back so they can actually be worn.
/// </summary>
public class TheSalvagersSuit : ExploringDeepQuest
{
    public TheSalvagersSuit()
    {
        Activated = true;
        OneTimeOnly = true;

        Title = "Костюм спасателя";
        Description = 1154263;
        RefusalMessage = 1154320;
        InProgressMessage = 1154319;
        CompletionMessage = 1154319;
        CompletionNotice = CompletionNoticeShort;

        Objectives.Add(new CollectObjective(1, typeof(BootsOfBallast), "ботинки балласта"));
        Objectives.Add(new CollectObjective(1, typeof(CanvassRobe), "парусиновый халат"));
        Objectives.Add(new CollectObjective(1, typeof(AquaPendant), "аква-подвеска"));
        Objectives.Add(new LensObjective());

        Rewards.Add(new SuitReward());
    }

    protected override Type Prerequisite => typeof(TheSorcerersSchematics);

    /// <summary>
    ///     Accepts either lens. <see cref="CollectObjective.AcceptedType" /> is widened to <see cref="Item" />
    ///     because the two lens types do not share a base class; <see cref="CheckItem" /> does the real
    ///     filtering, and that is the only place <c>AcceptedType</c> is consulted with it.
    /// </summary>
    private class LensObjective : CollectObjective
    {
        public LensObjective() : base(1, typeof(Item), "мигательные линзы")
        {
        }

        public override bool CheckItem(Item item) => item is NictitatingLens or GargishNictitatingLens;
    }

    private class SuitReward : BaseReward
    {
        public SuitReward() : base(1154269) // Map to an Unknown Shipwreck
        {
        }

        public override void AddRewardItems(PlayerMobile pm, List<Item> rewards)
        {
            rewards.Add(new BootsOfBallast());
            rewards.Add(new CanvassRobe());
            rewards.Add(new AquaPendant());
            rewards.Add(pm.Race == Race.Gargoyle ? new GargishNictitatingLens() : new NictitatingLens());
            rewards.Add(new UnknownShipwreck());
        }
    }
}

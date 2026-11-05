using Server.Engines.ExploringTheDeep;
using System;
using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Items/QuestItems/SorcerersRegs.cs): the five
///     "perfect" reagents for the Sorcerer's Dungeon plate puzzle and the matching decorative samples that
///     hand them out. ServUO declared each as its own near-identical class; each pair is still its own type
///     so existing tooltips, spawns and serialized items keep working, but the shared behaviour lives in
///     <see cref="SorcerersReagentDecor" /> and <see cref="SorcerersReagent" />.
///
///     Bug fixed while porting: every decor's range check only *sent* "That is too far away" and then went on
///     to hand out the reagent anyway, so a player could fish the reagents out from across the room. The
///     check returns now.
/// </summary>
[SerializationGenerator(0, false)]
public abstract partial class SorcerersReagentDecor : Item
{
    public SorcerersReagentDecor(int itemID) : base(itemID) => Movable = false;

    /// <summary>The reagent this sample hands out.</summary>
    protected abstract Type ReagentType { get; }

    protected abstract Item CreateReagent();

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(this, 2))
        {
            from.SendLocalizedMessage(500446); // That is too far away.
            return;
        }

        if (from.Backpack?.FindItemByType(ReagentType) != null)
        {
            // You already have one of these. You don't need another.
            from.SendLocalizedMessage(1154331);
            return;
        }

        if (from is not PlayerMobile pm ||
            !ExploringDeepChain.IsDoing(pm, typeof(Server.Engines.MLQuests.Definitions.TheSorcerersSchematics)))
        {
            // You feel as though by doing this you are missing out on an important part of your journey...
            from.SendLocalizedMessage(1154325);
            return;
        }

        from.Backpack?.TryDropItem(from, CreateReagent(), false);
        from.SendLocalizedMessage(1154489); // You received a Quest Item!
    }
}

/// <summary>A perfect reagent; it rots away after five in-game hours.</summary>
[SerializationGenerator(0, false)]
public abstract partial class SorcerersReagent : BaseDecayingItem
{
    public SorcerersReagent(int itemID) : base(itemID)
    {
        Stackable = false;
        LootType = LootType.Blessed;
        Weight = 1;
        QuestItem = true;
    }

    public override int Lifespan => 3600;
    public override bool UseSeconds => false;
}

[SerializationGenerator(0, false)]
public partial class PerfectBlackPearlDecor : SorcerersReagentDecor
{
    [Constructible]
    public PerfectBlackPearlDecor() : base(0xF7A)
    {
    }

    public override int LabelNumber => 1154257;

    protected override Type ReagentType => typeof(PerfectBlackPearl);

    protected override Item CreateReagent() => new PerfectBlackPearl();
}

[SerializationGenerator(0, false)]
public partial class PerfectBlackPearl : SorcerersReagent
{
    [Constructible]
    public PerfectBlackPearl() : base(0xF7A)
    {
    }

    public override int LabelNumber => 1154257;
}

[SerializationGenerator(0, false)]
public partial class BurstingBrimstoneDecor : SorcerersReagentDecor
{
    [Constructible]
    public BurstingBrimstoneDecor() : base(0xF7F)
    {
    }

    public override int LabelNumber => 1154258;

    protected override Type ReagentType => typeof(BurstingBrimstone);

    protected override Item CreateReagent() => new BurstingBrimstone();
}

[SerializationGenerator(0, false)]
public partial class BurstingBrimstone : SorcerersReagent
{
    [Constructible]
    public BurstingBrimstone() : base(0xF7F)
    {
    }

    public override int LabelNumber => 1154258;
}

[SerializationGenerator(0, false)]
public partial class BrightDaemonBloodDecor : SorcerersReagentDecor
{
    [Constructible]
    public BrightDaemonBloodDecor() : base(0xF7D)
    {
    }

    public override int LabelNumber => 1154259;

    protected override Type ReagentType => typeof(BrightDaemonBlood);

    protected override Item CreateReagent() => new BrightDaemonBlood();
}

[SerializationGenerator(0, false)]
public partial class BrightDaemonBlood : SorcerersReagent
{
    [Constructible]
    public BrightDaemonBlood() : base(0xF7D)
    {
    }

    public override int LabelNumber => 1154259;
}

[SerializationGenerator(0, false)]
public partial class MightyMandrakeDecor : SorcerersReagentDecor
{
    [Constructible]
    public MightyMandrakeDecor() : base(0xF86)
    {
    }

    public override int LabelNumber => 1154260;

    protected override Type ReagentType => typeof(MightyMandrake);

    protected override Item CreateReagent() => new MightyMandrake();
}

[SerializationGenerator(0, false)]
public partial class MightyMandrake : SorcerersReagent
{
    [Constructible]
    public MightyMandrake() : base(0xF86)
    {
    }

    public override int LabelNumber => 1154260;
}

[SerializationGenerator(0, false)]
public partial class BurlyBoneDecor : SorcerersReagentDecor
{
    [Constructible]
    public BurlyBoneDecor() : base(0xF7E)
    {
    }

    public override int LabelNumber => 1154261;

    protected override Type ReagentType => typeof(BurlyBone);

    protected override Item CreateReagent() => new BurlyBone();
}

[SerializationGenerator(0, false)]
public partial class BurlyBone : SorcerersReagent
{
    [Constructible]
    public BurlyBone() : base(0xF7E)
    {
    }

    public override int LabelNumber => 1154261;
}


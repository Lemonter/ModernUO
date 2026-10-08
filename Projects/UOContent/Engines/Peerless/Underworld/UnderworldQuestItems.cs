using ModernUO.Serialization;

namespace Server.Items;

/// <summary>The objects the Underworld quest line asks players to fetch. Ported from ServUO
/// (Scripts/Items/Quest/SAQuestItems.cs and Scripts/Items/Quest/).
///
/// `barrelofbarley` and `flintslogbook` also appear as spawner objects in
/// Distribution/XmlSpawner/underworld.xml (the UnderworldQuest_Flint spawners) — they were two
/// of the names failing to resolve in badspawn.log.</summary>
[SerializationGenerator(0, false)]
public partial class BarrelOfBarley : Item
{
    [Constructible]
    public BarrelOfBarley() : base(4014) => Weight = 25.0;

    public override int LabelNumber => 1094999; // barrel of barley
}

[SerializationGenerator(0, false)]
public partial class FlintsLogbook : Item
{
    [Constructible]
    public FlintsLogbook() : base(7185)
    {
    }

    public override int LabelNumber => 1095000; // Flint's logbook
}

/// <summary>The goblin floor traps Tobin wants examples of. The original picks one of four
/// graphics at random.</summary>
[SerializationGenerator(0, false)]
public partial class FloorTrapComponent : Item
{
    [Constructible]
    public FloorTrapComponent() : base(Utility.RandomMinMax(3117, 3120))
    {
    }

    public override int LabelNumber => 1095001; // floor trap component
}

/// <summary>Xenrr's catch. Ported from ServUO (Scripts/Items/Quest/MudPuppy.cs).</summary>
[SerializationGenerator(0, false)]
public partial class MudPuppy : BigFish
{
    [Constructible]
    public MudPuppy() => Hue = 643;

    public override int LabelNumber => 1095117; // mud puppy
}

/// <summary>Barreraak's catch. Ported from ServUO (Scripts/Items/Quest/RedHerring.cs).
///
/// The original bases it on BigHerring, a High Seas fish class that isn't ported here; BigFish
/// is the nearest base that exists and behaves identically for a quest token. Swap the base
/// when High Seas lands.</summary>
[SerializationGenerator(0, false)]
public partial class RedHerring : BigFish
{
    [Constructible]
    public RedHerring() => Hue = 337;

    public override int LabelNumber => 1095046; // red herring
}

/// <summary>Flint's first reward. Ported from ServUO (SAQuestItems.cs) — an ale bottle by any
/// other name.</summary>
[SerializationGenerator(0, false)]
public partial class BottleOfFlintsPungnentBrew : BaseBeverage
{
    [Constructible]
    public BottleOfFlintsPungnentBrew() : base(BeverageType.Ale)
    {
    }

    public override double DefaultWeight => 1.0;

    public override int BaseLabelNumber => 1094967; // Bottle of Flint's Pungnent Brew
    public override int MaxQuantity => 5;
    public override bool Fillable => false;

    public override int ComputeItemID() => IsEmpty ? 0 : 0x99F;
}

/// <summary>Flint's second reward — the keg that refills the bottle. Ported from ServUO
/// (SAQuestItems.cs).
///
/// The original is flipable between graphics 6870 and 6871; this codebase has no Flipable
/// attribute, so it keeps the one graphic.</summary>
[SerializationGenerator(0, false)]
public partial class KegOfFlintsPungnentBrew : Item
{
    [Constructible]
    public KegOfFlintsPungnentBrew() : base(6870) => Weight = 25.0;

    public override int LabelNumber => 1113608; // Keg of Flint's Pungnent Brew
}

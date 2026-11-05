using ModernUO.Serialization;
using System;
using Server.Network;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Decorative/CollectionStatuettes.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class CollectionStatuette : BaseStatuette
{
    public CollectionStatuette(int itemID)
        : base(itemID)
    {
        this.LootType = LootType.Blessed;
        this.Weight = 1.0;
    }

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        if (this.TurnedOn && this.IsLockedDown && (!m.Hidden || m.Player) && Utility.InRange(m.Location, this.Location, 2) && !Utility.InRange(oldLocation, this.Location, 2))
        {
            this.PublicOverheadMessage(MessageType.Regular, 0x3B2, Utility.RandomMinMax(1073207, 1073216));
            Effects.PlaySound(this.Location, this.Map, Utility.RandomMinMax(0x5A, 0xE9));
        }

        base.OnMovement(m, oldLocation);
    }
}

[SerializationGenerator(0, false)]
public partial class SilverSteedZooStatuette : CollectionStatuette
{
    [Constructible]
    public SilverSteedZooStatuette()
        : base(0x259D)
    {
    }

    public override int LabelNumber => 1073219; // Interactive Silver Steed Contribution Statue
}

[SerializationGenerator(0, false)]
public partial class QuagmireZooStatuette : CollectionStatuette
{
    [Constructible]
    public QuagmireZooStatuette()
        : base(0x2614)
    {
    }

    public override int LabelNumber => 1074848; // Interactive Quagmire Contribution Statue from the Britannia Royal Zoo.
}

[SerializationGenerator(0, false)]
public partial class BakeKitsuneZooStatuette : CollectionStatuette
{
    [Constructible]
    public BakeKitsuneZooStatuette()
        : base(0x2763)
    {
    }

    public override int LabelNumber => 1074849; // Interactive Bake Kitsune Contribution Statue from the Britannia Royal Zoo.
}

[SerializationGenerator(0, false)]
public partial class DireWolfZooStatuette : CollectionStatuette
{
    [Constructible]
    public DireWolfZooStatuette()
        : base(0x25D0)
    {
    }

    public override int LabelNumber => 1073196; // Interactive Dire Wolf Contribution Statue from the Britannia Royal Zoo.
}

[SerializationGenerator(0, false)]
public partial class CraneZooStatuette : CollectionStatuette
{
    [Constructible]
    public CraneZooStatuette()
        : base(0x2764)
    {
    }

    public override int LabelNumber => 1073197; // An Interactive Crane Contribution Statue from the Britannia Royal Zoo.
}

[SerializationGenerator(0, false)]
public partial class PolarBearZooStatuette : CollectionStatuette
{
    [Constructible]
    public PolarBearZooStatuette()
        : base(0x20E1)
    {
    }

    public override int LabelNumber => 1074851; // Interactive Polar Bear Contribution Statue from the Britannia Royal Zoo.
}

[SerializationGenerator(0, false)]
public partial class ChangelingZooStatuette : CollectionStatuette
{
    [Constructible]
    public ChangelingZooStatuette()
        : base(0x2D8A)
    {
    }

    public override int LabelNumber => 1074850; // Interactive Changeling Contribution Statue from the Britannia Royal Zoo.
}

[SerializationGenerator(0, false)]
public partial class ReptalonZooStatuette : CollectionStatuette
{
    [Constructible]
    public ReptalonZooStatuette()
        : base(0x2D95)
    {
    }

    public override int LabelNumber => 1074852; // Interactive Reptalon Contribution Statue from the Britannia Royal Zoo.
}

[SerializationGenerator(0, false)]
public partial class SpecialAchievementZooStatuette : CollectionStatuette
{
    [Constructible]
    public SpecialAchievementZooStatuette()
        : base(0x2FF6)
    {
        this.Weight = 10.0;
    }

    public override int LabelNumber => 1073226; // Britannia Royal Zoo Special Achievement Award
}

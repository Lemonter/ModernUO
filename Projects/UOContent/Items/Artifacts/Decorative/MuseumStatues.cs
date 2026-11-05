using ModernUO.Serialization;
using System;
using Server.Network;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Decorative/MuseumStatues.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class VesperCollectionStatuette : BaseStatuette
{
    public VesperCollectionStatuette(int itemID)
        : base(itemID)
    {
        this.LootType = LootType.Blessed;
        this.Weight = 1.0;
    }

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        if (this.TurnedOn && this.IsLockedDown && (!m.Hidden || m.Player) && Utility.InRange(m.Location, this.Location, 2) && !Utility.InRange(oldLocation, this.Location, 2))
        {
            int cliloc = Utility.RandomMinMax(1073266, 1073286);

            if (cliloc == 1073282)
            {
                cliloc -= 1;
            }

            this.PublicOverheadMessage(MessageType.Regular, 0x3B2, cliloc);
            Effects.PlaySound(this.Location, this.Map, Utility.Random(0x17));
        }

        base.OnMovement(m, oldLocation);
    }
}

[SerializationGenerator(0, false)]
public partial class TrollStatuette : VesperCollectionStatuette
{
    [Constructible]
    public TrollStatuette()
        : base(0x20E9)
    {
    }

    public override int LabelNumber => 1073242; // G'Thunk the Troll - Museum of Vesper Replica
}

[SerializationGenerator(0, false)]
public partial class CrystalBallStatuette : VesperCollectionStatuette
{
    [Constructible]
    public CrystalBallStatuette()
        : base(0xE2E)
    {
    }

    public override int LabelNumber => 1073244; // Nystul's Crystal Ball - Museum of Vesper Replica
}

[SerializationGenerator(0, false)]
public partial class DevourerStatuette : VesperCollectionStatuette
{
    [Constructible]
    public DevourerStatuette()
        : base(0x2623)
    {
    }

    public override int LabelNumber => 1073245; // Dangerous Creatures Replica: Devourer of Souls - Museum of Vesper
}

[SerializationGenerator(0, false)]
public partial class SnowLadyStatuette : VesperCollectionStatuette
{
    [Constructible]
    public SnowLadyStatuette()
        : base(0x276C)
    {
    }

    public override int LabelNumber => 1075016; // Dangerous Creatures Replica: Lady of the Snow - Museum of Vesper
}

[SerializationGenerator(0, false)]
public partial class GolemStatuette : VesperCollectionStatuette
{
    [Constructible]
    public GolemStatuette()
        : base(0x2610)
    {
    }

    public override int LabelNumber => 1075017; // Dangerous Creatures Replica: Golem - Museum of Vesper
}

[SerializationGenerator(0, false)]
public partial class ExodusOverseerStatuette : VesperCollectionStatuette
{
    [Constructible]
    public ExodusOverseerStatuette()
        : base(0x260C)
    {
    }

    public override int LabelNumber => 1075018; // Dangerous Creatures Replica: Exodus Overseer - Museum of Vesper
}

[SerializationGenerator(0, false)]
public partial class JukaLordStatuette : VesperCollectionStatuette
{
    [Constructible]
    public JukaLordStatuette()
        : base(0x25FC)
    {
    }

    public override int LabelNumber => 1075019; // Dangerous Creatures Replica: Juka Lord- Museum of Vesper
}

[SerializationGenerator(0, false)]
public partial class MeerCaptainStatuette : VesperCollectionStatuette
{
    [Constructible]
    public MeerCaptainStatuette()
        : base(0x25FA)
    {
    }

    public override int LabelNumber => 1075020; // Dangerous Creatures Replica: Meer Captain - Museum of Vesper
}

[SerializationGenerator(0, false)]
public partial class MeerEternalStatuette : VesperCollectionStatuette
{
    [Constructible]
    public MeerEternalStatuette()
        : base(0x25F8)
    {
    }

    public override int LabelNumber => 1075021; // Dangerous Creatures Replica: Meer Eternal - Museum of Vesper
}

[SerializationGenerator(0, false)]
public partial class SolenQueenStatuette : VesperCollectionStatuette
{
    [Constructible]
    public SolenQueenStatuette()
        : base(0x2602)
    {
    }

    public override int LabelNumber => 1075022; // Dangerous Creatures Replica: Solen Queen - Museum of Vesper
}

[SerializationGenerator(0, false)]
public partial class VesperSpecialAchievementReplica : VesperCollectionStatuette
{
    [Constructible]
    public VesperSpecialAchievementReplica()
        : base(0x2D4E)
    {
    }

    public override int LabelNumber => 1073265; // Museum of Vesper Special Achievement Replica
}

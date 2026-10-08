using ModernUO.Serialization;

namespace Server.Items;

/// <summary>The four offerings the Palace of Paroxysmus altar accepts, ported from ServUO
/// (Scripts/Items/Quest/). Kept in one file — the originals are four separate files only
/// because ServUO keeps one type per file.</summary>
[SerializationGenerator(0, false)]
public partial class PartiallyDigestedTorso : PeerlessKey
{
    [Constructible]
    public PartiallyDigestedTorso() : base(0x1D9F) => Weight = 1.0;

    public override int LabelNumber => 1074326; // partially digested torso
}

[SerializationGenerator(0, false)]
public partial class CoagulatedLegs : PeerlessKey
{
    [Constructible]
    public CoagulatedLegs() : base(0x1CDF) => Weight = 1.0;

    public override int LabelNumber => 1074327; // coagulated legs
}

[SerializationGenerator(0, false)]
public partial class GelatanousSkull : PeerlessKey
{
    [Constructible]
    public GelatanousSkull() : base(0x1AE0) => Weight = 1.0;

    public override int LabelNumber => 1074328; // gelatanous skull
}

[SerializationGenerator(0, false)]
public partial class SpleenOfThePutrefier : PeerlessKey
{
    [Constructible]
    public SpleenOfThePutrefier() : base(0x1CEE) => Weight = 1.0;

    public override int LabelNumber => 1074329; // spleen of the putrefier
}

/// <summary>The key the Palace of Paroxysmus altar hands back.</summary>
[SerializationGenerator(0, false)]
public partial class ParoxysmusKey : MasterKey
{
    [Constructible]
    public ParoxysmusKey() : base(0xEFB)
    {
        Weight = 1.0;
        Hue = 0x497;
    }

    public override int LabelNumber => 1074330; // slimy ointment
}

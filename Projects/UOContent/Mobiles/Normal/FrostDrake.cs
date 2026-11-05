using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/FrostDrake.cs). ServUO reuses the cold
/// drake's name verbatim; here it gets its own Russian name so the two are distinguishable.</summary>
[SerializationGenerator(0, false)]
public partial class FrostDrake : ColdDrake
{
    [Constructible]
    public FrostDrake()
    {
    }

    public override string CorpseName => "труп дрейка";
    public override string DefaultName => "морозный дрейк";
}

using ModernUO.Serialization;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class GrizzledMare : HellSteed
{
    public override string DefaultName => "седая кобыла";

    [Constructible]
    public GrizzledMare()
    {
    }

    public override bool DeleteOnRelease => true;
}

using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/SpeckledScorpion.cs). Extends the
/// local Scorpion base (confirmed to exist, already ModernUO-native).</summary>
[SerializationGenerator(0, false)]
[CorpseName("a speckled scorpion corpse")]
public partial class SpeckledScorpion : Scorpion
{
    [Constructible]
    public SpeckledScorpion()
    {
        Tamable = false;
    }

    public override string DefaultName => "пятнистый скорпион";

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        if (Utility.RandomDouble() < 0.4)
        {
            c.DropItem(new SpeckledPoisonSac());
        }
    }
}

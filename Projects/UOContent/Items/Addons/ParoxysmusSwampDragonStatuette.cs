using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/ParoxysmusSwampDragonStatuette.cs).
///     Dropped APIs: none.
/// </summary>
[SerializationGenerator(0, false)]
public partial class ParoxysmusSwampDragonStatuette : BaseImprisonedMobile
{
    [Constructible]
    public ParoxysmusSwampDragonStatuette() : base(0x2619)
    {
        Weight = 1.0;
        Hue = 0x851;
    }

    public override int LabelNumber => 1072084; // Paroxysmus' Swamp Dragon

    public override BaseCreature Summon => new ParoxysmusSwampDragon();
}

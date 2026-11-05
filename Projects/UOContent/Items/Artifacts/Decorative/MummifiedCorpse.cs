using ModernUO.Serialization;
using System;
using Server.Network;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Decorative/MummifiedCorpse.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class MummifiedCorpse : Item
{
    public override int LabelNumber => 1112400; //a mummified corpse

    [Constructible]
    public MummifiedCorpse() : base( 0x1C20 )
    {
    }
}

using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Juggernaut/MalekisHonor.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class MalekisHonor : MetalKiteShield
{
    [Constructible]
    public MalekisHonor()
        : base()
    {
    }

    public override int LabelNumber => 1074312; // Maleki's Honor (Juggernaut Set)

    public override int BasePhysicalResistance => 3;

    public override int BaseFireResistance => 3;

    public override int BaseColdResistance => 3;

    public override int BasePoisonResistance => 3;

    public override int BaseEnergyResistance => 3;
}

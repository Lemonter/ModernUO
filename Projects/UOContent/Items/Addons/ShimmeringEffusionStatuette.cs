using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/ShimmeringEffusionStatuette.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class ShimmeringEffusionStatuette : BaseStatuette
{
    private static readonly int[] m_Sounds = new int[]
    {
        0x58D, 0x58E, 0x58F, 0x590, 0x591, 0x592, 0x593, 0x594, 0x595
    };
    [Constructible]
    public ShimmeringEffusionStatuette()
        : base(0x2D87)
    {
        this.Weight = 1.0;
    }

    public ShimmeringEffusionStatuette(int itemID) : base(itemID) => this.Weight = 1.0;

    public override int LabelNumber => 1074503; // Shimmering Effusion Statuette
    public override void PlaySound(Mobile to)
    {
        Effects.PlaySound(this.Location, this.Map, m_Sounds[Utility.Random(m_Sounds.Length)]);
    }
}

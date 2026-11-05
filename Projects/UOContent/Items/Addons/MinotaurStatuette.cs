using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/MinotaurStatuette.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class MinotaurStatuette : BaseStatuette
{
    private static readonly int[] m_Sounds = new int[]
    {
        0x596, 0x597, 0x598, 0x599, 0x59A, 0x59B, 0x59C, 0x59D
    };
    [Constructible]
    public MinotaurStatuette()
        : base(0x2D89)
    {
        this.Name = "статуэтка минотавра";
        this.Weight = 1.0;
    }

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        if (this.TurnedOn && this.IsLockedDown && (!m.Hidden || m.Player) && Utility.InRange(m.Location, this.Location, 2) && !Utility.InRange(oldLocation, this.Location, 2))
            Effects.PlaySound(this.Location, this.Map, m_Sounds[Utility.Random(m_Sounds.Length)]);

        base.OnMovement(m, oldLocation);
    }
}

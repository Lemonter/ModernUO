using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/DreadHornStatuette.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class DreadHornStatuette : BaseStatuette
{
    private static readonly int[] m_Sounds = new int[]
    {
        0xA8, 0xA9, 0xAA, 0xAB, 0xAC
    };
    [Constructible]
    public DreadHornStatuette()
        : base(0x2D83)
    {
        this.Name = "Ужасный Рог";
        this.Weight = 5.0;
    }

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        if (this.TurnedOn && this.IsLockedDown && (!m.Hidden || m.Player) && Utility.InRange(m.Location, this.Location, 2) && !Utility.InRange(oldLocation, this.Location, 2))
            Effects.PlaySound(this.Location, this.Map, m_Sounds[Utility.Random(m_Sounds.Length)]);

        base.OnMovement(m, oldLocation);
    }
}

using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/DreadSpiderStatuette.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class DreadSpiderStatuette : BaseStatuette
{
    private static readonly int[] m_Sounds = new int[]
    {
        0x493, 0x494, 0x495, 0x496, 0x497
    };
    [Constructible]
    public DreadSpiderStatuette()
        : base(0x25C4)
    {
        this.Name = "Ужасный Паук";
        this.Weight = 5.0;
    }

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        if (this.TurnedOn && this.IsLockedDown && (!m.Hidden || m.Player) && Utility.InRange(m.Location, this.Location, 2) && !Utility.InRange(oldLocation, this.Location, 2))
            Effects.PlaySound(this.Location, this.Map, m_Sounds[Utility.Random(m_Sounds.Length)]);

        base.OnMovement(m, oldLocation);
    }
}

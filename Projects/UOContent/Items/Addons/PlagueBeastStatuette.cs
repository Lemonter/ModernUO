using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/PlagueBeastStatuette.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class PlagueBeastStatuette : BaseStatuette
{
    private static readonly int[] m_Sounds = new int[]
    {
        0x1BF, 0x1C0, 0x1C1, 0x1C2
    };
    [Constructible]
    public PlagueBeastStatuette()
        : base(0x2613)
    {
        this.Name = "чумной зверь";
        this.Weight = 5.0;
    }

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        if (this.TurnedOn && this.IsLockedDown && (!m.Hidden || m.Player) && Utility.InRange(m.Location, this.Location, 2) && !Utility.InRange(oldLocation, this.Location, 2))
            Effects.PlaySound(this.Location, this.Map, m_Sounds[Utility.Random(m_Sounds.Length)]);

        base.OnMovement(m, oldLocation);
    }
}

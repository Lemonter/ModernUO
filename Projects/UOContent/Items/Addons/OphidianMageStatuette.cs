using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/OphidianMageStatuette.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class OphidianMageStatuette : BaseStatuette
{
    private static readonly int[] m_Sounds = new int[]
    {
        0x280, 0x281, 0x282, 0x283, 0x284
    };
    [Constructible]
    public OphidianMageStatuette()
        : base(0x25AB)
    {
        this.Name = "офидианский маг";
        this.Weight = 5.0;
    }

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        if (this.TurnedOn && this.IsLockedDown && (!m.Hidden || m.Player) && Utility.InRange(m.Location, this.Location, 2) && !Utility.InRange(oldLocation, this.Location, 2))
            Effects.PlaySound(this.Location, this.Map, m_Sounds[Utility.Random(m_Sounds.Length)]);

        base.OnMovement(m, oldLocation);
    }
}

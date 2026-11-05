using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/OphidianKnightStatuette.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class OphidianKnightStatuette : BaseStatuette
{
    private static readonly int[] m_Sounds = new int[]
    {
        0x27B, 0x27C, 0x27D, 0x27E, 0x27F
    };
    [Constructible]
    public OphidianKnightStatuette()
        : base(0x25AA)
    {
        this.Name = "офидианский рыцарь";
        this.Weight = 5.0;
    }

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        if (this.TurnedOn && this.IsLockedDown && (!m.Hidden || m.Player) && Utility.InRange(m.Location, this.Location, 2) && !Utility.InRange(oldLocation, this.Location, 2))
            Effects.PlaySound(this.Location, this.Map, m_Sounds[Utility.Random(m_Sounds.Length)]);

        base.OnMovement(m, oldLocation);
    }
}

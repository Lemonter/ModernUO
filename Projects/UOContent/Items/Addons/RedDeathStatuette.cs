using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/RedDeathStatuette.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class RedDeathStatuette : BaseStatuette
{
    private static readonly int[] m_Sounds = new int[]
    {
        0xE5, 0xE6, 0xE7, 0xE8, 0xE9
    };
    [Constructible]
    public RedDeathStatuette()
        : base(0x2617)
    {
        this.Name = "Красная Смерть";
        this.Weight = 5.0;
    }

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        if (this.TurnedOn && this.IsLockedDown && (!m.Hidden || m.Player) && Utility.InRange(m.Location, this.Location, 2) && !Utility.InRange(oldLocation, this.Location, 2))
            Effects.PlaySound(this.Location, this.Map, m_Sounds[Utility.Random(m_Sounds.Length)]);

        base.OnMovement(m, oldLocation);
    }
}

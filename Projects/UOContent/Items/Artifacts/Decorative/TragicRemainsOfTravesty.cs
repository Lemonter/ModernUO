using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Decorative/TragicRemainsOfTravesty.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class TragicRemainsOfTravesty : BaseStatuette
{
    private static readonly int[] _sounds = new int[]
    {
        0x314, 0x315, 0x316, 0x317  // TODO check
    };

    [Constructible]
    public TragicRemainsOfTravesty()
        : base(Utility.Random(0x122A, 6))
    {
        this.Weight = 1.0;
        this.Hue = Utility.RandomList(0x11E, 0x846);
    }

    public override int LabelNumber => 1074500; // Tragic Remains of the Travesty

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        if (this.TurnedOn && this.IsLockedDown && (!m.Hidden || m.Player) && Utility.InRange(m.Location, this.Location, 2) && !Utility.InRange(oldLocation, this.Location, 2))
        {
            Effects.PlaySound(this.Location, this.Map, _sounds[Utility.Random(_sounds.Length)]);
        }

        base.OnMovement(m, oldLocation);
    }
}

using System;
using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items;

/// <summary>The Blighted Grove altar. Ported from ServUO
/// (Scripts/Items/Functional/BlightedGroveAltar.cs). Takes three Dryad's Blessings and wakes
/// Lady Melisande.</summary>
[SerializationGenerator(0, false)]
public partial class BlightedGroveAltar : PeerlessAltar
{
    [Constructible]
    public BlightedGroveAltar() : base(0x207B)
    {
        BossLocation = new Point3D(6483, 947, 23);
        TeleportDest = new Point3D(6518, 946, 36);
        ExitDest = new Point3D(587, 1641, -1);
    }

    public override int KeyCount => 3;
    public override MasterKey MasterKey => new BlightedGroveKey();

    public override Type[] Keys { get; } = { typeof(DryadsBlessing) };

    public override BasePeerless Boss => new LadyMelisande();

    public override Rectangle2D[] BossBounds { get; } = { new(6456, 922, 84, 47) };
}

/// <summary>Ported from ServUO. The key the Blighted Grove altar hands back.</summary>
[SerializationGenerator(0, false)]
public partial class BlightedGroveKey : MasterKey
{
    [Constructible]
    public BlightedGroveKey() : base(0x1012)
    {
        Hue = 0x494;
    }
}

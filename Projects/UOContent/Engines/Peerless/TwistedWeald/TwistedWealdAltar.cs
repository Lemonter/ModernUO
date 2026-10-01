using System;
using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items;

/// <summary>The Twisted Weald altar. Ported from ServUO
/// (Scripts/Items/Functional/TwistedWealdAltar.cs). Takes three of the Weald's six offerings
/// and wakes Dread Horn.</summary>
[SerializationGenerator(0, false)]
public partial class TwistedWealdAltar : PeerlessAltar
{
    [Constructible]
    public TwistedWealdAltar() : base(0x207C)
    {
        BossLocation = new Point3D(2137, 1247, -60);
        TeleportDest = new Point3D(2151, 1261, -60);
        ExitDest = new Point3D(1448, 1537, -28);
    }

    public override int KeyCount => 3;
    public override MasterKey MasterKey => new TwistedWealdKey();

    public override Type[] Keys { get; } =
    {
        typeof(BlightedCotton),
        typeof(GnawsFang),
        typeof(IrksBrain),
        typeof(LissithsSilk),
        typeof(SabrixsEye),
        typeof(ThornyBriar)
    };

    public override BasePeerless Boss => new DreadHorn();

    public override Rectangle2D[] BossBounds { get; } = { new(2126, 1237, 33, 38) };
}

/// <summary>Ported from ServUO. The key the Twisted Weald altar hands back.</summary>
[SerializationGenerator(0, false)]
public partial class TwistedWealdKey : MasterKey
{
    [Constructible]
    public TwistedWealdKey() : base(0x1012)
    {
        Hue = 0x2D1;
    }
}

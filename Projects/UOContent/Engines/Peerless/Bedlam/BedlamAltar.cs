using System;
using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items;

/// <summary>The Bedlam altar. Ported from ServUO
/// (Scripts/Items/Functional/BedlamAltar.cs). Three librarian's keys wake the Monstrous
/// Interred Grizzle.</summary>
[SerializationGenerator(0, false)]
public partial class BedlamAltar : PeerlessAltar
{
    [Constructible]
    public BedlamAltar() : base(0x207E)
    {
        BossLocation = new Point3D(106, 1615, 90);
        TeleportDest = new Point3D(101, 1623, 50);
        ExitDest = new Point3D(2068, 1372, -75);
    }

    public override int KeyCount => 3;
    public override MasterKey MasterKey => new BedlamKey();

    public override Type[] Keys { get; } = { typeof(LibrariansKey) };

    public override BasePeerless Boss => new MonstrousInterredGrizzle();

    public override Rectangle2D[] BossBounds { get; } = { new(99, 1609, 14, 18) };
}

/// <summary>The offering Bedlam's altar takes, ported from ServUO
/// (Scripts/Items/Quest/LibrariansKey.cs).</summary>
[SerializationGenerator(0, false)]
public partial class LibrariansKey : PeerlessKey
{
    [Constructible]
    public LibrariansKey() : base(0xFF3)
    {
        Weight = 1.0;
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1074347; // librarian's key
}

/// <summary>The key the Bedlam altar hands back.</summary>
[SerializationGenerator(0, false)]
public partial class BedlamKey : MasterKey
{
    [Constructible]
    public BedlamKey() : base(0xFF3)
    {
    }
}

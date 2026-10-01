using System;
using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items;

/// <summary>The Citadel altar. Ported from ServUO
/// (Scripts/Items/Functional/CitadelAltar.cs). Takes one key from each of the three clans and
/// wakes Travesty.
///
/// The only altar whose arena and entrance are on different facets: the fight is on Malas, but
/// players come in from — and go back out to — Isamu-Jima, which is why ExitMap is overridden.
/// The way in is the crate at Tokuno 1344,769 (Items/Misc/The Citadel/CitadelTele.cs).</summary>
[SerializationGenerator(0, false)]
public partial class CitadelAltar : PeerlessAltar
{
    [Constructible]
    public CitadelAltar() : base(0x207E)
    {
        BossLocation = new Point3D(86, 1955, 0);
        TeleportDest = new Point3D(111, 1955, 0);
        ExitDest = new Point3D(1355, 779, 17);
    }

    public override int KeyCount => 3;
    public override MasterKey MasterKey => new CitadelKey();

    public override Type[] Keys { get; } =
    {
        typeof(TigerClawKey),
        typeof(SerpentFangKey),
        typeof(DragonFlameKey)
    };

    public override BasePeerless Boss => new Travesty();

    public override Rectangle2D[] BossBounds { get; } = { new(66, 1936, 51, 39) };

    public override Map ExitMap => Map.Tokuno;
}

/// <summary>The third clan key, ported from ServUO (Scripts/Items/Quest/TigerClawKey.cs). Its
/// two siblings, SerpentFangKey and DragonFlameKey, were ported earlier and live in
/// Items/Misc/The Citadel/.</summary>
[SerializationGenerator(0, false)]
public partial class TigerClawKey : PeerlessKey
{
    [Constructible]
    public TigerClawKey() : base(0x2002)
    {
        Weight = 2.0;
        Hue = 105;
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1074342; // tiger claw key
}

/// <summary>The key the Citadel altar hands back.</summary>
[SerializationGenerator(0, false)]
public partial class CitadelKey : MasterKey
{
    [Constructible]
    public CitadelKey() : base(0x1012)
    {
        Hue = 0x489;
    }

    public override int LabelNumber => 1074344; // black order key
}

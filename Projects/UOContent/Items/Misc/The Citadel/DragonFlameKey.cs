using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Ported from ServUO (Scripts/Items/Quest/DragonFlameKey.cs) — dropped by
/// DragonsFlameGrandMage, one of the three clan keys the Citadel altar takes.
///
/// This was originally written as a plain Item, with a comment saying the PeerlessKey base
/// "doesn't exist here" and that nothing ported the room mechanic it would have unlocked.
/// Both are now true again: the base is in Engines/Peerless and CitadelAltar wants exactly
/// this type.</summary>
[SerializationGenerator(0, false)]
public partial class DragonFlameKey : PeerlessKey
{
    [Constructible]
    public DragonFlameKey() : base(0x2002)
    {
        Weight = 2.0;
        Hue = 42;
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1074343; // dragon flame key
}

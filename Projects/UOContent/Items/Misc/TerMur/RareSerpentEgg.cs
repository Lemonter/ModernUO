using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Ported from ServUO (Scripts/Services/InstancedPeerless/Medusa/RareSerpentEgg.cs).
/// Medusa's offering.
///
/// Written as a plain Item originally because the PeerlessKey base hadn't been ported; it now
/// sits on the real base. Medusa herself belongs to ServUO's separate InstancedPeerless system,
/// which is not ported here — this is the key with no lock yet.</summary>
[SerializationGenerator(0, false)]
public partial class RareSerpentEgg : PeerlessKey
{
    [Constructible]
    public RareSerpentEgg() : base(0x41BF)
    {
        Weight = 1.0;
        LootType = LootType.Blessed;
        Hue = Utility.RandomList(0x21, 0x4AC, 0x41C, 0xA21);
    }

    public override int LabelNumber => 1112575; // a rare serpent egg
}

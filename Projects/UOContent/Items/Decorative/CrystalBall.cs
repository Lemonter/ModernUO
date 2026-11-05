using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MagicCrystalBall : Item
{
    [Constructible]
    public MagicCrystalBall() : base(0xE2E)
    {
        Name = "магический хрустальный шар";
        Weight = 10.0;
        Stackable = false;
        LootType = LootType.Blessed;
        Light = LightType.Circle150;
    }

    public override void OnDoubleClick(Mobile from)
    {
        PublicOverheadMessage(MessageType.Regular, 0x3B2, 1007000 + Utility.Random(28));
    }
}

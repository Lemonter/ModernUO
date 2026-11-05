using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SecretShadowWallNS : BaseSliding
{
    [Constructible]
    public SecretShadowWallNS() : base(0x363A, 0x3619) => Name = "секретная дверь";
}

[SerializationGenerator(0, false)]
public partial class SecretDungeonWallNS : BaseSliding
{
    [Constructible]
    public SecretDungeonWallNS() : base(0x0242, 0x0244) => Name = "секретная дверь";
}

[SerializationGenerator(0, false)]
public partial class SecretStoneWallNS : BaseSliding
{
    [Constructible]
    public SecretStoneWallNS() : base(0x3C9, 0x3CA)
    {
        Name = "секретная дверь";
        Hue = 744;
    }
}

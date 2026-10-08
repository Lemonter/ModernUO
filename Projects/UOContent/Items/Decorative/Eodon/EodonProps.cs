using ModernUO.Serialization;

namespace Server.Items;

// Original content — see Mobiles/Monsters/Reptile/Eodon/Dinosaurs.cs header for why
// (ServUO has no Eodon content to port from). Plain decorative statics referenced by
// Distribution/XmlSpawner/Eodon.xml's spawn points; placeholder ItemIDs, swap for real
// Time of Legends art later if the client assets are ever confirmed.

[SerializationGenerator(0, false)]
public partial class NestWithEgg : Item
{
    [Constructible]
    public NestWithEgg() : base(0x915)
    {
        Movable = false;
        Name = "гнездо с яйцом";
    }
}

[SerializationGenerator(0, false)]
public partial class CubeEnclosure : Item
{
    [Constructible]
    public CubeEnclosure() : base(0x319)
    {
        Movable = false;
        Name = "загон";
    }
}

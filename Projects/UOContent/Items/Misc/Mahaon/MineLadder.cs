using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MineLadder : Item
{
    [Constructible]
    public MineLadder() : base(0x79E)
    {
        Movable = false;
        Name = "лестница вглубь шахты";
    }
}

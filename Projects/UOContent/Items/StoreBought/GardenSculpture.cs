using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0xA565, 0xA566)]
[SerializationGenerator(0, false)]
public partial class GardenSculpture : Item, IDyable
{
    [Constructible]
    public GardenSculpture() : base(0xA565)
    {
    }

    public override string DefaultName => "садовая скульптура";

    public bool Dye(Mobile from, DyeTub sender)
    {
        if (Deleted)
        {
            return false;
        }

        Hue = sender.DyedHue;

        return true;
    }
}

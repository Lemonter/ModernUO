using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKhaldun/Items/Meteorite.cs).
///     The polished flag keeps ServUO's side effect (re-art the item) through a <c>[SerializableProperty]</c>
///     setter, as the task rules allow for setters that change state.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Meteorite : Item
{
    [SerializableProperty(0)]
    [CommandProperty(AccessLevel.GameMaster)]
    public bool Polished
    {
        get => _polished;
        set
        {
            if (_polished == value)
            {
                return;
            }

            _polished = value;

            if (_polished)
            {
                Polish();
            }
            else
            {
                UnPolish();
            }

            InvalidateProperties();
        }
    }

    public override int LabelNumber => _polished ? 1125446 : 1158640; // Polished/Rough Meteorite

    [Constructible]
    public Meteorite() : base(Utility.Random(0x1364, 3))
    {
        Hue = 1910;
    }

    public void TryPolish(Mobile from)
    {
        if (0.75 > Utility.RandomDouble())
        {
            Polished = true;
        }
        else
        {
            from.SendLocalizedMessage(1158694); // You attempt to polish the meteorite, but its delicate structure crumbles at the attempt.
            Delete();
        }
    }

    private void Polish()
    {
        Hue = 0;
        ItemID = Utility.Random(0xA1CE, 12);
    }

    private void UnPolish()
    {
        Hue = 1910;
        ItemID = Utility.Random(0x1364, 3);
    }
}

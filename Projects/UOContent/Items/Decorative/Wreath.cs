using ModernUO.Serialization;
using Server.Multis;

namespace Server.Items;

/// <summary>
///     Портировано из ServUO. Признак артефакта (IsArtifact) в этом репозитории отсутствует,
///     поэтому он опущен.
/// </summary>
[Flippable(0xA12E, 0xA12F)]
[SerializationGenerator(0, false)]
public partial class HolidayWreath : Item, IDyable
{
    [SerializableField(0)]
    private int _madeID;

    [Constructible]
    public HolidayWreath() : base(0xA12E) => _madeID = Utility.Random(1114138, 13);

    public override int LabelNumber => 1029004; // wreath

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1158828, $"#{_madeID}"); // Made From Handpicked Trees Near ~1_WHERE~
    }

    public virtual bool Dye(Mobile from, DyeTub sender)
    {
        if (Deleted)
        {
            return false;
        }

        var house = BaseHouse.FindHouseAt(this);

        if (house == null || !house.IsCoOwner(from))
        {
            return false;
        }

        if (!from.InRange(GetWorldLocation(), 1))
        {
            from.SendLocalizedMessage(500295); // You are too far away to do that.
            return false;
        }

        Hue = sender.DyedHue;
        return true;
    }
}

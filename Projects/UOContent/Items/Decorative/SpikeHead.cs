using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SpikeHead : Item
{
    private static readonly string[] _spikeHeadType =
    {
        "Голова МиссТраблмейкер на колу",
        "Голова Брутрина на колу",
        "Голова Стетуна на колу",
        "Голова Ракбана на колу",
        "Голова Тэтблока на колу",
        "Голова Грифона на колу",
        "Голова Кироникса на колу",
        "Голова Миска на колу",
        "Голова Блика на колу",
        "Голова Онифрка на колу",
        "Голова Мезанны на колу"
    };

    [SerializableField(0)]
    private int _gumpID;

    [Constructible]
    public SpikeHead() : this(Utility.Random(_spikeHeadType.Length))
    {
    }

    [Constructible]
    public SpikeHead(int type) : base(0x9955 + type)
    {
        _gumpID = 30522 + type;

        Name = _spikeHeadType[type];
    }

    public override void OnDoubleClick(Mobile m)
    {
        if (!m.InRange(GetWorldLocation(), 3))
        {
            m.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            return;
        }

        var gump = new Gump(100, 100);
        gump.AddImage(0, 0, _gumpID);

        m.SendGump(gump);
    }
}

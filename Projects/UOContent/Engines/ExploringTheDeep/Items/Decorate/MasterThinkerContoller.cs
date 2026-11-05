using Server.Mobiles;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Engines.ExploringTheDeep;

namespace Server.Items;

public enum DecorType
{
    Tunic,
    Pant,
    Book
}

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Items/Decorate/MasterThinkerContoller.cs).
///     The three Master Thinker artefacts in the Ice Dungeon workshop; reading all three is what tips the
///     player off that Cousteau Perron is the tinker they were sent to find.
///
///     ServUO kept the per-player click bookkeeping in a <c>static List&lt;MasterThinkerArray&gt;</c> that
///     leaked an entry for every player who ever clicked and started empty again after a save. It is a
///     per-controller dictionary here, cleared as soon as the set is complete, and it is not serialized —
///     the same "resets on restart" behaviour without the leak. The quest gate that consumed it
///     ("HeplerPaulsonComplete") is <see cref="ExploringDeepChain.SalvageMasterDone" /> here, and since the
///     tinker's quest now requires exactly that, the click set only unlocks the flavour of finding him.
/// </summary>
[SerializationGenerator(0, false)]
public partial class MasterThinkerContoller : Item
{
    private readonly Dictionary<Mobile, int> _progress = new();

    [SerializableField(0, setter: "private")]
    private MasterThinkerDecor _book;

    [SerializableField(1, setter: "private")]
    private MasterThinkerDecor _pant;

    [SerializableField(2, setter: "private")]
    private MasterThinkerDecor _tunic;

    [Constructible]
    public MasterThinkerContoller() : base(0x1F13)
    {
        Name = "контроллер мастера-мыслителя — не удалять!";
        Visible = false;
        Movable = false;

        _book = new MasterThinkerDecor(0x42BF, 0, DecorType.Book, this);
        _book.MoveToWorld(new Point3D(1651, 1549, 49), Map.Trammel);

        _pant = new MasterThinkerDecor(0x1539, 2017, DecorType.Pant, this);
        _pant.MoveToWorld(new Point3D(1651, 1545, 47), Map.Trammel);

        _tunic = new MasterThinkerDecor(0x1FA2, 398, DecorType.Tunic, this);
        _tunic.MoveToWorld(new Point3D(1653, 1549, 47), Map.Trammel);
    }

    public void Note(Mobile from, DecorType type)
    {
        _progress.TryGetValue(from, out var seen);

        seen |= 1 << (int)type;
        _progress[from] = seen;

        if (seen != 0b111)
        {
            return;
        }

        // All three artefacts have been read; the tinker is the next stop on the trail.
        _progress.Remove(from);
    }
}

[SerializationGenerator(0, false)]
public partial class MasterThinkerDecor : Item
{
    [SerializableField(0, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    [InvalidateProperties]
    private DecorType _type;

    [SerializableField(1, setter: "private")]
    private MasterThinkerContoller _controller;

    [Constructible]
    public MasterThinkerDecor() : base(0x42BF) => Movable = false;

    [Constructible]
    public MasterThinkerDecor(int id, int hue, DecorType type, MasterThinkerContoller controller)
    {
        ItemID = id;
        _controller = controller;
        _type = type;
        Hue = hue;
        Movable = false;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (_controller == null)
        {
            return;
        }

        _controller.Note(from, _type);

        if (_type == DecorType.Book)
        {
            // *You thumb through the pages of the book, it seems to describe the anatomy of a variety of
            // frost creatures*
            from.PublicOverheadMessage(MessageType.Regular, 0x3B2, 1154222);
        }
        else
        {
            // *You carefully examine the garment and take note of it's superior quality. You surmise it
            // would be useful in keeping you warm in a cold environment*
            from.PublicOverheadMessage(MessageType.Regular, 0x3B2, 1154221);
        }
    }
}

using Server.Engines.ExploringTheDeep;
using ModernUO.Serialization;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Items/SorcerersPlates.cs). The reagent
///     identification puzzle in the Sorcerer's Dungeon: five plates, each wanting one specific "perfect"
///     reagent, and a reward chest that opens with a key once all five are correct.
///
///     Differences from the original:
///     <list type="bullet">
///         <item>
///             The <see cref="SorcerersPlate.RegsTarget" /> target object kept its mobile and plate in
///             static fields, so two players targeting at once clobbered each other. They are instance
///             fields here.
///         </item>
///         <item>
///             The original's <c>QuestHelper</c> gate (only during the "Sorcerers" step of the chain)
///             becomes <see cref="ExploringDeepChain.IsDoing" /> against
///             <c>TheSorcerersSchematics</c>.
///         </item>
///     </list>
/// </summary>
[SerializationGenerator(0, false)]
public partial class SorcerersPlateController : Item
{
    public static readonly string EntityName = "sorcerersplate";

    [SerializableField(0, setter: "private")]
    private SorcerersPlate _perfectBlackPearl;

    [SerializableField(1, setter: "private")]
    private SorcerersPlate _burstingBrimstone;

    [SerializableField(2, setter: "private")]
    private SorcerersPlate _brightDaemonBlood;

    [SerializableField(3, setter: "private")]
    private SorcerersPlate _mightyMandrake;

    [SerializableField(4, setter: "private")]
    private SorcerersPlate _burlyBone;

    [Constructible]
    public SorcerersPlateController() : base(0x1F13)
    {
        Name = "контроллер пластин чародеев — не удалять!";
        Visible = false;
        Movable = false;

        MoveToWorld(new Point3D(100, 49, -22), Map.Ilshenar);

        CreateSorcerersPlates();

        new BrightDaemonBloodDecor().MoveToWorld(new Point3D(149, 26, -28), Map.Ilshenar);
        new BurlyBoneDecor().MoveToWorld(new Point3D(159, 7, -23), Map.Ilshenar);
        new BurstingBrimstoneDecor().MoveToWorld(new Point3D(125, 9, -28), Map.Ilshenar);
        new MightyMandrakeDecor().MoveToWorld(new Point3D(98, 36, -18), Map.Ilshenar);
        new PerfectBlackPearlDecor().MoveToWorld(new Point3D(161, 63, -21), Map.Ilshenar);
        new SorcerersRewardChest().MoveToWorld(new Point3D(100, 41, -22), Map.Ilshenar);
    }

    /// <summary>Rebuilds the whole puzzle room, replacing any previous copy.</summary>
    public static void Generate()
    {
        WeakEntityCollection.Delete(EntityName);

        WeakEntityCollection.Add(EntityName, new SorcerersPlateController());
    }

    public void CreateSorcerersPlates()
    {
        _brightDaemonBlood = new SorcerersPlate(this, SorcerersPlate.RegsType.BrightDaemonBlood);
        _brightDaemonBlood.MoveToWorld(new Point3D(101, 45, -22), Map.Ilshenar);

        _burlyBone = new SorcerersPlate(this, SorcerersPlate.RegsType.BurlyBone);
        _burlyBone.MoveToWorld(new Point3D(101, 43, -22), Map.Ilshenar);

        _burstingBrimstone = new SorcerersPlate(this, SorcerersPlate.RegsType.BurstingBrimstone);
        _burstingBrimstone.MoveToWorld(new Point3D(101, 46, -22), Map.Ilshenar);

        _mightyMandrake = new SorcerersPlate(this, SorcerersPlate.RegsType.MightyMandrake);
        _mightyMandrake.MoveToWorld(new Point3D(101, 44, -22), Map.Ilshenar);

        _perfectBlackPearl = new SorcerersPlate(this, SorcerersPlate.RegsType.PerfectBlackPearl);
        _perfectBlackPearl.MoveToWorld(new Point3D(101, 47, -22), Map.Ilshenar);
    }

    public void Validate()
    {
        if (!Validate(_perfectBlackPearl) || !Validate(_burstingBrimstone) || !Validate(_brightDaemonBlood) ||
            !Validate(_mightyMandrake) || !Validate(_burlyBone))
        {
            return;
        }

        if (TheMasterInstructor.Spawn(new Point3D(105, 38, -28), Map.Ilshenar, this) == null)
        {
            return;
        }

        Clear(_perfectBlackPearl);
        Clear(_burstingBrimstone);
        Clear(_brightDaemonBlood);
        Clear(_mightyMandrake);
        Clear(_burlyBone);
    }

    public void Clear(SorcerersPlate plate)
    {
        if (plate == null)
        {
            return;
        }

        Effects.SendBoltEffect(plate);

        plate.Reg?.Delete();
        plate.Delete();
    }

    public bool Validate(SorcerersPlate plate) => plate?.Reg?.Deleted == false;
}

[SerializationGenerator(0, false)]
public partial class SorcerersPlate : Item
{
    public enum RegsType
    {
        PerfectBlackPearl,
        BurstingBrimstone,
        BrightDaemonBlood,
        MightyMandrake,
        BurlyBone
    }

    [SerializableField(0, setter: "private")]
    [InvalidateProperties]
    private RegsType _type;

    [SerializableField(1, setter: "private")]
    private SorcerersPlateController _controller;

    [SerializableField(2, setter: "private", fieldChanged: nameof(OnRegChanged))]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Item _reg;

    [Constructible]
    public SorcerersPlate() : this(null, RegsType.PerfectBlackPearl)
    {
    }

    [Constructible]
    public SorcerersPlate(SorcerersPlateController controller, RegsType type) : base(0x9D7)
    {
        _type = type;
        _controller = controller;
        Movable = false;
        Hue = 2406;
    }

    public override int LabelNumber => 1011199; // Plate


    private void OnRegChanged(Item oldValue, Item newValue) => _controller?.Validate();

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        // The Plate is Inscribed: ~1_CLUE~
        list.Add(1154248, $"#{1154249 + (int)_type}");
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from is PlayerMobile)
        {
            from.Target = new RegsTarget(from, this);
        }
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (_controller == null)
        {
            Delete();
        }
    }

    private class RegsTarget : Target
    {
        private readonly Mobile _from;
        private readonly SorcerersPlate _plate;

        public RegsTarget(Mobile from, SorcerersPlate plate) : base(2, false, TargetFlags.None)
        {
            _from = from;
            _plate = plate;

            from.SendLocalizedMessage(1154254); // What do you wish to put on the plate?
        }

        private static bool CheckRegs(Item item) =>
            item is PerfectBlackPearl or BurstingBrimstone or BrightDaemonBlood or MightyMandrake or BurlyBone;

        protected override void OnTarget(Mobile from, object o)
        {
            if (o is not Item item)
            {
                return;
            }

            if (!item.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1054107); // This item must be in your backpack.
                return;
            }

            if (!CheckRegs(item))
            {
                // *You place the item on the plate but it simply melts away. You had better search for a
                // more pure reagent*
                from.PublicOverheadMessage(MessageType.Regular, 0x3B2, 1154255);
                return;
            }

            if (from is not PlayerMobile pm || !ExploringDeepChain.IsDoing(pm, typeof(Server.Engines.MLQuests.Definitions.TheSorcerersSchematics)))
            {
                // You feel as though by doing this you are missing out on an important part of your journey...
                from.SendLocalizedMessage(1154325);
                return;
            }

            if (item.GetType().Name != _plate.Type.ToString())
            {
                from.SendLocalizedMessage(500309); // Nothing Happens.
                return;
            }

            // *You place the reagent on the plate and hear an ominous click in the background...*
            from.PublicOverheadMessage(MessageType.Regular, 0x3B2, 1154256);

            item.MoveToWorld(_plate.GetWorldTop(), _plate.Map);
            item.Movable = false;
            _plate.Reg = item;
        }
    }
}

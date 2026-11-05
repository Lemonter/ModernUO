using ModernUO.Serialization;
using Server.Targeting;

namespace Server.Items;

public class PumpkinDefinition
{
    public PumpkinDefinition(int unlit, int lit)
    {
        UnlitItemID = unlit;
        LitItemID = lit;
    }

    public int UnlitItemID { get; set; }
    public int LitItemID { get; set; }
}

[Flippable(0x992D, 0x992E)]
[SerializationGenerator(0, false)]
public partial class PumpkinCarvingKit : Item
{
    [Constructible]
    public PumpkinCarvingKit() : base(0x992D)
    {
        Weight = 1.0;
        Hue = 1258;
    }

    public override int LabelNumber => 1154271; // Jack O' Lantern Carving Kit

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1080058); // This must be in your backpack to use it.
            return;
        }

        from.SendLocalizedMessage(1154273); // What do you wish to carve?
        from.Target = new InternalTarget(this);
    }

    private class InternalTarget : Target
    {
        private readonly Item _item;

        public InternalTarget(Item item) : base(2, true, TargetFlags.None) => _item = item;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is not BaseCarvablePumpkin pumpkin)
            {
                from.SendLocalizedMessage(1154272); // That is not suitable for carving.
                return;
            }

            from.PlaySound(0x249);

            var definitions = pumpkin.PumpkinDefinition;

            pumpkin.ItemID = definitions[Utility.Random(definitions.Length)].UnlitItemID;
            pumpkin.CarvedBy = from.Name;

            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1154339); // *You carefully carve the pumpkin*

            _item.Delete();
        }
    }
}

[SerializationGenerator(0, false)]
public partial class BaseCarvablePumpkin : BaseLight
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _carvedBy;

    [Constructible]
    public BaseCarvablePumpkin(int id) : base(id)
    {
    }

    public override int LabelNumber => _carvedBy == null ? 1123239 : 1096937; // Carvable Pumpkin | carved pumpkin

    public virtual PumpkinDefinition[] PumpkinDefinition => [];

    public override int LitItemID
    {
        get
        {
            foreach (var definition in PumpkinDefinition)
            {
                if (definition.UnlitItemID == ItemID)
                {
                    return definition.LitItemID;
                }
            }

            return 0;
        }
    }

    public override int UnlitItemID
    {
        get
        {
            foreach (var definition in PumpkinDefinition)
            {
                if (definition.LitItemID == ItemID)
                {
                    return definition.UnlitItemID;
                }
            }

            return 0;
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (_carvedBy == null)
        {
            return;
        }

        base.OnDoubleClick(from);
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_carvedBy != null)
        {
            list.Add(1154350, _carvedBy); // Carved By ~1_NAME~
        }
    }
}

[SerializationGenerator(0, false)]
public partial class CarvablePumpkinTall : BaseCarvablePumpkin
{
    private static readonly PumpkinDefinition[] _definitions =
    [
        new(0x9934, 0x9931),
        new(0x9935, 0x9936),
        new(0x9939, 0x993A),
        new(0x993D, 0x993E),
        new(0x9941, 0x9942),
        new(0x9945, 0x9946),
        new(0x9949, 0x994A),
        new(0x9951, 0x9952)
    ];

    [Constructible]
    public CarvablePumpkinTall() : base(0x992F)
    {
    }

    public override PumpkinDefinition[] PumpkinDefinition => _definitions;

    public override double DefaultWeight => 16.0;
}

[SerializationGenerator(0, false)]
public partial class CarvableGordPumpkinTall : BaseCarvablePumpkin
{
    private static readonly PumpkinDefinition[] _definitions =
    [
        new(0x9D23, 0x9D24),
        new(0x9D27, 0x9D28)
    ];

    [Constructible]
    public CarvableGordPumpkinTall() : base(0x9D22)
    {
    }

    public override PumpkinDefinition[] PumpkinDefinition => _definitions;

    public override double DefaultWeight => 18.0;
}

[SerializationGenerator(0, false)]
public partial class CarvablePlainPumpkin : BaseCarvablePumpkin
{
    private static readonly PumpkinDefinition[] _definitions =
    [
        new(0x9F23, 0x9F24),
        new(0x9F27, 0x9F28)
    ];

    [Constructible]
    public CarvablePlainPumpkin() : base(0x9F51)
    {
    }

    public override PumpkinDefinition[] PumpkinDefinition => _definitions;

    public override double DefaultWeight => 18.0;
}

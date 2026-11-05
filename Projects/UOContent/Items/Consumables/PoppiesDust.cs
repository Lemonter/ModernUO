using ModernUO.Serialization;
using Server.Engines.Plants;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class PoppiesDust : Item, IUsesRemaining
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _usesRemaining;

    [Constructible]
    public PoppiesDust(int charges = 8) : base(0xC4D)
    {
        Weight = 1.0;
        Stackable = false;
        _usesRemaining = charges;
    }

    public override int LabelNumber => 1095223; // poppies dust

    public bool ShowUsesRemaining
    {
        get => true;
        set { }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1060741, _usesRemaining.ToString()); // charges: ~1_val~
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042664); // You must have the object in your backpack to use it.
            return;
        }

        from.Target = new InternalTarget(this);
        from.SendLocalizedMessage(500343); // What do you wish to appraise and identify?
    }

    private class InternalTarget : Target
    {
        private readonly PoppiesDust _dust;

        public InternalTarget(PoppiesDust dust) : base(3, false, TargetFlags.None) => _dust = dust;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (_dust.Deleted || _dust.UsesRemaining <= 0)
            {
                return;
            }

            if (!_dust.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042664); // You must have the object in your backpack to use it.
            }
            else if (targeted is PoppiesDust other && other != _dust)
            {
                other.UsesRemaining += _dust.UsesRemaining;
                _dust.Delete();

                from.SendLocalizedMessage(1158448); // You combine the charges on the items.
            }
            else if (targeted is Seed seed)
            {
                if (seed.ShowType)
                {
                    from.SendLocalizedMessage(1114369, "", 946); // This seed has already been identified.
                    return;
                }

                seed.ShowType = true;

                --_dust.UsesRemaining;
            }
        }
    }
}

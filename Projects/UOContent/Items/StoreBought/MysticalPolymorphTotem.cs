using System;
using ModernUO.Serialization;
using Server.Engines.BuffIcons;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MysticalPolymorphTotem : Item
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _costumeBody;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _costumeHue = -1;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _costumeCreatureName;

    [SerializableField(3)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _duration;

    [SerializableField(4)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _transformed;

    private Mobile _owner;
    private TimerExecutionToken _timerToken;

    [Constructible]
    public MysticalPolymorphTotem() : base(0xA276)
    {
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1158780; // Mystical Polymorph Totem

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_costumeCreatureName != null)
        {
            list.Add(1158707, _costumeCreatureName); // a ~1_name~ costume
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1060640); // The item must be in your backpack to use it.
            return;
        }

        if (_costumeBody == 0)
        {
            // Target the costume that you wish to merge with this Mystical Polymorph Totem...
            from.SendLocalizedMessage(1158781);
            from.Target = new InternalTarget(this);
        }
        else if (!_transformed)
        {
            EnMask(from);
        }
        else
        {
            DeMask(from);
        }
    }

    public override bool DropToWorld(Mobile from, Point3D p)
    {
        var drop = base.DropToWorld(from, p);

        if (_transformed)
        {
            DeMask(from);
        }

        return drop;
    }

    private void EnMask(Mobile from)
    {
        if (from.Mounted || from.Flying)
        {
            from.SendLocalizedMessage(1010097); // You cannot use this while mounted or flying.
            return;
        }

        if (from.IsBodyMod || from.HueMod > -1)
        {
            from.SendLocalizedMessage(1158010); // You cannot use that item in this form.
            return;
        }

        _duration = 28800;
        _owner = from;

        if (!_timerToken.Running)
        {
            Timer.StartTimer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), Slice, out _timerToken);
        }

        (from as PlayerMobile)?.AddBuff(
            new BuffInfo(
                BuffIcon.MysticalPolymorphTotem,
                1158780,
                1158017,
                TimeSpan.FromSeconds(_duration),
                _costumeCreatureName
            )
        );

        ItemID = 0xA20B;
        from.BodyMod = _costumeBody;
        from.HueMod = _costumeHue;
        _transformed = true;
    }

    private void Slice()
    {
        if (_owner == null || _owner.Deleted)
        {
            _timerToken.Cancel();
            return;
        }

        if (_duration > 0)
        {
            _duration--;
            return;
        }

        DeMask(_owner);
    }

    private void DeMask(Mobile from)
    {
        ItemID = 0xA276;
        from.BodyMod = 0;
        from.HueMod = -1;
        _transformed = false;

        Effects.SendLocationParticles(
            EffectItem.Create(from.Location, from.Map, EffectItem.DefaultDuration),
            0x3728,
            8,
            20,
            5042
        );

        from.PlaySound(250);
        (from as PlayerMobile)?.RemoveBuff(BuffIcon.MysticalPolymorphTotem);

        _timerToken.Cancel();
        _owner = null;
    }

    [AfterDeserialization]
    private void AfterDeserialization() => ItemID = 0xA276;

    public override void OnAfterDelete()
    {
        _timerToken.Cancel();
        _owner = null;

        base.OnAfterDelete();
    }

    private class InternalTarget : Target
    {
        private readonly MysticalPolymorphTotem _totem;

        public InternalTarget(MysticalPolymorphTotem totem) : base(12, true, TargetFlags.None) => _totem = totem;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (_totem.Deleted)
            {
                return;
            }

            if (!_totem.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1060640); // The item must be in your backpack to use it.
                return;
            }

            if (targeted is not BaseCostume costume)
            {
                return;
            }

            _totem._costumeCreatureName = costume.CreatureName;
            _totem._costumeBody = costume.CostumeBody;

            _totem.InvalidateProperties();

            costume.Delete();
        }
    }
}

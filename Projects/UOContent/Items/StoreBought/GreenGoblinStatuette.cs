using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     В этом репозитории у MonsterStatuette нет типов GreenGoblin/GreyGoblin и виртуальных
///     TurnOn/TurnOff, поэтому статуэтка сделана самостоятельным предметом с тем же поведением.
/// </summary>
[SerializationGenerator(0, false)]
public partial class GreenGoblinStatuette : Item
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Mobile _user;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _turnedOn;

    [Constructible]
    public GreenGoblinStatuette() : base(0xA097)
    {
        LootType = LootType.Blessed;
        Weight = 1.0;
    }

    public override string DefaultName => "статуэтка зелёного гоблина";

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            return;
        }

        if (_turnedOn)
        {
            TurnOff();
        }
        else if (from.Mounted)
        {
            from.SendLocalizedMessage(1010097); // You cannot use this while mounted.
        }
        else if (from.Flying)
        {
            from.SendLocalizedMessage(1113414); // You can't use this while flying!
        }
        else if (from.IsBodyMod)
        {
            from.SendLocalizedMessage(1111896); // You may only change forms while in your original body.
        }
        else
        {
            TurnOn(from);
        }
    }

    public void TurnOn(Mobile from)
    {
        _user = from;
        _turnedOn = true;

        from.BodyMod = 723;
        from.HueMod = 0;

        ItemID = 0xA098;

        from.FixedParticles(0x3728, 1, 13, 5042, EffectLayer.Waist);
    }

    public void TurnOff()
    {
        _turnedOn = false;

        if (_user != null)
        {
            _user.BodyMod = 0;
            _user.HueMod = -1;
        }

        ItemID = 0xA097;

        _user = null;
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (_user != null)
        {
            TurnOn(_user);
        }
    }

    public override bool OnDragLift(Mobile from)
    {
        if (_turnedOn)
        {
            TurnOff();
        }

        return base.OnDragLift(from);
    }

    public override void OnDelete()
    {
        if (_turnedOn)
        {
            TurnOff();
        }

        base.OnDelete();
    }

    public override void OnItemRemoved(Item item)
    {
        if (_turnedOn)
        {
            TurnOff();
        }

        base.OnItemRemoved(item);
    }

    public override void OnRemoved(IEntity parent)
    {
        if (_turnedOn)
        {
            TurnOff();
        }

        base.OnRemoved(parent);
    }

    public override void OnAfterDelete()
    {
        _user = null;

        base.OnAfterDelete();
    }
}

using System;
using ModernUO.Serialization;
using Server.Spells;
using Server.Spells.Seventh;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKhaldun/Items/MaskOfKhalAnkur.cs).
///     ServUO's <c>IsArtifact</c> is dropped (absent here), the recharge timer is a token-based repeat timer
///     cancelled in <see cref="OnAfterDelete" />, and the charges line moved from <c>AddWeightProperty</c> into
///     <c>GetProperties</c>.
/// </summary>
[SerializationGenerator(0, false)]
public partial class MaskOfKhalAnkur : BaseHat
{
    [SerializableField(0, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _chargesLeft;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _chargeTime;

    private TimerExecutionToken _chargeToken;

    public override int LabelNumber => 1158701; // Mask of Khal Ankur

    [Constructible]
    public MaskOfKhalAnkur() : this(0)
    {
    }

    [Constructible]
    public MaskOfKhalAnkur(int hue) : base(0xA1C7, hue)
    {
        Weight = 3;

        Charges = 1;

        Attributes.BonusHits = 10;
        Attributes.BonusMana = 15;
        Attributes.EnhancePotions = 35;
        Attributes.LowerManaCost = 10;
    }

    public int Charges
    {
        get => _chargesLeft;
        set
        {
            _chargesLeft = value;

            if (_chargesLeft == 0)
            {
                StartTimer();
            }

            InvalidateProperties();
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (Parent != from)
        {
            from.SendLocalizedMessage(1116250); // That must be equipped before you can use it.
        }
        else if (_chargeTime > 0)
        {
            from.SendLocalizedMessage(1074882, $"{_chargeTime}"); // You must wait ~1_val~ seconds for this to recharge.
        }
        else if (SpellHelper.CheckTown(from.Location, from))
        {
            new MeteorSwarmSpell(from).Cast();
        }
    }

    public override int BasePhysicalResistance => 15;

    public override int BaseFireResistance => 15;

    public override int BaseColdResistance => 15;

    public override int BasePoisonResistance => 15;

    public override int BaseEnergyResistance => 15;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1158732, $"{_chargesLeft}"); // Meteor Breath Charges: ~1_VAL~
    }

    public virtual void StartTimer()
    {
        _chargeTime = 300;

        if (!_chargeToken.Running)
        {
            Timer.StartTimer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), Slice, out _chargeToken);
        }
    }

    public virtual void StopTimer() => _chargeToken.Cancel();

    public virtual void Slice()
    {
        if (_chargeTime > 0)
        {
            _chargeTime--;
            return;
        }

        _chargeTime = 0;
        Charges = 1;

        StopTimer();
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        _chargeToken.Cancel();
    }
}


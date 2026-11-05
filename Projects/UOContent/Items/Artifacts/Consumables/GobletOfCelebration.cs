using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Consumables/GobletOfCelebration.cs).
///     ServUO keeps the refill timer in a Timer field; here it is a TimerExecutionToken cancelled in
///     OnAfterDelete, per this codebase's timer rules.
/// </summary>
[SerializationGenerator(0, false)]
public partial class GobletOfCelebration : Item
{
    private bool _full;
    private TimerExecutionToken _fillToken;

    [Constructible]
    public GobletOfCelebration() : base(0x99A)
    {
        LootType = LootType.Blessed;
        Weight = 1.0;
        Hue = 19;

        _full = true;
    }

    public override int LabelNumber => 1075430; // Goblet of Celebration

    [CommandProperty(AccessLevel.GameMaster)]
    public bool Full
    {
        get => _full;
        set
        {
            if (_full == value)
            {
                return;
            }

            _full = value;
            InvalidateProperties();

            _fillToken.Cancel();

            if (!_full)
            {
                Timer.StartTimer(TimeSpan.FromDays(1.0), () => Full = true, out _fillToken);
            }
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        base.OnDoubleClick(from);

        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1075438); // You can only drink from the goblet of celebration when its in your inventory.
            return;
        }

        if (!_full)
        {
            from.SendLocalizedMessage(1075439); // You need to wait a day for the goblet of celebration to be replenished.
            return;
        }

        from.SendLocalizedMessage(1075272); // You drink from the goblet of celebration

        Full = false;

        from.BAC = 60;
        BaseBeverage.CheckHeaveTimer(from);
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(_full ? 1042972 : 1042975); // It's full. / It's empty.
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        _fillToken.Cancel();
    }
}

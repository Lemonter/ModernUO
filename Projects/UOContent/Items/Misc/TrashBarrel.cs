using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Engines.CleanUpBritannia;
using Server.Multis;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class TrashBarrel : Container, IChoppable
{
    private Timer _timer;

    // Сдаваемые предметы и их стоимость: очки выдаются при опустошении бочки, как в ServUO.
    private readonly List<(Mobile From, Item Item, double Points)> _pending = new();

    [Constructible]
    public TrashBarrel() : base(0xE77)
    {
        Hue = 0x3B2;
        Movable = false;
    }

    public override int LabelNumber => 1041064; // a trash barrel

    public override int DefaultMaxWeight => 0; // A value of 0 signals unlimited weight

    public override bool IsDecoContainer => false;

    public void OnChop(Mobile from)
    {
        var house = BaseHouse.FindHouseAt(from);

        if (house?.IsCoOwner(from) == true)
        {
            Effects.PlaySound(Location, Map, 0x3B3);
            from.SendLocalizedMessage(500461); // You destroy the item.
            Destroy();
        }
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        if (Items.Count > 0)
        {
            _timer = new EmptyTimer(this);
            _timer.Start();
        }
    }

    private void InvalidateContents(Mobile from)
    {
        if (TotalItems >= 50)
        {
            Empty(501478); // The trash is full!  Emptying!
        }
        else
        {
            SendLocalizedMessageTo(from, 1010442); // The item will be deleted in three minutes

            if (_timer != null)
            {
                _timer.Stop();
            }
            else
            {
                _timer = new EmptyTimer(this);
            }

            _timer.Start();
        }
    }

    public override bool OnDragDrop(Mobile from, Item dropped)
    {
        if (base.OnDragDrop(from, dropped))
        {
            RecordForCleanup(from, dropped);
            InvalidateContents(from);
            return true;
        }

        return false;
    }

    public override bool OnDragDropInto(Mobile from, Item item, Point3D p)
    {
        if (base.OnDragDropInto(from, item, p))
        {
            RecordForCleanup(from, item);
            InvalidateContents(from);
            return true;
        }

        return false;
    }

    private void RecordForCleanup(Mobile from, Item item)
    {
        if (!CleanUpBritanniaData.Enabled)
        {
            return;
        }

        var points = TrashTurnIn.Collect(item);

        if (points > 0)
        {
            _pending.Add((from, item, points));
        }
    }

    public void Empty(int message)
    {
        var items = Items;

        if (items.Count > 0)
        {
            PublicOverheadMessage(MessageType.Regular, 0x3B2, message);

            TrashTurnIn.AwardPending(this, _pending);

            for (var i = items.Count - 1; i >= 0; --i)
            {
                if (i >= items.Count)
                {
                    continue;
                }

                items[i].Delete();
            }
        }

        _pending.Clear();

        _timer?.Stop();
        _timer = null;
    }

    private class EmptyTimer : Timer
    {
        private TrashBarrel _barrel;

        public EmptyTimer(TrashBarrel barrel) : base(TimeSpan.FromMinutes(3.0)) => _barrel = barrel;

        protected override void OnTick()
        {
            _barrel.Empty(501479); // Emptying the trashcan!
        }
    }
}

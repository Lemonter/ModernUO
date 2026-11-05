using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class UnderworldSwitchWE : BaseSwitch
{
    [Constructible]
    public UnderworldSwitchWE() : base(0x1091, 0x1092, 1042901, 1042900, true)
    {
        // 1042901 - You hear a deep rumbling as something seems to happen.
        // 1042900 - There seems to be no further effect right now.
    }

    public override void DoSomethingSpecial(Mobile from)
    {
        foreach (var item in GetItemsInRange(8))
        {
            if (item.ItemID == 0x3660 && item.Hue == 1000) // Dark Globe of Sosaria
            {
                new MoveTimer(item).Start();
            }
        }
    }

    private class MoveTimer : Timer
    {
        private readonly Item _item;
        private int _stage;
        private int _cycle;

        public MoveTimer(Item sphere) : base(TimeSpan.Zero, TimeSpan.FromSeconds(1.5)) => _item = sphere;

        protected override void OnTick()
        {
            if (_item.Deleted)
            {
                Stop();
                return;
            }

            _stage++;

            if (_cycle == 0)
            {
                _item.Z += 1;
            }
            else if (_cycle == 1)
            {
                _item.Z += 0;
            }
            else
            {
                _item.Z += -1;
            }

            if (_stage == 8 || _stage == 14)
            {
                _cycle++;
            }
            else if (_stage == 22)
            {
                Stop();
            }
        }
    }
}

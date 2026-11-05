using ModernUO.Serialization;
using System;
using Server.Mobiles;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Decorative/GrapeVine.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class GrapeVine : Item
{
    private DateTime _nextHarvest;

    private readonly double HarvestWait = 4;

    public override int LabelNumber => 1149954;

    [Constructible]
    public GrapeVine() : base(Utility.Random(3355, 10))
    {
        _nextHarvest = Core.Now;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if((ItemID == 3358 || ItemID == 3363) && Movable == false && _nextHarvest < Core.Now)
        {
            from.AddToBackpack(new GrapeBunch());
            _nextHarvest = Core.Now + TimeSpan.FromHours(HarvestWait);
        }
    }
}

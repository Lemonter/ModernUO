using ModernUO.Serialization;
using Server.Engines.Despise;

namespace Server.Items;

/// <summary>Ported from ServUO's Despise Revamped dungeon (Scripts/Items/Quest/
/// PutridHeart.cs) — dropped for a possessed creature's owner when their pet lands the
/// killing blow on an enemy DespiseCreature (see DespiseRegion.OnBeforeDeath). Double-click
/// to redeem for DespiseCrystals points (see that file for why it's a standalone tracker
/// instead of ServUO's generic PointsSystem).</summary>
[SerializationGenerator(0, false)]
public partial class PutridHeart : Item
{
    public override int LabelNumber => 1153424; // putrid heart

    [Constructible]
    public PutridHeart() : this(1)
    {
    }

    [Constructible]
    public PutridHeart(int amount) : base(0xF91)
    {
        Stackable = true;
        Amount = amount;
        Hue = 2599;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!Deleted && DespiseController.Instance != null)
        {
            DespiseCrystals.AwardPoints(from, Amount);
            Delete();
        }
    }
}

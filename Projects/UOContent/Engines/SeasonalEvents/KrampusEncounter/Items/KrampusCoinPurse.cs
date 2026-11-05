using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/KrampusEncounter/Items/KrampusCoinPurse.cs).</summary>
[SerializationGenerator(0, false)]
public partial class KrampusCoinPurse : Bag
{
    [Constructible]
    public KrampusCoinPurse() : this(0)
    {
    }

    [Constructible]
    public KrampusCoinPurse(int karma)
    {
        Name = "кошель Крампуса";

        for (var i = 0; i < 25; i++)
        {
            TryDropItem(null, Loot.RandomGem(), false);
        }

        DropItem(new Gold(Utility.RandomMinMax(2000, 5000)));

        if (Utility.RandomDouble() < 0.25)
        {
            if (Utility.RandomBool())
            {
                DropItem(new DirtySnowballs());
            }
            else if (karma > 0)
            {
                DropItem(new GoldBranch());
            }
            else
            {
                DropItem(new SilverBranch());
            }
        }
    }
}

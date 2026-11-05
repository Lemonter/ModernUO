using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/TigersClawMaster.cs). Also carries the key
/// and sect badge on death — both items exist in this repository.</summary>
[SerializationGenerator(0, false)]
public partial class TigersClawMaster : TigersClawThief
{
    [Constructible]
    public TigersClawMaster()
    {
        Title = "из секты Змеиного Клыка";

        SetStr(440, 460);
        SetDex(400, 415);
        SetInt(200, 215);

        SetHits(850, 875);

        SetDamage(15, 20);

        Fame = 25000;
        Karma = -25000;

        VirtualArmor = 60;
    }

    public override string CorpseName => "труп мастера Чёрного ордена";
    public override string DefaultName => "мастер Чёрного ордена";

    public override bool AlwaysMurderer => true;
    public override bool ShowFameTitle => false;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.AosFilthyRich, 6);
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        c.DropItem(new TigerClawKey());

        if (Utility.RandomDouble() < 0.5)
        {
            c.DropItem(new TigerClawSectBadge());
        }
    }
}

using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/DragonsFlameGrandMage.cs) — the
/// Dragon's Flame Sect's leader; drops the key needed for its own quest chest.</summary>
[SerializationGenerator(0, false)]
[CorpseName("a black order grand mage corpse")]
public partial class DragonsFlameGrandMage : DragonsFlameMage
{
    [Constructible]
    public DragonsFlameGrandMage()
    {
        Title = "of the Dragon's Flame Sect";

        SetStr(340, 360);
        SetDex(200, 215);
        SetInt(500, 515);

        SetHits(800);

        SetDamage(15, 20);

        Fame = 25000;
        Karma = -25000;
    }

    public override string DefaultName => "великий маг Чёрного ордена";

    public override void GenerateLoot()
    {
        AddLoot(LootPack.FilthyRich, 6);
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        c.DropItem(new DragonFlameKey());

        if (Utility.RandomDouble() < 0.5)
        {
            c.DropItem(new DragonFlameSectBadge());
        }
    }
}

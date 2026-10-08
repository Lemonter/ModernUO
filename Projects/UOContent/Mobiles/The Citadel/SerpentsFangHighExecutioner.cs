using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/SerpentsFangHighExecutioner.cs) —
/// the Serpent's Fang Sect's leader; drops the key needed for its own quest chest.</summary>
[SerializationGenerator(0, false)]
[CorpseName("a black order high executioner corpse")]
public partial class SerpentsFangHighExecutioner : SerpentsFangAssassin
{
    [Constructible]
    public SerpentsFangHighExecutioner()
    {
        Title = "of the Serpent's Fang Sect";

        SetStr(545, 560);
        SetDex(160, 175);
        SetInt(160, 175);

        SetHits(800);
        SetStam(190, 205);

        SetDamage(15, 20);

        Fame = 25000;
        Karma = -25000;
    }

    public override string DefaultName => "верховный палач Чёрного ордена";

    public override void GenerateLoot()
    {
        AddLoot(LootPack.FilthyRich, 6);
    }

    public override void AlterMeleeDamageFrom(Mobile from, ref int damage)
    {
        from?.Damage(damage / 2, from);
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        c.DropItem(new SerpentFangKey());

        if (Utility.RandomDouble() < 0.5)
        {
            c.DropItem(new SerpentFangSectBadge());
        }
    }
}

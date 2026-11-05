using ModernUO.Serialization;
using Server.Commands;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.VoidPool;

/// <summary>
///     Ported from ServUO's Covetous void pool (Creatures/Vela.cs) — the reward vendor standing in
///     Cove. She holds one of each artifact in her pack as display stock; the actual purchase is the
///     reward gump, charged against the player's banked void pool points.
///
///     SetWearable doesn't exist here — clothes are added with their hue already set, which is the same
///     outcome (a vendor with fixed clothing, no dressing logic).
/// </summary>
[SerializationGenerator(0, false)]
public partial class VelaTheSorceress : BaseCreature
{
    public override bool IsInvulnerable => true;

    [Constructible]
    public VelaTheSorceress() : base(AIType.AI_Vendor, FightMode.None, 2, 1)
    {
        Name = "Вела";
        Title = "колдунья";
        Blessed = true;

        SetStr(110);
        SetDex(100);
        SetInt(1000);

        Hue = Race.Human.RandomSkinHue();
        Body = 0x191;
        HairItemID = 0x203C;
        HairHue = 0x46D;

        AddItem(new FancyShirt { Hue = 1928 });
        AddItem(new LeatherLegs { Hue = 1928 });
        AddItem(new ThighBoots { Hue = 1917 });

        AddDisplayItem(new BraceletOfProtection());
        AddDisplayItem(new Hephaestus());
        AddDisplayItem(new BlightOfTheTundra());
        AddDisplayItem(new GargishBlightOfTheTundra());
        AddDisplayItem(new Brightblade());
    }

    private void AddDisplayItem(Item item)
    {
        item.Movable = false;
        PackItem(item);
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from is PlayerMobile pm && from.InRange(Location, 5))
        {
            pm.SendGump(new VoidPoolRewardGump(this, pm));
        }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1152664); // Covetous Void Pool Reward Vendor
    }
}


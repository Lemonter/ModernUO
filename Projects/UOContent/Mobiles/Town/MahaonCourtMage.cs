using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Mobiles;

/// <summary>
///     Придворный маг — same shape as MahaonGuardSergeant: stationary, no vendor/shop logic,
///     double-click opens Gumps.MahaonCourtMageGump, which hands off to
///     Systems.MahaonQuests.CourtMageQuestSystem (a "defeat N magical creatures" quest paying
///     Systems.MahaonQuests.CourtMageSystem points, which grant a ManaMax bonus scaled by how
///     much of a Magic-category profession the player's own choice actually is).
///
///     Mahaon: was briefly a BaseVendor selling Runebook + Mysticism scrolls — moved to
///     SBMage.cs instead per the shard owner's ask ("перенеси в продажу нпс маг, которые
///     уже продают рунбуки и свитки другие"), so it's back to this simpler shape.
/// </summary>
[SerializationGenerator(0, false)]
public partial class MahaonCourtMage : BaseCreature
{
    [Constructible]
    public MahaonCourtMage() : base(AIType.AI_Vendor)
    {
        Body = 0x190;
        Name = "Придворный маг";
        Hue = Race.Human.RandomSkinHue();
        Title = "маг";

        SetStr(50, 60);
        SetDex(40, 50);
        SetInt(90, 100);
        SetHits(45, 55);

        CantWalk = true;
        Blessed = true; // never actually fightable — a quest-giver, not a target

        AddItem(new Items.Robe { Hue = 1157 });
        AddItem(new Items.WizardsHat { Hue = 1157 });
        AddItem(new Items.Sandals());
        AddItem(new Items.GnarledStaff());

        Utility.AssignRandomHair(this);
    }

    public override bool ClickTitle => true;
    public override bool ShowFameTitle => false;

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(Location, 3))
        {
            from.SendLocalizedMessage(500446); // That is too far away.
            return;
        }

        if (from is Mobiles.PlayerMobile player)
        {
            player.SendGump(new Gumps.MahaonCourtMageGump(player, this));
        }
    }
}

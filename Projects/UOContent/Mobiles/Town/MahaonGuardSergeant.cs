using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Mobiles;

/// <summary>
///     Сержант Гвидо — face of the city guard (see Systems.MahaonGuard.GuardSystem):
///     double-click shows the player's own guard rank/progress in a gump (with dialogue
///     that mocks or shows respect depending on how the player's rank compares to Guido's
///     own, fixed at "Сержант") and offers a guard/raid-themed kill quest (see
///     Systems.MahaonQuests.GuardQuestSystem) from the same gump.
/// </summary>
[SerializationGenerator(0, false)]
public partial class MahaonGuardSergeant : BaseCreature
{
    [Constructible]
    public MahaonGuardSergeant() : base(AIType.AI_Vendor)
    {
        Body = 0x190;
        Name = "Сержант Гвидо";
        Hue = Race.Human.RandomSkinHue();
        Title = "сержант городской стражи";

        SetStr(80, 90);
        SetDex(60, 70);
        SetInt(40, 50);
        SetHits(70, 80);

        CantWalk = true;
        Blessed = true; // never actually fightable — a quest-giver, not a target

        AddItem(new Items.PlateChest());
        AddItem(new Items.PlateArms());
        AddItem(new Items.PlateLegs());
        AddItem(new Items.PlateGloves());
        AddItem(new Items.Boots());
        AddItem(new Items.Halberd());
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

        if (from is not PlayerMobile pm)
        {
            return;
        }

        from.SendGump(new Gumps.MahaonGuardSergeantGump(pm, this));
    }
}

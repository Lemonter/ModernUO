using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>
///     Лесничий Питэр — simplest possible quest-giver, same shape as MahaonMayor:
///     stationary, no vendor/shop logic, double-click hands off to
///     Systems.MahaonQuests.RangerQuestSystem (a "kill N wildlife" quest paying
///     Systems.MahaonQuests.RangerSystem points, which grant a Stamina bonus scaled by
///     how much of a Ranger the player's own profession actually is).
/// </summary>
[SerializationGenerator(0, false)]
public partial class MahaonRanger : BaseCreature
{
    [Constructible]
    public MahaonRanger() : base(AIType.AI_Vendor)
    {
        Body = 0x190;
        Name = "Лесничий Питэр";
        Hue = Race.Human.RandomSkinHue();
        Title = "лесничий";

        SetStr(60, 70);
        SetDex(70, 80);
        SetInt(40, 50);
        SetHits(55, 65);

        CantWalk = true;
        Blessed = true; // never actually fightable — a quest-giver, not a target

        AddItem(new Items.LeatherChest());
        AddItem(new Items.LeatherLegs());
        AddItem(new Items.LeatherGloves());
        AddItem(new Items.Boots());
        AddItem(new Items.Bow());
        AddItem(new Items.TricorneHat { Hue = Utility.RandomNondyedHue() });
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

        Systems.MahaonQuests.RangerQuestSystem.Talk(from, this);
    }
}

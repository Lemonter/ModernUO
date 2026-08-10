using ModernUO.CodeGeneratedEvents;
using Server.Items;
using Server.Mobiles;

namespace Server.Systems.MahaonSoulStones;

public static class BloodDropSystem
{
    private const double DropChance = 0.10;

    [OnEvent(nameof(BaseCreature.CreatureDeathEvent))]
    public static void OnCreatureDeath(BaseCreature bc)
    {
        var killer = bc.LastKiller is BaseCreature masterCreature
            ? masterCreature.GetDamageMaster(bc)
            : bc.LastKiller;

        if (killer is not PlayerMobile player || Utility.RandomDouble() >= DropChance)
        {
            return;
        }

        var blood = new VialOfBlood(Utility.RandomMinMax(1, 2));

        if (player.Backpack?.TryDropItem(player, blood, false) != true)
        {
            blood.MoveToWorld(bc.Location, bc.Map);
        }
    }
}

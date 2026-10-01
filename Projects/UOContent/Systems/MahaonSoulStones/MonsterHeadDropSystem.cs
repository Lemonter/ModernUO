using ModernUO.CodeGeneratedEvents;
using Server.Items;
using Server.Mobiles;

namespace Server.Systems.MahaonSoulStones;

/// <summary>
///     Drops a MonsterHead on a modest chance from any monster kill — the raw material for
///     Sakuro jewelry. Unlike soul stones this isn't gated behind a tattoo; heads were
///     described as just something you get "for a monster's head", no special condition.
///     Lands in the corpse, not straight into the killer's pack — per the shard owner's
///     ask, same reasoning as the rest of the "cut the corpse open" autoloot design: things
///     shouldn't just appear in your backpack unprompted.
/// </summary>
public static class MonsterHeadDropSystem
{
    private const double DropChance = 0.08;

    [OnEvent(nameof(BaseCreature.CreatureDeathEvent))]
    public static void OnCreatureDeath(BaseCreature bc)
    {
        var killer = bc.LastKiller is BaseCreature masterCreature
            ? masterCreature.GetDamageMaster(bc)
            : bc.LastKiller;

        if (killer is not PlayerMobile)
        {
            return;
        }

        if (Utility.RandomDouble() >= DropChance)
        {
            return;
        }

        var head = new MonsterHead(bc.Name ?? bc.GetType().Name, bc.Fame);

        if (bc.Corpse?.Deleted == false)
        {
            bc.Corpse.DropItem(head);
        }
        else
        {
            head.MoveToWorld(bc.Location, bc.Map);
        }
    }
}

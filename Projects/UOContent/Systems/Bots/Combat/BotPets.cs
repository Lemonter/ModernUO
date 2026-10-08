using Server.Mobiles;

namespace Server.Systems.Bots;

/// <summary>Orders for a bot's followers, given as a player's pet commands would: attack my foe,
/// then follow me.</summary>
public static class BotPets
{
    public static void Attack(PlayerMobile bot, Mobile foe)
    {
        if (bot.AllFollowers == null)
        {
            return;
        }

        foreach (var m in bot.AllFollowers)
        {
            if (m is BaseCreature { Deleted: false, Alive: true, Controlled: true } pet && pet.ControlMaster == bot &&
                !BotStable.IsWorkAnimal(pet) && pet.ControlTarget != foe && pet.Map == bot.Map && pet.InRange(bot, 14))
            {
                pet.ControlTarget = foe;
                pet.ControlOrder = OrderType.Attack;
            }
        }
    }

    public static void Follow(PlayerMobile bot)
    {
        if (bot.AllFollowers == null)
        {
            return;
        }

        foreach (var m in bot.AllFollowers)
        {
            if (m is BaseCreature { Deleted: false, Controlled: true } pet && pet.ControlMaster == bot &&
                pet.ControlOrder != OrderType.Follow)
            {
                pet.ControlTarget = bot;
                pet.ControlOrder = OrderType.Follow;
            }
        }
    }
}

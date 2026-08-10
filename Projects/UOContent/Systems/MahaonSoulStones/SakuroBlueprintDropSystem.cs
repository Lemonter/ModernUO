using ModernUO.CodeGeneratedEvents;
using Server.Items;
using Server.Mobiles;

namespace Server.Systems.MahaonSoulStones;

public static class BlueprintDropSystem
{
    private const double DropChance = 0.03;

    [OnEvent(nameof(BaseCreature.CreatureDeathEvent))]
    public static void OnCreatureDeath(BaseCreature bc)
    {
        var killer = bc.LastKiller is BaseCreature masterCreature
            ? masterCreature.GetDamageMaster(bc)
            : bc.LastKiller;

        if (killer is not PlayerMobile player)
        {
            return;
        }

        if (Utility.RandomDouble() >= DropChance)
        {
            return;
        }

        var unknown = SakuroBlueprintKnowledge.UnknownTypes(player);
        if (unknown.Count == 0)
        {
            return; // knows everything already — nothing left to drop
        }

        var type = unknown.RandomElement();
        var blueprint = new SakuroBlueprint(type);

        if (player.Backpack?.TryDropItem(player, blueprint, false) != true)
        {
            blueprint.MoveToWorld(bc.Location, bc.Map);
        }

        player.SendMessage(0x59, $"Выпадает чертёж: сакуро-перстень «{SakuroTypeRu(type)}».");
    }

    private static string SakuroTypeRu(SakuroType type) => type switch
    {
        SakuroType.Power   => "Сила",
        SakuroType.Agility => "Ловкость",
        SakuroType.Wisdom  => "Мудрость",
        SakuroType.Balance => "Баланс",
        SakuroType.Fox     => "Лис",
        SakuroType.Bear    => "Медведь",
        SakuroType.Owl     => "Сова",
        SakuroType.Titan   => "Титан",
        _                  => type.ToString()
    };
}

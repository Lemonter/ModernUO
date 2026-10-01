using ModernUO.CodeGeneratedEvents;
using Server.Items;
using Server.Mobiles;

namespace Server.Systems.MahaonImbuing;

/// <summary>Дроп материалов наложения чар с монстров — тот же приём, что и у
/// MonsterHeadDropSystem. Магический остаток падает с любого монстра понемногу;
/// осколок реликвии — только с достаточно опасных существ (высокий Fame), чтобы верхний
/// ярус наложения нельзя было просто нафармить с рядовых мобов.</summary>
public static class ImbuingMaterialDropSystem
{
    private const double ResidueDropChance = 0.10;
    private const double FragmentDropChance = 0.04;
    private const int FragmentMinFame = 6000; // примерно уровень боссов чемпионских спавнов и выше

    [OnEvent(nameof(CreatureEvents.CreatureDeathEvent))]
    public static void OnCreatureDeath(BaseCreature bc)
    {
        var killer = bc.LastKiller is BaseCreature masterCreature
            ? masterCreature.GetDamageMaster(bc)
            : bc.LastKiller;

        if (killer is not PlayerMobile player)
        {
            return;
        }

        if (Utility.RandomDouble() < ResidueDropChance)
        {
            GiveOrDrop(player, bc, new MagicalResidue(Utility.RandomMinMax(1, 3)));
        }

        if (bc.Fame >= FragmentMinFame && Utility.RandomDouble() < FragmentDropChance)
        {
            GiveOrDrop(player, bc, new RelicFragment());
        }
    }

    private static void GiveOrDrop(PlayerMobile player, BaseCreature bc, Item item)
    {
        if (player.Backpack?.TryDropItem(player, item, false) != true)
        {
            item.MoveToWorld(bc.Location, bc.Map);
        }
    }
}

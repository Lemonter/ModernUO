using System;
using ModernUO.CodeGeneratedEvents;
using Server.Items;
using Server.Mobiles;
using Server.Spells.Shamanism;

namespace Server.Systems.MahaonShamanism;

/// <summary>
///     Откуда берутся книга шамана и компоненты вершин.
///
///     Владелец шарда не любит «хлам в рюкзаке с любого убийства» (см. MasteryPrimerDropSystem,
///     SakuroBlueprintDropSystem), поэтому обе выдачи узкие:
///
///         • книга — только с шаманов-существ (дикарь-шаман, орк-маг и т.п.) и только тому,
///           у кого её ещё нет;
///         • компонент — только тому, кто уже шаман (Шаманство ≥ 60, то есть вершины ему хоть
///           как-то светят), и только с существ «своего» раздела: орки дают уголь из жерла,
///           гоблины и бесы — гнилое сердце, огры, эттины, титаны и циклопы — сердце горы,
///           воздушные и грозовые твари — перо грозовой птицы. Шанс растёт со славой (Fame).
/// </summary>
public static class ShamanDrops
{
    public const double ComponentSkill = 60.0;

    [OnEvent(nameof(CreatureEvents.CreatureDeathEvent))]
    public static void OnCreatureDeath(BaseCreature bc)
    {
        if (bc.Summoned || bc.Controlled)
        {
            return;
        }

        var killer = bc.LastKiller is BaseCreature pet ? pet.GetDamageMaster(bc) : bc.LastKiller;

        if (killer is not PlayerMobile player)
        {
            return;
        }

        var name = bc.GetType().Name;

        TryBook(player, bc, name);
        TryComponent(player, bc, name);
    }

    private static void TryBook(PlayerMobile player, BaseCreature bc, string typeName)
    {
        if (!typeName.Contains("Shaman") && typeName != "OrcishMage")
        {
            return;
        }

        if (Utility.RandomDouble() >= 0.08 || player.Backpack?.FindItemByType<ShamanBook>() != null)
        {
            return;
        }

        Give(player, bc, new ShamanBook(), "Среди пожитков шамана — потрёпанная книга обрядов.");
    }

    private static void TryComponent(PlayerMobile player, BaseCreature bc, string typeName)
    {
        if (player.Skills.Shamanism.Value < ComponentSkill)
        {
            return;
        }

        var tradition = TraditionOf(typeName);

        if (tradition == null)
        {
            return;
        }

        var chance = 0.02 + Math.Min(0.08, bc.Fame / 150000.0);

        if (Utility.RandomDouble() >= chance)
        {
            return;
        }

        var item = ShamanComponent.Create(tradition.Value);
        Give(player, bc, item, $"Ты добываешь {ShamanComponent.NameFor(tradition.Value)}.");
    }

    private static ShamanTradition? TraditionOf(string typeName)
    {
        if (typeName.Contains("Orc"))
        {
            return ShamanTradition.Orc;
        }

        if (typeName.Contains("Goblin") || typeName.Contains("Imp") || typeName.Contains("Gremlin"))
        {
            return ShamanTradition.Goblin;
        }

        if (typeName.Contains("Ogre") || typeName.Contains("Ettin") || typeName.Contains("Titan") ||
            typeName.Contains("Cyclop") || typeName.Contains("EarthElemental"))
        {
            return ShamanTradition.Ogre;
        }

        if (typeName.Contains("AirElemental") || typeName.Contains("Storm") || typeName.Contains("Harpy") ||
            typeName.Contains("Thunder") || typeName.Contains("Phoenix"))
        {
            return ShamanTradition.Ancient;
        }

        return null;
    }

    private static void Give(PlayerMobile player, BaseCreature bc, Item item, string message)
    {
        if (player.Backpack?.TryDropItem(player, item, false) != true)
        {
            item.MoveToWorld(bc.Location, bc.Map);
        }

        player.SendMessage(0x59, message);
    }
}

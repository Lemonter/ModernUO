using Server.Items;
using Server.Mobiles;

namespace Server.Systems.MahaonSoulStones;

public static class BlueprintDropSystem
{
    // Mahaon: creature-kill drop removed — Sakuro blueprints kept mysteriously appearing in
    // players' packs from any random kill (3% flat chance, no source indication), which the
    // shard owner didn't want. SakuroBlueprint is meant to be chest loot only, via
    // BaseTreasureChest.AddMahaonDrops — this class stays only for SakuroTypeRu, still used
    // by whatever chest-side drop message references it.
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

using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Портировано из ServUO (Scripts/Items/Decorative/DecorativeStableSet/HorseStatue.cs).
///     В ServUO это наследник MonsterStatuette с типом MonsterStatuetteType.Horse, но такого
///     значения в здешнем MonsterStatuetteType нет, поэтому статуя сделана обычным предметом
///     с теми же ItemID и подписью.
/// </summary>
[Flippable(0xA511, 0xA512)]
[SerializationGenerator(0, false)]
public partial class HorseStatue : Item
{
    [Constructible]
    public HorseStatue() : base(0xA511)
    {
        LootType = LootType.Blessed;
        Weight = 1.0;
    }

    public override int LabelNumber => 1018263; // horse
}

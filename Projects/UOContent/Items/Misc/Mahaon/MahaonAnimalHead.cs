using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Real trophy item — dropped into the corpse of any RangerQuestSystem wildlife target
///     on death (any kill, not just while a matching quest is active — a standing trophy,
///     not a quest-only spawn), carried by the player and handed to Лесничий Питэр to make
///     progress. Was a silent kill-counter with no physical item at all — the shard owner
///     specifically wanted a real "head" you loot and turn in, matching the classic bounty-
///     board feel. One class + a type string (matches MahaonOre/MahaonSoulStone's own "one
///     class, not N", per this codebase's established convention) rather than one class per
///     species. Reuses Head.cs's own severed-head graphic (0x1DA0) — there's no separate
///     per-species animal-head art in this asset set, so every trophy looks the same for
///     now; swap in real per-species graphics later if that matters.
/// </summary>
[SerializationGenerator(0, false)]
public partial class MahaonAnimalHead : Item
{
    [SerializableField(0)]
    private string _animalType; // matches BaseCreature.GetType().Name, e.g. "GreatHart"

    [Constructible]
    public MahaonAnimalHead(string animalType = "", string ruName = "") : base(0x1DA0)
    {
        _animalType = animalType;
        Weight = 2.0;
        Name = string.IsNullOrEmpty(ruName) ? "голова животного" : $"голова: {ruName}";
    }
}

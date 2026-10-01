using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MonsterHead : Item
{
    [SerializableField(0)]
    private string _monsterName;

    // Captured from the source creature's Fame at drop time — a rough proxy for how
    // tough/rare it was to get this particular head. Drives the archer stamina quest
    // reward: harder monsters give more stamina, not a flat amount per unique head.
    [SerializableField(1)]
    private int _difficulty;

    [Constructible]
    public MonsterHead(string monsterName = "существо", int difficulty = 0) : base(0x1DA0)
    {
        _monsterName = monsterName;
        _difficulty = difficulty;
    }

    public override string DefaultName => $"голова: {_monsterName}";

    // Mahaon: the direct double-click-for-stamina turn-in (ArcherStaminaQuestSystem) was
    // removed — heads mysteriously topping up stamina with no NPC involved wasn't wanted.
    // MonsterHead is still real crafting material for Sakuro items via SakuroCraftingTool,
    // so the item/drop itself stays; double-click now just points at that instead.
    public override void OnDoubleClick(Mobile from)
    {
        from.SendMessage("Используй резцовый набор (Sakuro Crafting Tool), чтобы применить эту голову.");
    }
}

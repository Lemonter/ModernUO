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

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendMessage("Это должно быть у тебя в рюкзаке, чтобы использовать.");
            return;
        }

        if (from is not Mobiles.PlayerMobile player)
        {
            return;
        }

        from.SendMessage("Ты сдаёшь голову для квеста лучника на стамину. (Подсказка: если хочешь скрафтить из головы сакуро — используй резцовый набор вместо этого.)");

        if (Systems.MahaonArtifacts.ArcherStaminaQuestSystem.TryTurnIn(player, this))
        {
            Delete();
        }
    }
}

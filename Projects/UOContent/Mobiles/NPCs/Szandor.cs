using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>
///     ServUO derives this NPC from MondainQuester, which does not exist in ModernUO. Ported without the
///     quest dialogue and quest list, which depend on the missing ML quest classes.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Szandor : BaseCreature
{
    [Constructible]
    public Szandor() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Title = "покойный архитектор";

        SetSpeed(0.5, 2.0);
        InitStats(100, 100, 25);

        Body = 0x32;
        Hue = 0x83F2;
    }

    public override bool IsInvulnerable => true;
    public override string DefaultName => "Сандор";
    public override bool CanTeach => true;
}

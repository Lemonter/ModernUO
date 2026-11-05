using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>
///     ServUO derives this NPC from MondainQuester, which does not exist in ModernUO. Ported without the
///     quest dialogue and quest list, which depend on the missing ML quest classes.
/// </summary>
[SerializationGenerator(0, false)]
public partial class MaulBear : BaseCreature
{
    [Constructible]
    public MaulBear() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        InitStats(100, 100, 25);

        Female = false;
        Body = 212;
    }

    public override bool IsInvulnerable => true;
    public override string DefaultName => "Маул";
    public override bool CanTeach => true;
}

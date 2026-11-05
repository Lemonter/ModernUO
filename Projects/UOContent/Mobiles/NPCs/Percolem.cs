using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     ServUO derives this NPC from MondainQuester and marks it with ITierQuester, neither of which exists in
///     ModernUO. Ported without the quest dialogue, quest list and tier quest info.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Percolem : BaseCreature
{
    [Constructible]
    public Percolem() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Title = "охотник";
        Race = Race.Human;
        Female = false;
        Body = 0x190;
        Hue = 0x840C;

        SetSpeed(0.5, 2.0);
        InitStats(100, 100, 25);

        HairItemID = 0x203C;
        HairHue = 0x3B3;

        CantWalk = true;
        Blessed = true;

        AddItem(new Boots());
        AddItem(
            new Shirt
            {
                Hue = 1436
            }
        );
        AddItem(
            new ShortPants
            {
                Hue = 1436
            }
        );
        AddItem(new CompositeBow());
    }

    public override bool IsInvulnerable => true;
    public override string DefaultName => "Перколем";
    public override bool CanTeach => true;
}

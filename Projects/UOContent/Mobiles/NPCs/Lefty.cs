using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     ServUO derives this NPC from MondainQuester, which does not exist in ModernUO. Ported without the
///     quest dialogue and quest list, which depend on the missing ML quest classes.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Lefty : BaseCreature
{
    [Constructible]
    public Lefty() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Title = "продавец билетов";
        Race = Race.Human;
        Female = false;
        Body = 0x190;
        Hue = 0x83F4;

        SetSpeed(0.5, 2.0);
        InitStats(100, 100, 25);

        CantWalk = true;

        HairItemID = 0x203B;
        HairHue = 0x470;

        AddItem(
            new ThighBoots
            {
                Hue = 0x901
            }
        );
        AddItem(
            new LongPants
            {
                Hue = 0x70D
            }
        );
        AddItem(
            new Tunic
            {
                Hue = 0x30
            }
        );
        AddItem(
            new Cloak
            {
                Hue = 0x30
            }
        );
    }

    public override bool IsInvulnerable => true;
    public override string DefaultName => "Лефти";
    public override bool CanTeach => true;
}

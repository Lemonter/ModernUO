using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     ServUO derives this NPC from MondainQuester, which does not exist in ModernUO. Ported without the
///     quest dialogue and quest list, which depend on the missing ML quest chain classes.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Brae : BaseCreature
{
    [Constructible]
    public Brae() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Title = "мудрец";
        Race = Race.Elf;
        Female = true;
        Body = 0x25E;
        Hue = 0x80BF;

        SetSpeed(0.5, 2.0);
        InitStats(100, 100, 25);

        HairItemID = 0x2FC2;
        HairHue = 0x8E;

        AddItem(
            new ElvenBoots
            {
                Hue = 0x901
            }
        );
        AddItem(new GemmedCirclet());
        AddItem(
            new FemaleElvenRobe
            {
                Hue = 0x44
            }
        );
    }

    public override bool IsInvulnerable => true;
    public override string DefaultName => "старейшина Брэй";
    public override bool CanTeach => true;
}

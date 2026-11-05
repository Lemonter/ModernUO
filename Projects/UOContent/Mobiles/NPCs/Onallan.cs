using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     ServUO derives this NPC from MondainQuester, which does not exist in ModernUO. Ported without the
///     quest dialogue and quest list, which depend on the missing ML quest classes.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Onallan : BaseCreature
{
    [Constructible]
    public Onallan() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Title = "мудрец";

        SetSpeed(0.5, 2.0);
        InitStats(100, 100, 25);

        Race = Race.Elf;
        Female = false;
        Body = 0x25D;
        CantWalk = true;

        Hue = Race.RandomSkinHue();
        HairItemID = 0x2FD0;
        HairHue = 0x322;

        AddItem(
            new Shoes
            {
                Hue = 0x70A
            }
        );
        AddItem(
            new Cloak
            {
                Hue = 0x651
            }
        );
        AddItem(new WildStaff());
    }

    public override bool IsInvulnerable => true;
    public override string DefaultName => "старейшина Оналлан";
    public override bool CanTeach => true;
}

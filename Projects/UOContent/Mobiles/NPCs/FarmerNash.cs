using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     ServUO derives this NPC from MondainQuester, which does not exist in ModernUO. Ported without the
///     quest dialogue and quest list, which depend on the missing ML quest classes.
/// </summary>
[SerializationGenerator(0, false)]
public partial class FarmerNash : BaseCreature
{
    [Constructible]
    public FarmerNash() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Female = false;
        Race = Race.Gargoyle;
        Hue = 0x840C;

        SetSpeed(0.5, 2.0);
        InitStats(100, 100, 25);

        HairItemID = 0x2045;
        HairHue = 0x453;

        AddItem(new Backpack());
        AddItem(
            new Sandals
            {
                Hue = 0x74A
            }
        );
        AddItem(
            new Robe
            {
                Hue = 0x498
            }
        );
    }

    public override bool IsInvulnerable => true;
    public override string DefaultName => "фермер Нэш";
    public override bool CanTeach => true;
}

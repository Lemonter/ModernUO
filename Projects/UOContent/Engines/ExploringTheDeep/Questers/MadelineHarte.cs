using Server.Engines.MLQuests;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Questers/MadelineHarte.cs). The seamstress at
///     the Adventurer's Needle in Jhelom; sews the canvass robe for the hat that is all she has left of her
///     son. Her story gumps become the ML offer/reward gumps of <c>CanvassRobeQuest</c>.
/// </summary>
[QuesterName("Мэдлин Харт (Джеллом)")]
[SerializationGenerator(0, false)]
public partial class MadelineHarte : BaseCreature
{
    [Constructible]
    public MadelineHarte() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Title = "швея";
        Female = true;
        Race = Race.Human;

        Blessed = true;
        Hue = Race.RandomSkinHue();

        Utility.AssignRandomHair(this);

        AddItem(new Backpack());
        AddItem(new Shoes(0x737));
        AddItem(new Skirt(0x1BB));
        AddItem(new FancyShirt(0x535));
        AddItem(new HalfApron(0x1BB));
    }

    public override string DefaultName => "Мэдлин Харт";

    public override bool IsInvulnerable => true;
    public override bool CanTeach => false;
}

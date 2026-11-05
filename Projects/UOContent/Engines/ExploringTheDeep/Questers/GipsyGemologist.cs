using Server.Engines.MLQuests;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Questers/GipsyGemologist.cs). Zalia, the gypsy
///     gemologist of north-eastern Ilshenar; cuts the aqua pendant for the Aqua Gem. Her story gumps become
///     the ML offer/reward gumps of <c>AquaPendantQuest</c>.
/// </summary>
[QuesterName("Залия (Илшена́р)")]
[SerializationGenerator(0, false)]
public partial class GipsyGemologist : BaseCreature
{
    [Constructible]
    public GipsyGemologist() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Title = "цыганка-геммолог";
        Female = true;
        Race = Race.Human;

        Blessed = true;
        Hue = Race.RandomSkinHue();
        AddItem(new Backpack());
        AddItem(new Shoes(0x737));
        AddItem(new Skirt(0x1BB));
        AddItem(new FancyShirt(0x535));

        Utility.AssignRandomHair(this);
    }

    public override string DefaultName => "Залия";

    public override bool IsInvulnerable => true;
    public override bool CanTeach => false;
}

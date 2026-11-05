using Server.Engines.MLQuests;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Questers/JosefSkimmons.cs). The master
///     blacksmith of Buccaneer's Den; grinds the nictitating lenses in exchange for the orc drilling machine
///     schematics. His story gumps become the ML offer/reward gumps of <c>NictitatingLenses</c>.
/// </summary>
[QuesterName("Джозеф Скиммонс (Буканьерский логов)")]
[SerializationGenerator(0, false)]
public partial class JosefSkimmons : BaseCreature
{
    [Constructible]
    public JosefSkimmons() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Title = "мастер-кузнец";
        Female = false;
        Race = Race.Human;

        Blessed = true;
        Hue = Race.RandomSkinHue();

        Utility.AssignRandomHair(this);

        AddItem(new Backpack());
        AddItem(new Shoes(0x737));
        AddItem(new LongPants(0x1BB));
        AddItem(new FancyShirt(0x535));
        AddItem(new HalfApron(0x1BB));
    }

    public override string DefaultName => "Джозеф Скиммонс";

    public override bool IsInvulnerable => true;
    public override bool CanTeach => false;
}

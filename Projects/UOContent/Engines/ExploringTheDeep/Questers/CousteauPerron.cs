using Server.Engines.MLQuests;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Questers/CusteauPerron.cs). Cousteau Perron,
///     the Master Tinker of the Ice Dungeon; gives the Ice Wyrm errand and later takes the salvager suit
///     plans. ServUO drove her with four hand-written story gumps and an <c>OnDragDrop</c> hand-in; she is
///     an ML quest giver here (see <c>ExploringTheDeepQuests.cs</c>).
/// </summary>
[QuesterName("Кусто Перрон (Ледяная темница)")]
[SerializationGenerator(0, false)]
public partial class CousteauPerron : BaseCreature
{
    [Constructible]
    public CousteauPerron() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Title = "мастер-механик";
        Female = true;
        Race = Race.Human;

        CantWalk = true;
        Blessed = true;
        Hue = Race.RandomSkinHue();

        Utility.AssignRandomHair(this);

        AddItem(new Backpack());
        AddItem(new FurBoots(2017));
        AddItem(new LongPants(2017));
        AddItem(new Doublet(1326));
        AddItem(new Cloak(2017));
        AddItem(new Cap(398));
        AddItem(new LeatherGloves { Hue = 2213 });
    }

    public override string DefaultName => "Кусто Перрон";

    public override bool IsInvulnerable => true;
    public override bool CanTeach => false;
}

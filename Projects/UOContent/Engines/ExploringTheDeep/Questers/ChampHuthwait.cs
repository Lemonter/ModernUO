using Server.Engines.MLQuests;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Questers/ChampHuthwait.cs). The seedy cobbler
///     who trades the Boots of Ballast for proof that Mercutio is dead. His two story gumps become the ML
///     offer/reward gumps of the <c>BallastBoots</c> quest.
/// </summary>
[QuesterName("Чемп Хатвейт (Виспер)")]
[SerializationGenerator(0, false)]
public partial class ChampHuthwait : BaseCreature
{
    [Constructible]
    public ChampHuthwait() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Title = "тёмный сапожник";
        Female = false;
        Race = Race.Human;

        CantWalk = true;
        Blessed = true;
        Hue = Race.RandomSkinHue();

        Utility.AssignRandomHair(this);

        AddItem(new Backpack());
        AddItem(new Boots(2017));
        AddItem(new LongPants(2017));
        AddItem(new FancyShirt(1432));
        AddItem(new JinBaori(1408));
    }

    public override string DefaultName => "Чемп Хатвейт";

    public override bool IsInvulnerable => true;
    public override bool CanTeach => false;
}

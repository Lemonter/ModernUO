using Server.Engines.MLQuests;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Questers/HeplerPaulson.cs). The Salvage Master
///     at the Sons of the Sea in Trinsic; the start and the end of the suit chain.
///
///     Substitutions: ServUO gave him <c>GetProperties</c> that added the "Quest Giver" cliloc by hand, and
///     a <c>Blessed</c> flag; the property comes from <see cref="BaseCreature.CanGiveMLQuest" /> here and
///     invulnerability from <see cref="IsInvulnerable" />.
/// </summary>
[QuesterName("Хеплер Полсон (Тринсик)")]
[SerializationGenerator(0, false)]
public partial class HeplerPaulson : BaseVendor
{
    private readonly List<SBInfo> _sbInfos = new();

    [Constructible]
    public HeplerPaulson() : base("мастер спасения")
    {
        Race = Race.Human;
        CantWalk = true;
        Hue = Race.RandomSkinHue();

        Utility.AssignRandomHair(this);
        InitOutfit();
    }

    protected override List<SBInfo> SBInfos => _sbInfos;

    public override void InitSBInfo() => _sbInfos.Add(new SBHepler());

    public override string DefaultName => "Хеплер Полсон";

    public override bool IsInvulnerable => true;
    public override bool CanTeach => false;

    public override void InitOutfit()
    {
        AddItem(new Backpack());
        AddItem(new Shoes(0x737));
        AddItem(new LongPants(0x1BB));
        AddItem(new FancyShirt(0x535));
    }
}

using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/NPCs/GargishRefugee.cs). A peaceful
/// (FightMode.None) townsperson NPC. GargishClothChest/Kilt/Legs map to this codebase's
/// Type1 variants (see GargishOutcast.cs). SetWearable converted to AddItem.</summary>
[SerializationGenerator(0, false)]
public partial class GargishRefugee : BaseCreature
{
    [Constructible]
    public GargishRefugee() : base(AIType.AI_Melee, FightMode.None, 10, 1)
    {
        Female = Utility.RandomBool();

        if (Female)
        {
            Body = 667;
            HairItemID = 17067;
            HairHue = 1762;
            AddItem(new GargishClothChestType1());
            AddItem(new GargishClothKiltType1 { Hue = Utility.RandomNeutralHue() });
        }
        else
        {
            Body = 666;
            HairItemID = 16987;
            HairHue = 1801;
            AddItem(new GargishClothChestType1());
            AddItem(new GargishClothLegsType1 { Hue = Utility.RandomNeutralHue() });
        }
    }

    public override string DefaultName => "беженец";
}

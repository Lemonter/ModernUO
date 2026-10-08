using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Custom/GargishWarrior.cs). A peaceful
/// (FightMode.None) townsperson NPC. SetWearable(item, dropChance) doesn't exist here —
/// converted to AddItem(...). The full Gargish plate armor set (FemaleGargishPlateChest/Kilt/
/// Legs/Arms, GargishPlateChest/Kilt/Legs/Arms) and PlateTalons don't exist anywhere in this
/// codebase (verified) — dropped, kept only GlassSword (confirmed to exist).</summary>
[SerializationGenerator(0, false)]
public partial class GargishWarrior : BaseCreature
{
    [Constructible]
    public GargishWarrior() : base(AIType.AI_Melee, FightMode.None, 10, 1)
    {
        Title = "Warrior";
        Female = Utility.RandomBool();

        if (Female)
        {
            Body = 667;
            HairItemID = 17067;
            HairHue = 1762;
        }
        else
        {
            Body = 666;
            HairItemID = 16987;
            HairHue = 1801;
        }

        AddItem(new GlassSword());
    }

    public override string DefaultName => "воин";
}

using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class GargishNoble : BaseEscortable
{
    [Constructible]
    public GargishNoble()
    {
        Title = "гаргойский аристократ";

        SetSkill(SkillName.Parry, 80.0, 100.0);
        SetSkill(SkillName.Swords, 80.0, 100.0);
        SetSkill(SkillName.Tactics, 80.0, 100.0);

        if (Female = Utility.RandomBool())
        {
            Body = 667;
            HairItemID = 17067;
            HairHue = 1762;

            AddItem(new GargishClothChestType1 { Hue = Utility.RandomNeutralHue() });
            AddItem(new GargishClothKiltType1 { Hue = Utility.RandomNeutralHue() });
        }
        else
        {
            Body = 666;
            HairItemID = 16987;
            HairHue = 1801;

            AddItem(new GargishClothChestType1 { Hue = Utility.RandomNeutralHue() });
            AddItem(new GargishClothKiltType1 { Hue = Utility.RandomNeutralHue() });
            AddItem(new GargishClothLegsType1 { Hue = Utility.RandomNeutralHue() });

            PackGold(200, 250);
        }
    }
}

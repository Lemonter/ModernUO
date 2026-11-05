using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class Paladin : BaseCreature
{
    [Constructible]
    public Paladin() : base(AIType.AI_Melee, FightMode.Aggressor, 10, 1)
    {
        SetStr(100);
        SetDex(150);
        SetInt(40);

        SetSpeed(0.2, 0.4);

        Body = 0x190;
        Hue = Race.Human.RandomSkinHue();

        switch (Utility.Random(5))
        {
            case 0:
                AddItem(new Helmet());
                break;
            case 1:
                AddItem(new NorseHelm());
                break;
            case 2:
                AddItem(new PlateHelm());
                break;
            case 3:
                AddItem(new Bascinet());
                break;
            case 4:
                AddItem(new ChainCoif());
                break;
        }

        AddItem(new PlateLegs());
        AddItem(new PlateArms());
        AddItem(new PlateGloves());
        AddItem(new PlateChest());
        AddItem(new StuddedGorget());
        AddItem(new VikingSword());
        AddItem(
            new MetalKiteShield
            {
                Hue = 1158
            }
        );

        switch (Utility.Random(3))
        {
            case 0:
                AddItem(
                    new Tunic
                    {
                        Hue = GetRandomHue()
                    }
                );
                break;
            case 1:
                AddItem(
                    new Doublet
                    {
                        Hue = GetRandomHue()
                    }
                );
                break;
            case 2:
                AddItem(
                    new BodySash
                    {
                        Hue = GetRandomHue()
                    }
                );
                break;
        }

        SetSkill(SkillName.Swords, 120);
        SetSkill(SkillName.Tactics, 120);
        SetSkill(SkillName.Anatomy, 120);
        SetSkill(SkillName.MagicResist, 120);
    }

    public override string DefaultName => "паладин";

    private static int GetRandomHue()
    {
        return Utility.Random(5) switch
        {
            1 => Utility.RandomGreenHue(),
            2 => Utility.RandomRedHue(),
            3 => Utility.RandomYellowHue(),
            4 => Utility.RandomNeutralHue(),
            _ => Utility.RandomBlueHue() // 0
        };
    }
}

using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class Aminia : BaseCreature
{
    [Constructible]
    public Aminia() : base(AIType.AI_Melee, FightMode.None, 2, 1)
    {
        Title = "жена главного оружейника";
        Blessed = true;
        SetSpeed(0.5, 2.0);
        InitStats(100, 100, 25);

        Female = true;
        Race = Race.Human;

        Hue = 0x83ED;
        HairItemID = 0x203B;
        HairHue = 0x454;

        AddItem(new Backpack());
        AddItem(
            new Sandals
            {
                Hue = 0x75B
            }
        );
        AddItem(
            new Tunic
            {
                Hue = 0x4BF
            }
        );
        AddItem(
            new Skirt
            {
                Hue = 0x8FD
            }
        );
    }

    public override string DefaultName => "Аминия";

    public override void OnThink()
    {
        Clock.GetTime(Map, Location.X, Location.Y, out var hours, out int _);

        if (hours == 21)
        {
            Blessed = false;
            Body = 0x17;
        }
        else
        {
            Blessed = true;
            Body = 0x191;
        }
    }
}

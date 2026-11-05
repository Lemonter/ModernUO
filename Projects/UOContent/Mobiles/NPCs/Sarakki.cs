using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Quest list lives in <see cref="Server.Engines.MLQuests.Definitions.FriendsOfTheLibraryQuests" />, the
///     friends of the library chain. ServUO also gives Black Order Badges and Evidence here; those quests are
///     not ported yet.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Sarakki : BaseCreature
{
    [Constructible]
    public Sarakki() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Title = "нотариус";
        Race = Race.Human;
        Female = true;
        Body = 0x191;
        Hue = 0x841E;

        SetSpeed(0.5, 2.0);
        InitStats(100, 100, 25);

        HairItemID = 0x2049;
        HairHue = 0x1BB;

        AddItem(new Backpack());
        AddItem(
            new Shoes
            {
                Hue = 0x740
            }
        );
        AddItem(
            new FancyShirt
            {
                Hue = 0x72C
            }
        );
        AddItem(
            new Skirt
            {
                Hue = 0x53C
            }
        );
    }

    public override bool IsInvulnerable => true;
    public override string DefaultName => "Саракки";
    public override bool CanTeach => true;
}

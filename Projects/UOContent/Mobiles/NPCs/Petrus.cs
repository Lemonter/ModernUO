using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Quest list lives in <see cref="Server.Engines.MLQuests.Definitions.FriendsOfTheLibraryQuests" />, the
///     friends of the library chain.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Petrus : BaseCreature
{
    [Constructible]
    public Petrus() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Title = "пасечник";
        Race = Race.Human;
        Female = false;
        Body = 0x190;
        Hue = 0x840C;

        SetSpeed(0.5, 2.0);
        InitStats(100, 100, 25);

        HairItemID = 0x203C;
        HairHue = 0x3B3;

        AddItem(new Backpack());
        AddItem(
            new Sandals
            {
                Hue = 0x1BB
            }
        );
        AddItem(
            new ShortPants
            {
                Hue = 0x71C
            }
        );
        AddItem(
            new Tunic
            {
                Hue = 0x5EF
            }
        );
    }

    public override bool IsInvulnerable => true;
    public override string DefaultName => "Петрус";
    public override bool CanTeach => true;
}

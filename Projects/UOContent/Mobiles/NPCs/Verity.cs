using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Quest list lives in <see cref="Server.Engines.MLQuests.Definitions.FriendsOfTheLibraryQuests" />, the
///     friends of the library chain.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Verity : BaseCreature
{
    [Constructible]
    public Verity() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Title = "библиотекарь";
        Race = Race.Human;
        Female = true;
        Body = 0x191;
        Hue = 0x83EF;

        SetSpeed(0.5, 2.0);
        InitStats(100, 100, 25);

        HairItemID = 0x2047;
        HairHue = 0x3B3;

        AddItem(new Backpack());
        AddItem(
            new Shoes
            {
                Hue = 0x754
            }
        );
        AddItem(
            new Shirt
            {
                Hue = 0x653
            }
        );
        AddItem(
            new Cap
            {
                Hue = 0x901
            }
        );
        AddItem(
            new Kilt
            {
                Hue = 0x901
            }
        );
    }

    public override bool IsInvulnerable => true;
    public override string DefaultName => "Верити";
    public override bool CanTeach => true;
}

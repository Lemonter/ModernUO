using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Initiation/InitiationBag.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class InitiationSuitBag : Bag
{
    public override string DefaultName => "Initiation Suit Bag";

    [Constructible]
    public InitiationSuitBag()
    {
        Hue = 0x30;

        DropItem( new InitiationArms() );
        DropItem( new InitiationCap() );
        DropItem( new InitiationChest() );
        DropItem( new InitiationGloves() );
        DropItem( new InitiationGorget() );
        DropItem( new InitiationLegs() );
    }
}

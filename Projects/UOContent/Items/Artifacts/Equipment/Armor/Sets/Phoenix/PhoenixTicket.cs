using ModernUO.Serialization;
using System;
using Server.Network;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Phoenix/PhoenixTicket.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class PhoenixTicket : Item
{
    [Constructible]
    public PhoenixTicket() : base (0x14F0)
    {
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1041234; // Ticket for a piece of phoenix armor

    public override void OnDoubleClick(Mobile from)
    {
         if (!IsChildOf(from.Backpack))
     {
         from.SendLocalizedMessage(1042001);
     }
     else
         {
             switch (Utility.Random(6))
             {
                 case 0: from.AddToBackpack(new PhoenixArms()); break;
                 case 1: from.AddToBackpack(new PhoenixChest()); break;
                 case 2: from.AddToBackpack(new PhoenixGloves()); break;
                 case 3: from.AddToBackpack(new PhoenixGorget()); break;
                 case 4: from.AddToBackpack(new PhoenixHelm()); break;
                 case 5: from.AddToBackpack(new PhoenixLegs()); break;
             }
             this.Delete();
             from.SendLocalizedMessage(502064); // A piece of phoenix armor has been placed in your backpack.
         }

     }
}

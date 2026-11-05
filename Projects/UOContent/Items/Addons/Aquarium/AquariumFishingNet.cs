using ModernUO.Serialization;
using System;
using Server.Engines.Quests;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/Aquarium/AquariumFishingNet.cs).
/// </summary>
// Legacy code
[SerializationGenerator(0, false)]
public partial class AquariumFishingNet : Item
{
    public AquariumFishingNet()
    {
    }

    public override int LabelNumber => 1074463; // An aquarium fishing net
    public override void OnDoubleClick(Mobile from)
    {
        if (!this.IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return;
        }

        Item replacement = this.CreateReplacement();

        if (!from.PlaceInBackpack(replacement))
        {
            replacement.Delete();
            from.SendLocalizedMessage(500720); // You don't have enough room in your backpack!
        }
        else
        {
            this.Delete();
            from.Use(replacement);
        }
    }

    private Item CreateReplacement()
    {
        Item result = new AquariumFishNet();
        result.Hue = this.Hue;
        result.LootType = this.LootType;
        result.Movable = this.Movable;
        result.Name = this.Name;
        result.QuestItem = this.QuestItem;
        result.Visible = this.Visible;

        return result;
    }
}

using System.Collections.Generic;
using ModernUO.Serialization;

namespace Server.Items;

// Ported from JustUO (github.com/JustUO/JustUO, a ServUO fork) — see
// Regions/UnderworldRegion.cs header for why ServUO itself wasn't the source here.

[SerializationGenerator(0, false)]
public partial class AcidPopper : Item
{
    [Constructible]
    public AcidPopper() : this(1)
    {
    }

    [Constructible]
    public AcidPopper(int amount) : base(0x44C1)
    {
        Hue = 68;
        Stackable = true;
        Amount = amount;
    }

    public override int LabelNumber => 1095058; // Acid Popper

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1060640); // The item must be in your backpack to use it.
            return;
        }

        var list = new List<NavreyParalyzingWeb>();
        foreach (var item in Map.GetItemsInRange(GetWorldLocation(), 0))
        {
            if (item is NavreyParalyzingWeb web)
            {
                list.Add(web);
            }
        }

        if (list.Count == 0)
        {
            return;
        }

        Consume();
        from.SendLocalizedMessage(1113240); // The acid popper bursts and burns away the webbing.

        foreach (var web in list)
        {
            web.Delete();
        }
    }
}

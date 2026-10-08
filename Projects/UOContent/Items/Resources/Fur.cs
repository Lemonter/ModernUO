using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items;

/// <summary>Sheared from a live boura or kepetch, or cut from its corpse. Ported from ServUO
/// (Scripts/Items/Decorative/Fur.cs) — one stackable item for all four colours, hued by
/// <see cref="FurType" />.</summary>
[SerializationGenerator(0, false)]
[TypeAlias("Server.Items.BouraFur", "Server.Items.KepetchFur")]
public partial class Fur : Item
{
    [Constructible]
    public Fur(FurType type = FurType.None, int amount = 1) : base(0x1875)
    {
        Stackable = true;
        Amount = amount;

        Hue = type switch
        {
            FurType.Green      => 58,
            FurType.LightBrown => 1541,
            FurType.Yellow     => 153,
            FurType.Brown      => 343,
            _                  => 0
        };
    }
}

/// <summary>The live-shearing half of the fur mechanic. ServUO repeats this body verbatim in
/// all five fur-bearing creatures, differing only in which clilocs it sends; here it is one
/// helper they all call, so the creatures keep just their own flag and their own messages.</summary>
public static class FurShearing
{
    /// <summary>Tries to put a creature's fur straight into the shearer's pack. Returns true
    /// only if it got there — the caller sets its own "already sheared" flag on that.</summary>
    public static bool TryShear(BaseCreature creature, Mobile from, int fullPackCliloc, int successCliloc)
    {
        var fur = new Fur(creature.FurType, creature.Fur);

        if (from.Backpack?.TryDropItem(from, fur, false) != true)
        {
            from.SendLocalizedMessage(fullPackCliloc);
            fur.Delete();
            return false;
        }

        from.SendLocalizedMessage(successCliloc);
        return true;
    }
}

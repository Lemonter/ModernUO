using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Items/QuestItems/CanvassRobe.cs). One of the
///     four pieces of the Salvager's Suit, sewn by Madeline Harte for her son's hat. The original's
///     <c>CanBeWornByGargoyles</c> override is dropped: this codebase's <see cref="Item.RequiredRaces" />
///     defaults to <see cref="Race.AllowAllRaces" />, so gargoyles can already wear it.
/// </summary>
[SerializationGenerator(0, false)]
public partial class CanvassRobe : Robe
{
    [Constructible]
    public CanvassRobe()
    {
        Hue = 2720;
        LootType = LootType.Blessed;
        StrRequirement = 10;
        QuestItem = true;
    }

    public override int LabelNumber => 1154238; // A Canvass Robe

    public override void OnDoubleClick(Mobile from)
    {
        base.OnDoubleClick(from);

        // *It is a thick canvass robe with specially sewn seals around the wrists and ankles. It appears
        // as though it would protect its wearer from the harsh conditions of a deep aquatic environment.*
        from.PublicOverheadMessage(MessageType.Regular, 0x3B2, 1154239);
    }
}

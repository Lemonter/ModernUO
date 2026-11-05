using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfDoom/Items/TinctureOfSilver.cs).
///     The one-hour silver-slayer application is dropped: it relied on ServUO's <c>SlayerSocket</c>/<c>GetSocket</c>
///     item-socket framework, which does not exist here. The item and its tooltip entries are kept.
/// </summary>
[SerializationGenerator(0, false)]
public partial class TinctureOfSilver : Item
{
    public override int LabelNumber => 1155619; // Tincture of Silver

    [Constructible]
    public TinctureOfSilver() : base(0x183B)
    {
        Hue = 1900;
        LootType = LootType.Blessed;
    }

    public override void OnDoubleClick(Mobile m)
    {
        if (!IsChildOf(m.Backpack))
        {
            return;
        }

        m.SendLocalizedMessage(1155615); // You cannot apply Tincture of Silver to this item.
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1155620);             // When Applied:
        list.Add(1155621, "#1155622"); // 200% Damage Increase towards Monsters of ~1_NAME~ : Undead Vulnerability
    }
}

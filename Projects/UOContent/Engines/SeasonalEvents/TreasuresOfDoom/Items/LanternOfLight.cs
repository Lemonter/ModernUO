using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfDoom/Items/LanternOfLight.cs).</summary>
[SerializationGenerator(0, false)]
public partial class LanternOfLight : Lantern
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _ownerName;

    [Constructible]
    public LanternOfLight()
    {
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1155597, _ownerName ?? string.Empty); // ~1_NAME~'s Lantern of Light
    }
}

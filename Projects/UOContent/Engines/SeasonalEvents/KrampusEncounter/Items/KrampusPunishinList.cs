using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/KrampusEncounter/Items/KrampusPunishinList.cs).</summary>
[SerializationGenerator(0, false)]
[Flippable(0x46AE, 0x46AF)]
public partial class KrampusPunishinList : Item
{
    [SerializableField(0)]
    private string _ownerName;

    [Constructible]
    public KrampusPunishinList() : this(null)
    {
    }

    [Constructible]
    public KrampusPunishinList(string name) : base(0x46AE)
    {
        _ownerName = name;
        Hue = 0x21;
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1158834, _ownerName ?? string.Empty); // Krampus' Punishment List
    }
}

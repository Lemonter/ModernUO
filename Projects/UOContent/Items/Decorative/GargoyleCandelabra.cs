using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class GargoyleCandelabra : BaseLight, IShipwreckedItem
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _isShipwreckedItem;

    [Constructible]
    public GargoyleCandelabra() : base(0x4039)
    {
        Duration = TimeSpan.Zero; // никогда не прогорает
        Burning = false;
        Light = LightType.Circle225;
        Weight = 3.0;
    }

    public override int LitItemID => 0x40BE;

    public override int UnlitItemID => 0x4039;

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_isShipwreckedItem)
        {
            list.Add(1041645); // recovered from a shipwreck
        }
    }
}

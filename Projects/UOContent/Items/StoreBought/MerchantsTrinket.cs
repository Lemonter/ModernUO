using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MerchantsTrinket : GoldEarrings
{
    [SerializableField(0, fieldChanged: nameof(OnGreaterChanged))]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _greater;

    [Constructible]
    public MerchantsTrinket(bool greater = false)
    {
        _greater = greater;
        LootType = LootType.Blessed;
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Bonus => _greater ? 10 : 5;

    public override int LabelNumber => _greater ? 1156828 : 1156827; // Merchant's Trinket - 5% / 10%

    private void OnGreaterChanged(bool oldValue, bool newValue) => InvalidateProperties();
}

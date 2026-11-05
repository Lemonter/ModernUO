using ModernUO.Serialization;

namespace Server.Items;

public enum SBType
{
    DrunkWomans,
    DrunkMans,
    Bedlam,
    SosarianSteeds,
    BlueBoar
}

[SerializationGenerator(0, false)]
public partial class StoreSingingBall : SingingBall
{
    [SerializableField(0, fieldChanged: nameof(OnTypeChanged))]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private SBType _type;

    [Constructible]
    public StoreSingingBall() : base(0x468A)
    {
        _type = (SBType)Utility.Random(5);

        Weight = 1.0;
        LootType = LootType.Regular;
        ApplyHue();
    }

    public override int LabelNumber => 1152323 + (int)_type;

    private void OnTypeChanged(SBType oldValue, SBType newValue) => ApplyHue();

    private void ApplyHue()
    {
        Hue = _type switch
        {
            SBType.Bedlam      => 2611,
            SBType.BlueBoar    => 2514,
            SBType.DrunkMans   => 2659,
            SBType.DrunkWomans => 2596,
            _                  => 2554
        };
    }

    [AfterDeserialization]
    private void AfterDeserialization() => ApplyHue();

    public override int SoundList() =>
        _type switch
        {
            SBType.Bedlam      => Utility.RandomList(897, 1005, 889, 1001, 1002, 1004, 1005, 894, 893, 889, 1003),
            SBType.BlueBoar    => Utility.RandomList(1073, 1085, 811, 799, 1066, 794, 801, 1075, 803, 811, 1071),
            SBType.DrunkMans   => Utility.RandomMinMax(1049, 1098),
            SBType.DrunkWomans => Utility.RandomMinMax(778, 823),
            _                  => Utility.RandomList(1218, 751, 629, 1226, 1305, 1246, 1019, 1508, 674, 1241)
        };
}

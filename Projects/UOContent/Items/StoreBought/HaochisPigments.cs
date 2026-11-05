using ModernUO.Serialization;

namespace Server.Items;

public enum HaochisPigmentType
{
    None,
    HeartwoodSienna,
    CampionWhite,
    YewishPine,
    MinocianFire,
    NinjaBlack,
    Olive,
    DarkReddishBrown,
    Yellow,
    PrettyPink,
    MidnightBlue,
    Emerald,
    SmokyGold,
    GhostsGrey,
    OceanBlue,
    CelticLime
}

[SerializationGenerator(0, false)]
public partial class HaochisPigment : BasePigmentsOfTokuno
{
    public static HoachisPigmentInfo[] Table { get; } =
    [
        new(HaochisPigmentType.None, 0, -1),
        new(HaochisPigmentType.HeartwoodSienna, 2739, 1157275),
        new(HaochisPigmentType.CampionWhite, 2738, 1157274),
        new(HaochisPigmentType.YewishPine, 2737, 1157273),
        new(HaochisPigmentType.MinocianFire, 2736, 1157272),
        new(HaochisPigmentType.NinjaBlack, 1175, 1071246),
        new(HaochisPigmentType.Olive, 2730, 1018352),
        new(HaochisPigmentType.DarkReddishBrown, 1148, 1071247),
        new(HaochisPigmentType.Yellow, 1169, 1071245),
        new(HaochisPigmentType.PrettyPink, 1168, 1071244),
        new(HaochisPigmentType.MidnightBlue, 1176, 1071248),
        new(HaochisPigmentType.Emerald, 1173, 1023856),
        new(HaochisPigmentType.SmokyGold, 1801, 1115467),
        new(HaochisPigmentType.GhostsGrey, 1000, 1115468),
        new(HaochisPigmentType.OceanBlue, 1195, 1115471),
        new(HaochisPigmentType.CelticLime, 2733, 1157269),
    ];

    [SerializableField(0, fieldChanged: nameof(OnTypeChanged))]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private HaochisPigmentType _type;

    [Constructible]
    public HaochisPigment(HaochisPigmentType type = HaochisPigmentType.None, int uses = 50) : base(uses)
    {
        Weight = 1.0;
        _type = type;
        ApplyType();
    }

    public override int LabelNumber => 1071249; // Haochi's Pigments

    public static HoachisPigmentInfo Find(HaochisPigmentType type)
    {
        foreach (var info in Table)
        {
            if (info.Type == type)
            {
                return info;
            }
        }

        return null;
    }

    private void OnTypeChanged(HaochisPigmentType oldValue, HaochisPigmentType newValue) => ApplyType();

    private void ApplyType()
    {
        var info = Find(_type);

        if (info == null)
        {
            Hue = 0;
            Label = -1;
            return;
        }

        Hue = info.Hue;
        Label = info.Localization;
    }

    [AfterDeserialization]
    private void AfterDeserialization() => ApplyType();

    public class HoachisPigmentInfo
    {
        public HoachisPigmentInfo(HaochisPigmentType type, int hue, int localization)
        {
            Type = type;
            Hue = hue;
            Localization = localization;
        }

        public HaochisPigmentType Type { get; }
        public int Hue { get; }
        public int Localization { get; }
    }
}

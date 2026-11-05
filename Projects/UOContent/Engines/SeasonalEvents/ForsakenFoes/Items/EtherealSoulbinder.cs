using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/ForsakenFoes/Items/EtherealSoulbinder.cs).
///     ServUO's <c>EventSink.CreatureDeath</c> soul-collection hook is dropped: there is no creature-death event
///     here, so the binder fills through a direct <see cref="SoulPoint" /> assignment instead.
/// </summary>
[SerializationGenerator(0, false)]
public partial class EtherealSoulbinder : Item
{
    [SerializableField(0, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private double _soulPoints;

    public override int LabelNumber => 1159167; // ethereal soulbinder

    public double MaxSoulPoint { get; set; } = 100;

    [Constructible]
    public EtherealSoulbinder() : base(0xA1E7)
    {
    }

    public double SoulPoint
    {
        get => _soulPoints;
        set
        {
            var clamped = value;

            if (clamped < 0)
            {
                clamped = 0;
            }
            else if (clamped > MaxSoulPoint)
            {
                clamped = MaxSoulPoint;
            }

            _soulPoints += clamped;

            SetHue();
            InvalidateProperties();
        }
    }

    private void SetHue() => Hue = _soulPoints switch
    {
        <= 0  => 0,
        <= 1  => 1910, // Meager
        <= 25 => 1916, // Grand
        <= 50 => 1914, // Exalted
        <= 90 => 1922, // Legendary
        _     => 1919  // Mythical
    };

    private int GetDescription() => _soulPoints switch
    {
        <= 0  => 1159177, // An Empty Soulbinder
        <= 1  => 1159176, // Meager
        <= 25 => 1159175, // Grand
        <= 50 => 1159174, // Exalted
        <= 90 => 1159173, // Legendary
        _     => 1159172  // Mythical
    };

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        var desc = GetDescription();

        if (desc == 1159177)
        {
            list.Add(1159177); // An Empty Soulbinder
        }
        else
        {
            list.Add(1159178, $"#{desc}"); // Contains a ~1_TYPE~ Soul
        }
    }
}


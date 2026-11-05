using System;
using ModernUO.Serialization;
using Server.Targeting;

namespace Server.Items;

public enum CompassionPigmentType
{
    PhoenixRed = 0,
    AuraOfAmber,
    DeepViolet,
    PolishedBronze,
    VibrantCrimson,
    Lavender,
    GleamingFuchsia,
    DeepBlue,
    GlossyFuchsia,
    DarkVoid,
    MurkySeagreen,
    ReflectiveShadow,
    LiquidSunshine,
    ShadowyBlue,
    BlackAndGreen,
    GlossyBlue,
    HunterGreen,
    SlateBlue,
    MotherOfPearl,
    StarBlue,
    MurkyAmber,
    VibranSeagreen,
    VibrantOcher,
    MossyGreen,
    OliveGreen,
    MottledSunsetBlue,
    TyrianPurple,
    IntenseTeal
}

public class CompassionPigmentInfo
{
    private static readonly CompassionPigmentInfo[] _table =
    {
        new(CompassionPigmentType.PhoenixRed, 1964, 1151651),       // Phoenix Red Pigment
        new(CompassionPigmentType.AuraOfAmber, 1967, 1152308),      // Aura of Amber Pigment
        new(CompassionPigmentType.DeepViolet, 1929, 1151912),       // Deep Violet Pigment
        new(CompassionPigmentType.PolishedBronze, 1944, 1151909),   // Polished Bronze Pigment
        new(CompassionPigmentType.VibrantCrimson, 1964, 1153386),   // Vibrant Crimson Pigment
        new(CompassionPigmentType.Lavender, 1951, 1151650),         // Lavender Pigment
        new(CompassionPigmentType.GleamingFuchsia, 1930, 1152311),  // Gleaming Fuchsia Pigment
        new(CompassionPigmentType.DeepBlue, 1939, 1152348),         // Deep Blue Pigment
        new(CompassionPigmentType.GlossyFuchsia, 1919, 1152347),    // Glossy Fuchsia Pigment
        new(CompassionPigmentType.DarkVoid, 2068, 1154214),         // Dark Void Pigment
        new(CompassionPigmentType.MurkySeagreen, 1992, 1152309),    // Murky Seagreen Pigment
        new(CompassionPigmentType.ReflectiveShadow, 1910, 1153387), // Reflective Shadow Pigment
        new(CompassionPigmentType.LiquidSunshine, 1923, 1154213),   // Liquid Sunshine Pigment
        new(CompassionPigmentType.ShadowyBlue, 1960, 1152310),      // Shadowy Blue Pigment
        new(CompassionPigmentType.BlackAndGreen, 1979, 1151911),    // Black And Green Pigment
        new(CompassionPigmentType.GlossyBlue, 1916, 1151910),       // Glossy Blue Pigment
        new(CompassionPigmentType.HunterGreen, 1936, 1151649),      // Hunter Green Pigment
        new(CompassionPigmentType.SlateBlue, 1983, 1151653),        // Slate Blue Pigment
        new(CompassionPigmentType.MotherOfPearl, 2720, 1154120),    // Mother Of Pearl Pigment
        new(CompassionPigmentType.StarBlue, 2723, 1154121),         // Star Blue Pigment
        new(CompassionPigmentType.MurkyAmber, 1989, 1152350),       // Murky Amber Pigment
        new(CompassionPigmentType.VibranSeagreen, 1970, 1152349),   // Vibran Seagreen Pigment
        new(CompassionPigmentType.VibrantOcher, 2725, 1154736),     // Vibrant Ocher Pigment
        new(CompassionPigmentType.MossyGreen, 2684, 1154731),       // Mossy Green Pigment
        new(CompassionPigmentType.OliveGreen, 2709, 1154733),       // Olive Green Pigment
        new(CompassionPigmentType.MottledSunsetBlue, 2714, 1154734), // Mottled Sunset Blue Pigment
        new(CompassionPigmentType.TyrianPurple, 2716, 1154735),     // Tyrian Purple Pigment
        new(CompassionPigmentType.IntenseTeal, 2691, 1154732)       // Intense Teal Pigment
    };

    public CompassionPigmentType Type { get; }

    public int Hue { get; }

    public int LabelNumber { get; }

    private CompassionPigmentInfo(CompassionPigmentType type, int hue, int labelNumber)
    {
        Type = type;
        Hue = hue;
        LabelNumber = labelNumber;
    }

    public static CompassionPigmentInfo GetInfo(CompassionPigmentType type)
    {
        var value = (int)type;

        if (value < 0 || value >= _table.Length)
        {
            value = 0;
        }

        return _table[value];
    }
}

/// <summary>
///     Пигмент, которым можно перекрашивать артефакты и улучшенные магические предметы.
/// </summary>
[SerializationGenerator(0, false)]
public partial class CompassionPigment : Item, IUsesRemaining
{
    [InvalidateProperties]
    [SerializableField(0, fieldChanged: nameof(OnTypeChanged))]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private CompassionPigmentType _type;

    [EncodedInt]
    [InvalidateProperties]
    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _usesRemaining;

    [Constructible]
    public CompassionPigment() : this(GetRandomType())
    {
    }

    [Constructible]
    public CompassionPigment(CompassionPigmentType type) : this(type, 5)
    {
    }

    [Constructible]
    public CompassionPigment(CompassionPigmentType type, int uses) : base(0xEFF)
    {
        _type = type;
        _usesRemaining = uses;

        Weight = 1.0;

        InvalidateHue();
    }

    public override int LabelNumber => CompassionPigmentInfo.GetInfo(_type).LabelNumber;

    bool IUsesRemaining.ShowUsesRemaining
    {
        get => true;
        set { }
    }

    private static CompassionPigmentType GetRandomType() =>
        (CompassionPigmentType)Utility.Random(Enum.GetValues<CompassionPigmentType>().Length);

    private void OnTypeChanged(CompassionPigmentType oldValue, CompassionPigmentType newValue) => InvalidateHue();

    private void InvalidateHue()
    {
        Hue = CompassionPigmentInfo.GetInfo(_type).Hue;
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1060584, _usesRemaining); // uses remaining: ~1_val~
    }

    public override void OnDoubleClick(Mobile from)
    {
        from.SendLocalizedMessage(1070929); // Select the artifact or enhanced magic item to dye.

        from.Target = new DyeTarget(this);
    }

    private class DyeTarget : Target
    {
        private readonly CompassionPigment _pigment;

        public DyeTarget(CompassionPigment pigment) : base(8, false, TargetFlags.None) => _pigment = pigment;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (_pigment.Deleted || _pigment.UsesRemaining <= 0)
            {
                return;
            }

            if (targeted is not Item item)
            {
                return;
            }

            if (!item.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
            }
            else if (item is BasePigmentsOfTokuno || item is CompassionPigment)
            {
                from.SendLocalizedMessage(1042083); // You cannot dye that.
            }
            else if (item.IsLockedDown)
            {
                from.SendLocalizedMessage(1070932); // You may not dye artifacts and enhanced magic items which are locked down.
            }
            else if (BasePigmentsOfTokuno.IsValidItem(item))
            {
                item.Hue = _pigment.Hue;

                _pigment.UsesRemaining--;

                if (_pigment.UsesRemaining <= 0)
                {
                    _pigment.Delete();
                }
            }
            else
            {
                from.SendLocalizedMessage(1070931); // You can only dye artifacts and enhanced magic items with this tub.
            }
        }
    }
}

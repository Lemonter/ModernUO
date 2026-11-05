using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Instruments/AudChar.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class AudChar : BaseInstrument
{
    [Constructible]
    public AudChar()
        : base(0x403B, 0x392, 0x44)
    {
        Weight = 10.0;
    }
}

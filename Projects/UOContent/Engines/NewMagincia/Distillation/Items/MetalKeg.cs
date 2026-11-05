/// <summary>
///     Портировано из ServUO (Scripts/Services/New Magincia/Distillation/Items/MetalKeg.cs).
///     Отличие от обычного <see cref="Keg" /> только в подписи и в том, что из него делают перегонный куб.
/// </summary>
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MetalKeg : Keg
{
    [Constructible]
    public MetalKeg()
    {
    }

    public override int LabelNumber => 1150675; // metal keg
}

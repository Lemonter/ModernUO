using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     ServUO-конструктор принимает только id предмета; по умолчанию взят первый из
///     вариантов магазина Ultima Store (0xA0AB..0xA0AF).
/// </summary>
[SerializationGenerator(0, false)]
public partial class HoodedBritanniaRobe : BaseOuterTorso
{
    [Constructible]
    public HoodedBritanniaRobe(int id = 0xA0AB) : base(id)
    {
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1125155; // Hooded Britannia Robe
}

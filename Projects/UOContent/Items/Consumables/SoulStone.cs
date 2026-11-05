using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Портировано из ServUO. Остальные камни души из этого файла в репозитории уже есть, здесь
///     только отсутствующий фиолетовый.
/// </summary>
[SerializationGenerator(0, false)]
public partial class VioletSoulstone : SoulStone
{
    [Constructible]
    public VioletSoulstone(string account = null) : base(account) => Hue = 2598;
}

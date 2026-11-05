using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Портировано из ServUO. Признак артефакта (IsArtifact) в этом репозитории отсутствует,
///     поэтому он опущен — предметы остаются обычными декоративными.
/// </summary>
[Flippable(0xA33B, 0xA33C)]
[SerializationGenerator(0, false)]
public partial class AncientWeapon1 : Item
{
    [Constructible]
    public AncientWeapon1() : base(0xA33B)
    {
    }

    public override int LabelNumber => 1125811; // ancient weapon
}

/// <summary>
///     Портировано из ServUO. Признак артефакта (IsArtifact) в этом репозитории отсутствует,
///     поэтому он опущен — предметы остаются обычными декоративными.
/// </summary>
[Flippable(0xA33D, 0xA33E)]
[SerializationGenerator(0, false)]
public partial class AncientWeapon2 : Item
{
    [Constructible]
    public AncientWeapon2() : base(0xA33D)
    {
    }

    public override int LabelNumber => 1125811; // ancient weapon
}

/// <summary>
///     Портировано из ServUO. Признак артефакта (IsArtifact) в этом репозитории отсутствует,
///     поэтому он опущен — предметы остаются обычными декоративными.
/// </summary>
[Flippable(0xA33F, 0xA340)]
[SerializationGenerator(0, false)]
public partial class AncientWeapon3 : Item
{
    [Constructible]
    public AncientWeapon3() : base(0xA33F)
    {
    }

    public override int LabelNumber => 1125811; // ancient weapon
}

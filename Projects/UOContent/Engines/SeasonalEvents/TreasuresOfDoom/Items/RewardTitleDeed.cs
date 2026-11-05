using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfDoom/Items/RewardTitleDeed.cs).
///     ServUO kept a bare cliloc in the deed; the text is kept as a <see cref="TextDefinition" />.
/// </summary>
[SerializationGenerator(0, false)]
public partial class TreasuresOfDoomRewardDeed : BaseRewardTitleDeed
{
    [SerializableField(0)]
    private TextDefinition _titleCliloc;

    public override TextDefinition Title => _titleCliloc;

    [Constructible]
    public TreasuresOfDoomRewardDeed() : this(1155600)
    {
    }

    [Constructible]
    public TreasuresOfDoomRewardDeed(int localization)
    {
        _titleCliloc = localization;
    }
}

using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Cut from the corpse of a boura or a slith. Ported from ServUO
/// (Scripts/Items/Resource/DragonBlood.cs).</summary>
[SerializationGenerator(0, false)]
public partial class DragonBlood : BaseReagent
{
    [Constructible]
    public DragonBlood(int amount = 1) : base(0x4077, amount)
    {
    }
}

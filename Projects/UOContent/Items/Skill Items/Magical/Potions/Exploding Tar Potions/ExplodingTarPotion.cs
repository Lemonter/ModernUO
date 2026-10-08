using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Gretchen's payment for "Curiosities". Ported from ServUO
/// (Scripts/Items/Consumables/ExplodingTarPotion.cs) — including its 20-tile blast, which is
/// what the original gives it.</summary>
[SerializationGenerator(0, false)]
public partial class ExplodingTarPotion : BaseExplodingTarPotion
{
    [Constructible]
    public ExplodingTarPotion() : base(PotionEffect.ExplodingTarPotion)
    {
    }

    public override int Radius => 20;

    public override int LabelNumber => 1095147; // a Greater Confusion Blast potion
}

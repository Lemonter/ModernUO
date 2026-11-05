using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class FruitBowl : Food, ICommodity
{
    [Constructible]
    public FruitBowl() : base(0x2D4F)
    {
        Weight = 1.0;
        FillFactor = 20;
    }

    public int DescriptionNumber => LabelNumber;

    public bool IsDeedable => true;

    public override int LabelNumber => 1072950; // fruit bowl

    public override bool Eat(Mobile from)
    {
        if (!FillHunger(from, FillFactor))
        {
            return false;
        }

        var modName = Serial.ToString();

        from.AddStatMod(new StatMod(StatType.Str, modName + "Str", 5, TimeSpan.FromSeconds(120)));
        from.AddStatMod(new StatMod(StatType.Dex, modName + "Dex", 5, TimeSpan.FromSeconds(120)));
        from.AddStatMod(new StatMod(StatType.Int, modName + "Int", 5, TimeSpan.FromSeconds(120)));

        from.PlaySound(0x1EA);
        from.FixedParticles(0x373A, 10, 15, 5018, EffectLayer.Waist);

        Consume();

        return true;
    }
}

using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/ResonantShieldOfVengeance.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class ResonantShieldOfVengeance : GargishWoodenShield
{
    public override int LabelNumber => 1150357; // Resonant Shield of Vengeance

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public ResonantShieldOfVengeance()
    {
        Hue = 2076;

        switch (Utility.Random(5))
        {
        }

        Attributes.SpellChanneling = 1;
        Attributes.ReflectPhysical = 20;
        Attributes.DefendChance = 8;

        switch (Utility.Random(5))
        {
            case 0: PhysicalBonus = 10; break;
            case 1: FireBonus = 10; break;
            case 2: ColdBonus = 10; break;
            case 3: PoisonBonus = 10; break;
            case 4: EnergyBonus = 10; break;
        }
    }
}

[SerializationGenerator(0, false)]
public partial class ResonantShieldOfVengeanceHuman : BronzeShield
{
    public override int LabelNumber => 1150357; // Resonant Shield of Vengeance

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public ResonantShieldOfVengeanceHuman()
    {
        Hue = 2076;

        switch (Utility.Random(5))
        {
        }

        Attributes.SpellChanneling = 1;
        Attributes.ReflectPhysical = 20;
        Attributes.DefendChance = 8;

        switch (Utility.Random(5))
        {
            case 0: PhysicalBonus = 10; break;
            case 1: FireBonus = 10; break;
            case 2: ColdBonus = 10; break;
            case 3: PoisonBonus = 10; break;
            case 4: EnergyBonus = 10; break;
        }
    }
}

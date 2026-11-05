using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/TongueoftheBeast.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class TongueOfTheBeast : WoodenKiteShield
{
    public override int LabelNumber => 1112405; // Tongue of the Beast [Replica]

    [Constructible]
    public TongueOfTheBeast()
    {
        Hue = 153;
        Attributes.SpellChanneling = 1;
        Attributes.RegenStam = 3;
        Attributes.RegenMana = 3;
    }

    public override int BasePhysicalResistance => 10;

    public override int BaseEnergyResistance => 5;

    public override int InitMinHits => 150;

    public override int InitMaxHits => 150;

    public override bool CanFortify => false;
}

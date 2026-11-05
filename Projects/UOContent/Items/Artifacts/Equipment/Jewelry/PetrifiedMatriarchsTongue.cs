using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Jewelry/PetrifiedMatriarchsTongue.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class PetrifiedMatriarchsTongue : GoldRing
{
    public override int LabelNumber => 1115776; // Petrified Matriarch's Tongue

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public PetrifiedMatriarchsTongue()
    {
        Hue = 2006; //TODO: get proper hue, this is a guess
        Attributes.RegenMana = 2;
        Attributes.AttackChance = 10;
        Attributes.CastSpeed = 1;
        Attributes.CastRecovery = 2;
        Attributes.LowerManaCost = 4;
        Resistances.Poison = 5;
    }
}

using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Jewelry/TokenofHolyFavor.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class TokenOfHolyFavor : GoldBracelet
{
    public override int LabelNumber => 1113652; // Token of Holy Favor

    [Constructible]
    public TokenOfHolyFavor()
    {
        Hue = 96;
        Attributes.BonusHits = 5;
        Attributes.CastRecovery = 2;
        Attributes.CastSpeed = 1;
        Attributes.DefendChance = 10;
        Attributes.AttackChance = 10;
        Attributes.SpellDamage = 4;
        Resistances.Cold = 5;
        Resistances.Poison = 5;
    }

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}

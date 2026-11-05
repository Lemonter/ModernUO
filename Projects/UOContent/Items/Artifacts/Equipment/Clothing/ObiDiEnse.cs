using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Clothing/ObiDiEnse.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class ObiDiEnse : Obi
{
    [Constructible]
    public ObiDiEnse()
        : base(0x27A0)
    {
        Hue = 0;
        Attributes.BonusInt = 5;
        Attributes.NightSight = 1;
        SkillBonuses.SetValues(0, SkillName.Focus, 5.0);
    }

    public override int LabelNumber => 1112406; // Obi Di Ense [Replica]

    public override int InitMinHits => 150;

    public override int InitMaxHits => 150;

    public override bool CanFortify => false;
}

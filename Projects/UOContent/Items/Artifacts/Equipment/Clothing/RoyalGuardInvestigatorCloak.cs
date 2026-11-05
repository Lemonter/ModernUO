using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Clothing/RoyalGuardInvestigatorCloak.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class RoyalGuardInvestigatorsCloak : Cloak
{
    [Constructible]
    public RoyalGuardInvestigatorsCloak()
        : base()
    {
        Hue = 1163;
        SkillBonuses.SetValues(0, SkillName.Stealth, 20.0);
    }

    public override int InitMinHits => 150;

    public override int InitMaxHits => 150;

    public override int LabelNumber => 1112409; // Royal Guard Investigator's Cloak [Replica]
}

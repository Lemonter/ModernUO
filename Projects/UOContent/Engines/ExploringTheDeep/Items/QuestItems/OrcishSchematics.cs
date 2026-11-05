using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Items/QuestItems/OrcishSchematics.cs). Taken
///     from the Orc Engineer in the Orc Cave and traded to Josef Skimmons for the lenses. Its lifespan is
///     five hours but, as in the original, it is displayed in whole units rather than seconds.
/// </summary>
[SerializationGenerator(0, false)]
public partial class OrcishSchematics : BaseDecayingItem
{
    [Constructible]
    public OrcishSchematics() : base(0x2258)
    {
        Hue = 1945;
        Weight = 1.0;
        LootType = LootType.Blessed;
        QuestItem = true;
    }

    public override int LabelNumber => 1154232; // Schematic for an Orcish Drilling Machine

    public override int Lifespan => 3600;
    public override bool UseSeconds => false;

    public override void OnDoubleClick(Mobile from)
    {
        base.OnDoubleClick(from);

        // *It appears to be the crude schematic to a drilling machine of Orcish origin. It is poorly
        // devised and looks as if one were to build it the machine would explode*
        from.PublicOverheadMessage(MessageType.Regular, 0x3B2, 1154233);
    }
}

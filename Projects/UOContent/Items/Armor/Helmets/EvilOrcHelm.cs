using ModernUO.Serialization;

namespace Server.Items;

/// <summary>A cursed orc helm — ten strength for ten off both intelligence and dexterity.
/// Ported from ServUO (Scripts/Items/Equipment/Armor/EvilOrcHelm.cs); the orc chopper drops one
/// on a tenth of its deaths.
///
/// The original expresses the penalty through its UseIntOrDexProperty/IntOrDexPropertyValue
/// pair, a BaseArmor hook this codebase does not have; the effect is the same two attribute
/// lines written out directly.</summary>
[SerializationGenerator(0, false)]
public partial class EvilOrcHelm : OrcHelm
{
    [Constructible]
    public EvilOrcHelm()
    {
        Hue = 0x96E;

        Attributes.BonusStr = 10;
        Attributes.BonusInt = -10;
        Attributes.BonusDex = -10;
    }

    public override int LabelNumber => 1062021; // an evil orc helm
}

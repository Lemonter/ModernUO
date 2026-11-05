using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     ServUO's SBGardener shop info class does not exist in ModernUO, so InitSBInfo is left empty and the
///     gardener does not sell anything.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Gardener : BaseVendor
{
    private readonly List<SBInfo> _sbInfos = new();

    [Constructible]
    public Gardener() : base("садовник")
    {
    }

    public override VendorShoeType ShoeType => VendorShoeType.ThighBoots;

    protected override List<SBInfo> SBInfos => _sbInfos;

    public override void InitSBInfo()
    {
    }

    public override int GetShoeHue() => 0;

    public override void InitOutfit()
    {
        base.InitOutfit();

        AddItem(new WideBrimHat(Utility.RandomNeutralHue()));
    }
}

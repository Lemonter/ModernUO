using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items;

/// <summary>The masonry runic, ported from ServUO (Scripts/Items/Tools/RunicMalletAndChisel.cs)
/// — the stonecrafting counterpart to the runic hammer, sewing kit and fletcher's tool already
/// here. The Soulforge reward boxes hand one out on a 5 % roll.</summary>
[SerializationGenerator(0, false)]
public partial class RunicMalletAndChisel : BaseRunicTool
{
    [Constructible]
    public RunicMalletAndChisel(CraftResource resource) : base(resource, 0x12B3) =>
        Hue = CraftResources.GetHue(resource);

    [Constructible]
    public RunicMalletAndChisel(CraftResource resource, int uses) : base(resource, uses, 0x12B3) =>
        Hue = CraftResources.GetHue(resource);

    public override double DefaultWeight => 2.0;

    public override CraftSystem CraftSystem => DefMasonry.CraftSystem;

    public override int LabelNumber
    {
        get
        {
            var index = CraftResources.GetIndex(Resource);

            if (index >= 1 && index <= 8)
            {
                return 1111795 + index;
            }

            return 1045128; // mallet and chisel
        }
    }

    public override void AddNameProperties(IPropertyList list)
    {
        base.AddNameProperties(list);

        var index = CraftResources.GetIndex(Resource);

        if (index >= 1 && index <= 8)
        {
            return;
        }

        if (!CraftResources.IsStandard(Resource))
        {
            var num = CraftResources.GetLocalizationNumber(Resource);

            if (num > 0)
            {
                list.Add(num);
            }
            else
            {
                list.Add(CraftResources.GetName(Resource));
            }
        }
    }
}

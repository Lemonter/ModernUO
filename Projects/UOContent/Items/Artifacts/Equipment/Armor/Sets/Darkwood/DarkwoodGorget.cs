using ModernUO.Serialization;
using System;
using Server.Engines.Craft;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Darkwood/DarkwoodGorget.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class DarkwoodGorget : WoodlandGorget
{
    [Constructible]
    public DarkwoodGorget()
        : base()
    {
        this.Hue = 0x455;

        this.Attributes.BonusHits = 2;
        this.Attributes.DefendChance = 5;

    }

    public override int LabelNumber => 1073483; // Darkwood Gorget

    public override int BasePhysicalResistance => 8;

    public override int BaseFireResistance => 5;

    public override int BaseColdResistance => 5;

    public override int BasePoisonResistance => 7;

    public override int BaseEnergyResistance => 5;

    public override int OnCraft(int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool, CraftItem craftItem, int resHue)
    {
        if (resHue > 0)
        {
            this.Hue = resHue;
        }

        Type resourceType = typeRes;

        if (resourceType == null)
        {
            resourceType = craftItem.Resources[0].ItemType;
        }

        this.Resource = CraftResources.GetFromType(resourceType);

        switch ( this.Resource )
        {
            case CraftResource.Bloodwood:
                this.Attributes.RegenHits = 2;
                break;
            case CraftResource.Heartwood:
                this.Attributes.Luck = 40;
                break;
            case CraftResource.YewWood:
                this.Attributes.RegenHits = 1;
                break;
        }

        return 0;
    }
}

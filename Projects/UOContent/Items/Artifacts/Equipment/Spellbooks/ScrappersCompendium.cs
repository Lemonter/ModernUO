using ModernUO.Serialization;
using System;
using Server.Engines.Craft;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Spellbooks/ScrappersCompendium.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class ScrappersCompendium : Spellbook
{
    [Constructible]
    public ScrappersCompendium()
        : base()
    {
        Hue = 0x494;
        Attributes.SpellDamage = 25;
        Attributes.LowerManaCost = 10;
        Attributes.CastSpeed = 1;
        Attributes.CastRecovery = 1;
    }

    public override int LabelNumber => 1072940; // scrappers compendium

    public override int OnCraft(int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool, CraftItem craftItem, int resHue)
    {
        double magery = from.Skills.Magery.Value - 100;

        if (magery < 0)
        {
            magery = 0;
        }

        int count = (int)Math.Round(magery * Utility.RandomDouble() / 5);

        if (count > 2)
        {
            count = 2;
        }

        if (Utility.RandomDouble() < 0.5)
        {
            count = 0;
        }
        else
        {
            BaseRunicTool.ApplyAttributesTo(this, true, 0, count, 70, 80);
        }

        this.Attributes.SpellDamage = 25;
        this.Attributes.LowerManaCost = 10;
        this.Attributes.CastSpeed = 1;
        this.Attributes.CastRecovery = 1;

        if (makersMark)
        {
            this.Crafter = from.RawName;
        }

        return quality;
    }
}

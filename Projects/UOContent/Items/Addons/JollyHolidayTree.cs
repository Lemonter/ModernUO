using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/JollyHolidayTree.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class JollyHolidayTreeAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new JollyHolidayTreeDeed(DisplayName);
    public override bool ForceShowProperties => true;

    private string _DisplayName;

    [CommandProperty(AccessLevel.GameMaster)]
    public string DisplayName { get { return _DisplayName; } set { _DisplayName = value; InvalidateProperties(); } }

    public JollyHolidayTreeAddon(string name)
    {
        DisplayName = name;

        AddComponent(new JollyHolidayTreeComponent(0xA4D0, 1124426), 0, 0, 0);
    }

    [SerializationGenerator(0, false)]
    private partial class JollyHolidayTreeComponent : LocalizedAddonComponent, IDyable
    {
        public JollyHolidayTreeComponent(int id, int labelnumber)
            : base(id, labelnumber)
        {
        }

        public bool Dye(Mobile from, DyeTub sender)
        {
            if (Deleted)
                return false;

            if (Addon != null)
                Addon.Hue = sender.DyedHue;

            return true;
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            string name = ((JollyHolidayTreeAddon)Addon).DisplayName;

            if (!string.IsNullOrEmpty(name))
            {
                list.Add(1159271, name); // <BASEFONT COLOR=#FFD24D>Jolly Holiday Tree from ~1_NAME~<BASEFONT COLOR=#FFFFFF>
            }
        }
    }
}

[SerializationGenerator(0, false)]
public partial class JollyHolidayTreeDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new JollyHolidayTreeAddon(DisplayName);
    public override int LabelNumber => 1159256; // Jolly Holiday Tree

    private string _DisplayName;

    [CommandProperty(AccessLevel.GameMaster)]
    public string DisplayName { get { return _DisplayName; } set { _DisplayName = value; InvalidateProperties(); } }

    [Constructible]
    public JollyHolidayTreeDeed()
        : this(null)
    {
    }

    public JollyHolidayTreeDeed(string name)
    {
        LootType = LootType.Blessed;

        if (name == null)
            _DisplayName = _Names[Utility.Random(_Names.Length)];
        else
            _DisplayName = name;
    }

    public static string[] _Names =
    {
        "Cursed Powder Obsidian", "Lover Noggin Artemis", "Cursed Tooth Aeon", "Short Noggin Takako", "Stink Hook Ashmedia", "Iron Eye Greg", "Mad Back Riccia",
        "Mad Blade Vereor", "Bloody Ear Aumakua", "Long Powder Jim", "Lazy Tongue Silver Fox", "Lazy Pants Hanarin", "Fat Hook Bonnie", "Short Hook Michael", "Bonny Eye Elizabella",
        "Glass Blade Artemis", "Stink Strap Raine", "Bloody Tongue Aumakua", "Bloody Tooth Asiantam", "Short Blade Topaz", "Lazy Beard Raine", "Drunk Nail Takako", "Rude Powder Obsidian",
        "Peg Mouth Thrixx", "Iron Hook Stephen", "Lost Nail Tori", "Lone Patch Michael", "Burnin Nail Aumakua"
    };

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (!string.IsNullOrEmpty(_DisplayName))
        {
            list.Add(1159271, _DisplayName); // <BASEFONT COLOR=#FFD24D>Jolly Holiday Tree from ~1_NAME~<BASEFONT COLOR=#FFFFFF>
        }
    }
}

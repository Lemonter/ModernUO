using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

/// <summary>
///     Портировано из ServUO. ICustomizableMessageItem и гумпы редактирования текста в этом
///     репозитории отсутствуют, поэтому надпись только выводится в свойствах и не
///     редактируется игроком.
/// </summary>
[SerializationGenerator(0, false)]
public partial class CustomizableSquaredDoorMatAddon : BaseAddon
{
    [SerializableField(0)]
    private string[] _lines;

    [Constructible]
    public CustomizableSquaredDoorMatAddon(DirectionType type)
    {
        _lines = new string[3];

        switch (type)
        {
            case DirectionType.South:
                {
                    AddComponent(new CustomizableSquaredDoorMatComponent(0x4AB6), 0, 0, 0);
                    AddComponent(new CustomizableSquaredDoorMatComponent(0x4AB7), 1, 0, 0);
                    break;
                }
            case DirectionType.East:
                {
                    AddComponent(new CustomizableSquaredDoorMatComponent(0x4AB4), 0, 1, 0);
                    AddComponent(new CustomizableSquaredDoorMatComponent(0x4AB5), 0, 0, 0);
                    break;
                }
        }
    }

    public override BaseAddonDeed Deed => new CustomizableSquaredDoorMatDeed();
}

[SerializationGenerator(0, false)]
public partial class CustomizableSquaredDoorMatComponent : LocalizedAddonComponent
{
    [Constructible]
    public CustomizableSquaredDoorMatComponent(int id) : base(id, 1097996) // door mat
    {
    }

    public override bool ForceShowProperties => true;

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (Addon is not CustomizableSquaredDoorMatAddon addon || addon.Lines == null)
        {
            return;
        }

        for (var i = 0; i < addon.Lines.Length; i++)
        {
            if (!string.IsNullOrEmpty(addon.Lines[i]))
            {
                list.Add(1150301 + i, addon.Lines[i]); // [ ~1_LINE0~ ]
            }
        }
    }
}

[SerializationGenerator(0, false)]
public partial class CustomizableSquaredDoorMatDeed : BaseAddonDeed, IRewardOption
{
    private DirectionType _type;

    [Constructible]
    public CustomizableSquaredDoorMatDeed() => LootType = LootType.Blessed;

    public override int LabelNumber => 1151806; // squared door mat deed

    public override BaseAddon Addon => new CustomizableSquaredDoorMatAddon(_type);

    public override void OnDoubleClick(Mobile from)
    {
        if (IsChildOf(from.Backpack))
        {
            RewardOptionGump.DisplayTo(from, this, LabelNumber);
        }
        else
        {
            from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
        }
    }

    public void GetOptions(RewardOptionList list)
    {
        list.Add((int)DirectionType.South, 1151815);
        list.Add((int)DirectionType.East, 1151816);
    }

    public void OnOptionSelected(Mobile from, int choice)
    {
        _type = (DirectionType)choice;

        if (!Deleted)
        {
            base.OnDoubleClick(from);
        }
    }
}

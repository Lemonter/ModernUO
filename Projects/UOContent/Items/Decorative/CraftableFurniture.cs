using System;
using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items;

/// <summary>
///     Портировано из ServUO. ItemQuality и IResource в этом репозитории отсутствуют: качество
///     хранится как ArmorQuality, интерфейс IResource опущен, OnCraft принимает BaseTool.
/// </summary>
[SerializationGenerator(0, false)]
public partial class CraftableFurniture : Item, ICraftable
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Mobile _crafter;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private CraftResource _resource;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private ArmorQuality _quality;

    public CraftableFurniture(int itemID) : base(itemID)
    {
    }

    public virtual bool ShowCrafterName => true;

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (ShowCrafterName && _crafter != null)
        {
            list.Add(1050043, _crafter.Name); // crafted by ~1_NAME~
        }

        if (_quality == ArmorQuality.Exceptional)
        {
            list.Add(1060636); // exceptional
        }

        if (!CraftResources.IsStandard(_resource))
        {
            var info = CraftResources.GetInfo(_resource);

            if (info != null && info.Number > 0)
            {
                list.Add(info.Number);
            }
        }
    }

    public int OnCraft(
        int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool,
        CraftItem craftItem, int resHue
    )
    {
        Quality = (ArmorQuality)quality;

        if (makersMark)
        {
            Crafter = from;
        }

        var resourceType = typeRes ?? craftItem.Resources[0].ItemType;

        Resource = CraftResources.GetFromType(resourceType);
        Hue = CraftResources.GetHue(_resource);

        return quality;
    }
}

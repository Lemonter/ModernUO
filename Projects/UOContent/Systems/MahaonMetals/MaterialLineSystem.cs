using Server.Items;

namespace Server.Systems.MahaonMetals;

/// <summary>
///     Mahaon: the "what is this made of" line on weapons, armour, clothing and jewellery.
///
///     It used to be printed only when the item carried a NON-standard CraftResource, so
///     anything made of plain iron, plain leather, plain wood or cloth said nothing at all
///     - and a dyed item said nothing either, because dye only changes Hue and leaves the
///     resource standard. The result was a pack full of coloured things with no way to tell
///     what any of them actually were. Now every item answers the question, and when the
///     resource is standard or was never set, the answer is derived from what the item
///     physically is: a bow is wood, a plate chest is iron, a tunic is cloth.
/// </summary>
public static class MaterialLineSystem
{
    /// <summary>Mahaon metal wins - it is the shard's own, richer material system and
    /// overrides whatever CraftResource the item was born with.</summary>
    private static string MetalLine(Item item)
    {
        var metal = MahaonMetalTracker.GetMetal(item);

        return metal == null ? null : $"Металл: {MahaonMetalTable.Get(metal.Value).RuName}";
    }

    private static string Line(CraftResource resource) =>
        $"{CraftResources.GetRuTypeLabel(resource)}: {CraftResources.GetName(resource)}";

    public static string Describe(BaseWeapon weapon)
    {
        var metal = MetalLine(weapon);

        if (metal != null)
        {
            return metal;
        }

        var resource = weapon.Resource;

        if (resource == CraftResource.None)
        {
            // Bows and crossbows are wooden; everything else is forged.
            resource = weapon is BaseRanged ? CraftResource.RegularWood : CraftResource.Iron;
        }

        return Line(resource);
    }

    public static string Describe(BaseArmor armor)
    {
        var metal = MetalLine(armor);

        if (metal != null)
        {
            return metal;
        }

        var resource = armor.Resource;

        if (resource != CraftResource.None)
        {
            return Line(resource);
        }

        // No resource recorded - fall back to the piece's own material type. Cloth, bone
        // and stone have no CraftResource at all, so they get their own wording.
        return armor.MaterialType switch
        {
            ArmorMaterialType.Cloth   => "Материал: Ткань",
            ArmorMaterialType.Bone    => "Материал: Кость",
            ArmorMaterialType.Stone   => "Материал: Камень",
            ArmorMaterialType.Dragon  => "Чешуя: Драконья",
            ArmorMaterialType.Studded => "Кожа: Клёпаная",
            ArmorMaterialType.Leather => Line(CraftResource.RegularLeather),
            ArmorMaterialType.Spined  => Line(CraftResource.SpinedLeather),
            ArmorMaterialType.Horned  => Line(CraftResource.HornedLeather),
            ArmorMaterialType.Barbed  => Line(CraftResource.BarbedLeather),
            ArmorMaterialType.Wood    => Line(CraftResource.RegularWood),
            _                         => Line(CraftResource.Iron) // ringmail / chainmail / plate
        };
    }

    /// <summary>
    ///     Clothing had no material line at all, which is where the complaint actually bit:
    ///     a dye tub only changes Hue, so a dyed robe used to be a coloured thing with no
    ///     stated material anywhere. Cloth has no CraftResource of its own, so an item that
    ///     never recorded one is simply cloth.
    /// </summary>
    public static string Describe(BaseClothing clothing)
    {
        var metal = MetalLine(clothing);

        if (metal != null)
        {
            return metal;
        }

        var resource = clothing.Resource;

        return resource == CraftResource.None ? "Материал: Ткань" : Line(resource);
    }

    public static string Describe(BaseJewel jewel)
    {
        var metal = MetalLine(jewel);

        if (metal != null)
        {
            return metal;
        }

        var resource = jewel.Resource;

        return Line(resource == CraftResource.None ? CraftResource.Iron : resource);
    }
}

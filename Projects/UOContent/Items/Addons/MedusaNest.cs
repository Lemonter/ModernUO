using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/MedusaNest.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class MedusaSNestAddon : BaseAddon
{
    private static readonly int[,] m_AddOnSimpleComponents = new int[,]
    {
        { 7045, -1, 1, 0 }, { 12795, 0, 0, 0 }, { 7054, 1, 0, 0 }// 1    2    3
        ,
        { 7042, 1, 1, 0 }, { 7046, -1, -1, 0 }, { 7054, -1, 0, 0 }// 4    5    6
        ,
        { 7065, 1, -1, 0 }, { 7054, 0, 1, 0 }, { 7054, 0, -1, 0 }// 7    8    9
    };
    [Constructible]
    public MedusaSNestAddon()
    {
        for (int i = 0; i < m_AddOnSimpleComponents.Length / 4; i++)
            this.AddComponent(new AddonComponent(m_AddOnSimpleComponents[i, 0]), m_AddOnSimpleComponents[i, 1], m_AddOnSimpleComponents[i, 2], m_AddOnSimpleComponents[i, 3]);
    }

    public override BaseAddonDeed Deed => new MedusaSNestAddonDeed();
}

[SerializationGenerator(0, false)]
public partial class MedusaSNestAddonDeed : BaseAddonDeed
{
    [Constructible]
    public MedusaSNestAddonDeed()
    {
        this.Name = "гнездо Медузы";
    }

    public override BaseAddon Addon => new MedusaSNestAddon();
}

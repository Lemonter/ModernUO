using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/ParoxysmusSwampDragon.cs). Barding is
/// supported by this repository's SwampDragon, so the iron exceptional barding is kept.</summary>
[SerializationGenerator(0, false)]
public partial class ParoxysmusSwampDragon : SwampDragon
{
    [Constructible]
    public ParoxysmusSwampDragon()
    {
        BardingResource = CraftResource.Iron;
        BardingExceptional = true;
        BardingHP = BardingMaxHP;
        HasBarding = true;
        Hue = 1155;
    }

    public override string DefaultName => "болотный дракон Пароксисма";

    public override bool DeleteOnRelease => true;

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1049646); // (summoned)
    }
}

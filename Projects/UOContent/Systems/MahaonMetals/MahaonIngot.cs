using ModernUO.Serialization;
using Server.Systems.MahaonMetals;

namespace Server.Items;

/// <summary>Слиток на все 24 металла — та же логика "один класс, металл полем", что и у
/// MahaonOre.</summary>
[SerializationGenerator(0, false)]
public partial class MahaonIngot : Item
{
    [SerializableField(0)]
    private MahaonMetal _metal;

    public const int IngotGraphic = 0x1BF2; // тот же общий графон у всех тиров ингота в ванили

    [Constructible]
    public MahaonIngot(MahaonMetal metal = MahaonMetal.Iron, int amount = 1) : base(IngotGraphic)
    {
        _metal = metal;
        Stackable = true;
        Amount = amount;
        Weight = 0.1;

        var info = MahaonMetalTable.Get(metal);
        Name = $"слиток ({info.RuName})";
        Hue = info.Hue;
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        var info = MahaonMetalTable.Get(_metal);
        list.Add(info.EffectDescription);
    }
}

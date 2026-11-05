using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKhaldun/Items/CaddelliteTools.cs).
///     ServUO's <c>Caddellite</c> item socket (which keyed the harvest bonuses off the socket) does not exist here,
///     so the tools are plain marked subclasses: the "can harvest Caddellite resources" tooltip is kept and the
///     socket-driven harvest/metropolis behaviour is not ported.
/// </summary>
public interface ICaddelliteTool
{
}

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKhaldun/Items/CaddelliteTools.cs).</summary>
[SerializationGenerator(0, false)]
public partial class CaddellitePickaxe : Pickaxe, ICaddelliteTool
{
    public override int LabelNumber => 1158689; // Caddellite Pickaxe

    [Constructible]
    public CaddellitePickaxe()
    {
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1158692); // * Can Harvest Caddellite Infused Resources in the Lost Lands *
    }
}

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKhaldun/Items/CaddelliteTools.cs).</summary>
[SerializationGenerator(0, false)]
public partial class CaddelliteHatchet : Hatchet, ICaddelliteTool
{
    public override int LabelNumber => 1158690; // Caddellite Hatchet

    [Constructible]
    public CaddelliteHatchet()
    {
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1158692); // * Can Harvest Caddellite Infused Resources in the Lost Lands *
    }
}

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKhaldun/Items/CaddelliteTools.cs).</summary>
[SerializationGenerator(0, false)]
public partial class CaddelliteFishingPole : FishingPole, ICaddelliteTool
{
    public override int LabelNumber => 1158691; // Caddellite Fishing Pole

    [Constructible]
    public CaddelliteFishingPole()
    {
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1158692); // * Can Harvest Caddellite Infused Resources in the Lost Lands *
    }
}

using ModernUO.Serialization;
using Server.Gumps;
using Server.Items;

namespace Server.Engines.VvV;

// Ported from real OSI/ServUO content (Scripts/Services/ViceVsVirtue/Items/Rewards/Banners/*.cs)
// — 16 near-identical virtue/vice banner decorations, one class each matching ServUO's own
// per-file convention there. All share the same shape: `[Flippable]`, a double-click that
// shows the banner artwork full-size in a gump, and a "vvv item" property line.
[SerializationGenerator(0, false)]
public abstract partial class BaseVvVBanner : Item
{
    protected abstract int GumpImage { get; }

    protected BaseVvVBanner(int itemId) : base(itemId)
    {
    }

    public override void OnDoubleClick(Mobile m)
    {
        if (!m.InRange(GetWorldLocation(), 2))
        {
            return;
        }

        var g = new Gump(50, 50);
        g.AddImage(0, 0, GumpImage);
        m.SendGump(g);
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add(1154937); // vvv item
    }
}

[SerializationGenerator(0, false)]
[Flippable(39351, 39352)]
public partial class CompassionBanner : BaseVvVBanner
{
    public override int LabelNumber => 1123375;
    protected override int GumpImage => 30575;
    [Constructible]
    public CompassionBanner() : base(39351)
    {
    }
}

[SerializationGenerator(0, false)]
[Flippable(39335, 39336)]
public partial class CovetousBanner : BaseVvVBanner
{
    public override int LabelNumber => 1123359;
    protected override int GumpImage => 30567;
    [Constructible]
    public CovetousBanner() : base(39335)
    {
    }
}

[SerializationGenerator(0, false)]
[Flippable(39337, 39338)]
public partial class DeceitBanner : BaseVvVBanner
{
    public override int LabelNumber => 1123361;
    protected override int GumpImage => 30568;
    [Constructible]
    public DeceitBanner() : base(39337)
    {
    }
}

[SerializationGenerator(0, false)]
[Flippable(39339, 39340)]
public partial class DespiseBanner : BaseVvVBanner
{
    public override int LabelNumber => 1123363;
    protected override int GumpImage => 30569;
    [Constructible]
    public DespiseBanner() : base(39339)
    {
    }
}

[SerializationGenerator(0, false)]
[Flippable(39341, 39342)]
public partial class DestardBanner : BaseVvVBanner
{
    public override int LabelNumber => 1123365;
    protected override int GumpImage => 30570;
    [Constructible]
    public DestardBanner() : base(39341)
    {
    }
}

[SerializationGenerator(0, false)]
[Flippable(39353, 39354)]
public partial class HonestyBanner : BaseVvVBanner
{
    public override int LabelNumber => 1123377;
    protected override int GumpImage => 30576;
    [Constructible]
    public HonestyBanner() : base(39353)
    {
    }
}

[SerializationGenerator(0, false)]
[Flippable(39355, 39356)]
public partial class HonorBanner : BaseVvVBanner
{
    public override int LabelNumber => 1123379;
    protected override int GumpImage => 30577;
    [Constructible]
    public HonorBanner() : base(39355)
    {
    }
}

[SerializationGenerator(0, false)]
[Flippable(39357, 39358)]
public partial class HumilityBanner : BaseVvVBanner
{
    public override int LabelNumber => 1123381;
    protected override int GumpImage => 30578;
    [Constructible]
    public HumilityBanner() : base(39357)
    {
    }
}

[SerializationGenerator(0, false)]
[Flippable(39343, 39344)]
public partial class HythlothBanner : BaseVvVBanner
{
    public override int LabelNumber => 1123367;
    protected override int GumpImage => 30571;
    [Constructible]
    public HythlothBanner() : base(39343)
    {
    }
}

[SerializationGenerator(0, false)]
[Flippable(39359, 39360)]
public partial class JusticeBanner : BaseVvVBanner
{
    public override int LabelNumber => 1123383;
    protected override int GumpImage => 30579;
    [Constructible]
    public JusticeBanner() : base(39359)
    {
    }
}

[SerializationGenerator(0, false)]
[Flippable(39345, 39346)]
public partial class PrideBanner : BaseVvVBanner
{
    public override int LabelNumber => 1123369;
    protected override int GumpImage => 30572;
    [Constructible]
    public PrideBanner() : base(39345)
    {
    }
}

[SerializationGenerator(0, false)]
[Flippable(39361, 39362)]
public partial class SacraficeBanner : BaseVvVBanner
{
    public override int LabelNumber => 1123385;
    protected override int GumpImage => 30580;
    [Constructible]
    public SacraficeBanner() : base(39361)
    {
    }
}

[SerializationGenerator(0, false)]
[Flippable(39347, 39348)]
public partial class ShameBanner : BaseVvVBanner
{
    public override int LabelNumber => 1123371;
    protected override int GumpImage => 30573;
    [Constructible]
    public ShameBanner() : base(39347)
    {
    }
}

[SerializationGenerator(0, false)]
[Flippable(39363, 39364)]
public partial class SpiritualityBanner : BaseVvVBanner
{
    public override int LabelNumber => 1123387;
    protected override int GumpImage => 30581;
    [Constructible]
    public SpiritualityBanner() : base(39363)
    {
    }
}

[SerializationGenerator(0, false)]
[Flippable(39365, 39366)]
public partial class ValorBanner : BaseVvVBanner
{
    public override int LabelNumber => 1123389;
    protected override int GumpImage => 30582;
    [Constructible]
    public ValorBanner() : base(39365)
    {
    }
}

[SerializationGenerator(0, false)]
[Flippable(39349, 39350)]
public partial class WrongBanner : BaseVvVBanner
{
    public override int LabelNumber => 1123373;
    protected override int GumpImage => 30574;
    [Constructible]
    public WrongBanner() : base(39349)
    {
    }
}

using ModernUO.Serialization;
using Server.Engines.SeasonalEvents;
using Server.Mobiles;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/ForsakenFoes/BlackthornDungeonTeleporter.cs).
///     The chain gate reads <see cref="FellowshipData" /> instead of ServUO's file-backed
///     <c>Worker.FellowshipChainList</c>.
/// </summary>
[SerializationGenerator(0, false)]
public partial class BlackthornDungeonTeleporter : Item
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Point3D _dest;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private FellowshipChain _chain;

    [Constructible]
    public BlackthornDungeonTeleporter() : this(FellowshipChain.None)
    {
    }

    [Constructible]
    public BlackthornDungeonTeleporter(FellowshipChain chain) : base(0x1827)
    {
        _chain = chain;
        Visible = true;
        Movable = false;
    }

    public override bool OnMoveOver(Mobile m)
    {
        if (m is PlayerMobile)
        {
            if (_chain == FellowshipChain.None ||
                FellowshipData.TryGetChain(m, out var playerChain) && playerChain == _chain)
            {
                Timer.StartTimer(() => DoTeleport(m));
            }
            else
            {
                m.SendLocalizedMessage(1159235); // You may not pass.
            }
        }

        return base.OnMoveOver(m);
    }

    public virtual void DoTeleport(Mobile m)
    {
        var map = Map;

        if (map == null || map == Map.Internal)
        {
            map = m.Map;
        }

        var p = _dest;

        if (p == Point3D.Zero)
        {
            p = m.Location;
        }

        BaseCreature.TeleportPets(m, p, map);

        m.MoveToWorld(p, map);

        m.PlaySound(510);
    }
}

using System;
using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items;

/// <summary>
///     Ported from ServUO's Wrong Revamped dungeon (Soap.cs, MysteriousTunnel.cs, StinkingCauldron.cs,
///     WrongMazeWall.cs) — the small props that make Wrong what it is: soap left for the prisoners, the
///     tunnel that drops you into the dungeon, the cauldron that hatches maggots, and the maze walls that
///     only appear when someone walks past.
///
///     Adapted: WrongMazeWall's single-click uses an AsciiMessage, which this codebase doesn't have — it
///     falls back to the default item label. IPooledEnumerable loops became plain foreach.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Soap : Item
{
    public override int LabelNumber => 1152267; // soap

    [Constructible]
    public Soap() : this(1)
    {
    }

    [Constructible]
    public Soap(int amount) : base(0x1422)
    {
        Hue = 1285;
        Weight = 1.0;
        Stackable = true;
        Amount = amount;
    }
}

[SerializationGenerator(0, false)]
public partial class MysteriousTunnel : Item
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Point3D _pointDest;

    public override int LabelNumber => 1152265; // mysterious tunnel

    [Constructible]
    public MysteriousTunnel() : base(0x1B71)
    {
        Movable = false;
    }

    public override bool OnMoveOver(Mobile m)
    {
        if (m is not PlayerMobile)
        {
            return true;
        }

        var destination = _pointDest;
        m.MoveToWorld(destination, Map);
        BaseCreature.TeleportPets(m, destination, Map);

        return false;
    }
}

[SerializationGenerator(0, false)]
public partial class StinkingCauldron : Item
{
    public override int LabelNumber => 1152163; // stinking cauldron

    [Constructible]
    public StinkingCauldron() : base(0x142A)
    {
        Movable = false;
        Hue = 363;
        Weight = 0.0;

        Timer.DelayCall(TimeSpan.FromSeconds(10), HatchMaggots);
    }

    private void HatchMaggots()
    {
        if (Map is { } map && map != Map.Internal)
        {
            var amount = Utility.RandomMinMax(1, 2);

            for (var i = 0; i < amount; ++i)
            {
                var maggots = new GooeyMaggots();

                var x = Location.X + Utility.RandomMinMax(-2, 2);
                var y = Location.Y + Utility.RandomMinMax(-2, 2);
                maggots.MoveToWorld(new Point3D(x, y, map.GetAverageZ(x, y)), map);

                Effects.SendLocationParticles(
                    EffectItem.Create(Location, map, EffectItem.DefaultDuration),
                    0x3728,
                    8,
                    20,
                    5042
                );
            }
        }

        Delete();
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        Timer.DelayCall(TimeSpan.FromSeconds(10), HatchMaggots);
    }
}

[SerializationGenerator(0, false)]
public partial class WrongMazeWall : BaseWall
{
    [Constructible]
    public WrongMazeWall() : base(578)
    {
        Movable = false;
        Visible = false;
    }

    public override bool HandlesOnMovement => true;

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        var visible = false;

        foreach (var mobile in GetMobilesInRange(2))
        {
            if (mobile.InRange(Location, 1))
            {
                visible = true;
                break;
            }
        }

        Visible = visible;
    }
}

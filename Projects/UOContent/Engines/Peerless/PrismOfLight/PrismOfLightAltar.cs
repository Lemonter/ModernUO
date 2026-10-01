using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items;

/// <summary>The Prism of Light altar. Ported from ServUO
/// (Scripts/Items/Functional/PrismOfLightAltar.cs).
///
/// The one altar that isn't offered to directly: it is invisible, and the crystals go onto six
/// pedestals standing around it, each of which accepts exactly one kind and lights up when it
/// has it. The pedestals forward the drop to the altar, so all the offering logic stays in the
/// base class.</summary>
[SerializationGenerator(0, false)]
public partial class PrismOfLightAltar : PeerlessAltar
{
    [Tidy]
    [SerializableField(0)]
    private List<PrismOfLightPillar> _pedestals;

    [Constructible]
    public PrismOfLightAltar() : base(0x2206)
    {
        Visible = false;

        _pedestals = new List<PrismOfLightPillar>();

        BossLocation = new Point3D(6520, 122, -20);
        TeleportDest = new Point3D(6520, 139, -20);
        ExitDest = new Point3D(3785, 1107, 20);
    }

    public override int KeyCount => 3;
    public override MasterKey MasterKey => new PrismOfLightKey();

    public override Type[] Keys { get; } =
    {
        typeof(JaggedCrystals),
        typeof(BrokenCrystals),
        typeof(PiecesOfCrystal),
        typeof(CrushedCrystals),
        typeof(ScatteredCrystals),
        typeof(ShatteredCrystals)
    };

    public override BasePeerless Boss => new ShimmeringEffusion();

    public override Rectangle2D[] BossBounds { get; } = { new(6500, 111, 45, 35) };

    /// <summary>Clearing the offering also puts the pedestals back to their own colours.</summary>
    protected override void ClearContainer()
    {
        base.ClearContainer();

        foreach (var pedestal in _pedestals)
        {
            if (pedestal?.Deleted == false)
            {
                pedestal.ResetHue();
            }
        }
    }
}

/// <summary>One of the six pedestals around the Prism of Light altar. Each takes the crystal at
/// its own index in the altar's Keys array and nothing else.</summary>
[SerializationGenerator(0, false)]
public partial class PrismOfLightPillar : Container
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private PrismOfLightAltar _altar;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _pillarId;

    [SerializableField(2)]
    private int _originalHue;

    [Constructible]
    public PrismOfLightPillar(PrismOfLightAltar altar = null, int hue = 0) : base(0x207D)
    {
        Movable = false;

        _altar = altar;
        _originalHue = hue;

        Hue = hue;
    }

    public override bool OnDragDrop(Mobile from, Item dropped)
    {
        var altar = _altar;

        if (altar == null)
        {
            return false;
        }

        var keys = altar.Keys;

        if (_pillarId < 0 || _pillarId >= keys.Length || !keys[_pillarId].IsAssignableFrom(dropped.GetType()))
        {
            from.SendLocalizedMessage(1072682); // This is not the proper key.
            return false;
        }

        if (!altar.OnDragDrop(from, dropped))
        {
            return false;
        }

        Hue = 36;
        return true;
    }

    public void ResetHue()
    {
        Hue = _originalHue;
    }
}

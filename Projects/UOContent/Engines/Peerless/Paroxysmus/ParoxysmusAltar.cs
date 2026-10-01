using System;
using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items;

/// <summary>The Palace of Paroxysmus altar. Ported from ServUO
/// (Scripts/Items/Functional/ParoxysmusAltar.cs). Takes sixteen offerings — four kinds, and
/// the altar hands back sixteen keys, which is why this encounter is the one people bring a
/// crowd to.</summary>
[SerializationGenerator(0, false)]
public partial class ParoxysmusAltar : PeerlessAltar
{
    [Constructible]
    public ParoxysmusAltar() : base(0x207A)
    {
        Hue = 0x465;

        BossLocation = new Point3D(6517, 357, 0);
        TeleportDest = new Point3D(6519, 381, 0);
        ExitDest = new Point3D(5623, 3038, 15);
    }

    public override int KeyCount => 16;
    public override MasterKey MasterKey => new ParoxysmusKey();

    public override Type[] Keys { get; } =
    {
        typeof(CoagulatedLegs),
        typeof(PartiallyDigestedTorso),
        typeof(GelatanousSkull),
        typeof(SpleenOfThePutrefier)
    };

    public override BasePeerless Boss => new ChiefParoxysmus();

    public override Rectangle2D[] BossBounds { get; } = { new(6501, 351, 35, 48) };
}

/// <summary>Ported from ServUO. The way out of the Paroxysmus arena — only for people who are
/// actually in the fight, so it can't be used as a back door in.</summary>
[SerializationGenerator(0, false)]
public partial class ParoxysmusIronGate : Item
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private ParoxysmusAltar _altar;

    [Constructible]
    public ParoxysmusIronGate() : base(0x857)
    {
        Movable = false;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.Alive || _altar == null || !_altar.Fighters.Contains(from))
        {
            return;
        }

        // The rusty gate cracks open as you step through.
        from.SendLocalizedMessage(1112061);

        _altar.Exit(from);
    }
}

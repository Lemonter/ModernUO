using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items;

/// <summary>An invisible tile that only lets a player through once the Sacred Quest is done.
/// Ported from ServUO (Scripts/Items/Internal/AbyssBarrier.cs).</summary>
[SerializationGenerator(0, false)]
public partial class AbyssBarrier : Item
{
    [Constructible]
    public AbyssBarrier() : base(0x49E)
    {
        Movable = false;
        Visible = false;
    }

    public override bool OnMoveOver(Mobile m)
    {
        if (m.AccessLevel > AccessLevel.Player)
        {
            return true;
        }

        if (m is not PlayerMobile pm)
        {
            return false;
        }

        if (!pm.AbyssEntry)
        {
            pm.SendLocalizedMessage(1112226); // Thou must be on a Sacred Quest to pass through.
            return false;
        }

        pm.SendLocalizedMessage(1113708);
        return true;
    }
}

/// <summary>The same gate in the Tomb of Kings, where the stone guardians stand. Ported from
/// ServUO (Scripts/Services/Tomb of Kings/SacredQuestBlocker.cs). Unlike the barrier above it
/// also lets a pet through on its master's credentials.</summary>
[SerializationGenerator(0, false)]
public partial class SacredQuestBlocker : Item
{
    [Constructible]
    public SacredQuestBlocker() : base(0x1BC3)
    {
        Name = "Sacred Quest Blocker";
        Movable = false;
        Visible = false;
    }

    public override bool OnMoveOver(Mobile m)
    {
        if (!base.OnMoveOver(m))
        {
            return false;
        }

        var pm = m switch
        {
            BaseCreature bc => bc.ControlMaster as PlayerMobile,
            PlayerMobile p  => p,
            _               => null
        };

        if (pm?.AbyssEntry == true)
        {
            m.SendLocalizedMessage(1112227); // May the Virtues guide thine quest.
            return true;
        }

        m.SendLocalizedMessage(1112226); // Thou must be on a Sacred Quest to pass through.
        return false;
    }
}

/// <summary>One of the Underworld's own teleporters, which refuses anyone who has not finished
/// the Sacred Quest. Ported from ServUO (Scripts/Items/Internal/UnderworldTele.cs).</summary>
[SerializationGenerator(0, false)]
public partial class UnderworldTele : Teleporter
{
    [Constructible]
    public UnderworldTele()
    {
    }

    public override bool OnMoveOver(Mobile m)
    {
        if (m is not PlayerMobile player)
        {
            return true;
        }

        if (player.AbyssEntry)
        {
            return base.OnMoveOver(m);
        }

        player.SendLocalizedMessage(1077196); // You may not enter this area.
        return true;
    }
}

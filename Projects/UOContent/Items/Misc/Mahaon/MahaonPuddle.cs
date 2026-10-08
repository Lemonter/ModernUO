using System;
using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     A rain puddle — purely decorative, self-deletes via its own timer (or immediately
///     when PuddleSystem clears everything on weather change). Persists like any other
///     Item across a save/restart is fine — its lifespan timer just resumes counting down
///     the moment it deserializes back in, same as any other DelayCall-driven item.
/// </summary>
[SerializationGenerator(0, false)]
public partial class MahaonPuddle : Item
{
    public MahaonPuddle(int itemId, TimeSpan lifespan) : base(itemId)
    {
        Movable = false;
        Visible = true;

        Timer.DelayCall(lifespan, Delete);
    }

    // The lifespan timer above only gets armed on original creation, not on deserializing
    // back after a restart (timers don't survive that, only the item's raw state does) —
    // catch that case and clean it up shortly after load instead of leaving a stray puddle
    // sitting around forever.
    [AfterDeserialization]
    private void AfterDeserialization()
    {
        Timer.DelayCall(TimeSpan.FromMinutes(1), Delete);
    }

    public override bool OnMoveOver(Mobile m) => true; // never blocks — decorative only
}

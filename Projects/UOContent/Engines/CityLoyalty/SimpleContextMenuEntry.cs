/// <summary>
///     A minimal <see cref="ContextMenuEntry" /> that runs a delegate instead of a hand-written subclass — the
///     equivalent of ServUO's <c>SimpleContextMenuEntry</c> helper, which does not exist in this codebase.
/// </summary>
using System;
using Server.ContextMenus;

namespace Server.Engines.CityLoyalty;

public class SimpleContextMenuEntry : ContextMenuEntry
{
    private readonly Action<Mobile> _callback;

    public SimpleContextMenuEntry(Mobile from, int number, Action<Mobile> callback, int range = 3, bool enabled = true)
        : base(number, range)
    {
        _callback = callback;
        Enabled = enabled && from != null;
    }

    public override void OnClick(Mobile from, IEntity target) => _callback(from);
}

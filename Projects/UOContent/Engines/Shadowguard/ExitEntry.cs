using Server.ContextMenus;

namespace Server.Engines.Shadowguard;

// Not wired into PlayerMobile.GetContextMenuEntries — that's a shared core file this port
// deliberately didn't touch. Add `if (ShadowguardController.GetInstance(Location, Map) is { } inst
// && inst.Region.Contains(Location)) list.Add(new ExitEntry(this));` there if the right-click
// "Exit Shadowguard" menu option is wanted; the gump's own queue-exit button and the normal
// encounter-completion teleport already cover leaving.
public class ExitEntry : ContextMenuEntry
{
    private readonly Mobile _from;

    public ExitEntry(Mobile from) : base(1156287, -1) => _from = from; // Exit Shadowguard

    public override void OnClick(Mobile from, IEntity target)
    {
        var instance = ShadowguardController.GetInstance(_from.Location, _from.Map);

        if (instance == null || !instance.Region.Contains(_from.Location))
        {
            return;
        }

        ShadowguardEncounter.MovePlayer(_from, ShadowguardController.Instance.KickLocation);

        instance.Encounter?.CheckPlayerStatus(_from);
    }
}

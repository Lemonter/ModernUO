namespace Server.Items;

// Small interfaces ServUO's VvV reward items depend on that don't exist in this codebase
// (IOwnerRestricted/IAccountRestricted/IRevealableItem) — defined here rather than in a
// generic location since VvV is the only system using them in this port.
public interface IVvVItem
{
    bool IsVvVItem { get; set; }
}

public interface IOwnerRestricted
{
    Mobile Owner { get; set; }
    string OwnerName { get; set; }
}

public interface IAccountRestricted
{
    string Account { get; set; }
}

public interface IRevealableItem
{
    bool CheckWhenHidden { get; }
    bool CheckReveal(Mobile m);
    void OnRevealed(Mobile m);
}

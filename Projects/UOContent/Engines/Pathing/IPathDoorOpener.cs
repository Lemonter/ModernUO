namespace Server;

/// <summary>
/// A non-creature walker that opens doors in its way, such as a bot. The local A* then plans
/// through closed doors as creatures that can open them do, and the walker opens each door before
/// stepping into it (see <see cref="Engines.Pathing.Nav.NavFollower"/>).
/// </summary>
public interface IPathDoorOpener
{
    bool OpensDoors { get; }
}

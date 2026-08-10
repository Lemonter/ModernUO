namespace Server.Systems.MahaonRaids;

/// <summary>
///     Marks a creature as part of a Mahaon raid event. RewardMultiplier scales the base
///     guard-points/gold reward for killing it (e.g. dragons pay x20 per the original server).
/// </summary>
public interface IRaidSpawn
{
    int RewardMultiplier { get; }
}

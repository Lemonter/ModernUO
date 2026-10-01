namespace Server.Systems.MahaonBots;

/// <summary>
///     A modifier layer on top of BotArchetype — same archetype, different "feel". Rolled
///     once per bot at registration, stored on BotProfile (not BotMobile — a possessed
///     real player under [BecomeBot gets one too). Deliberately just a few numeric knobs,
///     not a separate AI branch per trait — keeps every existing decision point able to
///     just read profile.Personality.XxxMultiplier instead of needing its own bespoke
///     personality logic.
/// </summary>
public enum PersonalityTrait
{
    Balanced,
    Aggressive,  // fights longer before fleeing, picks fights it might lose
    Cautious,    // flees earlier, avoids close calls
    Greedy,      // won't share/drop loot, prioritizes gold-making activities
    Generous     // shares more readily, less money-motivated
}

public readonly struct BotPersonality
{
    public readonly PersonalityTrait Trait;

    public BotPersonality(PersonalityTrait trait)
    {
        Trait = trait;
    }

    // Multiplies FleeHealthThreshold — >1 flees earlier (more cautious), <1 fights longer.
    public double FleeThresholdMultiplier => Trait switch
    {
        PersonalityTrait.Aggressive => 0.5,  // fights down to half the normal flee point
        PersonalityTrait.Cautious   => 1.6,  // bails much earlier
        _                            => 1.0
    };

    // Multiplies the base chance to engage a fight it could otherwise avoid.
    public double EngageChanceMultiplier => Trait switch
    {
        PersonalityTrait.Aggressive => 1.5,
        PersonalityTrait.Cautious   => 0.4,
        _                            => 1.0
    };

    // Multiplies how readily this bot lists spare loot for auction / shares with party vs
    // just banking it for itself.
    public double GenerosityMultiplier => Trait switch
    {
        PersonalityTrait.Greedy    => 0.3,
        PersonalityTrait.Generous  => 1.8,
        _                           => 1.0
    };

    public static BotPersonality Roll() => new(
        Utility.RandomDouble() switch
        {
            < 0.15 => PersonalityTrait.Aggressive,
            < 0.30 => PersonalityTrait.Cautious,
            < 0.45 => PersonalityTrait.Greedy,
            < 0.55 => PersonalityTrait.Generous,
            _      => PersonalityTrait.Balanced
        }
    );
}

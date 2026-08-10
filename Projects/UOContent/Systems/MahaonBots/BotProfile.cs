using System;
using Server.Items;
using Server.Mobiles;

namespace Server.Systems.MahaonBots;

/// <summary>
///     Everything the scheduler needs to know about one bot between decision ticks.
///     Kept separate from the Mobile itself so we're not fighting PlayerMobile's own
///     serialization for bot-specific bookkeeping.
/// </summary>
public class BotProfile
{
    public BotActivity Activity = BotActivity.Idle;
    public DateTime NextDecisionTime;

    public Point3D HomeLocation;
    public Map HomeMap;
    public string HomeCity;
    public string CurrentCity;

    // If true, this bot never rolls into city-to-city travel (used for bots meant to stay
    // put in one city, like a named local fixture).
    public bool Stationary;

    // Strider: always picks a fight with any PK bot he finds — and loses, that's his fate.
    public bool IsChallenger;

    // Alard: panics and flees from any PK or combat attempt instead of fighting.
    public bool IsCoward;
    public DateTime LastPanicMessage;

    public string TravelDestinationCity;

    // Mage bots: who to heal once the pending GreaterHeal cast resolves its target.
    public Mobile PendingHealTarget;

    // Set while gathering/crafting/traveling — how many more decision cycles this
    // activity has left before it naturally wraps up.
    public int CyclesRemaining;

    public SkillName GatherSkill = SkillName.Mining;
    public Point3D GatherDestination;

    // How many consecutive ticks TryFindHarvestTarget has failed at the current spot —
    // once this crosses GatherGiveUpAfter, the spot's a bust and it's worth a real
    // re-scout instead of jittering to a fresh random point every single failed tick.
    public int GatherSearchFailures;


    // Mage bots: don't re-buff every single tick — refresh on a cooldown instead.
    public DateTime NextBuffTime;

    // Which field item a pending DispelField cast should resolve against.
    public Item PendingFieldTarget;

    // Which creature a pending AnimalTaming attempt should resolve against.
    public Mobile PendingTameTarget;

    // The pet an archer "unpacked" for this fight — put away again once combat's over.
    public BaseCreature SummonedPet;

    // Which school a mage bot leans on for combat — Magery or Necromancy. Chosen once at
    // creation, not something they switch between mid-fight.
    public bool PrefersNecromancy;

    // If this bot was spawned by a beacon statue, it respawns there instead of wherever it
    // died — null for bots seeded the normal way (cities/travelers/named).
    public Item OwnerBeacon;

    // Real pathfinding state for long trips — recreated whenever the destination changes,
    // reused across ticks otherwise so it isn't recomputing the route every single step.
    public PathFollower ActivePath;
    public Point3D ActivePathGoal;

    // Stuck detection — if a bot doesn't actually move for too many consecutive movement
    // ticks while supposedly traveling, something's wrong (bad pathing, boxed in) and it
    // should just teleport home instead of standing there forever.
    public Point3D LastCheckedPosition;
    public int StuckTicks;

    public BotParty Party;

    // -- Bard skills (Provocation/Peacemaking/Discordance) ------------------------------

    public enum PendingBardAction
    {
        None,
        ProvokeFirst,
        ProvokeSecond,
        Discord,
        Peace
    }

    // Don't roll bard skills every single tick — refresh on a cooldown instead, same
    // pattern as NextBuffTime for mages.
    public DateTime NextBardSkillTime;

    // Which bard skill's Target we're currently waiting on the resolution of. Set right
    // before calling the real SkillHandlers.*.OnUse, read back on the next tick once
    // bot.Target is populated — can't pattern-match on the Target's concrete type since
    // Provocation/Peacemaking/Discordance all keep their Target classes private.
    public PendingBardAction BardAction;

    // Provocation targets two creatures against each other — who we already picked as
    // target #1 while waiting for the second tick's Target to pick #2.
    public Mobile PendingProvokeFirst;

    // -- Mage escape kit (Paralyze/Wall/Teleport when caught at melee range) ------------

    // Where an escape-cast Fire/Poison Field should land — a blocking point between the
    // mage and whoever's chasing, as opposed to the normal offensive use (cast on top of
    // the target). Null means "use the normal offensive spot". Also reused as the summon
    // point for Blade Spirits/Energy Vortex (dropped right at the mage's own feet).
    public Point3D? PendingGroundTarget;

    // Combat-only summon (Blade Spirits/Energy Vortex/Summon Daemon) — don't try to recast
    // every single tick while the last cast is still resolving (multi-tick target flow).
    public DateTime NextSummonAttempt;

    // Simple per-bot "personality" knob: higher = more likely to go do something productive
    // instead of idling. Lets us have a mix of AFK-forever bots and industrious ones.
    public double Ambition = Utility.RandomDouble();

    // A minority of bots are player-killers: instead of the usual peaceful loop, they
    // hunt down anyone nearby (other bots or real players) that isn't in their own party.
    // Sourced from BotMobile.IsPk (persisted there, not rerolled here) — see BotController.RegisterBot.
    public bool IsPK;

    public BotProfile(Point3D homeLocation, Map homeMap)
    {
        HomeLocation = homeLocation;
        HomeMap = homeMap;
    }
}

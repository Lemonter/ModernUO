using Server.Items;
using Server.Mobiles;
using Server.Regions;
using Server.Spells;

namespace Server.Engines.VoidPool;

/// <summary>
///     Ported from ServUO's Covetous void pool (Region.cs). The region owns the "did this creature die
///     inside the battle" hook that feeds the controller's score, and refuses recall/gate/mark so the
///     fight can't be bypassed from outside.
///
///     CanUseStuckMenu is not overridden: this codebase's Region has no such virtual (the original used
///     it to deny help-stuck inside the pool). Travel rules below already keep players from teleporting
///     in, which is what the denial was really for.
/// </summary>
public class VoidPoolRegion : BaseRegion
{
    private static readonly Rectangle2D[] Bounds =
    {
        new(5383, 1960, 236, 80),
        new(5429, 1948, 12, 10)
    };

    public VoidPoolRegion(VoidPoolController controller, Map map) : base("Бассейн Пустоты", map, DefaultPriority, Bounds)
    {
        Controller = controller;
    }

    public VoidPoolController Controller { get; }

    public void SendRegionMessage(int localization)
    {
        foreach (var m in GetMobiles())
        {
            if (m.Player)
            {
                m.SendLocalizedMessage(localization);
            }
        }
    }

    public void SendRegionMessage(int localization, int hue)
    {
        foreach (var m in GetMobiles())
        {
            if (m.Player)
            {
                m.SendLocalizedMessage(localization, "", hue);
            }
        }
    }

    public void SendRegionMessage(int localization, string args)
    {
        foreach (var m in GetMobiles())
        {
            if (m.Player)
            {
                m.SendLocalizedMessage(localization, args);
            }
        }
    }

    public override void OnDeath(Mobile m)
    {
        if (m is BaseCreature { Controlled: false, Summoned: false } creature && Controller is { OnGoing: true })
        {
            Controller.OnCreatureKilled(creature);

            if (creature is CovetousCreature { VoidSpawn: true } spawn)
            {
                var wave = spawn.Level;
                var bump = wave > 10 ? (double)System.Math.Min(60, wave - 10) / 1000.0 : 0;
                var chance = 0.001 + bump;

                if (chance > Utility.RandomDouble())
                {
                    var player = DemonKnight.FindRandomPlayer(creature);
                    var artifact = player == null ? null : VoidPoolRewards.DropRandomArtifact();

                    if (artifact != null)
                    {
                        var pack = player.Backpack;

                        if (pack == null || !pack.TryDropItem(player, artifact, false))
                        {
                            player.BankBox.DropItem(artifact);
                        }

                        // For your valor in combating the fallen beast, a special artifact has been bestowed on you.
                        player.SendLocalizedMessage(1062317);
                    }
                }
            }
        }

        base.OnDeath(m);
    }

    public override bool OnDoubleClick(Mobile m, object o)
    {
        if (o is Corpse corpse && m.AccessLevel == AccessLevel.Player &&
            (corpse.Owner == null || corpse.Owner is CovetousCreature { VoidSpawn: true }))
        {
            corpse.LabelTo(m, 1152684); // There is no loot on the corpse.
            return false;
        }

        return base.OnDoubleClick(m, o);
    }

    public override void AlterLightLevel(Mobile m, ref int global, ref int personal)
    {
        global = LightCycle.DungeonLevel;
    }

    public override bool CheckTravel(Mobile m, Point3D newLocation, TravelCheckType travelType, out TextDefinition message)
    {
        message = null;

        if (m.AccessLevel > AccessLevel.Player)
        {
            return true;
        }

        return travelType switch
        {
            TravelCheckType.RecallTo or TravelCheckType.GateTo or TravelCheckType.Mark => false,
            _ => true
        };
    }
}

using System;
using System.Collections.Generic;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Regions;

// Ported from real OSI/ServUO content (Scripts/Services/Dungeons/Underworld/Maze of
// Death/Region.cs) — a corridor where only a hidden "safe path" of tiles is trap-free.
// Wandering off it (or a GoldenCompass owner checking it) triggers a random damage trap;
// dying inside kicks the corpse back to the entrance. Registered programmatically (not
// through regions.json) by UnderworldPuzzleController, same pattern as ShadowguardRegion.
public class MazeOfDeathRegion : Region
{
    private static readonly Point2D[] SeedPoints =
    {
        new(1062, 1060), new(1062, 1059), new(1062, 1058), new(1061, 1058),
        new(1060, 1058), new(1060, 1057), new(1059, 1057), new(1059, 1056),
        new(1059, 1055), new(1060, 1055), new(1060, 1054), new(1060, 1053),
        new(1059, 1053), new(1059, 1052), new(1059, 1051), new(1059, 1050),
        new(1058, 1050), new(1058, 1049), new(1057, 1049), new(1057, 1048),
        new(1057, 1047), new(1057, 1046), new(1058, 1047), new(1059, 1047),
        new(1059, 1046), new(1059, 1045), new(1059, 1044), new(1060, 1044),
        new(1061, 1044), new(1061, 1043), new(1060, 1042), new(1059, 1042),
        new(1058, 1042), new(1057, 1042), new(1061, 1042), new(1061, 1041),
        new(1061, 1042), new(1062, 1042), new(1062, 1041), new(1062, 1040),
        new(1063, 1040), new(1063, 1041), new(1063, 1040), new(1063, 1039),
        new(1062, 1039), new(1062, 1038), new(1061, 1038), new(1061, 1037),
        new(1060, 1037), new(1059, 1037), new(1058, 1037), new(1057, 1037),
        new(1057, 1036), new(1057, 1035), new(1058, 1035), new(1059, 1035),
        new(1057, 1034), new(1057, 1033), new(1057, 1032), new(1058, 1032),
        new(1059, 1032), new(1060, 1032), new(1060, 1031), new(1060, 1030),
        new(1060, 1029), new(1061, 1029), new(1061, 1028), new(1061, 1027),
        new(1062, 1027), new(1063, 1027), new(1064, 1027), new(1061, 1026),
        new(1062, 1026), new(1063, 1026), new(1064, 1026), new(1061, 1026),
        new(1061, 1025), new(1061, 1024), new(1061, 1023), new(1061, 1022),
        new(1060, 1026), new(1059, 1026), new(1058, 1026), new(1058, 1025),
        new(1058, 1024), new(1058, 1023), new(1058, 1022), new(1058, 1021),
        new(1057, 1021), new(1057, 1020), new(1057, 1019), new(1057, 1018),
        new(1058, 1018), new(1059, 1018), new(1060, 1018), new(1061, 1018),
        new(1061, 1017), new(1061, 1016), new(1061, 1015), new(1061, 1014),
        new(1061, 1013), new(1061, 1012), new(1061, 1011), new(1061, 1010),
        new(1060, 1010), new(1059, 1010), new(1059, 1009), new(1059, 1008),
        new(1059, 1007), new(1059, 1006), new(1059, 1005), new(1059, 1004),
        new(1058, 1004), new(1057, 1004), new(1057, 1003), new(1057, 1002),
        new(1057, 1001), new(1057, 1000), new(1057, 999), new(1058, 999),
        new(1059, 999), new(1060, 999), new(1061, 999), new(1062, 999),
        new(1063, 999), new(1063, 998), new(1063, 997), new(1063, 996),
        new(1063, 995), new(1063, 994), new(1062, 994), new(1061, 994),
        new(1061, 993), new(1061, 992), new(1061, 991)
    };

    private static readonly Rectangle2D FrontEntrance = new(1057, 1062, 8, 6);
    private static readonly Rectangle2D RoomRect = new(1065, 1055, 11, 11);
    private static readonly Rectangle2D CorridorRect = new(1056, 990, 9, 72);
    private static readonly Rectangle2D PuzzleRoomRect = new(1065, 1023, 8, 8);
    private static readonly Rectangle2D RearEntrance = new(1056, 986, 9, 4);

    private static readonly Rectangle2D[] Bounds =
    {
        FrontEntrance, RoomRect, CorridorRect, PuzzleRoomRect, RearEntrance
    };

    private static readonly Rectangle2D DeathBounds = new(1057, 1062, 7, 5);
    private static readonly Rectangle2D TrapCorridor = new(1056, 991, 9, 70);

    public static List<Point2D> Path { get; private set; }

    private static MazeOfDeathRegion _instance;

    public static void Initialize()
    {
        if (_instance != null)
        {
            return;
        }

        _instance = new MazeOfDeathRegion();
        _instance.Register();

        Path = new List<Point2D>(SeedPoints);

        var toAdd = 33;

        while (toAdd > 0)
        {
            var x = Utility.RandomMinMax(TrapCorridor.X, TrapCorridor.X + TrapCorridor.Width);
            var y = Utility.RandomMinMax(TrapCorridor.Y, TrapCorridor.Y + TrapCorridor.Height);

            var p = new Point2D(x, y);

            if (!Path.Contains(p))
            {
                Path.Add(p);
                toAdd--;
            }
        }
    }

    public MazeOfDeathRegion() : base("Maze of Death", Map.TerMur, DefaultPriority, Bounds)
    {
    }

    public override bool OnBeginSpellCast(Mobile m, ISpell s)
    {
        if (m.AccessLevel > AccessLevel.Player)
        {
            return true;
        }

        if (s is Spells.Sixth.MarkSpell or Spells.Seventh.GateTravelSpell or Spells.Third.TeleportSpell)
        {
            m.SendLocalizedMessage(501802); // that spell doesn't seem to work.
            return false;
        }

        return base.OnBeginSpellCast(m, s);
    }

    public override void OnEnter(Mobile m)
    {
        if (m is not PlayerMobile { Alive: true } pm || (!FrontEntrance.Contains(pm.Location) && !RearEntrance.Contains(pm.Location)))
        {
            return;
        }

        pm.Frozen = true;
        pm.LocalOverheadMessage(MessageType.Regular, 33, 1113580); // You are filled with a sense of dread and impending doom!

        Timer.DelayCall(TimeSpan.FromSeconds(2.0), () =>
        {
            pm.LocalOverheadMessage(
                MessageType.Regular, 946,
                pm.Backpack?.FindItemByType<GoldenCompass>() != null ? 1113582 : 1113581
                // 1113582: I better proceed with caution.
                // 1113581: I might need something to help me navigate through this.
            );

            Timer.DelayCall(TimeSpan.FromSeconds(2.0), () => pm.Frozen = false);
        });
    }

    public override void OnLocationChanged(Mobile m, Point3D oldLocation)
    {
        base.OnLocationChanged(m, oldLocation);

        if (m == null)
        {
            return;
        }

        if (m.Alive)
        {
            if (TrapCorridor.Contains(m.Location) && !Path.Contains(new Point2D(m.Location.X, m.Location.Y)))
            {
                SpringTrap(m);
            }
            else if (CorridorRect.Contains(m.Location) && m.Backpack?.FindItemByType(typeof(GoldenCompass)) != null)
            {
                m.CloseGump<CompassDirectionGump>();
                m.SendGump(new CompassDirectionGump(m));
            }
            else if (m.HasGump<CompassDirectionGump>())
            {
                m.CloseGump<CompassDirectionGump>();
            }
        }
        else if (TrapCorridor.Contains(m.Location))
        {
            m.MoveToWorld(new Point3D(1060, 1066, -42), Map.TerMur);
        }
    }

    public override void OnDeath(Mobile m)
    {
        base.OnDeath(m);

        if (m.Player && TrapCorridor.Contains(m.Location))
        {
            Timer.DelayCall(TimeSpan.FromSeconds(3), () => KickToEntrance(m));
        }
    }

    private static void SpringTrap(Mobile from)
    {
        if (from is not { Alive: true })
        {
            return;
        }

        int cliloc;
        var damage = Utility.RandomMinMax(75, 150);

        switch (Utility.Random(4))
        {
            default:
            case 0:
                Effects.SendLocationEffect(from, 0x3709, 1, 30);
                from.PlaySound(0x54);
                cliloc = 1010524; // Searing heat scorches thy skin.
                AOS.Damage(from, damage, 0, 100, 0, 0, 0);
                break;
            case 1:
                from.PlaySound(0x223);
                cliloc = 1010525; // Pain lances through thee from a sharp metal blade.
                AOS.Damage(from, damage, 100, 0, 0, 0, 0);
                break;
            case 2:
                from.BoltEffect(0);
                cliloc = 1010526; // Lightning arcs through thy body.
                AOS.Damage(from, damage, 0, 0, 0, 0, 100);
                break;
            case 3:
                Effects.SendLocationEffect(from, 0x113A, 10, 20);
                from.PlaySound(0x231);
                from.ApplyPoison(from, Poison.Deadly);
                cliloc = 1010523; // A toxic vapor envelops thee.
                AOS.Damage(from, damage, 0, 0, 0, 100, 0);
                break;
        }

        from.LocalOverheadMessage(MessageType.Regular, 0xEE, cliloc);
    }

    private static void KickToEntrance(Mobile from)
    {
        if (from?.Map == null)
        {
            return;
        }

        var x = Utility.RandomMinMax(DeathBounds.X, DeathBounds.X + DeathBounds.Width);
        var y = Utility.RandomMinMax(DeathBounds.Y, DeathBounds.Y + DeathBounds.Height);
        var z = from.Map.GetAverageZ(x, y);

        var p = new Point3D(x, y, z);

        from.MoveToWorld(p, Map.TerMur);

        if (from.Player && !from.Alive && from.Corpse?.Deleted == false)
        {
            from.Corpse.MoveToWorld(p, Map.TerMur);
        }

        from.SendLocalizedMessage(1113566); // You will find your remains at the entrance of the maze.
    }
}

using System;
using System.Linq;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.VvV;

// Ported from real OSI/ServUO content (Scripts/Services/ViceVsVirtue/Gumps/BattleWarningGump.cs).
// `PublicMoongate.Moongates` (a static list) doesn't exist here — replaced with a `World.Items`
// scan. `Map.GetRandomSpawnPoint(Rectangle2D)` (doesn't exist here) replaced with a manual
// nearby-point search, same fix as elsewhere in this port.
public class BattleWarningGump : Gump
{
    public PlayerMobile User { get; }
    public Timer Timer { get; set; }

    public BattleWarningGump(PlayerMobile pm) : base(50, 50)
    {
        User = pm;

        AddBackground(0, 0, 500, 200, 83);

        AddHtmlLocalized(0, 25, 500, 20, 1154645, "#1155582", GumpColor.Convert32To16(0xFF0000));
        AddHtmlLocalized(10, 55, 480, 100, 1154645, "#1155583", 0xFFFF); // You are in an active Vice vs Virtue battle region!

        AddButton(463, 168, 4005, 4007, 1);
        AddHtmlLocalized(250, 171, 250, 20, 1155647, 0xFFFF); // Teleport to nearest Moongate?

        Timer = Timer.DelayCall(
            TimeSpan.FromMinutes(1),
            () =>
            {
                User.GetGumps().Close<BattleWarningGump>();
                ViceVsVirtueSystem.AddTempParticipant(User, null);
            }
        );
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        Timer?.Stop();
        Timer = null;

        if (info.ButtonID == 1)
        {
            PublicMoongate closestGate = null;
            var closestDist = 0.0;

            foreach (var gate in World.Items.Values.OfType<PublicMoongate>().Where(mg => mg.Map == User.Map))
            {
                var dist = User.GetDistanceToSqrt(gate);

                if (closestGate == null || dist < closestDist)
                {
                    closestDist = dist;
                    closestGate = gate;
                }
            }

            if (closestGate?.Map != null)
            {
                for (var i = 0; i < 25; i++)
                {
                    var x = Utility.RandomMinMax(closestGate.X - 5, closestGate.X + 5);
                    var y = Utility.RandomMinMax(closestGate.Y - 5, closestGate.Y + 5);
                    var z = closestGate.Map.GetAverageZ(x, y);

                    if (closestGate.Map.CanFit(x, y, z, 16, false, true))
                    {
                        var p = new Point3D(x, y, z);
                        BaseCreature.TeleportPets(User, p, closestGate.Map);
                        User.MoveToWorld(p, closestGate.Map);

                        return;
                    }
                }
            }
            else
            {
                User.SendLocalizedMessage(1155584); // You are now open to attack!
                ViceVsVirtueSystem.AddTempParticipant(User, null);
            }
        }
        else
        {
            User.SendLocalizedMessage(1155584); // You are now open to attack!
            ViceVsVirtueSystem.AddTempParticipant(User, null);
        }
    }
}

using System;
using Server.Gumps;
using Server.Mobiles;
using Server.Multis;
using Server.Items;
using Server.Regions;
using Server.Spells;
using Server.Targeting;

namespace Server.Engines.Shadowguard;

public class ShadowguardRegion : BaseRegion
{
    public ShadowguardInstance Instance { get; }

    public ShadowguardRegion(Rectangle2D bounds, string regionName, ShadowguardInstance instance)
        : base($"Shadowguard_{regionName}", Map.TerMur, DefaultPriority, bounds)
    {
        Instance = instance;
    }

    public override bool CheckTravel(Mobile m, Point3D newLocation, TravelCheckType travelType, out TextDefinition message)
    {
        message = null;

        if (Instance.InUse)
        {
            return travelType is TravelCheckType.TeleportFrom or TravelCheckType.TeleportTo;
        }

        return true;
    }

    public override void OnDeath(Mobile m)
    {
        if (!Instance.InUse)
        {
            return;
        }

        if (m is PlayerMobile)
        {
            Timer.DelayCall(TimeSpan.FromSeconds(2), () => Instance.Encounter?.CheckPlayerStatus(m));
        }
        else if (m is BaseCreature bc && Instance.Encounter != null)
        {
            Instance.Encounter.OnCreatureKilled(bc);
        }
    }

    public override bool OnTarget(Mobile m, Target t, object o)
    {
        if (m.AccessLevel >= AccessLevel.GameMaster)
        {
            return true;
        }

        if (o is AddonComponent addonComponent && addonComponent.ItemData.Height + addonComponent.Z > m.Z + 3)
        {
            return false;
        }

        if (o is StaticTarget staticTarget && staticTarget.Z > m.Z + 3)
        {
            return false;
        }

        if (t.Flags == TargetFlags.Harmful &&
            (o is LadyMinax || o is ShadowguardGreaterDragon dragon && dragon.Z > m.Z))
        {
            return false;
        }

        return base.OnTarget(m, t, o);
    }

    public override void OnSpeech(SpeechEventArgs args)
    {
        var m = args.Mobile;

        if (m.AccessLevel >= AccessLevel.GameMaster && args.Speech?.ToLowerInvariant().Trim() == "getprops")
        {
            if (Instance.Encounter != null)
            {
                m.SendGump(new PropertiesGump(m, Instance.Encounter));
            }
            else
            {
                m.SendMessage("Для этого экземпляра сейчас нет встречи.");
            }
        }
    }
}

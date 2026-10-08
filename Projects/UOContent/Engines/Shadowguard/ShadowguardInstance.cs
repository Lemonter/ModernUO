using System;
using System.Collections.Generic;
using System.Linq;
using Server.Engines.PartySystem;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Shadowguard;

[PropertyObject]
public class ShadowguardInstance
{
    private static readonly Type[] DeleteList = { typeof(ShadowguardCanal) };

    [CommandProperty(AccessLevel.GameMaster)]
    public Point3D Center { get; }

    [CommandProperty(AccessLevel.GameMaster)]
    public ShadowguardEncounter Encounter { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Index { get; }

    public ShadowguardRegion Region { get; }

    [CommandProperty(AccessLevel.GameMaster)]
    public ShadowguardController Controller { get; }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool InUse => Encounter != null;

    // Instance slots 13+ are the four Roof-finale rooms (see ShadowguardController.CenterPoints/EncounterBounds).
    [CommandProperty(AccessLevel.GameMaster)]
    public bool IsRoof => Index >= 13;

    public override string ToString() => "...";

    public ShadowguardInstance(ShadowguardController controller, Point3D center, Rectangle2D bounds, int index)
    {
        Controller = controller;
        Center = center;
        Index = index;

        Region = new ShadowguardRegion(bounds, index >= 13 ? $"Roof {index - 12}" : index.ToString(), this);
        Region.Register();
    }

    public bool TryBeginEncounter(Mobile m, bool fromQueue, EncounterType type)
    {
        var p = Party.Get(m);

        if (!fromQueue && p != null)
        {
            foreach (var info in p.Members)
            {
                if (!Controller.Lobby.Contains(new Point2D(info.Mobile.X, info.Mobile.Y)))
                {
                    // All members of your party must remain in the lobby of Shadowguard while your
                    // encounter is prepared. Make sure all members of your party are in the lobby
                    // and try again.
                    m.SendLocalizedMessage(1156186);
                    return false;
                }
            }
        }

        Encounter = ConstructEncounter(type);
        Controller.AddEncounter(Encounter);
        Encounter.OnBeforeBegin(m);

        return true;
    }

    private ShadowguardEncounter ConstructEncounter(EncounterType type) =>
        type switch
        {
            EncounterType.Orchard  => new OrchardEncounter(this),
            EncounterType.Armory   => new ArmoryEncounter(this),
            EncounterType.Fountain => new FountainEncounter(this),
            EncounterType.Belfry   => new BelfryEncounter(this),
            EncounterType.Roof     => new RoofEncounter(this),
            _                      => new BarEncounter(this)
        };

    public void CompleteEncounter()
    {
        if (InUse)
        {
            Encounter = null;
        }
    }

    public void ClearRegion()
    {
        foreach (var item in Region.GetItems())
        {
            if (item is Corpse corpse)
            {
                if (corpse.Owner is PlayerMobile)
                {
                    corpse.MoveToWorld(Controller.KickLocation, Map.TerMur);
                }
                else
                {
                    corpse.Delete();
                }
            }
            else if (item is BaseAddon addon)
            {
                addon.Internalize();
            }
            else if (item.Movable || IsInDeleteList(item))
            {
                item.Delete();
            }
        }

        foreach (var m in Region.GetMobiles())
        {
            if (m is BaseCreature bc && bc.GetMaster() is not PlayerMobile)
            {
                m.Delete();
            }
        }
    }

    private static bool IsInDeleteList(Item item)
    {
        if (item == null)
        {
            return false;
        }

        var itemType = item.GetType();

        foreach (var t in DeleteList)
        {
            if (itemType == t)
            {
                return true;
            }
        }

        return false;
    }

    public static void Initialize()
    {
        var controller = ShadowguardController.Instance;

        if (controller == null)
        {
            return;
        }

        foreach (var addon in controller.Addons)
        {
            if (addon.Map == Map.Internal)
            {
                continue;
            }

            var instance = ShadowguardController.GetInstance(addon.Location, addon.Map);

            if (instance is { InUse: false })
            {
                instance.ClearRegion();
            }
        }
    }
}

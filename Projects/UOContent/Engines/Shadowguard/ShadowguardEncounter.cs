using System;
using System.Collections.Generic;
using System.Linq;
using Server.Engines.PartySystem;
using Server.Items;
using Server.Gumps;
using Server.Mobiles;

namespace Server.Engines.Shadowguard;

public class EncounterDef
{
    public Point3D StartLoc { get; }
    public Point3D[] SpawnPoints { get; }
    public Rectangle2D[] SpawnRecs { get; }

    public EncounterDef(Point3D start, Point3D[] points, Rectangle2D[] recs)
    {
        StartLoc = start;
        SpawnPoints = points;
        SpawnRecs = recs;
    }
}

[PropertyObject]
public abstract class ShadowguardEncounter
{
    [CommandProperty(AccessLevel.GameMaster)]
    public ShadowguardController Controller => ShadowguardController.Instance;

    [CommandProperty(AccessLevel.GameMaster)]
    public ShadowguardRegion Region => Instance.Region;

    [CommandProperty(AccessLevel.GameMaster)]
    public Point3D StartLoc => Def.StartLoc;

    public Point3D[] SpawnPoints => Def.SpawnPoints;
    public Rectangle2D[] SpawnRecs => Def.SpawnRecs;

    [CommandProperty(AccessLevel.GameMaster)]
    public EncounterType Encounter { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool HasBegun { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool DoneWarning { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool Completed { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public DateTime StartTime { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public Mobile PartyLeader { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public ShadowguardInstance Instance { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool ForceExpire
    {
        get => false;
        set
        {
            if (value)
            {
                Expire();
            }
        }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool ForceComplete
    {
        get => false;
        set
        {
            if (value)
            {
                CompleteEncounter();
            }
        }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public BaseAddon Addon { get; set; }

    public abstract Type AddonType { get; }
    public EncounterDef Def => Defs[Encounter];

    public virtual TimeSpan EncounterDuration => TimeSpan.FromMinutes(30);
    public virtual TimeSpan ResetDuration => TimeSpan.FromSeconds(60);

    [CommandProperty(AccessLevel.GameMaster)]
    public bool Active => Controller != null && Controller.Encounters.Contains(this);

    public List<PlayerMobile> Participants { get; private set; } = new();

    public ShadowguardEncounter(EncounterType encounter, ShadowguardInstance instance = null)
    {
        Encounter = encounter;
        Instance = instance;

        if (instance != null)
        {
            instance.Encounter = this;
        }
    }

    public override string ToString() => Encounter.ToString();

    public int PartySize()
    {
        if (PartyLeader == null || Region == null)
        {
            return 0;
        }

        var inRegion = Region.GetPlayerCount();

        if (inRegion > 0)
        {
            return inRegion;
        }

        var p = Party.Get(PartyLeader);
        return p?.Members.Count ?? 1;
    }

    public void OnBeforeBegin(Mobile m)
    {
        PartyLeader = m;
        StartTime = Core.Now;

        DoneWarning = false;
        Setup();

        /* Please wait while your Shadowguard encounter is prepared. Do no leave the area or
        logoff during this time. You will teleported when the encounter is ready and if you
        leave there is no way to enter once the encounter begins. */
        SendPartyMessage(1156185);
        CheckAddon();

        Timer.DelayCall(ShadowguardController.ReadyDuration, OnBeginEncounter);
    }

    public void CheckAddon()
    {
        var addonType = AddonType;

        if (addonType == null)
        {
            return;
        }

        var ad = Controller.Addons.Find(a => a.GetType() == addonType && a.Map == Map.Internal);

        if (ad == null)
        {
            ad = Activator.CreateInstance(addonType) as BaseAddon;
            Controller.Addons.Add(ad);
        }

        ad!.MoveToWorld(new Point3D(Instance.Center.X - 1, Instance.Center.Y - 1, Instance.Center.Z), Map.TerMur);
        Addon = ad;
    }

    public void OnBeginEncounter()
    {
        AddPlayers(PartyLeader);
        HasBegun = true;

        // There is a 30 minute time limit for each encounter. You will receive a time limit warning at 5 minutes.
        SendPartyMessage(1156251, 0x20);
    }

    public void AddPlayers(Mobile m)
    {
        if (m == null || !m.Alive || !m.InRange(Controller.Location, 25) || m.NetState == null)
        {
            Reset(true);
            return;
        }

        var p = Party.Get(m);

        if (p != null)
        {
            foreach (var info in p.Members)
            {
                AddPlayer(info.Mobile);
            }
        }

        AddPlayer(m);
    }

    protected void SendPartyMessage(int cliloc, int hue = 0x3B2)
    {
        if (HasBegun)
        {
            using var mobiles = Region.GetMobilesPooled();
            foreach (var m in mobiles)
            {
                if (m is PlayerMobile pm)
                {
                    pm.SendLocalizedMessage(cliloc, null, hue);
                }
            }

            return;
        }

        if (PartyLeader == null)
        {
            return;
        }

        var p = Party.Get(PartyLeader);

        if (p != null)
        {
            foreach (var info in p.Members)
            {
                info.Mobile.SendLocalizedMessage(cliloc, null, hue);
            }
        }
        else
        {
            PartyLeader.SendLocalizedMessage(cliloc, null, hue);
        }
    }

    protected void SendPartyMessage(string message, int hue = 0x3B2)
    {
        if (HasBegun)
        {
            using var mobiles = Region.GetMobilesPooled();
            foreach (var m in mobiles)
            {
                if (m is PlayerMobile pm)
                {
                    pm.SendMessage(hue, message);
                }
            }

            return;
        }

        if (PartyLeader == null)
        {
            return;
        }

        var p = Party.Get(PartyLeader);

        if (p != null)
        {
            foreach (var info in p.Members)
            {
                info.Mobile.SendMessage(hue, message);
            }
        }
        else
        {
            PartyLeader.SendMessage(hue, message);
        }
    }

    public void AddPlayer(Mobile m)
    {
        var p = StartLoc;
        ConvertOffset(ref p);

        MovePlayer(m, p);
        m.GetGumps().Close<ShadowguardGump>();

        if (m is PlayerMobile pm)
        {
            Participants.Add(pm);
        }
    }

    public void DoWarning()
    {
        using var mobiles = Region.GetMobilesPooled();
        foreach (var m in mobiles)
        {
            if (m is PlayerMobile)
            {
                m.SendLocalizedMessage(1156252); // You have 5 minutes remaining in the encounter!
            }
        }

        DoneWarning = true;
    }

    public virtual void Expire(bool message = true)
    {
        if (message)
        {
            using var mobiles = Region.GetMobilesPooled();
            foreach (var m in mobiles)
            {
                if (m is PlayerMobile)
                {
                    m.SendLocalizedMessage(1156253, "", 0x32); // The encounter timer has expired!
                }
            }
        }

        Timer.DelayCall(TimeSpan.FromSeconds(5), () => Reset(true));
    }

    public virtual void CompleteEncounter()
    {
        if (Completed)
        {
            return;
        }

        Timer.DelayCall(ResetDuration, () => Reset());

        if (this is RoofEncounter)
        {
            // Congratulations! You have bested Shadowguard and prevented Minax from exploiting
            // the Time Gate! You will be teleported out in a few minutes.
            SendPartyMessage(1156250);
        }
        else
        {
            // You have bested this tower of Shadowguard! You will be teleported out of the tower in 60 seconds!
            SendPartyMessage(1156244);
        }

        Completed = true;
    }

    public virtual void Reset(bool expired = false)
    {
        if (!Active)
        {
            return;
        }

        Controller.OnEncounterComplete(this, expired);

        RemovePlayers();
        PartyLeader = null;
        HasBegun = false;

        ClearItems();

        if (Addon != null)
        {
            Addon.Internalize();
            Addon = null;
        }

        Instance.ClearRegion();
        Instance.CompleteEncounter();
    }

    private void RemovePlayers()
    {
        using var mobiles = Region.GetMobilesPooled();
        foreach (var m in mobiles)
        {
            var isPet = m is BaseCreature bc && bc.GetMaster() is PlayerMobile;

            if (m is not PlayerMobile && !isPet)
            {
                continue;
            }

            MovePlayer(m, Controller.KickLocation, false);

            if (m is PlayerMobile pm)
            {
                Participants.Remove(pm);
            }
        }
    }

    public static void MovePlayer(Mobile m, Point3D p, bool pets = true)
    {
        if (pets)
        {
            BaseCreature.TeleportPets(m, p, m.Map);
        }

        m.MoveToWorld(p, m.Map);
        Effects.SendLocationParticles(EffectItem.Create(m.Location, m.Map, EffectItem.DefaultDuration), 0x3728, 10, 10, 5023);
        m.PlaySound(0x1FE);
    }

    public virtual void OnTick()
    {
    }

    public virtual void ClearItems()
    {
    }

    public virtual void Setup()
    {
    }

    public virtual void CheckEncounter()
    {
    }

    public virtual void OnCreatureKilled(BaseCreature bc)
    {
    }

    public void CheckPlayerStatus(Mobile m)
    {
        if (m is not PlayerMobile)
        {
            return;
        }

        using (var mobiles = Region.GetMobilesPooled())
        {
            foreach (var pm in mobiles)
            {
                if (pm is PlayerMobile { Alive: true, NetState: not null })
                {
                    return;
                }
            }
        }

        Expire(false);
        // All members of your party are dead, have logged off, or have chosen to exit
        // Shadowguard. You will be removed from the encounter shortly.
        SendPartyMessage(1156267);
    }

    public void ConvertOffset(ref Point3D p) =>
        p = new Point3D(Instance.Center.X + p.X, Instance.Center.Y + p.Y, Instance.Center.Z + p.Z);

    public void ConvertOffset(ref Rectangle2D rec) =>
        rec = new Rectangle2D(Instance.Center.X + rec.X, Instance.Center.Y + rec.Y, rec.Width, rec.Height);

    public virtual void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(2); // version

        writer.WriteEncodedInt(Participants.Count);
        foreach (var pm in Participants)
        {
            writer.Write(pm);
        }

        writer.Write(StartTime);
        writer.Write(Completed);
        writer.Write(HasBegun);

        writer.WriteEncodedInt(Instance.Index);
        writer.Write(PartyLeader);
        writer.Write(Addon);
    }

    public virtual void Deserialize(IGenericReader reader)
    {
        var version = reader.ReadEncodedInt();

        if (version >= 2)
        {
            var count = reader.ReadEncodedInt();
            Participants = new List<PlayerMobile>(count);
            for (var i = 0; i < count; i++)
            {
                if (reader.ReadEntity<PlayerMobile>() is { } pm)
                {
                    Participants.Add(pm);
                }
            }
        }

        if (version >= 1)
        {
            StartTime = reader.ReadDateTime();
            Completed = reader.ReadBool();
            HasBegun = reader.ReadBool();
        }

        var instanceIndex = reader.ReadEncodedInt();
        Instance = Controller?.Instances[instanceIndex];
        PartyLeader = reader.ReadEntity<Mobile>();
        Addon = reader.ReadEntity<Item>() as BaseAddon;

        if (Instance != null)
        {
            Instance.Encounter = this;
        }

        if (Completed)
        {
            Timer.DelayCall(ResetDuration, () => Reset());
        }
    }

    public static Dictionary<EncounterType, EncounterDef> Defs { get; private set; }

    public static void Configure()
    {
        Defs = new Dictionary<EncounterType, EncounterDef>
        {
            [EncounterType.Bar] = new(
                new Point3D(0, 0, 0),
                new[] { new Point3D(-16, 8, 0), new Point3D(-16, 4, 0), new Point3D(-16, -6, 0), new Point3D(-16, -10, 0) },
                new[] { new Rectangle2D(-15, -12, 1, 8), new Rectangle2D(-15, 2, 1, 8) }
            ),

            [EncounterType.Orchard] = new(
                new Point3D(0, 0, 0),
                new[]
                {
                    new Point3D(-10, -11, 0), new Point3D(-18, -15, 0), new Point3D(-11, -19, 0), new Point3D(-17, -10, 0),
                    new Point3D(-21, 10, 0), new Point3D(-17, 16, 0), new Point3D(-13, 12, 0), new Point3D(-11, 18, 0),
                    new Point3D(10, -20, 0), new Point3D(10, -11, 0), new Point3D(14, -15, 0), new Point3D(17, -10, 0),
                    new Point3D(10, 10, 0), new Point3D(9, 16, 0), new Point3D(13, 16, 0), new Point3D(15, 10, 0)
                },
                Array.Empty<Rectangle2D>()
            ),

            [EncounterType.Armory] = new(
                new Point3D(0, 0, 0),
                new[]
                {
                    new Point3D(5, -7, 0), new Point3D(5, -9, 0), new Point3D(5, -11, 0), new Point3D(5, -13, 0),
                    new Point3D(5, -17, 0), new Point3D(5, -19, 0), new Point3D(5, -21, 0), new Point3D(5, 16, 0),
                    new Point3D(5, 18, 0), new Point3D(5, 11, 0), new Point3D(5, 9, 0),
                    new Point3D(-23, -10, 0), new Point3D(-20, -15, 0), new Point3D(-16, -19, 0),
                    new Point3D(9, 5, 0), new Point3D(11, 5, 0), new Point3D(16, 5, 0), new Point3D(18, 5, 0),
                    new Point3D(-21, 5, 0), new Point3D(-19, 5, 0), new Point3D(-17, 5, 0), new Point3D(-12, 5, 0),
                    new Point3D(-10, 5, 0), new Point3D(-8, 5, 0), new Point3D(-23, 5, 0),
                    new Point3D(-18, -17, 0), new Point3D(-10, -23, 0), new Point3D(-13, -21, 0)
                },
                new[]
                {
                    new Rectangle2D(-25, -24, 18, 18), new Rectangle2D(-25, 4, 18, 18),
                    new Rectangle2D(4, 20, 18, 18), new Rectangle2D(4, -6, 18, 18)
                }
            ),

            [EncounterType.Fountain] = new(
                new Point3D(11, 11, 0),
                new[] { new Point3D(-6, 7, 0), new Point3D(5, 7, 0), new Point3D(7, 5, 0), new Point3D(7, -6, 0) },
                new[]
                {
                    new Rectangle2D(-24, 8, 45, 17), new Rectangle2D(-24, -25, 45, 16),
                    new Rectangle2D(-25, -8, 16, 15), new Rectangle2D(8, -8, 16, 15),
                    new Rectangle2D(12, -4, 2, 6), new Rectangle2D(-4, 12, 6, 2)
                }
            ),

            [EncounterType.Belfry] = new(
                new Point3D(15, 1, 0),
                new[] { new Point3D(0, 0, 22), new Point3D(-5, -5, 22) },
                new[] { new Rectangle2D(8, -9, 15, 15), new Rectangle2D(-24, -9, 15, 18) }
            ),

            [EncounterType.Roof] = new(
                new Point3D(-8, -8, 0),
                new[] { new Point3D(0, 0, 30) },
                Array.Empty<Rectangle2D>()
            )
        };
    }

    public static ShadowguardEncounter ConstructEncounter(EncounterType type) =>
        type switch
        {
            EncounterType.Orchard  => new OrchardEncounter(),
            EncounterType.Armory   => new ArmoryEncounter(),
            EncounterType.Fountain => new FountainEncounter(),
            EncounterType.Belfry   => new BelfryEncounter(),
            EncounterType.Roof     => new RoofEncounter(),
            _                      => new BarEncounter()
        };
}

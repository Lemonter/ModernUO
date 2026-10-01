using System;
using System.Collections.Generic;
using System.Linq;
using Server.Items;
using Server.Mobiles;
using Server.Regions;
using Server.Spells;

namespace Server.Engines.Despise;

/// <summary>Ported from ServUO's Despise Revamped dungeon (Scripts/Services/Dungeons/
/// DespiseRevamped/Region.cs). IPooledEnumerable-based enumeration converted to plain
/// foreach throughout (see AI.cs's class doc comment for why). The ML Quest tie-in
/// (Quests.WhisperingWithWispsQuest.OnBossSlain) is dropped — see DespiseController's class
/// doc comment for the full list of what this port intentionally left out.</summary>
public class DespiseRegion : BaseRegion
{
    private readonly bool _lowerLevel;

    public DespiseRegion(string name, Rectangle2D[] bounds) : this(name, bounds, false)
    {
    }

    public DespiseRegion(string name, Rectangle2D[] bounds, bool lowerLevel) : base(name, Map.Trammel, DefaultPriority, bounds)
    {
        _lowerLevel = lowerLevel;
        Register();
    }

    private readonly Rectangle2D _kickBounds = new(5576, 626, 6, 10);

    public bool IsInGoodRegion(Point3D loc) => DespiseController.GoodBounds.Any(rec => rec.Contains(loc));

    public bool IsInEvilRegion(Point3D loc) => DespiseController.EvilBounds.Any(rec => rec.Contains(loc));

    public bool IsInLowerRegion(Point3D loc) => DespiseController.LowerLevelBounds.Any(rec => rec.Contains(loc));

    public bool IsInStartRegion(Point3D loc) => !IsInLowerRegion(loc) && !IsInEvilRegion(loc) && !IsInGoodRegion(loc);

    public override void OnDeath(Mobile m)
    {
        base.OnDeath(m);

        if (m is DespiseBoss boss)
        {
            var controller = DespiseController.Instance;

            if (controller != null && controller.Boss == boss)
            {
                controller.OnBossSlain();
            }
        }
        else if (m is PlayerMobile && _lowerLevel)
        {
            KickFromRegion(m, false);
        }
    }

    public override bool OnBeforeDeath(Mobile m)
    {
        if (m is DespiseCreature dc && m.Region != null && m.Region.IsPartOf<DespiseRegion>() && !dc.Controlled && dc.Orb == null)
        {
            var creatures = new Dictionary<DespiseCreature, int>();

            foreach (var de in m.DamageEntries)
            {
                if (de.Damager is DespiseCreature creat)
                {
                    if (!creat.Controlled || creat.Orb == null)
                    {
                        continue;
                    }

                    creatures[creat] = creatures.GetValueOrDefault(creat) + de.DamageGiven;
                }
            }

            if (creatures.Count > 0)
            {
                DespiseCreature topDamager = null;
                var highest = 0;

                foreach (var (creature, damage) in creatures)
                {
                    if (topDamager == null || damage > highest)
                    {
                        topDamager = creature;
                        highest = damage;
                    }
                }

                if (topDamager != null && highest > 0)
                {
                    var mobKarma = Math.Abs(dc.Karma);
                    var karma = (int)((double)mobKarma / 10 * highest / dc.HitsMax);

                    if (karma < 1)
                    {
                        karma = 1;
                    }

                    if (dc.Karma > 0)
                    {
                        karma *= -1;
                    }

                    var master = topDamager.GetMaster();
                    var oldAlign = topDamager.Alignment;
                    var power = topDamager.Power;
                    topDamager.Karma += karma;
                    var newAlign = topDamager.Alignment;

                    if (master != null && karma > 0)
                    {
                        master.SendLocalizedMessage(1153281); // Your possessed creature has gained karma!
                    }
                    else if (master != null && karma < 0)
                    {
                        master.SendLocalizedMessage(1153282); // Your possessed creature has lost karma!
                    }

                    if (power < topDamager.MaxPower)
                    {
                        topDamager.AddProgress(dc.Power);

                        if (topDamager.Power > power && master != null)
                        {
                            master.SendLocalizedMessage(1153294, topDamager.Name); // ~1_NAME~ has achieved a new threshold in power!
                        }
                    }
                    else
                    {
                        master?.SendLocalizedMessage(1153309); // Your controlled creature cannot gain further power.
                    }

                    if (oldAlign != newAlign && newAlign != Alignment.Neutral && topDamager.MaxPower < 15)
                    {
                        topDamager.MaxPower = 15;

                        master?.SendLocalizedMessage(1153293, topDamager.Name); // ~1_NAME~ is growing in strength.

                        topDamager.Delta(MobileDelta.Noto);

                        topDamager.FixedEffect(0x373A, 10, 30);
                        topDamager.PlaySound(0x209);
                    }

                    if (master is { Map: not null } && master.Map != Map.Internal && master.Backpack != null)
                    {
                        var heart = new PutridHeart(Utility.RandomMinMax(dc.Power * 8, dc.Power * 10));

                        if (!master.Backpack.TryDropItem(master, heart, false))
                        {
                            heart.MoveToWorld(master.Location, master.Map);
                        }
                    }
                }
            }
        }

        return base.OnBeforeDeath(m);
    }

    public override bool OnDoubleClick(Mobile m, object o)
    {
        if (o is BallOfSummoning or BraceletOfBinding)
        {
            return false;
        }

        if (o is Corpse { Owner: null or DespiseCreature } && m.AccessLevel == AccessLevel.Player)
        {
            m.SendLocalizedMessage(1152684); // There is no loot on the corpse.
            return false;
        }

        return base.OnDoubleClick(m, o);
    }

    public static void GetArmyPower(ref int good, ref int evil)
    {
        foreach (var orb in WispOrb.Orbs)
        {
            if (orb.Alignment == Alignment.Good)
            {
                good += orb.GetArmyPower();
            }
            else if (orb.Alignment == Alignment.Evil)
            {
                evil += orb.GetArmyPower();
            }
        }
    }

    public override bool CheckTravel(Mobile from, Point3D p, TravelCheckType type, out TextDefinition message)
    {
        message = null; // default message

        if (from.AccessLevel > AccessLevel.Player)
        {
            return true;
        }

        return type switch
        {
            TravelCheckType.RecallFrom   => true,
            TravelCheckType.RecallTo     => false,
            TravelCheckType.GateFrom     => false,
            TravelCheckType.GateTo       => false,
            TravelCheckType.Mark         => false,
            TravelCheckType.TeleportFrom => true,
            TravelCheckType.TeleportTo   => true,
            _                            => false
        };
    }

    public override void OnEnter(Mobile m)
    {
        if (m.AccessLevel > AccessLevel.Player)
        {
            return;
        }

        if (!IsInStartRegion(m.Location) && m is BaseCreature { Controlled: true } or BaseCreature { Summoned: true }
            && m is not DespiseCreature and not CorruptedWisp and not EnsorcledWisp)
        {
            KickPet((BaseCreature)m);
        }

        if (m is PlayerMobile && IsInLowerRegion(m.Location))
        {
            var orb = DespiseController.GetWispOrb(m);

            if (orb == null)
            {
                Timer.DelayCall(TimeSpan.FromSeconds(1), () => KickCallback(m));
            }
        }
    }

    public override void OnExit(Mobile m)
    {
        base.OnExit(m);

        if (m is PlayerMobile)
        {
            // Fires immediately on leaving THIS sub-region — the player's Region may not
            // have settled on their new one yet at this exact instant (they could be moving
            // straight into an adjacent Despise sub-region), so defer one tick before
            // checking whether they've left Despise entirely.
            Timer.DelayCall(() => DespiseController.CheckOrbStillInRegion(m));
        }
    }

    public override void OnLocationChanged(Mobile m, Point3D oldLocation)
    {
        Timer.DelayCall(TimeSpan.FromSeconds(1.5), () =>
        {
            if (!IsInStartRegion(m.Location) && m is BaseCreature { Controlled: true } or BaseCreature { Summoned: true }
                && m is not DespiseCreature and not CorruptedWisp and not EnsorcledWisp)
            {
                var bc = (BaseCreature)m;

                if (bc.Summoned)
                {
                    bc.Delete();
                }
                else
                {
                    KickFromRegion(bc, false);
                }
            }
        });

        base.OnLocationChanged(m, oldLocation);
    }

    private void KickPet(BaseCreature bc)
    {
        Timer.DelayCall(TimeSpan.FromSeconds(0.5), () =>
        {
            if (bc.Summoned)
            {
                bc.Delete();
            }
            else
            {
                KickFromRegion(bc, false);
            }

            bc.GetMaster()?.SendLocalizedMessage(bc.Summoned ? 1153193 : 1153192); // Your pet has been teleported outside the Despise dungeon entrance.
        });
    }

    private void KickCallback(Mobile m)
    {
        KickFromRegion(m, true);
        m.SendLocalizedMessage(1153347); // Without the presence of a Wisp Orb, strong magical forces send you back to whence you came...
    }

    private void KickFromRegion(Mobile m, bool telepet)
    {
        while (true)
        {
            var x = Utility.RandomMinMax(_kickBounds.X, _kickBounds.X + _kickBounds.Width);
            var y = Utility.RandomMinMax(_kickBounds.Y, _kickBounds.Y + _kickBounds.Height);
            var z = Map.Trammel.GetAverageZ(x, y);
            var p = new Point3D(x, y, z);

            if (!Map.CanSpawnMobile(p))
            {
                continue;
            }

            if (m.Corpse != null)
            {
                m.Corpse.MoveToWorld(p, Map.Trammel);
            }

            m.MoveToWorld(p, Map.Trammel);

            if (telepet)
            {
                WispOrb.TeleportPet(m);
            }
            else
            {
                var orb = DespiseController.GetWispOrb(m);
                orb?.Pet?.Kill();
            }

            break;
        }
    }

    public override bool AllowHousing(Mobile from, Point3D p) => false;

    public override void AlterLightLevel(Mobile m, ref int global, ref int personal)
    {
        global = LightCycle.DungeonLevel;
    }
}

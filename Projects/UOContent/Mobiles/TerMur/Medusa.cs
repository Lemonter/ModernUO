using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Bosses/Medusa.cs). Substantially simplified —
/// this is the single largest/most-coupled file in the TerMur port. Dropped entirely (none of
/// these exist anywhere in this codebase, verified):
/// - BaseSABoss base (extends BaseCreature directly, like every other TerMur boss here).
/// - The whole Gorgon Lens deflection system (GorgonLense/LenseType/GorgonLenseCharges/
///   CheckBlockGaze/GetScaleEffectiveness) — Medusa's petrifying gaze now always lands on its
///   target; no in-game item can block it.
/// - BuffIcon.MedusaStone (not a member of this codebase's BuffIcon enum) — the BuffInfo.Add/
///   RemoveBuff calls that referenced it are dropped.
/// - IFreezable interface on MedusaClone (no consumers anywhere) and the packet-level
///   RemoveMobile/MobileIncoming/UpdateStatueAnimation "reveal the statue" trick (those packet
///   classes don't exist) — a released clone just becomes a normal, visible, mobile creature.
/// - UniqueSAList (Slither, Venom, PetrifiedSnake, StoneDragonsTooth,
///   MedusaFloorTileAddonDeed) and SharedSAList (SummonersKilt) — none exist. Medusa's own
///   weapon, IronwoodCompositeBow, also doesn't exist — substituted with the standard Bow.
/// - The whole scale-harvesting economy (MedusaDarkScales/MedusaBlood/MedusaLightScales/
///   MedusaStatue) — none of those items exist, so OnCarve's corpse drops, the live Carve()
///   shearing mechanic (and ICarvable), and the MedusaStatue rare-loot line are all dropped.
///   LootPack.LootItem&lt;T&gt; doesn't exist regardless (see WolfSpider.cs) — the Arrow drop
///   converted to a guaranteed OnDeath stack.
/// - GetRandomStoneMonster's UndeadGargoyle option (doesn't exist as a class here) — dropped,
///   picks among the remaining 5.
/// Kept faithful: stats/skills/resistances/loot tier, the petrification gaze mechanic (turns
/// a target into a frozen "clone" statue for a timed duration, tracked via m_TurnedToStone),
/// and the stone-guardian summon mechanic (SpawnStone/ReleaseStoneMonster, capped at 5 active
/// helpers). GazeTimer's original ": Timer" subclass rewritten as Timer.DelayCall closures
/// (matching the StygianDragon.cs precedent — no confirmed support for RunUO-era Timer
/// subclassing in this codebase). BaseCreature.Summon's local signature has no "bool
/// bValidLoc" middle parameter (5 args here vs ServUO's 6) — calls adjusted. IPooledEnumerable
/// enumeration replaced with Map.GetMobilesInRange throughout.</summary>
[SerializationGenerator(0, false)]
[CorpseName("a medusa corpse")]
public partial class Medusa : BaseCreature
{
    [SerializableField(0)]
    private List<Mobile> _turnedToStone = new();

    [SerializableField(1)]
    private List<Mobile> _helpers = new();

    private DateTime _gazeDelay;
    private DateTime _stoneDelay;

    [Constructible]
    public Medusa() : base(AIType.AI_Mage, FightMode.Closest, 10, 1)
    {
        Body = 728;

        SetStr(1235, 1391);
        SetDex(128, 139);
        SetInt(537, 664);

        SetHits(60000);

        SetDamage(21, 28);

        SetDamageType(ResistanceType.Physical, 60);
        SetDamageType(ResistanceType.Fire, 20);
        SetDamageType(ResistanceType.Energy, 20);

        SetResistance(ResistanceType.Physical, 55, 65);
        SetResistance(ResistanceType.Fire, 55, 65);
        SetResistance(ResistanceType.Cold, 55, 65);
        SetResistance(ResistanceType.Poison, 80, 90);
        SetResistance(ResistanceType.Energy, 60, 75);

        SetSkill(SkillName.Anatomy, 110.6, 116.1);
        SetSkill(SkillName.EvalInt, 100.0, 114.4);
        SetSkill(SkillName.Magery, 100.0);
        SetSkill(SkillName.Meditation, 118.2, 127.8);
        SetSkill(SkillName.MagicResist, 120.0);
        SetSkill(SkillName.Tactics, 111.9, 134.5);
        SetSkill(SkillName.Wrestling, 119.7, 128.9);

        Fame = 22000;
        Karma = -22000;

        AddItem(new Bow());
    }

    public override string DefaultName => "Медуза";

    public override bool IgnoreYoungProtection => true;
    public override bool AutoDispel => true;
    public override double AutoDispelChance => 1.0;
    public override bool BardImmune => true;
    public override Poison PoisonImmune => Poison.Lethal;
    public override Poison HitPoison => 0.8 >= Utility.RandomDouble() ? Poison.Deadly : Poison.Lethal;

    public override int GetIdleSound() => 1557;
    public override int GetAngerSound() => 1554;
    public override int GetHurtSound() => 1556;
    public override int GetDeathSound() => 1555;

    public override void OnGotMeleeAttack(Mobile attacker, int damage)
    {
        base.OnGotMeleeAttack(attacker, damage);

        if (0.05 > Utility.RandomDouble())
        {
            ReleaseStoneMonster();
        }
    }

    public override void OnDamagedBySpell(Mobile from, int damage)
    {
        base.OnDamagedBySpell(from, damage);

        if (0.05 > Utility.RandomDouble())
        {
            ReleaseStoneMonster();
        }
    }

    public override void OnHarmfulSpell(Mobile from)
    {
        base.OnHarmfulSpell(from);

        if (0.05 > Utility.RandomDouble())
        {
            ReleaseStoneMonster();
        }
    }

    public void RemoveAffectedMobile(Mobile toRemove)
    {
        _turnedToStone.Remove(toRemove);
    }

    private Mobile FindRandomMedusaTarget()
    {
        if (Map is not { } map)
        {
            return null;
        }

        var list = new List<Mobile>();

        foreach (var m in map.GetMobilesInRange(Location, 12))
        {
            if (m == this || _turnedToStone.Contains(m) || !CanBeHarmful(m) || !InLOS(m) ||
                m.AccessLevel > AccessLevel.Player)
            {
                continue;
            }

            if (m is PlayerMobile || (m is BaseCreature bc && bc.GetMaster() is PlayerMobile))
            {
                list.Add(m);
            }
        }

        return list.Count == 0 ? null : list[Utility.Random(list.Count)];
    }

    public override void OnThink()
    {
        base.OnThink();

        if (Combatant == null)
        {
            return;
        }

        if (_stoneDelay < DateTime.UtcNow)
        {
            SpawnStone();
        }

        if (_gazeDelay < DateTime.UtcNow)
        {
            DoGaze();
        }
    }

    private void DoGaze()
    {
        var target = FindRandomMedusaTarget();
        var map = Map;

        if (map == null || target == null)
        {
            return;
        }

        if ((target is BaseCreature tbc && tbc.SummonMaster != this) || CanBeHarmful(target))
        {
            var clone = new MedusaClone(target);

            var validLocation = false;
            var loc = Location;

            for (var j = 0; !validLocation && j < 10; ++j)
            {
                var x = X + Utility.Random(10) - 1;
                var y = Y + Utility.Random(10) - 1;
                var z = map.GetAverageZ(x, y);

                if (validLocation = map.CanFit(x, y, Z, 16, false, false))
                {
                    loc = new Point3D(x, y, Z);
                }
                else if (validLocation = map.CanFit(x, y, z, 16, false, false))
                {
                    loc = new Point3D(x, y, z);
                }
            }

            Effects.SendLocationEffect(loc, target.Map, 0x37B9, 10, 5);
            clone.Frozen = clone.Blessed = true;
            clone.SolidHueOverride = 761;

            target.Frozen = target.Blessed = true;
            target.SolidHueOverride = 761;

            Summon(clone, this, loc, 0, TimeSpan.FromMinutes(90));

            if (target is BaseCreature { Summoned: false } petTarget && petTarget.GetMaster() is { } master)
            {
                master.SendLocalizedMessage(1113281, null, 43); // Your pet has been petrified!
            }
            else
            {
                target.SendLocalizedMessage(1112768); // You have been turned to stone!!!
            }

            var duration = TimeSpan.FromSeconds(Utility.RandomMinMax(5, 10));

            Timer.DelayCall(duration, () =>
            {
                if (target.Deleted)
                {
                    return;
                }

                target.Frozen = false;
                target.SolidHueOverride = -1;
                target.Blessed = false;
                RemoveAffectedMobile(target);

                if (target is BaseCreature { Summoned: false } petTarget2 && petTarget2.GetMaster() is { } master2)
                {
                    master2.SendLocalizedMessage(1113285, null, 43); // Beware! A statue of your pet has been created!
                }
            });

            Timer.DelayCall(duration + duration, () =>
            {
                if (clone.Deleted)
                {
                    return;
                }

                clone.SolidHueOverride = -1;
                clone.Frozen = clone.Blessed = false;

                if (clone.Map is not { } cloneMap)
                {
                    return;
                }

                Mobile closest = null;
                var dist = 12;

                foreach (var m in cloneMap.GetMobilesInRange(clone.Location, 12))
                {
                    if (m is not PlayerMobile && !(m is BaseCreature mbc && mbc.GetMaster() is PlayerMobile))
                    {
                        continue;
                    }

                    m.SendLocalizedMessage(1112767); // Medusa releases one of the petrified creatures!!

                    var d = (int)clone.GetDistanceToSqrt(m.Location);
                    if (d < dist)
                    {
                        dist = d;
                        closest = m;
                    }
                }

                if (closest != null)
                {
                    clone.Combatant = closest;
                }
            });

            _helpers.Add(clone);
            _turnedToStone.Add(target);

            _gazeDelay = DateTime.UtcNow + TimeSpan.FromSeconds(Utility.RandomMinMax(45, 75));
            return;
        }

        _gazeDelay = DateTime.UtcNow + TimeSpan.FromSeconds(Utility.RandomMinMax(25, 65));
    }

    private void SpawnStone()
    {
        DefragHelpers();

        if (Map is not { } map)
        {
            return;
        }

        var stones = 0;
        foreach (var m in _helpers)
        {
            if (m is not MedusaClone)
            {
                ++stones;
            }
        }

        if (stones >= 5)
        {
            _stoneDelay = DateTime.UtcNow + TimeSpan.FromSeconds(Utility.RandomMinMax(30, 150));
            return;
        }

        var stone = GetRandomStoneMonster();

        var validLocation = false;
        var loc = Location;

        for (var j = 0; !validLocation && j < 10; ++j)
        {
            var x = X + Utility.Random(10) - 1;
            var y = Y + Utility.Random(10) - 1;
            var z = map.GetAverageZ(x, y);

            if (validLocation = map.CanFit(x, y, Z, 16, false, false))
            {
                loc = new Point3D(x, y, Z);
            }
            else if (validLocation = map.CanFit(x, y, z, 16, false, false))
            {
                loc = new Point3D(x, y, z);
            }
        }

        Summon(stone, this, loc, 0, TimeSpan.FromMinutes(90));
        stone.Frozen = stone.Blessed = true;
        stone.SolidHueOverride = 761;
        stone.Combatant = null;

        _helpers.Add(stone);

        _stoneDelay = DateTime.UtcNow + TimeSpan.FromSeconds(Utility.RandomMinMax(30, 150));
    }

    private void DefragHelpers()
    {
        _helpers.RemoveAll(m => m == null || !m.Alive || m.Deleted);
    }

    private static BaseCreature GetRandomStoneMonster() => Utility.Random(5) switch
    {
        0 => new OphidianWarrior(),
        1 => new OphidianArchmage(),
        2 => new WailingBanshee(),
        3 => new OgreLord(),
        _ => new Dragon()
    };

    private void ReleaseStoneMonster()
    {
        var stones = new List<Mobile>();

        foreach (var mob in _helpers)
        {
            if (mob is not MedusaClone && mob.Alive)
            {
                stones.Add(mob);
            }
        }

        if (stones.Count == 0)
        {
            return;
        }

        var m = stones[Utility.Random(stones.Count)];

        m.Frozen = m.Blessed = false;
        m.SolidHueOverride = -1;

        _helpers.Remove(m);

        if (m.Map is not { } map)
        {
            return;
        }

        Mobile closest = null;
        var dist = 12;

        foreach (var targ in map.GetMobilesInRange(m.Location, 12))
        {
            if (targ.Player)
            {
                targ.SendLocalizedMessage(1112767, null, 43); // Medusa releases one of the petrified creatures!!
                targ.Combatant = targ;
            }

            if (targ is PlayerMobile || (targ is BaseCreature tbc && tbc.GetMaster() is PlayerMobile))
            {
                var d = (int)m.GetDistanceToSqrt(targ.Location);

                if (d < dist)
                {
                    dist = d;
                    closest = targ;
                }
            }
        }

        if (closest != null)
        {
            m.Combatant = closest;
        }
    }

    public override void GenerateLoot()
    {
        AddLoot(LootPack.SuperBoss, 8);
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);
        c.DropItem(new Arrow(Utility.RandomMinMax(100, 200)));
    }

    public override void OnAfterDelete()
    {
        foreach (var m in _helpers)
        {
            if (m is { Deleted: false })
            {
                m.Delete();
            }
        }

        base.OnAfterDelete();
    }
}

/// <summary>Ported from ServUO (nested MedusaClone in Scripts/Mobiles/Bosses/Medusa.cs).
/// IFreezable dropped (no consumers anywhere in this codebase). The packet-level
/// OnRequestedAnimation statue-animation trick dropped (RemoveMobile/MobileIncoming/
/// UpdateStatueAnimation don't exist here). SetWearable converted to AddItem.</summary>
[SerializationGenerator(0, false)]
public partial class MedusaClone : BaseCreature
{
    public MedusaClone(Mobile m) : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        SolidHueOverride = 33;
        Clone(m);
    }

    public override bool DeleteCorpseOnDeath => true;
    public override bool ReacquireOnMovement => true;
    public override bool AlwaysMurderer => !Frozen;

    private void Clone(Mobile m)
    {
        if (m == null)
        {
            Delete();
            return;
        }

        Body = m.Body;

        Str = m.Str;
        Dex = m.Dex;
        Int = m.Int;

        Hits = m.HitsMax;

        Hue = m.Hue;
        Female = m.Female;

        Name = m.Name;
        NameHue = m.NameHue;

        Title = m.Title;
        Kills = m.Kills;

        HairItemID = m.HairItemID;
        HairHue = m.HairHue;

        FacialHairItemID = m.FacialHairItemID;
        FacialHairHue = m.FacialHairHue;

        BaseSoundID = m.BaseSoundID;

        for (var i = 0; i < m.Skills.Length; ++i)
        {
            Skills[i].Base = m.Skills[i].Base;
            Skills[i].Cap = m.Skills[i].Cap;
        }

        for (var i = 0; i < m.Items.Count; i++)
        {
            if (m.Items[i].Layer is not (Layer.Backpack or Layer.Mount or Layer.Bank))
            {
                AddItem(CloneItem(m.Items[i]));
            }
        }
    }

    private static Item CloneItem(Item item) => new(item.ItemID)
    {
        Layer = item.Layer,
        Name = item.Name,
        Hue = item.Hue,
        Weight = item.Weight,
        Movable = false
    };

    public override void OnDoubleClick(Mobile from)
    {
        if (Frozen)
        {
            DisplayPaperdollTo(from);
        }
        else
        {
            base.OnDoubleClick(from);
        }
    }

    public override void OnDelete()
    {
        Effects.SendLocationParticles(EffectItem.Create(Location, Map, EffectItem.DefaultDuration), 0x3728, 10, 15, 5042);

        base.OnDelete();
    }
}

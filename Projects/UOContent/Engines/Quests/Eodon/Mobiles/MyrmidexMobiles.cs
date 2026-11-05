using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Quests.Eodon;

/// <summary>
///     Bosses of the "Myrmidex Threat" Eodon line, ported from ServUO
///     (Scripts/Quests/Eodon/MyrmidexThreat/Mobiles/Creatures.cs).
///
///     Dropped, because the corresponding engine features do not exist here:
///     <list type="bullet">
///         <item><c>Zipactriotl</c>: <c>TeleportsTo</c>/<c>TeleportProb</c>/<c>GetTeleportTarget</c>, and the
///         <c>RampageSpell</c> mastery cast. Its special move is now the mana-converting energy wave.</item>
///         <item><c>IgnisFatalis</c>: <c>SetAreaEffect(AreaEffect.AuraOfEnergy)</c> — no area-effect aura framework.</item>
///         <item><c>MyrmidexQueen</c>: the rock-column <c>BleedAttack</c> and the ooze visual variants are simplified.</item>
///         <item><c>MoonstonePowerGeneratorAddon.ResetGenerators</c> on quest Zipactriotl deletion.</item>
///     </list>
/// </summary>
[SerializationGenerator(0, false)]
public partial class MyrmidexQueen : BaseCreature
{
    [SerializableField(0, setter: "private")]
    private List<BaseCreature> _spawn;

    private DateTime _nextCombo1;
    private DateTime _nextCombo2;
    private DateTime _nextEggThrow;

    [Constructible]
    public MyrmidexQueen() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Body = 1404;
        BaseSoundID = 959;

        SetHits(60000);
        SetStr(900, 1000);
        SetDex(535);
        SetInt(1000, 1200);

        SetDamage(18, 24);

        SetDamageType(ResistanceType.Physical, 40);
        SetDamageType(ResistanceType.Poison, 60);

        SetResistance(ResistanceType.Physical, 80, 90);
        SetResistance(ResistanceType.Fire, 60, 70);
        SetResistance(ResistanceType.Cold, 60, 70);
        SetResistance(ResistanceType.Poison, 80, 90);
        SetResistance(ResistanceType.Energy, 60, 70);

        SetSkill(SkillName.Wrestling, 110, 120);
        SetSkill(SkillName.Tactics, 120, 130);
        SetSkill(SkillName.MagicResist, 120, 130);
        SetSkill(SkillName.Anatomy, 0, 10);

        _spawn = new List<BaseCreature>();

        Fame = 35000;
        Karma = -35000;
    }

    public override string DefaultName => "королева мирмидекс";
    public override string CorpseName => "труп королевы мирмидекс";

    public override bool AlwaysMurderer => true;
    public override Poison PoisonImmune => Poison.Lethal;
    public override Poison HitPoison => Poison.Lethal;
    public override bool Unprovokable => true;

    public override void GenerateLoot() => AddLoot(LootPack.SuperBoss, 5);

    public override void OnThink()
    {
        base.OnThink();

        if (Combatant == null)
        {
            return;
        }

        if (_nextCombo1 < Core.Now && Utility.RandomDouble() < 0.1)
        {
            SpitOoze();
            _nextCombo1 = Core.Now + TimeSpan.FromSeconds(Utility.RandomMinMax(15, 30));
        }

        if (_nextCombo2 < Core.Now && Utility.RandomDouble() < 0.1)
        {
            if (Utility.RandomBool())
            {
                DropRocks();
            }
            else
            {
                RaiseRocks();
            }

            _nextCombo2 = Core.Now + TimeSpan.FromSeconds(Utility.RandomMinMax(30, 40));
        }

        if (_nextEggThrow < Core.Now && Utility.RandomDouble() < 0.1)
        {
            ThrowEggs();
            _nextEggThrow = Core.Now + TimeSpan.FromSeconds(Utility.RandomMinMax(90, 120));
        }
    }

    private void ThrowEggs()
    {
        if (Map == null)
        {
            return;
        }

        var delay = 0;

        for (byte i = 0; i <= 0x7; i++)
        {
            var direction = (Direction)i;
            int xOffset = 0;
            int yOffset = 0;

            Movement.Movement.Offset(direction, ref xOffset, ref yOffset);

            var x = X + 27 * xOffset;
            var y = Y + 27 * yOffset;

            var point = new Point3D(x, y, Map.GetAverageZ(x, y));

            if (!Map.CanFit(point, 16, false, false))
            {
                continue;
            }

            var spawnDelay = delay;

            Timer.StartTimer(
                TimeSpan.FromSeconds(spawnDelay),
                () =>
                {
                    var entity = new Entity(Serial.Zero, point, Map);
                    MovingParticles(entity, 4313, 10, 0, false, true, 1371, 0, 9502, 6014, 0x11D, EffectLayer.Waist, 0);

                    Timer.StartTimer(
                        TimeSpan.FromSeconds(Utility.RandomMinMax(2, 3)),
                        () =>
                        {
                            var type = Utility.RandomList(
                                typeof(MyrmidexWarrior),
                                typeof(MyrmidexDrone),
                                typeof(MyrmidexLarvae)
                            );

                            if (type.CreateInstance<BaseCreature>() is not { } creature)
                            {
                                return;
                            }

                            creature.MoveToWorld(point, Map);
                            _spawn.Add(creature);
                        }
                    );
                }
            );

            delay++;
        }
    }

    private void SpitOoze()
    {
        if (Map == null)
        {
            return;
        }

        foreach (var mobile in Map.GetMobilesInRange(Location, 7))
        {
            if (mobile == this || mobile.Map != Map || !CanBeHarmful(mobile, false))
            {
                continue;
            }

            var first = new OozeItem(this, 40222);
            first.MoveToWorld(mobile.Location, mobile.Map);
            first.OnMoveOver(mobile);

            var second = new OozeItem(this, Utility.Random(40214, 2));
            second.MoveToWorld(new Point3D(mobile.X - 1, mobile.Y, mobile.Z), mobile.Map);

            var third = new OozeItem(this, Utility.Random(40216, 2));
            third.MoveToWorld(new Point3D(mobile.X, mobile.Y + 1, mobile.Z), mobile.Map);

            var fourth = new OozeItem(this, Utility.Random(40218, 2));
            fourth.MoveToWorld(new Point3D(mobile.X, mobile.Y - 1, mobile.Z), mobile.Map);

            Timer.StartTimer(
                TimeSpan.FromSeconds(Utility.RandomMinMax(20, 30)),
                () =>
                {
                    first.Delete();
                    second.Delete();
                    third.Delete();
                    fourth.Delete();
                }
            );
        }
    }

    private void DropRocks()
    {
        var target = RandomPlayerInRange(12);

        if (target == null)
        {
            return;
        }

        var entity = new Entity(Serial.Zero, new Point3D(target.X, target.Y, target.Z + 40), target.Map);

        for (var i = 0; i < 5; i++)
        {
            var delay = i;

            Timer.StartTimer(
                TimeSpan.FromMilliseconds(100 * delay),
                () => Effects.SendMovingParticles(
                    entity,
                    target,
                    40136,
                    3,
                    60,
                    false,
                    true,
                    0,
                    0,
                    9502,
                    6014,
                    0x11D,
                    EffectLayer.Waist,
                    0
                )
            );
        }

        Timer.StartTimer(
            TimeSpan.FromMilliseconds(250),
            () =>
            {
                Effects.SendLocationEffect(target.Location, Map, 40136, 120);
                // *Crunch Crunch Crunch*
                target.PrivateOverheadMessage(MessageType.Regular, 0x21, 1156835, target.NetState);
            }
        );

        AOS.Damage(target, this, Utility.RandomMinMax(80, 100), 100, 0, 0, 0, 0);
    }

    private void RaiseRocks()
    {
        var target = RandomPlayerInRange(12);

        if (target == null)
        {
            return;
        }

        var direction = GetDirectionTo(target);
        var rect = direction switch
        {
            Direction.West  => new Rectangle2D(X - 24, Y - 2, 20, 5),
            Direction.North => new Rectangle2D(X - 2, Y - 24, 5, 20),
            Direction.East  => new Rectangle2D(X + 4, Y - 2, 20, 5),
            Direction.South => new Rectangle2D(X - 4, Y + 4, 20, 5),
            _               => new Rectangle2D(target.X - 8, target.Y - 2, 17, 5)
        };

        for (var x = rect.X; x <= rect.X + rect.Width; x++)
        {
            for (var y = rect.Y; y <= rect.Y + rect.Height; y++)
            {
                if (x > X - 4 && x < X + 4 && y > Y - 4 && y < Y + 4)
                {
                    continue;
                }

                if (Utility.RandomDouble() < 0.75)
                {
                    var id = Utility.RandomList(2282, 2273, 2277, 40106, 40107, 40108);
                    Effects.SendLocationEffect(new Point3D(x, y, Map.GetAverageZ(x, y)), Map, id, 60);
                }
            }
        }

        foreach (var mobile in Map.GetMobilesInBounds(rect))
        {
            if (mobile.Alive && mobile is PlayerMobile && CanBeHarmful(mobile, false) &&
                !(mobile.X > X - 4 && mobile.X < X + 4 && mobile.Y > Y - 4 && mobile.Y < Y + 4))
            {
                mobile.Freeze(TimeSpan.FromSeconds(2));
                AOS.Damage(mobile, this, Utility.RandomMinMax(100, 110), 100, 0, 0, 0, 0);
                // *Rising columns of rock rip through your flesh and concuss you!*
                mobile.PrivateOverheadMessage(MessageType.Regular, 0x21, 1156849, mobile.NetState);
            }
        }
    }

    private PlayerMobile RandomPlayerInRange(int range)
    {
        List<PlayerMobile> players = null;

        foreach (var mobile in Map.GetMobilesInRange(Location, range))
        {
            if (mobile.Alive && mobile is PlayerMobile pm && CanBeHarmful(pm, false))
            {
                (players ??= new List<PlayerMobile>()).Add(pm);
            }
        }

        return players?.Count > 0 ? players[Utility.Random(players.Count)] : null;
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        if (_spawn != null)
        {
            foreach (var creature in _spawn)
            {
                creature?.Delete();
            }

            _spawn = null;
        }
    }
}

[SerializationGenerator(0, false)]
public partial class OozeItem : Item
{
    [SerializableField(0)]
    private BaseCreature _owner;

    [Constructible]
    public OozeItem() : this(null, 40222)
    {
    }

    public OozeItem(BaseCreature owner, int itemID) : base(itemID)
    {
        _owner = owner;
        Movable = false;
        Hue = 2966;
    }

    public override int LabelNumber => 1156831; // Noxious Goo

    public override bool OnMoveOver(Mobile m)
    {
        if (m != _owner && _owner != null && _owner.Map == m.Map && _owner.CanBeHarmful(m, false))
        {
            if (Utility.RandomDouble() < 0.60)
            {
                // *The noxious goo has poisoned you!*
                m.PrivateOverheadMessage(MessageType.Regular, 0x21, 1156832, m.NetState);
                m.Poison = Poison.Deadly;
            }
            else
            {
                // *You are drenched in a noxious goo!*
                m.PrivateOverheadMessage(MessageType.Regular, 0x21, 1156830, m.NetState);
            }

            AOS.Damage(m, _owner, Utility.RandomMinMax(40, 60), 0, 0, 0, 100, 0);
        }

        return true;
    }
}

[SerializationGenerator(0, false)]
public partial class Zipactriotl : BaseCreature
{
    private DateTime _nextSpecial;

    [SerializableField(0)]
    private bool _isQuest;

    [Constructible]
    public Zipactriotl() : this(false)
    {
    }

    public Zipactriotl(bool quest) : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Body = 1405;
        BaseSoundID = 609;

        _isQuest = quest;

        SetHits(60000);
        SetStr(900, 1000);
        SetDex(800, 850);
        SetInt(1400, 1600);

        SetDamage(22, 26);

        SetDamageType(ResistanceType.Physical, 50);
        SetDamageType(ResistanceType.Energy, 50);

        SetResistance(ResistanceType.Physical, 80, 90);
        SetResistance(ResistanceType.Fire, 60, 70);
        SetResistance(ResistanceType.Cold, 60, 70);
        SetResistance(ResistanceType.Poison, 50, 60);
        SetResistance(ResistanceType.Energy, 70, 80);

        SetSkill(SkillName.Wrestling, 110, 120);
        SetSkill(SkillName.Tactics, 110, 120);
        SetSkill(SkillName.MagicResist, 120, 130);
        SetSkill(SkillName.Anatomy, 40, 60);

        Fame = 35000;
        Karma = -35000;
    }

    public override string DefaultName => "Зипактриотль";
    public override string CorpseName => "труп Зипактриотля";

    public override bool AlwaysMurderer => true;
    public override Poison PoisonImmune => Poison.Lethal;
    public override bool Unprovokable => true;

    public override WeaponAbility GetWeaponAbility() => WeaponAbility.Disarm;

    public override void GenerateLoot() => AddLoot(LootPack.SuperBoss, 5);

    public override void OnThink()
    {
        base.OnThink();

        if (Combatant == null || _nextSpecial >= Core.Now)
        {
            return;
        }

        DoSpecial();
        _nextSpecial = Core.Now + TimeSpan.FromSeconds(Utility.RandomMinMax(45, 60));
    }

    private void DoSpecial()
    {
        var map = Map;

        if (map == null || map == Map.Internal)
        {
            return;
        }

        foreach (var mobile in map.GetMobilesInRange(Location, 4))
        {
            if (mobile is PlayerMobile pm)
            {
                pm.Mana = 0;
                // *Your mana is converted to pure energy!*
                pm.PrivateOverheadMessage(MessageType.Regular, 0x21, 1156856, pm.NetState);
                AOS.Damage(pm, this, Utility.RandomMinMax(100, 150), 0, 0, 0, 0, 100);
            }
        }
    }
}

[SerializationGenerator(0, false)]
public partial class IgnisFatalis : BaseCreature
{
    [Constructible]
    public IgnisFatalis() : base(AIType.AI_Mage, FightMode.Closest, 10, 1)
    {
        Body = 0x105;
        BaseSoundID = 0x56B;

        SetHits(500);
        SetStr(350, 360);
        SetDex(100, 150);
        SetInt(580, 620);

        SetDamage(15, 22);

        SetDamageType(ResistanceType.Energy, 100);

        SetResistance(ResistanceType.Physical, 40, 50);
        SetResistance(ResistanceType.Fire, 40, 50);
        SetResistance(ResistanceType.Cold, 60, 70);
        SetResistance(ResistanceType.Poison, 70, 80);
        SetResistance(ResistanceType.Energy, 100);

        SetSkill(SkillName.Wrestling, 100);
        SetSkill(SkillName.Tactics, 100);
        SetSkill(SkillName.MagicResist, 100);
        SetSkill(SkillName.DetectHidden, 100.0);
        SetSkill(SkillName.Magery, 100.0);
        SetSkill(SkillName.EvalInt, 100.0);
        SetSkill(SkillName.Meditation, 100.0);
        SetSkill(SkillName.Focus, 100.0);
        SetSkill(SkillName.Spellweaving, 100.0);
    }

    public override string DefaultName => "Игнис Фаталис";
    public override string CorpseName => "труп Игниса Фаталиса";

    public override bool AlwaysMurderer => true;
}

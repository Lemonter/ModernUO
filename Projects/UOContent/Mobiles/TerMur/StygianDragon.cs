using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Bosses/StygianDragon.cs). BaseSABoss doesn't
/// exist here — extends BaseCreature directly (no dedicated SA-boss base class exists
/// anywhere in this codebase, and UniqueSAList/SharedSAList are BaseRenowned-only virtuals
/// that don't exist on plain BaseCreature — dropped along with the original's artifact list,
/// none of which exist anywhere in this codebase: BurningAmber, DraconisWrath,
/// DragonHideShield, FallenMysticsSpellbook, LifeSyphon, GargishSignOfOrder,
/// HumanSignOfOrder, VampiricEssence, AxesOfFury, SummonersKilt, GiantSteps,
/// TokenOfHolyFavor). SetWeaponAbility
/// called twice in the original (Bladeweave then TalonStrike) has no equivalent for a
/// GetWeaponAbility() override that can only return one value — kept Bladeweave, documented.
/// SetSpecialAbility dropped (see Rotworm.cs). StygianDragonHead (guaranteed OnDeath drop)
/// doesn't exist anywhere in this codebase — dropped, kept the ParagonChest roll (Paragon/
/// ParagonChest both confirmed to exist with matching signatures). The three combat
/// mechanics (Crimson Meteor rain, Fire Column, Stygian Fireball) are reimplemented with
/// Timer.StartTimer closures instead of the original's custom Timer-subclass pattern, since
/// this codebase's established timer convention (used throughout the Despise port) is
/// Timer.StartTimer/TimerExecutionToken, not RunUO-era ": Timer" subclassing.
/// IPooledEnumerable-based enumeration replaced with Map.GetMobilesInBounds (confirmed to
/// return a directly-foreach-able, non-pooled enumerable). SpellHelper.AdjustField and
/// ScreenLightFlash don't exist here — both dropped (the former only nudged effect Z-height
/// to ground level; the latter was a pure screen-flash cosmetic).</summary>
[SerializationGenerator(0, false)]
[CorpseName("a stygian dragon corpse")]
public partial class StygianDragon : BaseCreature
{
    private DateTime _delay;
    private TimerExecutionToken _meteorToken;
    private TimerExecutionToken _fireballToken;

    [Constructible]
    public StygianDragon() : base(AIType.AI_Mage, FightMode.Closest, 10, 1)
    {
        Body = 826;
        BaseSoundID = 362;

        SetStr(702);
        SetDex(250);
        SetInt(180);

        SetHits(30000);
        SetStam(431);
        SetMana(180);

        SetDamage(33, 55);

        SetDamageType(ResistanceType.Physical, 25);
        SetDamageType(ResistanceType.Fire, 50);
        SetDamageType(ResistanceType.Energy, 25);

        SetResistance(ResistanceType.Physical, 80, 90);
        SetResistance(ResistanceType.Fire, 80, 90);
        SetResistance(ResistanceType.Cold, 60, 70);
        SetResistance(ResistanceType.Poison, 80, 90);
        SetResistance(ResistanceType.Energy, 80, 90);

        SetSkill(SkillName.Anatomy, 100.0);
        SetSkill(SkillName.MagicResist, 150.0, 155.0);
        SetSkill(SkillName.Tactics, 120.7, 125.0);
        SetSkill(SkillName.Wrestling, 115.0, 117.7);

        Fame = 15000;
        Karma = -15000;

        Tamable = false;
    }

    public override string DefaultName => "стигийский дракон";

    public override WeaponAbility GetWeaponAbility() => WeaponAbility.Bladeweave;

    public override bool AlwaysMurderer => true;
    public override bool Unprovokable => false;
    public override bool BardImmune => false;
    public override bool AutoDispel => !Controlled;
    public override int Meat => 19;
    public override int Hides => 30;
    public override HideType HideType => HideType.Barbed;
    public override bool CanFlee => false;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.SuperBoss, 4);
        AddLoot(LootPack.Gems, 8);
    }

    public override void OnThink()
    {
        base.OnThink();

        if (Combatant is not Mobile combatant)
        {
            return;
        }

        if (DateTime.UtcNow > _delay)
        {
            switch (Utility.Random(3))
            {
                case 0:
                    CrimsonMeteor(combatant, 70, 125);
                    break;
                case 1:
                    DoStygianFireball(combatant);
                    break;
                case 2:
                    DoFireColumn();
                    break;
            }

            _delay = DateTime.UtcNow + TimeSpan.FromSeconds(Utility.RandomMinMax(30, 60));
        }
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        if (Paragon.ChestChance > Utility.RandomDouble())
        {
            c.DropItem(new ParagonChest(Name, 5));
        }
    }

    private void CrimsonMeteor(Mobile combatant, int minDamage, int maxDamage)
    {
        if (!combatant.Alive || combatant.Map is not { } map || map == Map.Internal)
        {
            return;
        }

        var loc = combatant.Location;
        var showerArea = new Rectangle2D(loc.X - 2, loc.Y - 2, 4, 4);
        var lastTarget = loc;

        var toDamage = new List<Mobile>();
        foreach (var m in map.GetMobilesInBounds(showerArea))
        {
            if (m != this && CanBeHarmful(m))
            {
                toDamage.Add(m);
            }
        }

        var count = 0;
        const int maxCount = 25;
        var doneDamage = false;

        Timer.StartTimer(TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(250), Callback, out _meteorToken);

        void Callback()
        {
            if (Deleted || map == Map.Internal)
            {
                _meteorToken.Cancel();
                return;
            }

            if (0.33 > Utility.RandomDouble())
            {
                var field = new StygianFireField(this, 25, Utility.RandomBool());
                field.MoveToWorld(lastTarget, map);
            }

            var finish = new Point3D(
                showerArea.X + Utility.Random(showerArea.Width),
                showerArea.Y + Utility.Random(showerArea.Height),
                Z
            );

            var start = new Point3D(finish.X + Utility.RandomMinMax(-4, 4), finish.Y - 15, finish.Z + 50);

            Effects.SendMovingParticles(
                new Entity(Serial.Zero, start, map),
                new Entity(Serial.Zero, finish, map),
                0x36D4, 15, 0, false, false, 0, 0, 9502, 1, 0, (EffectLayer)255, 0x100
            );

            Effects.PlaySound(finish, map, 0x11D);

            lastTarget = finish;
            count++;

            if (count >= maxCount / 2 && !doneDamage)
            {
                foreach (var mob in toDamage)
                {
                    var damage = Utility.RandomMinMax(minDamage, maxDamage);

                    DoHarmful(mob);
                    AOS.Damage(mob, this, damage, 0, 100, 0, 0, 0);

                    mob.FixedParticles(0x36BD, 1, 15, 9502, 0, 3, (EffectLayer)255);
                }

                doneDamage = true;
            }

            if (count >= maxCount)
            {
                _meteorToken.Cancel();
            }
        }
    }

    private void DoFireColumn()
    {
        if (Map is not { } map || Combatant is not Mobile combatant)
        {
            return;
        }

        var columnDir = Utility.GetDirection(Location, combatant.Location);
        var south = columnDir is Direction.East or Direction.West;

        var x = X;
        var y = Y;

        for (var i = 0; i < 8; i++)
        {
            Server.Movement.Movement.Offset(columnDir, ref x, ref y);

            var fire = new StygianFireField(this, Utility.RandomMinMax(25, 32), south);
            fire.MoveToWorld(new Point3D(x, y, Z), map);
        }
    }

    private void DoStygianFireball(Mobile combatant)
    {
        if (!InRange(combatant.Location, 10))
        {
            return;
        }

        PlaySound(0x1F3);

        var ticks = 0;

        Timer.StartTimer(TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(200), Callback, out _fireballToken);

        void Callback()
        {
            if (Deleted || combatant.Deleted)
            {
                _fireballToken.Cancel();
                return;
            }

            MovingParticles(combatant, 0x46E6, 7, 0, false, true, 1265, 0, 9502, 4019, 0x026, 0);

            if (ticks >= 10)
            {
                Timer.DelayCall(TimeSpan.FromSeconds(0.2), () =>
                {
                    var damage = Utility.RandomMinMax(120, 150);
                    DoHarmful(combatant);
                    AOS.Damage(combatant, this, damage, false, 0, 0, 0, 0, 0, 100, 0, false);
                });

                _fireballToken.Cancel();
                return;
            }

            ticks++;
        }
    }

}

/// <summary>Extracted as a top-level class (rather than nested inside StygianDragon) to avoid
/// untested nested-[SerializationGenerator] behavior — no precedent for that pattern
/// elsewhere in this codebase's ported content.</summary>
[SerializationGenerator(0, false)]
public partial class StygianFireField : Item
{
    private readonly Mobile _owner;
    private readonly DateTime _destroy;
    private TimerExecutionToken _tickToken;

    [Constructible]
    public StygianFireField(Mobile owner, int duration, bool south) : base(south ? 0x398C : 0x3996)
    {
        Movable = false;
        _destroy = DateTime.UtcNow + TimeSpan.FromSeconds(duration);
        _owner = owner;

        Timer.StartTimer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), OnTick, out _tickToken);
    }

    public override void OnAfterDelete()
    {
        _tickToken.Cancel();
    }

    private void OnTick()
    {
        if (DateTime.UtcNow > _destroy)
        {
            Delete();
            return;
        }

        if (Map is not { } map)
        {
            return;
        }

        foreach (var m in map.GetMobilesInRange(Location, 0))
        {
            if (CanTargetMob(m))
            {
                DealDamage(m);
            }
        }
    }

    public override bool OnMoveOver(Mobile m)
    {
        DealDamage(m);
        return true;
    }

    private void DealDamage(Mobile m)
    {
        if (m != _owner && CanTargetMob(m))
        {
            AOS.Damage(m, _owner, Utility.RandomMinMax(2, 4), 0, 100, 0, 0, 0);
        }
    }

    private bool CanTargetMob(Mobile m) =>
        m != _owner && _owner?.CanBeHarmful(m, false) == true &&
        (m is PlayerMobile || (m is BaseCreature bc && bc.GetMaster() is PlayerMobile));
}

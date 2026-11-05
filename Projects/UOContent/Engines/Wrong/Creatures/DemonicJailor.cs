using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;
using Server.Network;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO's Wrong Revamped dungeon (Mobile/DemonicJailor.cs) — the jailor that drags
///     attackers back to a cell and freezes spellcasters in place.
///
///     Adapted: <c>SetWearable(...)</c> is <see cref="Mobile.AddItem" /> here, and both
///     <c>OnDamagedBySpell</c> and <c>OnGaveMeleeAttack</c> take the damage amount as a second argument.
///     ServUO keeps its paralysis in a <c>static Hashtable</c> of <c>Timer</c>s; that became a per-jailor
///     table of <see cref="TimerExecutionToken" />s so the tokens can be cancelled in
///     <see cref="OnAfterDelete" />, and the five-second terror is tracked the same way (ServUO leaves an
///     unfreeze timer armed per movement tick, because its cooldown is only stamped when that timer
///     fires). <c>SendSpeedControl(SpeedControlType.*)</c> is
///     <c>SendSpeedControl(SpeedControlSetting.*)</c> on the net state here.
/// </summary>
[SerializationGenerator(0, false)]
public partial class DemonicJailor : BaseCreature
{
    private static readonly TimeSpan ParalyzeDuration = TimeSpan.FromSeconds(15.0);
    private static readonly TimeSpan TerrorDuration = TimeSpan.FromSeconds(5.0);
    private static readonly TimeSpan TerrorInterval = TimeSpan.FromMinutes(1.0);

    private readonly Dictionary<Mobile, TimerExecutionToken> _paralyzed = new();
    private readonly Dictionary<Mobile, TimerExecutionToken> _terrified = new();

    private DateTime _nextTerror;

    [Constructible]
    public DemonicJailor() : base(AIType.AI_Melee, FightMode.Closest, 15, 1)
    {
        Name = RussianNames.RandomMale();
        SpeechHue = Utility.RandomDyedHue();
        Title = "демонический тюремщик";
        Hue = 34531;
        Body = 0x190;

        SetStr(386, 400);
        SetDex(151, 165);
        SetInt(161, 175);

        SetDamage(8, 10);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 35, 45);
        SetResistance(ResistanceType.Fire, 25, 30);
        SetResistance(ResistanceType.Cold, 25, 30);
        SetResistance(ResistanceType.Poison, 10, 20);
        SetResistance(ResistanceType.Energy, 10, 20);

        SetSkill(SkillName.Anatomy, 125.0);
        SetSkill(SkillName.Fencing, 46.0, 77.5);
        SetSkill(SkillName.Macing, 35.0, 57.5);
        SetSkill(SkillName.Poisoning, 60.0, 82.5);
        SetSkill(SkillName.DetectHidden, 100);
        SetSkill(SkillName.MagicResist, 83.5, 92.5);
        SetSkill(SkillName.Swords, 125.0);
        SetSkill(SkillName.Tactics, 125.0);
        SetSkill(SkillName.Lumberjacking, 125.0);

        Fame = 5000;
        Karma = -5000;

        VirtualArmor = 40;

        AddItem(new ShortPants(Utility.RandomRedHue()));
        AddItem(new Sandals(Utility.RandomRedHue()));
        AddItem(new Shirt(Utility.RandomRedHue()));
        AddItem(new SkinningKnife());

        Utility.AssignRandomHair(this);
    }

    public override bool AlwaysMurderer => true;
    public override bool BardImmune => !Core.SE;
    public override bool Unprovokable => Core.SE;
    public override bool AreaPeaceImmune => Core.SE;
    public override Poison PoisonImmune => Poison.Lethal;

    public override void OnGaveMeleeAttack(Mobile defender, int damage)
    {
        base.OnGaveMeleeAttack(defender, damage);

        var loc = new Point3D(5703, 639, 0);
        var map = Map;

        Effects.SendLocationParticles(
            EffectItem.Create(loc, map, EffectItem.DefaultDuration),
            0x3728,
            10,
            10,
            0,
            0,
            2023,
            0
        );
        Effects.PlaySound(loc, map, 0x1FE);

        BaseCreature.TeleportPets(defender, loc, map);

        defender.MoveToWorld(loc, map);

        // You are captured by the jailor and returned to your cell.
        defender.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1152076);
    }

    public override void OnDamagedBySpell(Mobile from, int damage)
    {
        base.OnDamagedBySpell(from, damage);

        ParalyzeAttack(from);
    }

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        if (m == null)
        {
            return;
        }

        if (UseSkill(SkillName.DetectHidden))
        {
            m.RevealingAction();
        }

        base.OnMovement(m, oldLocation);

        if (_nextTerror >= Core.Now || m.AccessLevel != AccessLevel.Player || !InRange(m.Location, 3) ||
            _terrified.ContainsKey(m))
        {
            return;
        }

        m.Frozen = true;

        // Terror slices into your very being, destroying any chance of resisting ~1_name~ you might have had
        m.SendLocalizedMessage(1080342, Title, 33);

        _nextTerror = Core.Now + TerrorInterval;

        Timer.StartTimer(TerrorDuration, () => Terrorize(m), out var token);

        _terrified[m] = token;
    }

    public bool UnderParalyzeAttack(Mobile from) => from != null && _paralyzed.ContainsKey(from);

    public void ParalyzeAttack(Mobile to)
    {
        if (to?.Alive != true || !to.Player || _paralyzed.ContainsKey(to))
        {
            return;
        }

        to.NetState.SendSpeedControl(SpeedControlSetting.Walk);
        to.FixedEffect(0x376A, 6, 1);
        to.SendLocalizedMessage(500111); // You are frozen and cannot move.
        to.PlaySound(0x204);

        Timer.StartTimer(ParalyzeDuration, () => ParalyzeEnd(to), out var token);

        _paralyzed[to] = token;
    }

    public void ParalyzeEnd(Mobile from)
    {
        if (from == null)
        {
            return;
        }

        if (_paralyzed.Remove(from, out var token))
        {
            token.Cancel();
        }

        from.NetState.SendSpeedControl(SpeedControlSetting.Disable);
    }

    private void Terrorize(Mobile m)
    {
        _terrified.Remove(m);

        if (m.Deleted)
        {
            return;
        }

        m.Frozen = false;
        m.SendLocalizedMessage(1005603); // You can move again!
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        foreach (var (victim, token) in _paralyzed)
        {
            token.Cancel();
            victim.NetState.SendSpeedControl(SpeedControlSetting.Disable);
        }

        foreach (var (victim, token) in _terrified)
        {
            token.Cancel();
            victim.Frozen = false;
        }

        _paralyzed.Clear();
        _terrified.Clear();
    }

    public override void GenerateLoot()
    {
        AddLoot(LootPack.FilthyRich);
        AddLoot(LootPack.Meager);
    }
}

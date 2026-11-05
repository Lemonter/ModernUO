using System;
using ModernUO.Serialization;
using Server.Engines.ShameRevamped;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO's Shame Revamped dungeon (Mobiles/Bosses.cs) — the summonable level guardians
///     and their altar bookkeeping.
///
///     Adapted: ServUO's damage gate overrides <c>Damage</c> and returns an amount; here the method is
///     void, so it returns early instead. The flame elemental's dragon breath comes through
///     <see cref="GetMonsterAbilities" /> as <see cref="MonsterAbilities.FireBreath" />, and its
///     <c>IAuraCreature</c> / <c>SetAreaEffect(AreaEffect.AuraDamage)</c> pair is BaseCreature's
///     built-in aura (HasAura / Aura*Damage / AuraEffect), the same conversion FireDaemon uses; its
///     stats and ash drop remain.
/// </summary>
[SerializationGenerator(0, false)]
public partial class ShameGuardian : BaseCreature
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private ShameAltar _altar;

    protected ShameGuardian(AIType type) : base(type, FightMode.Aggressor, 10, 1)
    {
        Title = "страж";
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        _altar?.OnGuardianKilled();

        c.DropItem(new ShameCrystal(Utility.RandomMinMax(3, 5)));

        _altar = null;
    }

    public override void Damage(int amount, Mobile from = null, bool informMount = true, bool ignoreEvilOmen = false)
    {
        if (from == null)
        {
            return;
        }

        if (_altar?.Summoner == null)
        {
            base.Damage(amount, from, informMount, ignoreEvilOmen);
            return;
        }

        var good = from == _altar.Summoner;

        if (!good && _altar.DeadLine > Core.Now && _altar.DeadLine - Core.Now < TimeSpan.FromMinutes(10))
        {
            good = true;
        }

        if (!good && from is BaseCreature creature && creature.GetMaster() == _altar.Summoner)
        {
            good = true;
        }

        if (!good && ShameAltar.AllowParties)
        {
            var party = Server.Engines.PartySystem.Party.Get(from);

            if (party != null)
            {
                foreach (var member in party.Members)
                {
                    if (member.Mobile == from)
                    {
                        good = true;
                        break;
                    }
                }
            }
        }

        if (good)
        {
            base.Damage(amount, from, informMount, ignoreEvilOmen);
            return;
        }

        from.SendLocalizedMessage(1151633); // You did not summon this champion, so you may not attack it at this time.
    }

    public override bool AutoDispel => true;
    public override bool AlwaysMurderer => true;
    public override Poison PoisonImmune => Poison.Lethal;
}

[SerializationGenerator(0, false)]
public partial class QuartzElemental : ShameGuardian
{
    public override string CorpseName => "труп кварцевого элементаля";

    public override int TreasureMapLevel => 1;

    [Constructible]
    public QuartzElemental() : base(AIType.AI_Melee)
    {
        Name = "кварцевый элементаль";
        Body = 14;
        BaseSoundID = 268;
        Hue = 2575;

        SetStr(240, 260);
        SetDex(70, 80);
        SetInt(100, 110);

        SetHits(1000, 1100);

        SetDamage(14, 21);

        SetDamageType(ResistanceType.Physical, 60);
        SetDamageType(ResistanceType.Energy, 40);

        SetResistance(ResistanceType.Physical, 30, 35);
        SetResistance(ResistanceType.Fire, 20, 30);
        SetResistance(ResistanceType.Cold, 20, 30);
        SetResistance(ResistanceType.Poison, 20, 30);
        SetResistance(ResistanceType.Energy, 15, 25);

        SetSkill(SkillName.MagicResist, 110, 120);
        SetSkill(SkillName.Tactics, 100, 110.0);
        SetSkill(SkillName.Wrestling, 110, 120);

        Fame = 4500;
        Karma = -4500;
    }

    public override void GenerateLoot()
    {
        AddLoot(LootPack.UltraRich, 1);
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        if (c.Map?.Rules == MapRules.FeluccaRules || 0.5 > Utility.RandomDouble())
        {
            c.DropItem(new QuartzGrit());
        }
    }
}

[SerializationGenerator(0, false)]
public partial class FlameElemental : ShameGuardian
{
    public override string CorpseName => "труп элементаля пламени";

    private static readonly MonsterAbility[] _abilities = [MonsterAbilities.FireBreath];

    public override MonsterAbility[] GetMonsterAbilities() => _abilities;

    public override bool HasAura => true;
    public override int AuraBaseDamage => 5;
    public override int AuraRange => 4;

    public override void AuraEffect(Mobile m)
    {
        m.SendLocalizedMessage(1008112); // The intense heat is damaging you!
    }

    [Constructible]
    public FlameElemental() : base(AIType.AI_Mage)
    {
        Name = "элементаль пламени";
        Body = 15;
        BaseSoundID = 838;
        Hue = 1161;

        SetStr(420, 460);
        SetDex(160, 210);
        SetInt(120, 190);

        SetHits(700, 800);
        SetMana(1000, 1300);

        SetDamage(13, 15);

        SetDamageType(ResistanceType.Physical, 25);
        SetDamageType(ResistanceType.Fire, 75);

        SetResistance(ResistanceType.Physical, 40, 60);
        SetResistance(ResistanceType.Fire, 100);
        SetResistance(ResistanceType.Cold, 30, 40);
        SetResistance(ResistanceType.Poison, 60, 70);
        SetResistance(ResistanceType.Energy, 60, 70);

        SetSkill(SkillName.MagicResist, 90, 140);
        SetSkill(SkillName.Tactics, 90, 130.0);
        SetSkill(SkillName.Wrestling, 90, 120);
        SetSkill(SkillName.Magery, 100, 145);
        SetSkill(SkillName.EvalInt, 90, 140);
        SetSkill(SkillName.Meditation, 80, 120);
        SetSkill(SkillName.Parry, 100, 120);

        Fame = 4500;
        Karma = -4500;

        PackItem(new SulfurousAsh(5));
    }

    public override void GenerateLoot()
    {
        AddLoot(LootPack.UltraRich, 1);
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        if (c.Map?.Rules == MapRules.FeluccaRules || 0.5 > Utility.RandomDouble())
        {
            c.DropItem(new CorrosiveAsh());
        }
    }
}

[SerializationGenerator(0, false)]
public partial class WindElemental : ShameGuardian
{
    public override string CorpseName => "труп воздушного элементаля";

    [Constructible]
    public WindElemental() : base(AIType.AI_Mage)
    {
        Name = "воздушный элементаль";
        Body = 13;
        BaseSoundID = 655;
        Hue = 33765;

        SetStr(370, 460);
        SetDex(160, 250);
        SetInt(150, 220);

        SetHits(2500, 2600);
        SetMana(1000, 1300);

        SetDamage(15, 17);

        SetDamageType(ResistanceType.Physical, 20);
        SetDamageType(ResistanceType.Cold, 40);
        SetDamageType(ResistanceType.Energy, 40);

        SetResistance(ResistanceType.Physical, 65, 75);
        SetResistance(ResistanceType.Fire, 55, 65);
        SetResistance(ResistanceType.Cold, 55, 65);
        SetResistance(ResistanceType.Poison, 100);
        SetResistance(ResistanceType.Energy, 60, 75);

        SetSkill(SkillName.MagicResist, 60, 80);
        SetSkill(SkillName.Tactics, 60, 80.0);
        SetSkill(SkillName.Wrestling, 60, 80);
        SetSkill(SkillName.Magery, 60, 80);
        SetSkill(SkillName.EvalInt, 60, 80);

        Fame = 4500;
        Karma = -4500;
    }

    public override void GenerateLoot()
    {
        AddLoot(LootPack.UltraRich, 1);
        AddLoot(LootPack.HighScrolls, Utility.RandomMinMax(3, 5));
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        if (c.Map?.Rules == MapRules.FeluccaRules || 0.5 > Utility.RandomDouble())
        {
            c.DropItem(new CursedOilstone());
        }
    }
}

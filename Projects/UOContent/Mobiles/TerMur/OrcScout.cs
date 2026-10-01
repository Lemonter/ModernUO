using ModernUO.Serialization;
using Server.Items;
using Server.Misc;
using Server.Targeting;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/OrcScout.cs). AIType.AI_OrcScout
/// doesn't exist here (verified) — substituted AI_Archer, matching its bow/stealth kit.
/// CanStealth doesn't exist as an overridable member anywhere in this codebase (see
/// TrapdoorSpider.cs) — dropped. TribeType dropped (see GrayGoblin.cs). HealChance dropped — no such virtual on
/// BaseCreature. OrcishBow doesn't exist locally — always uses plain Bow (which does exist).
/// LootPack.LootItem&lt;T&gt; doesn't exist — Yeast doesn't exist anywhere in this codebase
/// either so that drop line is dropped outright; Apple/Arrow/Bandage converted to
/// chance-based OnDeath drops (see WolfSpider.cs). GetMobilesInRange/IPooledEnumerable.Free
/// converted to the modern Map.GetMobilesInRange foreach pattern (no Free() needed).</summary>
[SerializationGenerator(0, false)]
[CorpseName("an orcish corpse")]
public partial class OrcScout : BaseCreature
{
    [Constructible]
    public OrcScout() : base(AIType.AI_Archer, FightMode.Closest, 10, 7)
    {
        Body = 0xB5;
        BaseSoundID = 0x45A;

        SetStr(96, 120);
        SetDex(101, 130);
        SetInt(36, 60);

        SetHits(58, 72);
        SetMana(30, 60);

        SetDamage(5, 7);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 25, 35);
        SetResistance(ResistanceType.Fire, 30, 40);
        SetResistance(ResistanceType.Cold, 15, 25);
        SetResistance(ResistanceType.Poison, 15, 20);
        SetResistance(ResistanceType.Energy, 25, 30);

        SetSkill(SkillName.MagicResist, 50.1, 75.0);
        SetSkill(SkillName.Tactics, 55.1, 80.0);

        SetSkill(SkillName.Fencing, 50.1, 70.0);
        SetSkill(SkillName.Archery, 80.1, 120.0);
        SetSkill(SkillName.Parry, 40.1, 60.0);
        SetSkill(SkillName.Healing, 80.1, 100.0);
        SetSkill(SkillName.Anatomy, 50.1, 90.0);
        SetSkill(SkillName.DetectHidden, 100.1, 120.0);
        SetSkill(SkillName.Hiding, 100.0, 120.0);
        SetSkill(SkillName.Stealth, 80.1, 120.0);

        Fame = 1500;
        Karma = -1500;

        AddItem(new Bow());
    }

    public override string DefaultName => "орк-разведчик";

    public override bool CanRummageCorpses => true;
    public override int Meat => 1;

    public override InhumanSpeech SpeechType => InhumanSpeech.Orc;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Rich);
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        if (Utility.RandomDouble() < 0.5)
        {
            c.DropItem(new Apple(Utility.RandomMinMax(3, 5)));
        }

        c.DropItem(new Arrow(Utility.RandomMinMax(60, 70)));

        if (Utility.RandomDouble() < 0.5)
        {
            c.DropItem(new Bandage(Utility.RandomMinMax(1, 15)));
        }
    }

    public override bool IsEnemy(Mobile m)
    {
        if (m.Player && m.FindItemOnLayer(Layer.Helm) is OrcishKinMask)
        {
            return false;
        }

        return base.IsEnemy(m);
    }

    public override void AggressiveAction(Mobile aggressor, bool criminal)
    {
        base.AggressiveAction(aggressor, criminal);

        if (aggressor.FindItemOnLayer(Layer.Helm) is OrcishKinMask item)
        {
            AOS.Damage(aggressor, 50, 0, 100, 0, 0, 0);
            item.Delete();
            aggressor.FixedParticles(0x36BD, 20, 10, 5044, EffectLayer.Head);
            aggressor.PlaySound(0x307);
        }
    }

    public override void OnThink()
    {
        base.OnThink();

        if (Utility.RandomDouble() < 0.2)
        {
            TryToDetectHidden();
        }
    }

    private Mobile FindTarget()
    {
        foreach (var m in Map.GetMobilesInRange(Location, 10))
        {
            if (m.Player && m.Hidden)
            {
                return m;
            }
        }

        return null;
    }

    private void TryToDetectHidden()
    {
        var m = FindTarget();

        if (m != null && Core.TickCount >= NextSkillTime && UseSkill(SkillName.DetectHidden))
        {
            Target targ = Target;

            targ?.Invoke(this, this);

            Effects.PlaySound(Location, Map, 0x340);
        }
    }
}

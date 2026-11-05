using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Engines.ExploringTheDeep;
using Server.Items;

namespace Server.Mobiles;

public enum ShadowlordType
{
    Astaroth,
    Faulinei,
    Nosfentor
}

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Mobiles/Shadowlord.cs). The thing behind the
///     winch hatch at Gravewater Lake: it reflects spells while dark wisps are near, drains life, and drops
///     one of the High Seas shadowlord artifacts to a lucky looter.
///
///     API substitutions:
///     <list type="bullet">
///         <item>
///             ServUO's <c>OnDrainLife</c> hook does not exist here — drain life is a monster ability. The
///             original's wisp summoning and victim scattering are therefore re-attached by deriving
///             <see cref="ShadowlordDrainLife" /> from <see cref="DrainLifeAttack" /> and registering it
///             through <see cref="GetMonsterAbilities" />, which the ability framework calls at the same
///             point.
///         </item>
///         <item>
///             <c>GetProperties</c> wrote the "#{cliloc}" indirection by hand; the property list takes the
///             cliloc directly, which renders identically.
///         </item>
///         <item>
///             The instance list was an <c>ArrayList</c>; it is a typed list, and only construction and
///             deletion touch it.
///         </item>
///     </list>
/// </summary>
[SerializationGenerator(0, false)]
public partial class Shadowlord : BaseCreature
{
    private static readonly List<Shadowlord> _instances = new();

    private static readonly Type[] _artifactTypes =
    {
        typeof(Abhorrence), typeof(CaptainJohnesBlade), typeof(Craven),
        typeof(Equivocation), typeof(GargishCaptainJohnesBlade), typeof(GargishEquivocation),
        typeof(GargishPincer), typeof(Pincer)
    };

    [SerializableField(0, setter: "private")]
    [InvalidateProperties]
    private ShadowlordType _type;

    private TimerExecutionToken _selfDelete;

    [Constructible]
    public Shadowlord() : base(AIType.AI_NecroMage, FightMode.Closest, 10, 1)
    {
        _instances.Add(this);

        _type = (ShadowlordType)Utility.Random(3);
        Name = _type switch
        {
            ShadowlordType.Astaroth => "Астарот",
            ShadowlordType.Faulinei => "Фаулиней",
            _                       => "Носфентор"
        };

        Body = 146;
        BaseSoundID = 0x4B0;

        SetStr(981, 1078);
        SetDex(1003, 1114);
        SetInt(1098, 1245);

        SetHits(50000, 55000);
        SetStam(1003, 1114);

        SetDamage(35, 41);

        SetDamageType(ResistanceType.Physical, 20);
        SetDamageType(ResistanceType.Fire, 20);
        SetDamageType(ResistanceType.Cold, 20);
        SetDamageType(ResistanceType.Poison, 20);
        SetDamageType(ResistanceType.Energy, 20);

        SetResistance(ResistanceType.Physical, 70, 80);
        SetResistance(ResistanceType.Fire, 70, 80);
        SetResistance(ResistanceType.Cold, 70, 80);
        SetResistance(ResistanceType.Poison, 70, 80);
        SetResistance(ResistanceType.Energy, 70, 80);

        SetSkill(SkillName.EvalInt, 140.0);
        SetSkill(SkillName.Magery, 120.0);
        SetSkill(SkillName.Meditation, 140.0);
        SetSkill(SkillName.MagicResist, 110.2, 120.0);
        SetSkill(SkillName.Tactics, 110.1, 115.0);
        SetSkill(SkillName.Wrestling, 110.1, 115.0);
        SetSkill(SkillName.Necromancy, 120.0);
        SetSkill(SkillName.SpiritSpeak, 120.0);
        SetSkill(SkillName.Anatomy, 10.0, 20.0);

        Fame = 24000;
        Karma = -24000;

        VirtualArmor = 20;
        Hue = 902;

        Timer.StartTimer(TimeSpan.FromMinutes(180.0), Delete, out _selfDelete);
    }

    public static IReadOnlyList<Shadowlord> Instances => _instances;

    public override string CorpseName => "труп теневого лорда";

    public virtual Type[] ArtifactDrops => _artifactTypes;

    public override MonsterAbility[] GetMonsterAbilities() => [new ShadowlordDrainLife()];

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1154453 + (int)_type); // Shadowlord of ..
    }

    public override bool AlwaysMurderer => true;

    public override Poison PoisonImmune => Poison.Lethal;

    public static Shadowlord Spawn(Point3D platLoc, Map platMap)
    {
        if (_instances.Count > 0)
        {
            return null;
        }

        var creature = new Shadowlord
        {
            Home = platLoc,
            RangeHome = 4
        };

        creature.MoveToWorld(platLoc, platMap);

        return creature;
    }

    public override int GetAngerSound() => 1550;
    public override int GetHurtSound() => 1552;
    public override int GetDeathSound() => 1551;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.SuperBoss, 4);
        AddLoot(LootPack.FilthyRich);
    }

    public override void CheckReflect(Mobile caster, ref bool reflect)
    {
        // Reflect spells while the Shadowlord has wisps around it.
        if (Map == null)
        {
            return;
        }

        foreach (var m in Map.GetMobilesInRange(Location, 20))
        {
            if (m is DarkWisp)
            {
                reflect = true;
                return;
            }
        }
    }

    public override void OnDeath(Container c)
    {
        var rights = GetLootingRights(DamageEntries, HitsMax);

        for (var i = 0; i < rights.Count; i++)
        {
            var entry = rights[i];

            if (!entry.m_HasRight || entry.m_Mobile == null)
            {
                continue;
            }

            var luck = entry.m_Mobile.Luck;
            var chance = 75 + luck / 15;

            if (chance <= Utility.Random(5000))
            {
                continue;
            }

            var artifact = Loot.Construct(ArtifactDrops[Utility.Random(ArtifactDrops.Length)]);

            if (artifact == null)
            {
                continue;
            }

            if (entry.m_Mobile.Backpack == null ||
                !entry.m_Mobile.Backpack.TryDropItem(entry.m_Mobile, artifact, false))
            {
                entry.m_Mobile.BankBox?.DropItem(artifact);
                entry.m_Mobile.SendMessage("За доблесть в бою с павшим чудовищем особая награда помещена в ваш банковский ящик.");
            }
            else
            {
                // For your valor in combating the fallen beast, a special reward has been bestowed on you.
                entry.m_Mobile.SendLocalizedMessage(1062317);
            }
        }

        base.OnDeath(c);
    }

    public override void OnAfterDelete()
    {
        _selfDelete.Cancel();
        _instances.Remove(this);

        base.OnAfterDelete();
    }

    /// <summary>
    ///     ServUO's <c>OnDrainLife</c> override: every time the shadowlord drains someone, a dark wisp joins
    ///     the fight and badly hurt victims may be thrown across the hold.
    /// </summary>
    private class ShadowlordDrainLife : DrainLifeAttack
    {
        public override void Trigger(MonsterAbilityTrigger trigger, BaseCreature source, Mobile target)
        {
            base.Trigger(trigger, source, target);

            if (source is Shadowlord { Deleted: false, Map: not null } lord && target?.Alive == true)
            {
                SummonWisps(lord, target);
            }
        }

        private static void SummonWisps(Shadowlord lord, Mobile victim)
        {
            new DarkWisp().MoveToWorld(lord.Location, lord.Map);

            var teleportChance = (double)lord.Hits / lord.HitsMax;

            if (teleportChance >= Utility.RandomDouble() || !victim.Alive)
            {
                return;
            }

            var destination = Utility.Random(6) switch
            {
                0 => new Point3D(6431, 1664, 0),
                1 => new Point3D(6432, 1634, 0),
                2 => new Point3D(6401, 1657, 0),
                3 => new Point3D(6401, 1637, 0),
                _ => lord.Location
            };

            victim.MoveToWorld(destination, lord.Map);
        }
    }
}

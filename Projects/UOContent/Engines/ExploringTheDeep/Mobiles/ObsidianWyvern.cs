using ModernUO.Serialization;
using System;
using System.Collections.Generic;
using Server.Engines.ExploringTheDeep;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Mobiles/ObsidianWyvern.cs). The Destard wyvern
///     carrying Willem Harte's hat; spawned once per region entry and removed after an hour.
///
///     ServUO registered <c>SpecialAbility.DragonBreath</c>; that becomes
///     <see cref="MonsterAbilities.FireBreath" />, the ability framework's equivalent.
/// </summary>
[SerializationGenerator(0, false)]
public partial class ObsidianWyvern : BaseCreature
{
    private static readonly List<ObsidianWyvern> _instances = new();

    private TimerExecutionToken _selfDelete;

    [Constructible]
    public ObsidianWyvern() : base(AIType.AI_Mage, FightMode.Closest, 10, 1)
    {
        Body = 0x2E;
        Hue = 1910;
        BaseSoundID = 362;

        SetStr(1377, 1450);
        SetDex(125, 180);
        SetInt(780, 900);

        SetHits(1225, 1400);

        SetDamage(29, 35);

        SetDamageType(ResistanceType.Physical, 75);
        SetDamageType(ResistanceType.Fire, 25);

        SetResistance(ResistanceType.Physical, 67);
        SetResistance(ResistanceType.Fire, 80, 90);
        SetResistance(ResistanceType.Cold, 70, 80);
        SetResistance(ResistanceType.Poison, 60, 70);
        SetResistance(ResistanceType.Energy, 60, 70);

        SetSkill(SkillName.Magery, 108.7, 115.0);
        SetSkill(SkillName.Meditation, 60.0, 87.6);
        SetSkill(SkillName.EvalInt, 110.0, 115.0);
        SetSkill(SkillName.Wrestling, 110.0, 115.0);
        SetSkill(SkillName.Tactics, 119.6, 125.0);
        SetSkill(SkillName.MagicResist, 115.0, 130.8);
        SetSkill(SkillName.Parry, 75.0, 85.0);
        SetSkill(SkillName.DetectHidden, 100.0);

        Fame = 24000;
        Karma = -24000;

        VirtualArmor = 70;

        _instances.Add(this);

        Timer.StartTimer(TimeSpan.FromMinutes(60.0), Delete, out _selfDelete);
    }

    public static IReadOnlyList<ObsidianWyvern> Instances => _instances;

    public override string CorpseName => "труп обсидиановой виверны";
    public override string DefaultName => "обсидиановая виверна";

    public override MonsterAbility[] GetMonsterAbilities() => [MonsterAbilities.FireBreath];

    public override bool ReacquireOnMovement => true;
    public override Poison PoisonImmune => Poison.Deadly;
    public override Poison HitPoison => Poison.Deadly;
    public override bool AutoDispel => true;
    public override bool BardImmune => true;
    public override FoodType FavoriteFood => FoodType.Meat;

    public static ObsidianWyvern Spawn(Point3D platLoc, Map platMap)
    {
        if (_instances.Count > 0)
        {
            return null;
        }

        var creature = new ObsidianWyvern
        {
            Home = platLoc,
            RangeHome = 4
        };

        creature.MoveToWorld(platLoc, platMap);

        return creature;
    }

    public override void OnDeath(Container c)
    {
        ExploringDeepDrops.Give(this, ExploringDeepChain.ComponentQuest, static () => new WillemHartesHat());

        base.OnDeath(c);
    }

    public override void GenerateLoot()
    {
        AddLoot(LootPack.FilthyRich, 3);
        AddLoot(LootPack.Gems, 5);
    }

    public override int GetIdleSound() => 0x2D3;
    public override int GetHurtSound() => 0x2D1;

    public override void OnAfterDelete()
    {
        _selfDelete.Cancel();
        _instances.Remove(this);

        base.OnAfterDelete();
    }
}

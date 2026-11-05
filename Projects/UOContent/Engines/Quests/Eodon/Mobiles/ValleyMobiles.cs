using System;
using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Quests.Eodon;

/// <summary>
///     Creatures of the "Valley of One" Eodon line, ported from ServUO
///     (Scripts/Quests/Eodon/Valley of One Quest/Creatures.cs). <c>TRex</c>, <c>GreatApe</c> and
///     <c>VolcanoElemental</c> already exist in this codebase and are reused as-is.
///
///     Dropped: the quest-progress pushes (<c>PrideOfTheAmbushQuest.Update</c>, <c>EmptyNestQuest.Update</c>) —
///     the MLQuest engine cannot be driven from a creature, so those two quests count kills instead (see
///     EodonValleyOfOne.cs). The cub still walks home and burrows away.
/// </summary>
[SerializationGenerator(0, false)]
public partial class TigerCub : BaseCreature
{
    [SerializableField(0)]
    private Mobile _protector;

    [Constructible]
    public TigerCub() : base(AIType.AI_Animal, FightMode.Aggressor, 10, 1)
    {
        Body = 1309;
        BaseSoundID = 0x69;

        SetStr(100);
        SetDex(100);
        SetInt(20);

        SetHits(50, 60);
        SetMana(0);

        SetDamage(10, 12);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 5, 10);

        SetSkill(SkillName.MagicResist, 5.0);
        SetSkill(SkillName.Tactics, 40.0);
        SetSkill(SkillName.Wrestling, 50.0);

        Fame = 0;
        Karma = 500;

        ControlSlots = 1;
        MinTameSkill = 0;
    }

    public override string DefaultName => "тигрёнок";
    public override string CorpseName => "труп тигрёнка";

    public override int Meat => 1;
    public override FoodType FavoriteFood => FoodType.Meat | FoodType.Fish;
    public override PackInstinct PackInstinct => PackInstinct.Feline;

    public override void OnThink()
    {
        base.OnThink();

        if (_protector is not PlayerMobile protector || !InRange(Home, 2))
        {
            return;
        }

        protector.PrivateOverheadMessage(
            MessageType.Regular,
            0x35,
            1156501,
            protector.NetState
        ); // *You watch as the Tiger Cub safely returns to the Kurak Tribe*

        _protector = null;

        Timer.StartTimer(TimeSpan.FromSeconds(0.25), Delete);
    }

    public override void Damage(int amount, Mobile from = null, bool informMount = true, bool ignoreEvilOmen = false)
    {
        if (from != null && from == _protector)
        {
            // *The cub looks at you playfully. Your attack fails as you are overwhelmed by its cuteness*
            PrivateOverheadMessage(MessageType.Regular, 0x35, 1156500, from.NetState);
            return;
        }

        base.Damage(amount, from, informMount, ignoreEvilOmen);
    }
}

[SerializationGenerator(0, false)]
public partial class Trapper : Brigand
{
    private TimerExecutionToken _despawnToken;

    [Constructible]
    public Trapper()
    {
        Title = "зверолов";

        SetHits(2500);

        SetStr(125, 150);
        SetDex(200);
        SetInt(61, 75);

        SetDamage(20, 32);

        SetSkill(SkillName.Fencing, 100);
        SetSkill(SkillName.Macing, 100);
        SetSkill(SkillName.MagicResist, 120);
        SetSkill(SkillName.Swords, 100);
        SetSkill(SkillName.Tactics, 100);
        SetSkill(SkillName.Wrestling, 100);

        Fame = 12000;
        Karma = -12000;

        Timer.StartTimer(TimeSpan.FromMinutes(10), Delete, out _despawnToken);
    }

    public override void GenerateLoot() => AddLoot(LootPack.Rich, 3);

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        _despawnToken.Cancel();
    }
}

[SerializationGenerator(0, false)]
public partial class Poacher : Trapper
{
    [SerializableField(0)]
    private DragonTurtleNest _nest;

    [Constructible]
    public Poacher() => Title = "браконьер";

    public Poacher(DragonTurtleNest nest) : this() => _nest = nest;

    public override void GenerateLoot() => AddLoot(LootPack.Rich, 3);

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        if (_nest?.Deleted == false)
        {
            _nest.OnPoacherKilled(this);
        }
    }
}

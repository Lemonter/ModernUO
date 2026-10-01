using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

// Combat/web mechanic ported from JustUO (github.com/JustUO/JustUO) — see
// Regions/UnderworldRegion.cs header for why ServUO itself wasn't the first source checked.
// The Spawner/UsedPillars fields and OnDeath -> Spawner.OnNavreyKilled() hook were added
// from ServUO's *real* Navrey.cs (Scripts/Mobiles/Bosses/Navery/Navrey.cs — a different,
// simpler boss than JustUO's, found later) to integrate with the real NavreysController/
// NavreysPillar "three ruins" puzzle mechanic (see Items/Underworld/NavreysController.cs).
// Simplifications: the ~2.5% chance to drop one of ServUO's TOL artifacts (NightEyes,
// Tangle1) is dropped — neither exists in this codebase, and "Tangle" already names an
// unrelated ML creature here (ServUO renamed its own to "Tangle1" for the same reason).
// EyeOfNavrey is a plain rare trophy drop rather than a quest gate. Vernix and his "Green with
// Envy" quest have since been ported (Engines/ML Quests/Definitions/Underworld.cs) and the
// quest collects exactly this item, so the trophy drop now feeds it — no change needed here.
// UntranslatedAncientTome/TatteredAncientScroll loot dropped too — neither type exists here.
// The LuckyCoin drop is back, now that the coin and the Fountain of Fortune it feeds are both
// in place.
[SerializationGenerator(0, false)]
public partial class Navrey : BaseCreature
{
    [SerializableField(0)]
    private NavreysController _spawner;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _usedPillars;

    [Constructible]
    public Navrey() : this(null)
    {
    }

    [Constructible]
    public Navrey(NavreysController spawner) : base(AIType.AI_Mage, FightMode.Closest)
    {
        _spawner = spawner;

        Name = "Навреа Ночной Взор";
        Body = 735;
        BaseSoundID = 389;

        SetStr(1000, 1500);
        SetDex(200, 250);
        SetInt(150, 200);

        SetHits(30000, 35000);

        SetDamage(25, 40);

        SetDamageType(ResistanceType.Physical, 50);
        SetDamageType(ResistanceType.Fire, 25);
        SetDamageType(ResistanceType.Energy, 25);

        SetResistance(ResistanceType.Physical, 55, 65);
        SetResistance(ResistanceType.Fire, 45, 55);
        SetResistance(ResistanceType.Cold, 50, 70);
        SetResistance(ResistanceType.Poison, 100);
        SetResistance(ResistanceType.Energy, 60, 80);

        SetSkill(SkillName.Anatomy, 50.0, 80.0);
        SetSkill(SkillName.EvalInt, 90.0, 100.0);
        SetSkill(SkillName.Magery, 90.0, 100.0);
        SetSkill(SkillName.MagicResist, 100.0, 130.0);
        SetSkill(SkillName.Meditation, 80.0, 100.0);
        SetSkill(SkillName.Poisoning, 100.0);
        SetSkill(SkillName.Tactics, 90.0, 100.0);
        SetSkill(SkillName.Wrestling, 91.6, 98.2);

        Fame = 30000;
        Karma = -30000;

        VirtualArmor = 90;
    }

    public override string CorpseName => "труп гигантского паука";

    public override bool AlwaysMurderer => true;
    public override Poison PoisonImmune => Poison.Lethal;
    public override Poison HitPoison => Poison.Lethal;
    public override int Meat => 1;

    public override void GenerateLoot() => AddLoot(LootPack.FilthyRich, 2);

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        Spawner?.OnNavreyKilled();

        if (Utility.RandomBool())
        {
            c.DropItem(ScrollofTranscendence.CreateRandom(30, 30));
        }

        if (Utility.RandomDouble() < 0.1)
        {
            c.DropItem(new EyeOfNavrey());
        }

        if (Utility.RandomDouble() < 0.1)
        {
            c.DropItem(new LuckyCoin());
        }
    }

    public override void OnThink()
    {
        base.OnThink();

        if (Utility.RandomDouble() < 0.03)
        {
            DoSpecialAbility();
        }
    }

    // Ignores players paralyzed in webs.
    public override bool CanSee(object o)
    {
        if (o is Mobile { Paralyzed: true } && Utility.RandomDouble() > 0.25)
        {
            return false;
        }

        return base.CanSee(o);
    }

    public void DoSpecialAbility()
    {
        if (Map == null)
        {
            return;
        }

        var candidates = new List<Mobile>();
        foreach (var mob in Map.GetMobilesInRange(Location, RangePerception))
        {
            if (!mob.Deleted && !mob.Paralyzed && mob.AccessLevel == AccessLevel.Player)
            {
                candidates.Add(mob);
            }
        }

        if (candidates.Count == 0)
        {
            return;
        }

        var target = candidates[Utility.Random(candidates.Count)];
        Direction = GetDirectionTo(target);

        var web = new NavreyParalyzingWeb();

        if (Utility.RandomDouble() > 0.1)
        {
            target.Paralyze(TimeSpan.FromSeconds(60));
        }

        web.MoveToWorld(target.Location, Map);
    }
}

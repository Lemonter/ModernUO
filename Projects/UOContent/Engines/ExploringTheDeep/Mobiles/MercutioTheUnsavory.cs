using ModernUO.Serialization;
using System;
using System.Collections.Generic;
using Server.Engines.ExploringTheDeep;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Mobiles/MercutioTheUnsavory.cs). The card
///     sharp south-west of Trinsic whose cutlass Champ Huthwait wants. He summons brigands while fighting,
///     drops <see cref="Items.MercutiosCutlass" /> for players on the suit-parts step, and removes himself
///     after an hour.
///
///     Substitution: ServUO's <c>AddImmovableItem</c> helper is just "add it with blessed loot type", which
///     is spelled out inline here.
/// </summary>
[SerializationGenerator(0, false)]
public partial class MercutioTheUnsavory : BaseCreature
{
    private static readonly List<MercutioTheUnsavory> _instances = new();

    private TimerExecutionToken _selfDelete;

    [Constructible]
    public MercutioTheUnsavory() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Body = 0x190;
        Hue = Race.RandomSkinHue();
        Female = false;

        SetStr(1000, 1300);
        SetDex(101, 125);
        SetInt(61, 75);

        SetDamage(11, 24);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 10, 15);
        SetResistance(ResistanceType.Fire, 10, 15);
        SetResistance(ResistanceType.Poison, 10, 15);
        SetResistance(ResistanceType.Energy, 10, 15);

        SetSkill(SkillName.Fencing, 106.0, 117.5);
        SetSkill(SkillName.Macing, 105.0, 117.5);
        SetSkill(SkillName.MagicResist, 50.0, 90.5);
        SetSkill(SkillName.Swords, 105.0, 117.5);
        SetSkill(SkillName.Parry, 105.0, 117.5);
        SetSkill(SkillName.Tactics, 105.0, 117.5);
        SetSkill(SkillName.Wrestling, 55.0, 87.5);

        Fame = 3000;
        Karma = -3000;

        _instances.Add(this);

        AddImmovableItem(new Cutlass());
        AddImmovableItem(new ChainChest());
        AddImmovableItem(Loot.RandomShield());
        AddImmovableItem(new ShortPants(Utility.RandomNeutralHue()));
        AddImmovableItem(new Boots(Utility.RandomNeutralHue()));

        Utility.AssignRandomHair(this);

        Timer.StartTimer(TimeSpan.FromMinutes(60.0), Delete, out _selfDelete);
    }

    public static IReadOnlyList<MercutioTheUnsavory> Instances => _instances;

    public override string CorpseName => "труп Меркуцио";
    public override string DefaultName => "Меркуцио";

    public override bool ClickTitle => false;
    public override bool AlwaysMurderer => true;

    private void AddImmovableItem(Item item)
    {
        item.LootType = LootType.Blessed;
        AddItem(item);
    }

    public static MercutioTheUnsavory Spawn(Point3D platLoc, Map platMap)
    {
        if (_instances.Count > 0)
        {
            return null;
        }

        var creature = new MercutioTheUnsavory
        {
            Home = platLoc,
            RangeHome = 4
        };

        creature.MoveToWorld(platLoc, platMap);

        return creature;
    }

    public override void GenerateLoot() => AddLoot(LootPack.Average);

    public override void OnDeath(Container c)
    {
        ExploringDeepDrops.Give(this, ExploringDeepChain.ComponentQuest, static () => new MercutiosCutlass());

        base.OnDeath(c);
    }

    public override void OnGotMeleeAttack(Mobile attacker, int damage)
    {
        base.OnGotMeleeAttack(attacker, damage);

        if (0.1 >= Utility.RandomDouble())
        {
            SpawnBrigand(attacker);
        }
    }

    public override void OnGaveMeleeAttack(Mobile defender, int damage)
    {
        base.OnGaveMeleeAttack(defender, damage);

        if (0.1 >= Utility.RandomDouble())
        {
            SpawnBrigand(defender);
        }
    }

    public void SpawnBrigand(Mobile target)
    {
        var map = Map;

        if (map == null || Deleted)
        {
            return;
        }

        var brigands = 0;

        foreach (var m in map.GetMobilesInRange(Location, 10))
        {
            if (m is Brigand)
            {
                ++brigands;
            }
        }

        if (brigands >= 16)
        {
            return;
        }

        PlaySound(0x3D);

        var newBrigands = Utility.RandomMinMax(3, 6);

        for (var i = 0; i < newBrigands; ++i)
        {
            BaseCreature brigand = new Brigand();
            brigand.Team = Team;

            var validLocation = false;
            var loc = Location;

            for (var j = 0; !validLocation && j < 10; ++j)
            {
                int x = X + Utility.Random(3) - 1;
                int y = Y + Utility.Random(3) - 1;
                int z = map.GetAverageZ(x, y);

                if (validLocation = map.CanFit(x, y, Z, 16, false, false))
                {
                    loc = new Point3D(x, y, Z);
                }
                else if (validLocation = map.CanFit(x, y, z, 16, false, false))
                {
                    loc = new Point3D(x, y, z);
                }
            }

            brigand.MoveToWorld(loc, map);
            brigand.Combatant = target;
        }
    }

    public override void OnAfterDelete()
    {
        _selfDelete.Cancel();
        _instances.Remove(this);

        base.OnAfterDelete();
    }
}

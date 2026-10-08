using System;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Engines.Harvest;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/IronBeetle.cs). A tameable pack animal
/// that mines the tile it is standing on every five seconds and drops the ore at its feet, and
/// that eats any ore lying next to it — taking on that ore's hue when it does.
///
/// Dropped: the PetTrainingHelper branch in GetControlChance, which belongs to ServUO's pet
/// training system and isn't part of this codebase. Without it the original returns a flat 1.0,
/// which is what remains here — the beetle is subdue-to-tame, so the control roll is meant to
/// be a formality once it is down.
///
/// The original's repeating mining timer is never stopped when the beetle is deleted; here it
/// is a TimerExecutionToken cancelled in OnAfterDelete, and restarted after deserialization.</summary>
[SerializationGenerator(0, false)]
public partial class IronBeetle : BaseCreature
{
    private static readonly TimeSpan MiningInterval = TimeSpan.FromSeconds(5.0);

    private TimerExecutionToken _miningTimer;
    private long _nextOreEat;

    [Constructible]
    public IronBeetle() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Body = 714;
        BaseSoundID = 397;

        SetStr(816, 883);
        SetDex(68, 73);
        SetInt(40, 49);

        SetHits(762, 830);

        SetDamage(15, 20);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 55, 60);
        SetResistance(ResistanceType.Fire, 20, 30);
        SetResistance(ResistanceType.Cold, 20, 30);
        SetResistance(ResistanceType.Poison, 30, 40);
        SetResistance(ResistanceType.Energy, 45, 55);

        SetSkill(SkillName.Anatomy, 80.1, 85.0);
        SetSkill(SkillName.MagicResist, 125.1, 130.0);
        SetSkill(SkillName.Tactics, 90.1, 100.0);
        SetSkill(SkillName.Wrestling, 90.1, 110.0);
        SetSkill(SkillName.Mining, 50.1, 70.0);

        Skills.Mining.Cap = 120;

        Fame = 15000;
        Karma = -15000;

        Tamable = true;
        MinTameSkill = 71.1;
        ControlSlots = 4;

        VirtualArmor = 38;

        StartMiningTimer();
    }

    public override string CorpseName => "труп железного жука";
    public override string DefaultName => "железный жук";

    public override bool SubdueBeforeTame => true;
    public override bool StatLossAfterTame => true;

    public override bool OverrideBondingReqs() => true;

    public override double GetControlChance(Mobile m, bool useBaseSkill = false) => 1.0;

    public override int GetAngerSound() => 0x21D;
    public override int GetIdleSound() => 0x21D;
    public override int GetAttackSound() => 0x162;
    public override int GetHurtSound() => 0x163;
    public override int GetDeathSound() => 0x21D;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Meager);
        AddLoot(LootPack.Gems);
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        if (Controlled)
        {
            return;
        }

        if (Utility.RandomDouble() < 0.03)
        {
            c.DropItem(new LuckyCoin());
        }

        if (Utility.RandomDouble() < 0.1)
        {
            c.DropItem(new UndamagedIronBeetleScale());
        }
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();
        _miningTimer.Cancel();
    }

    [AfterDeserialization]
    private void AfterDeserialization() => StartMiningTimer();

    private void StartMiningTimer() =>
        Timer.StartTimer(MiningInterval, MiningInterval, DoMining, out _miningTimer);

    #region Mining

    /// <summary>A wild beetle grazes on loose ore, and takes on its hue.</summary>
    public override void OnThink()
    {
        base.OnThink();

        if (Map == null || Owners.Count > 0 || _nextOreEat > Core.TickCount)
        {
            return;
        }

        _nextOreEat = Core.TickCount + 3000;

        if (Utility.RandomDouble() >= 0.5)
        {
            return;
        }

        foreach (var ore in Map.GetItemsInRange<BaseOre>(Location, 1))
        {
            Hue = ore.Hue;
            ore.Delete();
            return;
        }
    }

    private static void GetMiningOffset(Direction d, ref int x, ref int y)
    {
        switch (d & Direction.Mask)
        {
            case Direction.North:
                {
                    --y;
                    break;
                }
            case Direction.South:
                {
                    ++y;
                    break;
                }
            case Direction.West:
                {
                    --x;
                    break;
                }
            case Direction.East:
                {
                    ++x;
                    break;
                }
            case Direction.Right:
                {
                    ++x;
                    --y;
                    break;
                }
            case Direction.Left:
                {
                    --x;
                    ++y;
                    break;
                }
            case Direction.Down:
                {
                    ++x;
                    ++y;
                    break;
                }
            case Direction.Up:
                {
                    --x;
                    --y;
                    break;
                }
        }
    }

    public void DoMining()
    {
        var map = Map;

        if (map == null || map == Map.Internal || Combatant != null)
        {
            return;
        }

        var system = Mining.System;
        var def = system.OreAndStone;

        // Its target is the land tile it is facing.
        var loc = Location;
        int x = 0, y = 0;
        GetMiningOffset(Direction, ref x, ref y);
        loc.X += x;
        loc.Y += y;

        var tileId = map.Tiles.GetLandTile(loc.X, loc.Y).ID & 0x3FFF;

        if (!def.Validate(tileId, true))
        {
            return;
        }

        var bank = def.GetBank(map, loc.X, loc.Y);

        if (bank == null || bank.Current < def.ConsumedPerHarvest)
        {
            return;
        }

        var vein = bank.Vein;

        if (vein == null)
        {
            return;
        }

        var primary = vein.PrimaryResource;
        var fallback = def.Resources[0];
        var resource = system.MutateResource(this, null, def, map, loc, vein, primary, fallback);

        var skillBase = Skills[def.Skill].Base;

        if (skillBase < resource.ReqSkill || !CheckSkill(def.Skill, resource.MinSkill, resource.MaxSkill))
        {
            return;
        }

        var type = system.GetResourceType(this, null, def, map, loc, resource);

        if (type != null)
        {
            type = system.MutateType(type, this, null, def, map, loc, resource);
        }

        if (type == null)
        {
            return;
        }

        var item = system.Construct(type, this);

        if (item == null)
        {
            return;
        }

        if (item.Stackable)
        {
            item.Amount = map == Map.Felucca ? def.ConsumedPerFeluccaHarvest : def.ConsumedPerHarvest;
        }

        bank.Consume(item.Amount, this);
        item.MoveToWorld(loc, map);

        system.DoHarvestingEffect(this, null, def, map, loc);
        system.DoHarvestingSound(this, null, def, null);

        // Mine for gems.
        var bonus = def.GetBonusResource();

        if (bonus?.Type != null && skillBase >= bonus.ReqSkill)
        {
            system.Construct(bonus.Type, this)?.MoveToWorld(loc, map);
        }
    }

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        if (!Controlled || ControlMaster != from || from is not PlayerMobile pm)
        {
            return;
        }

        list.Add(new ContextMenuEntry(pm.ToggleMiningStone ? 6179 : 6178) { Color = 0x421F });

        var stoneMining = pm.StoneMining && pm.Skills.Mining.Base >= 100.0;
        list.Add(new BaseHarvestTool.ToggleMiningStoneEntry(false, pm.ToggleMiningStone, 6176));
        list.Add(new BaseHarvestTool.ToggleMiningStoneEntry(true, !pm.ToggleMiningStone && stoneMining, 6177));
    }

    #endregion
}

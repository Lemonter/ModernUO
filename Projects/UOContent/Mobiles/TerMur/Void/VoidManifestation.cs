using System;
using ModernUO.Serialization;
using Server.Items;
using Server.Spells.Mysticism;

namespace Server.Mobiles;

/// <summary>What a Gargish Rouser is really for. Ported from ServUO
/// (Scripts/Mobiles/Normal/VoidManesfistation.cs — the filename's misspelling is the
/// original's).
///
/// It raises a Rising Colossus every half minute and flips between Mysticism and Magery every
/// ten to thirty seconds, keeping its target across the switch. Which void crystal it leaves
/// behind is decided by the rouser that called it.
///
/// This is the creature GargishRouser's port was missing; with the Void Creatures set in place
/// it is back, and so is the rouser's full summoning behaviour.</summary>
[SerializationGenerator(0, false)]
[CorpseName("a void corpse")]
public partial class VoidManifestation : BaseCreature
{
    private long _nextSummon;
    private long _nextAIChange;

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _crystalType;

    [Constructible]
    public VoidManifestation(int crystalType = 0) : base(AIType.AI_Mystic, FightMode.Closest, 10, 1)
    {
        _crystalType = crystalType;

        Body = 740;
        Hue = 2071;
        BaseSoundID = 684;

        SetStr(500);
        SetDex(150);
        SetInt(105);

        SetHits(2400);
        SetMana(60000);
        SetStam(200);

        SetDamage(25, 31);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 15, 30);
        SetResistance(ResistanceType.Fire, 50, 65);
        SetResistance(ResistanceType.Cold, 50, 65);
        SetResistance(ResistanceType.Poison, 50, 65);
        SetResistance(ResistanceType.Energy, 50, 65);

        SetSkill(SkillName.MagicResist, 140.0);
        SetSkill(SkillName.Tactics, 130.0);
        SetSkill(SkillName.Magery, 130.0);
        SetSkill(SkillName.EvalInt, 130.0);
        SetSkill(SkillName.Mysticism, 120.0);
        SetSkill(SkillName.Focus, 120.0);
        SetSkill(SkillName.Meditation, 120.0);
        SetSkill(SkillName.Wrestling, 130.0);
        SetSkill(SkillName.Necromancy, 120.0);
        SetSkill(SkillName.SpiritSpeak, 120.0);

        Fame = 15000;
        Karma = -15000;
    }

    public override string DefaultName => "воплощение Пустоты";

    // ServUO's Poison.Parasitic is an alias for DeadlyParasitic; this codebase names the
    // levels individually, so the level itself is written out.
    public override Poison PoisonImmune => Poison.DeadlyParasitic;
    public override bool AlwaysMurderer => true;
    public override bool ReacquireOnMovement => true;
    public override TimeSpan AcquireOnApproachDelay => TimeSpan.Zero;
    public override int AcquireOnApproachRange => 8;

    public override WeaponAbility GetWeaponAbility()
    {
        if (Weapon is BaseWeapon weapon)
        {
            return Utility.RandomBool() ? weapon.PrimaryAbility : weapon.SecondaryAbility;
        }

        return WeaponAbility.WhirlwindAttack;
    }

    public override void GenerateLoot()
    {
        AddLoot(LootPack.UltraRich, 3);
        AddLoot(LootPack.MedScrolls, 2);
        AddLoot(LootPack.HighScrolls, 3);
    }

    public override void OnThink()
    {
        base.OnThink();

        if (Combatant == null)
        {
            return;
        }

        if (_nextSummon <= Core.TickCount && Mana > 40 && Followers + 5 <= FollowersMax)
        {
            new RisingColossusSpell(this).Cast();
            _nextSummon = Core.TickCount + 30000;
        }

        if (_nextAIChange > Core.TickCount)
        {
            return;
        }

        var combatant = Combatant;

        ChangeAIType(AIObject is MysticAI ? AIType.AI_Mage : AIType.AI_Mystic);

        Combatant = combatant;

        _nextAIChange = Core.TickCount + Utility.RandomMinMax(10, 30) * 1000;
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        Item crystal = _crystalType switch
        {
            1 => new VoidCrystalOfCorruptedArcaneEssence(),
            2 => new VoidCrystalOfCorruptedSpiritualEssence(),
            3 => new VoidCrystalOfCorruptedMysticalEssence(),
            _ => null
        };

        if (crystal != null)
        {
            c.DropItem(crystal);
        }
    }
}

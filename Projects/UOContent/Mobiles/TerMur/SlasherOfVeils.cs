using System;
using ModernUO.Serialization;
using Server.Items;
using Server.Spells;

namespace Server.Mobiles;

/// <summary>The Slasher of Veils, the daemon at the bottom of the Abyss. Ported from ServUO
/// (Scripts/Mobiles/Bosses/SlasherOfVeils.cs).
///
/// Its signature move is the blink: spell it from across the room and half the time it simply
/// appears on top of the caster and turns on them.
///
/// Dropped: the four SetSpecialAbility / SetWeaponAbility registrations (AngryFire, ManaDrain,
/// TrueFear, Paralyzing Blow) — that is ServUO's pet-training ability table, which isn't part
/// of this codebase; the weapon ability comes back through GetWeaponAbility, the three special
/// abilities have no equivalent here (same simplification as Rotworm.cs).
///
/// Both artifact lists are empty: none of the thirteen artifacts the original names exist in
/// this codebase yet. Its fire ring is kept — note the original leaves the call site that
/// would trigger it commented out, so it never actually fires there either.</summary>
[SerializationGenerator(0, false)]
[CorpseName("a slasher of veils corpse")]
public partial class SlasherOfVeils : BaseSABoss
{
    private static readonly int[] _north = [-1, -1, 1, -1, -1, 2, 1, 2];
    private static readonly int[] _east = [-1, 0, 2, 0];

    [Constructible]
    public SlasherOfVeils() : base(AIType.AI_Mage, FightMode.Closest, 10, 1)
    {
        Body = 741;

        SetStr(901, 1010);
        SetDex(127, 153);
        SetInt(1078, 1263);

        SetHits(50000, 65000);
        SetMana(10000);

        SetDamage(10, 15);

        SetDamageType(ResistanceType.Physical, 20);
        SetDamageType(ResistanceType.Fire, 20);
        SetDamageType(ResistanceType.Cold, 20);
        SetDamageType(ResistanceType.Poison, 20);
        SetDamageType(ResistanceType.Energy, 20);

        SetResistance(ResistanceType.Physical, 65, 80);
        SetResistance(ResistanceType.Fire, 70, 80);
        SetResistance(ResistanceType.Cold, 70, 80);
        SetResistance(ResistanceType.Poison, 70, 80);
        SetResistance(ResistanceType.Energy, 70, 80);

        SetSkill(SkillName.Anatomy, 110.8, 129.7);
        SetSkill(SkillName.EvalInt, 113.4, 130.0);
        SetSkill(SkillName.Magery, 111.7, 130.0);
        SetSkill(SkillName.Spellweaving, 111.1, 125.0);
        SetSkill(SkillName.Meditation, 113.5, 129.9);
        SetSkill(SkillName.MagicResist, 110.0, 129.8);
        SetSkill(SkillName.Tactics, 110.5, 126.3);
        SetSkill(SkillName.Wrestling, 110.1, 130.0);
        SetSkill(SkillName.DetectHidden, 127.1);

        Fame = 35000;
        Karma = -35000;
    }

    public override string DefaultName => "Разрыватель Завес";

    public override Type[] UniqueSAList => Type.EmptyTypes;
    public override Type[] SharedSAList => Type.EmptyTypes;

    public override bool Unprovokable => false;
    public override bool BardImmune => false;
    public override bool AlwaysMurderer => true;

    public override WeaponAbility GetWeaponAbility() => WeaponAbility.ParalyzingBlow;

    public override int GetIdleSound() => 1589;
    public override int GetAngerSound() => 1586;
    public override int GetHurtSound() => 1588;
    public override int GetDeathSound() => 1587;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.SuperBoss, 4);
        AddLoot(LootPack.Gems, 8);
    }

    public override void FireRing()
    {
        var map = Map;

        if (map == null)
        {
            return;
        }

        SendRing(map, _north, 0x3E27);
        SendRing(map, _east, 0x3E31);
    }

    private void SendRing(Map map, int[] offsets, int effectId)
    {
        for (var i = 0; i < offsets.Length; i += 2)
        {
            var p = Location;
            p.X += offsets[i];
            p.Y += offsets[i + 1];

            IPoint3D po = p;
            SpellHelper.GetSurfaceTop(ref po);

            Effects.SendLocationEffect(new Point3D(po), map, effectId, 50);
        }
    }

    /// <summary>Half the time, answers a spell by appearing on top of whoever cast it.</summary>
    public override void OnDamagedBySpell(Mobile caster, int damage)
    {
        if (Map != null && caster != this && caster.Alive && caster.Map == Map &&
            caster.InRange(Location, 10) && Utility.RandomDouble() < 0.5)
        {
            MoveToWorld(caster.Location, Map);
            Effects.PlaySound(Location, Map, 0x1FE);

            Timer.StartTimer(() => Combatant = caster);
        }

        base.OnDamagedBySpell(caster, damage);
    }
}

using System;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO's Wrong Revamped dungeon (Mobile/Fezzik.cs and Mobile/HungryOgre.cs) — the ogre
///     cook at the cauldron and the hungry ogre in the cells.
///
///     Fezzik keeps his cauldron spawn: <see cref="StinkingCauldron" /> exists here, and the two-minute
///     cooldown is a <see cref="Core.Now" /> stamp instead of <c>DateTime.UtcNow</c>.
///     <c>OnGotMeleeAttack</c> takes the damage amount as a second argument here.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Fezzik : BaseCreature
{
    private static readonly TimeSpan CauldronDelay = TimeSpan.FromMinutes(2.0);

    private DateTime _nextCauldron;

    [Constructible]
    public Fezzik() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Name = "Феззик";
        Title = "огр-повар";
        Body = 1;
        BaseSoundID = 427;

        SetStr(1142, 1381);
        SetDex(73, 90);
        SetInt(52, 84);

        SetMana(0);

        SetDamage(25, 30);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 75, 80);
        SetResistance(ResistanceType.Fire, 70, 75);
        SetResistance(ResistanceType.Cold, 65, 75);
        SetResistance(ResistanceType.Poison, 55, 65);
        SetResistance(ResistanceType.Energy, 65, 75);

        SetSkill(SkillName.MagicResist, 133.3, 151.9);
        SetSkill(SkillName.Tactics, 120.3, 130.0);
        SetSkill(SkillName.Wrestling, 122.2, 128.9);
        SetSkill(SkillName.Anatomy, 10.0, 15.0);
        SetSkill(SkillName.DetectHidden, 90.0);
        SetSkill(SkillName.Parry, 95.0, 100.0);

        Fame = 3000;
        Karma = -3000;

        VirtualArmor = 52;

        PackItem(new Club());
    }

    public override string CorpseName => "труп огра";

    public override int Meat => 2;
    public override int TreasureMapLevel => 3;

    public override void AlterDamageScalarFrom(Mobile caster, ref double scalar)
    {
        base.AlterDamageScalarFrom(caster, ref scalar);

        if (0.5 >= Utility.RandomDouble())
        {
            SpawnGreenGoo();
        }
    }

    public override void OnGotMeleeAttack(Mobile attacker, int damage)
    {
        base.OnGotMeleeAttack(attacker, damage);

        if (0.5 >= Utility.RandomDouble())
        {
            SpawnGreenGoo();
        }
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        if (0.1 > Utility.RandomDouble())
        {
            c.DropItem(new RecipeScroll(603));
        }
    }

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Meager, 2);
    }

    private void SpawnGreenGoo()
    {
        if (_nextCauldron > Core.Now)
        {
            return;
        }

        new StinkingCauldron().MoveToWorld(Location, Map);

        _nextCauldron = Core.Now + CauldronDelay;
    }
}

/// <summary>
///     The hungry ogre; see <see cref="Феззик" /> for the substitutions.
/// </summary>
[SerializationGenerator(0, false)]
public partial class HungryOgre : BaseCreature
{
    [Constructible]
    public HungryOgre() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Name = "голодный огр";
        Body = 0x1;
        BaseSoundID = 427;

        SetStr(188, 223);
        SetDex(62, 79);
        SetInt(49, 59);

        SetHits(1107, 1205);
        SetMana(49, 59);

        SetDamage(15, 20);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 50, 60);
        SetResistance(ResistanceType.Fire, 70, 80);
        SetResistance(ResistanceType.Cold, 50, 60);
        SetResistance(ResistanceType.Poison, 70, 80);
        SetResistance(ResistanceType.Energy, 60, 70);

        SetSkill(SkillName.MagicResist, 61.1, 69.9);
        SetSkill(SkillName.Tactics, 102.3, 109.6);
        SetSkill(SkillName.Wrestling, 100.9, 108.7);

        Fame = 12000;
        Karma = -12000;

        VirtualArmor = 32;

        PackItem(new Club());
    }

    public override string CorpseName => "труп голодного огра";

    public override bool CanRummageCorpses => true;
    public override int TreasureMapLevel => 1;
    public override int Meat => 2;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Average);
        AddLoot(LootPack.Potions);
    }
}

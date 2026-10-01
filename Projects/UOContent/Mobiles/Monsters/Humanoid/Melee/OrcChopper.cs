using ModernUO.Serialization;
using Server.Items;
using Server.Misc;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/OrcChopper.cs) — the woodcutting orc,
/// hued green and carrying an executioner's axe, that the prisoner camps put on guard duty.
///
/// Dropped: the Yeast rare drop, which belongs to New Magincia's distillation system and has
/// no consumer here; and the two SetWeaponAbility registrations (Whirlwind, Crushing Blow),
/// which are ServUO's pet-training table — the ability comes back through GetWeaponAbility.
/// TribeType is not part of this codebase either, as with the other orcs already here.</summary>
[SerializationGenerator(0, false)]
[CorpseName("an orcish corpse")]
public partial class OrcChopper : BaseCreature
{
    [Constructible]
    public OrcChopper() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Body = 7;
        BaseSoundID = 0x45A;
        Hue = 0x96D;

        SetStr(147, 245);
        SetDex(91, 115);
        SetInt(61, 85);

        SetHits(97, 139);

        SetDamage(4, 13);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 25, 35);
        SetResistance(ResistanceType.Fire, 30, 40);
        SetResistance(ResistanceType.Cold, 15, 25);
        SetResistance(ResistanceType.Poison, 15, 25);
        SetResistance(ResistanceType.Energy, 25, 30);

        SetSkill(SkillName.MagicResist, 60.1, 85.0);
        SetSkill(SkillName.Tactics, 75.1, 90.0);
        SetSkill(SkillName.Wrestling, 60.1, 85.0);

        Fame = 4500;
        Karma = -4500;

        VirtualArmor = 54;

        PackItem(new Log(Utility.RandomMinMax(1, 10)));
        PackItem(new Board(Utility.RandomMinMax(10, 20)));
        PackItem(new ExecutionersAxe());

        // TODO: Skull?
        PackItem(
            Utility.Random(7) switch
            {
                0 => new Arrow(),
                1 => new Lockpick(),
                2 => new Shaft(),
                3 => new Ribs(),
                4 => new Bandage(),
                5 => new BeverageBottle(BeverageType.Wine),
                _ => new Jug(BeverageType.Cider) // 6
            }
        );

        if (Core.AOS)
        {
            PackItem(Loot.RandomNecromancyReagent());
        }
    }

    public override string DefaultName => "орк-дровосек";

    public override InhumanSpeech SpeechType => InhumanSpeech.Orc;

    public override bool CanRummageCorpses => true;
    public override int Meat => 1;

    public override OppositionGroup OppositionGroup => OppositionGroup.SavagesAndOrcs;

    public override WeaponAbility GetWeaponAbility() =>
        Utility.RandomBool() ? WeaponAbility.WhirlwindAttack : WeaponAbility.CrushingBlow;

    public override void GenerateLoot() => AddLoot(LootPack.Meager, 2);

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        c.DropItem(new DoubleAxe());

        if (Utility.RandomDouble() < 0.1)
        {
            c.DropItem(new EvilOrcHelm());
        }
    }

    public override bool IsEnemy(Mobile m) =>
        (!m.Player || m.FindItemOnLayer<OrcishKinMask>(Layer.Helm) == null) && base.IsEnemy(m);

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
}

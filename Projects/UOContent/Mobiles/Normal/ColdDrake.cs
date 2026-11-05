using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/ColdDrake.cs). IAuraCreature and
/// AreaEffect.AuraDamage do not exist; the same 5-damage, 4-tile, 5-second cold aura is expressed
/// with the built-in BaseCreature aura (HasAura/Aura*Damage), and AuraEffect supplies the original
/// visuals. Dragon breath maps to the ColdBreath monster ability.</summary>
[SerializationGenerator(0, false)]
public partial class ColdDrake : BaseCreature
{
    [Constructible]
    public ColdDrake() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Body = Utility.RandomList(60, 61);
        BaseSoundID = 362;

        Hue = Utility.RandomMinMax(1319, 1327);

        SetStr(610, 670);
        SetDex(130, 160);
        SetInt(150, 190);

        SetHits(450, 500);

        SetDamage(17, 20);

        SetDamageType(ResistanceType.Physical, 50);
        SetDamageType(ResistanceType.Cold, 50);

        SetResistance(ResistanceType.Physical, 50, 65);
        SetResistance(ResistanceType.Fire, 30, 40);
        SetResistance(ResistanceType.Cold, 75, 90);
        SetResistance(ResistanceType.Poison, 40, 50);
        SetResistance(ResistanceType.Energy, 40, 50);

        SetSkill(SkillName.MagicResist, 95.0, 110.0);
        SetSkill(SkillName.Tactics, 115.0, 140.0);
        SetSkill(SkillName.Wrestling, 115.0, 126.0);
        SetSkill(SkillName.Parry, 70.0, 80.0);
        SetSkill(SkillName.DetectHidden, 40.0, 50.0);

        Fame = 12000;
        Karma = -12000;

        VirtualArmor = 60;

        Tamable = true;
        ControlSlots = 3;
        MinTameSkill = 96.0;

        PackReg(3);

        for (var i = 0; i <= 1; i++)
        {
            var necroMax = (Core.SE ? Loot.SENecromancyScrollTypes : Loot.NecromancyScrollTypes).Length - 1;

            PackItem(
                Utility.RandomBool()
                    ? Loot.RandomScroll(0, necroMax, SpellbookType.Necromancer)
                    : Loot.RandomScroll(0, Loot.RegularScrollTypes.Length - 1, SpellbookType.Regular)
            );
        }
    }

    public override string CorpseName => "труп дрейка";
    public override string DefaultName => "холодный дрейк";

    private static readonly MonsterAbility[] _abilities = [MonsterAbilities.ColdBreath];

    public override MonsterAbility[] GetMonsterAbilities() => _abilities;

    public override bool HasAura => true;
    public override int AuraBaseDamage => 5;
    public override int AuraRange => 4;
    public override int AuraFireDamage => 0;
    public override int AuraColdDamage => 100;

    public override void AuraEffect(Mobile m)
    {
        m.FixedParticles(0x374A, 10, 30, 5052, Hue, 0, EffectLayer.Waist);
        m.PlaySound(0x5C6);

        m.SendLocalizedMessage(1008111, Name); //  : The intense cold is damaging you!
    }

    public override void GenerateLoot()
    {
        AddLoot(LootPack.FilthyRich, 3);
    }

    public override bool CanAngerOnTame => true;
    public override bool ReacquireOnMovement => !Controlled;
    public override int TreasureMapLevel => 3;
    public override int Meat => 10;
    public override int Hides => 22;
    public override HideType HideType => HideType.Horned;
    public override int DragonBlood => 8;
    public override FoodType FavoriteFood => FoodType.Fish;
}

using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKhaldun/Creatures/SkelementalKnight.cs).
///     Dropped: the <c>Caddellite</c> damage gate (no <c>OnBeforeDamage</c> / item-socket system here).
/// </summary>
[SerializationGenerator(0, false)]
public partial class SkelementalKnight : BaseCreature
{
    public enum SkeletalType
    {
        Fire,
        Cold,
        Poison,
        Energy
    }

    public override string CorpseName => "скелетный труп";

    [Constructible]
    public SkelementalKnight() : this(Utility.RandomBool() ? SkeletalType.Energy : SkeletalType.Poison)
    {
    }

    [Constructible]
    public SkelementalKnight(SkeletalType type) : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Name = "скелетный рыцарь";
        Body = 0x93;
        BaseSoundID = 451;

        int fire = 100, cold = 100, poison = 100, energy = 100;

        switch (type)
        {
            case SkeletalType.Fire:
                {
                    Hue = 2634;
                    SetDamageType(ResistanceType.Fire, 100);
                    cold = 5;
                    break;
                }
            case SkeletalType.Cold:
                {
                    Hue = 2581;
                    SetDamageType(ResistanceType.Cold, 100);
                    fire = 5;
                    break;
                }
            case SkeletalType.Poison:
                {
                    Hue = 2688;
                    SetDamageType(ResistanceType.Poison, 100);
                    energy = 5;
                    break;
                }
            case SkeletalType.Energy:
                {
                    Hue = 2717;
                    SetDamageType(ResistanceType.Energy, 100);
                    poison = 5;
                    break;
                }
        }

        SetStr(200, 250);
        SetDex(70, 95);
        SetInt(35, 60);

        SetHits(110, 150);

        SetDamage(8, 18);

        SetDamageType(ResistanceType.Physical, 0);

        SetResistance(ResistanceType.Physical, 95);
        SetResistance(ResistanceType.Fire, fire);
        SetResistance(ResistanceType.Cold, cold);
        SetResistance(ResistanceType.Poison, poison);
        SetResistance(ResistanceType.Energy, energy);

        SetSkill(SkillName.MagicResist, 60.0, 80.0);
        SetSkill(SkillName.Tactics, 85.0, 100.0);
        SetSkill(SkillName.Wrestling, 85.0, 100.0);
        SetSkill(SkillName.DetectHidden, 40.0);

        Fame = 3000;
        Karma = -3000;

        VirtualArmor = 40;

        switch (Utility.Random(6))
        {
            case 0:
                PackItem(new PlateArms());
                break;
            case 1:
                PackItem(new PlateChest());
                break;
            case 2:
                PackItem(new PlateGloves());
                break;
            case 3:
                PackItem(new PlateGorget());
                break;
            case 4:
                PackItem(new PlateLegs());
                break;
            case 5:
                PackItem(new PlateHelm());
                break;
        }

        PackItem(new Scimitar());
        PackItem(new WoodenShield());
    }

    public override bool BleedImmune => true;

    public override OppositionGroup OppositionGroup => OppositionGroup.FeyAndUndead;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Average);
        AddLoot(LootPack.Meager);
    }
}

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKhaldun/Creatures/SkelementalMage.cs).
///     Dropped: the <c>Caddellite</c> damage gate (no <c>OnBeforeDamage</c> / item-socket system here).
/// </summary>
[SerializationGenerator(0, false)]
public partial class SkelementalMage : BaseCreature
{
    public override string CorpseName => "скелетный труп";

    [Constructible]
    public SkelementalMage() : this(Utility.RandomBool() ? SkelementalKnight.SkeletalType.Cold : SkelementalKnight.SkeletalType.Fire)
    {
    }

    [Constructible]
    public SkelementalMage(SkelementalKnight.SkeletalType type) : base(AIType.AI_Mage, FightMode.Closest, 10, 1)
    {
        Name = "скелетный маг";
        Body = 0x32;
        BaseSoundID = 451;

        int fire = 100, cold = 100, poison = 100, energy = 100;

        switch (type)
        {
            case SkelementalKnight.SkeletalType.Fire:
                {
                    Hue = 2634;
                    SetDamageType(ResistanceType.Fire, 100);
                    cold = 5;
                    break;
                }
            case SkelementalKnight.SkeletalType.Cold:
                {
                    Hue = 2581;
                    SetDamageType(ResistanceType.Cold, 100);
                    fire = 5;
                    break;
                }
            case SkelementalKnight.SkeletalType.Poison:
                {
                    Hue = 2688;
                    SetDamageType(ResistanceType.Poison, 100);
                    energy = 5;
                    break;
                }
            case SkelementalKnight.SkeletalType.Energy:
                {
                    Hue = 2717;
                    SetDamageType(ResistanceType.Energy, 100);
                    poison = 5;
                    break;
                }
        }

        SetStr(200, 250);
        SetDex(70, 100);
        SetInt(100, 130);

        SetHits(100, 150);

        SetDamage(8, 18);

        SetDamageType(ResistanceType.Physical, 0);

        SetResistance(ResistanceType.Physical, 95);
        SetResistance(ResistanceType.Fire, fire);
        SetResistance(ResistanceType.Cold, cold);
        SetResistance(ResistanceType.Poison, poison);
        SetResistance(ResistanceType.Energy, energy);

        SetSkill(SkillName.MagicResist, 60.0, 80.0);
        SetSkill(SkillName.Tactics, 75.0, 100.0);
        SetSkill(SkillName.Wrestling, 85.0, 100.0);
        SetSkill(SkillName.DetectHidden, 50.0);
        SetSkill(SkillName.Magery, 110.0, 120.0);
        SetSkill(SkillName.Meditation, 150.0, 155.0);
        SetSkill(SkillName.Focus, 0.0, 60.0);

        Fame = 3000;
        Karma = -3000;

        VirtualArmor = 38;
        PackReg(3);
        PackNecroReg(3, 10);
        PackItem(new Bone());
    }

    public override bool BleedImmune => true;

    public override OppositionGroup OppositionGroup => OppositionGroup.FeyAndUndead;

    public override Poison PoisonImmune => Poison.Regular;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Average);
        AddLoot(LootPack.LowScrolls);
        AddLoot(LootPack.Potions);
    }
}

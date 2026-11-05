using System;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKhaldun/Creatures/KhalAnkurWarriors.cs).
///     Dropped: the <c>Caddellite</c> damage gate (no <c>OnBeforeDamage</c> / item-socket system here) and
///     <c>SetWearable</c> (now <c>AddItem</c>/<c>EquipItem</c>).
/// </summary>
[SerializationGenerator(0, false)]
public partial class KhalAnkurWarriors : BaseCreature
{
    public enum WarriorType
    {
        Scout,
        Corporal,
        Lieutenant,
        Captain,
        General
    }

    [SerializableField(0)]
    private WarriorType _type;

    public static WarriorType RandomType => (WarriorType)Utility.Random(Enum.GetValues<WarriorType>().Length);

    public override string CorpseName => "труп человека";

    [Constructible]
    public KhalAnkurWarriors() : this(RandomType)
    {
    }

    [Constructible]
    public KhalAnkurWarriors(WarriorType type) : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        _type = type;

        if (Female = Utility.RandomBool())
        {
            Body = 0x191;
            Name = NameList.RandomName("female");
        }
        else
        {
            Body = 0x190;
            Name = NameList.RandomName("male");
        }

        BaseSoundID = 0x45A;

        SetStr(150, 250);
        SetDex(150);
        SetInt(100, 150);

        SetDamage(20, 30);

        SetHits(600, 800);

        string title = _type switch
        {
            WarriorType.Scout      => "разведчик",
            WarriorType.Corporal   => "капрал",
            WarriorType.Lieutenant => "лейтенант",
            WarriorType.Captain    => "капитан",
            _                      => "генерал"
        };

        if (_type is WarriorType.Captain or WarriorType.General)
        {
            SetHits(1000, 1500);
        }

        Title = title;

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 20, 30);
        SetResistance(ResistanceType.Fire, 20, 30);
        SetResistance(ResistanceType.Cold, 20, 30);
        SetResistance(ResistanceType.Poison, 20, 30);
        SetResistance(ResistanceType.Energy, 20, 30);

        SetSkill(SkillName.Fencing, 105.0, 130.0);
        SetSkill(SkillName.Macing, 105.0, 130.0);
        SetSkill(SkillName.MagicResist, 105.0, 130.0);
        SetSkill(SkillName.Swords, 105.0, 130.0);
        SetSkill(SkillName.Tactics, 105.0, 130.0);
        SetSkill(SkillName.Archery, 105.0, 130.0);
        SetSkill(SkillName.Magery, 105.0, 130.0);
        SetSkill(SkillName.Meditation, 105.0, 130.0);

        Fame = 5000;
        Karma = -5000;

        var outfit = Utility.Random(4);
        var outfitHue = outfit == 2 ? 2684 : 2697;

        Hue = outfitHue;

        switch (outfit)
        {
            case 0:
                {
                    AddItem(new ChainChest { Hue = outfitHue });
                    AddItem(new ChainCoif { Hue = outfitHue });
                    AddItem(new Cloak { Hue = outfitHue });

                    break;
                }
            case 1:
                {
                    AddItem(new PlateChest { Hue = outfitHue });
                    AddItem(new PlateLegs { Hue = outfitHue });
                    AddItem(new Cloak { Hue = outfitHue });
                    break;
                }
            case 2:
                {
                    AddItem(new PlateChest { Hue = outfitHue });
                    AddItem(new PlateLegs { Hue = outfitHue });
                    AddItem(new PlateArms { Hue = outfitHue });
                    break;
                }
            case 3:
                {
                    AddItem(new ChainChest { Hue = outfitHue });
                    AddItem(new ChainLegs { Hue = outfitHue });
                    AddItem(new Boots { Hue = outfitHue });
                    break;
                }
        }

        switch (Utility.Random(2))
        {
            case 0:
                {
                    EquipItem(
                        Loot.Construct(
                            new[]
                            {
                                typeof(Spear), typeof(QuarterStaff), typeof(BlackStaff), typeof(Tessen), typeof(Cleaver),
                                typeof(Lajatang)
                            }
                        )
                    );

                    break;
                }
            case 1:
                {
                    EquipItem(
                        Loot.Construct(
                            new[] { typeof(Yumi), typeof(Crossbow), typeof(RepeatingCrossbow), typeof(HeavyCrossbow) }
                        )
                    );

                    RangeFight = 7;
                    AI = AIType.AI_Archer;

                    break;
                }
        }

        var hairHue = Utility.RandomNondyedHue();

        Utility.AssignRandomHair(this, hairHue);

        if (Utility.Random(7) != 0)
        {
            Utility.AssignRandomFacialHair(this, hairHue);
        }
    }

    public override bool AlwaysMurderer => true;

    public override bool ShowFameTitle => false;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Rich);
    }

    public override WeaponAbility GetWeaponAbility()
    {
        if (Weapon is BaseWeapon { } weapon and not Fists)
        {
            return Utility.RandomDouble() > 0.5 ? weapon.PrimaryAbility : weapon.SecondaryAbility;
        }

        return null;
    }
}

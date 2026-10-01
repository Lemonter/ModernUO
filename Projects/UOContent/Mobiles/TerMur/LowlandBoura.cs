using ModernUO.Serialization;
using Server.Items;
using Server.Network;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/LowlandBoura.cs) — the third and
/// tamest of the boura herd. SetSpecialAbility dropped (see Rotworm.cs); everything else,
/// including the live fur shearing, is the original's.</summary>
[SerializationGenerator(0, false)]
[CorpseName("a boura corpse")]
public partial class LowlandBoura : BaseCreature, ICarvable
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _gatheredFur;

    [Constructible]
    public LowlandBoura() : base(AIType.AI_Animal, FightMode.Aggressor, 10, 1)
    {
        Body = 715;

        SetStr(337, 411);
        SetDex(82, 93);
        SetInt(23, 25);

        SetHits(438, 553);

        SetDamage(18, 23);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 50, 60);
        SetResistance(ResistanceType.Fire, 35, 40);
        SetResistance(ResistanceType.Cold, 10, 20);
        SetResistance(ResistanceType.Poison, 30, 40);
        SetResistance(ResistanceType.Energy, 30, 40);

        SetSkill(SkillName.Anatomy, 81.2, 84.4);
        SetSkill(SkillName.MagicResist, 70.7, 75.0);
        SetSkill(SkillName.Tactics, 83.4, 86.7);
        SetSkill(SkillName.Wrestling, 95.1, 97.3);

        Tamable = true;
        ControlSlots = 3;
        MinTameSkill = 19.1;

        Fame = 5000;
        Karma = -3500;

        VirtualArmor = 16;
    }

    public override string DefaultName => "низинная боура";

    public override int Meat => 10;
    public override int Hides => 20;
    public override int DragonBlood => 8;
    public override HideType HideType => HideType.Horned;
    public override FoodType FavoriteFood => FoodType.FruitsAndVeggies;

    public override int Fur => _gatheredFur ? 0 : 30;
    public override FurType FurType => FurType.Green;

    public void Carve(Mobile from, Item item)
    {
        if (_gatheredFur)
        {
            // The boura glares at you and will not let you shear its fur.
            PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1112354, from.NetState);
            return;
        }

        if (FurShearing.TryShear(this, from, 1112352, 1112353))
        {
            GatheredFur = true;
        }
    }

    public override int GetIdleSound() => 1507;
    public override int GetAngerSound() => 1504;
    public override int GetHurtSound() => 1506;
    public override int GetDeathSound() => 1505;

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        if (!Controlled)
        {
            c.DropItem(new BouraSkin());
        }
    }
}

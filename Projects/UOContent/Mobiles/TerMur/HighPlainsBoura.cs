using ModernUO.Serialization;
using Server.Items;
using Server.Network;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/HighPlainsBoura.cs).
/// SetSpecialAbility dropped (see Rotworm.cs). The fur and dragon-blood carving it wanted has
/// since been built (Items/Resources/Fur.cs, BaseCreature.OnCarve) and is restored.</summary>
[SerializationGenerator(0, false)]
[CorpseName("a boura corpse")]
public partial class HighPlainsBoura : BaseCreature, ICarvable
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _gatheredFur;

    [Constructible]
    public HighPlainsBoura() : base(AIType.AI_Melee, FightMode.Aggressor, 10, 1)
    {
        Body = 715;

        SetStr(400, 435);
        SetDex(90, 96);
        SetInt(25, 30);

        SetHits(555, 618);

        SetDamage(20, 25);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 50, 60);
        SetResistance(ResistanceType.Fire, 35, 40);
        SetResistance(ResistanceType.Cold, 10, 20);
        SetResistance(ResistanceType.Poison, 30, 40);
        SetResistance(ResistanceType.Energy, 30, 40);

        SetSkill(SkillName.Anatomy, 95.2, 105.4);
        SetSkill(SkillName.MagicResist, 60.7, 70.0);
        SetSkill(SkillName.Tactics, 95.4, 105.7);
        SetSkill(SkillName.Wrestling, 105.1, 115.3);

        Tamable = true;
        ControlSlots = 3;
        MinTameSkill = 47.1;

        Fame = 5000;
        Karma = -5000;
    }

    public override string DefaultName => "боура высоких равнин";

    public override int Meat => 10;
    public override int Hides => 22;
    public override int DragonBlood => 8;
    public override HideType HideType => HideType.Horned;
    public override FoodType FavoriteFood => FoodType.FruitsAndVeggies;

    public override int Fur => _gatheredFur ? 0 : 30;
    public override FurType FurType => FurType.Yellow;

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

            if (Utility.RandomDouble() <= 0.005)
            {
                c.DropItem(new BouraTailShield());
            }
        }
    }
}

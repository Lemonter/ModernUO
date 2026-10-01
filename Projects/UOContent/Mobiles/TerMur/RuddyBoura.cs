using ModernUO.Serialization;
using Server.Items;
using Server.Network;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/RuddyBoura.cs). SetSpecialAbility
/// dropped (see Rotworm.cs). The live-fur-shearing ICarvable mechanic was dropped at first
/// because neither Fur nor FurType existed on BaseCreature here; both do now
/// (Items/Resources/Fur.cs), so it is restored along with the dragon-blood carve.</summary>
[SerializationGenerator(0, false)]
[CorpseName("a boura corpse")]
public partial class RuddyBoura : BaseCreature, ICarvable
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _gatheredFur;

    [Constructible]
    public RuddyBoura() : base(AIType.AI_Melee, FightMode.Aggressor, 10, 1)
    {
        Body = 715;

        SetStr(396, 480);
        SetDex(68, 82);
        SetInt(16, 20);

        SetHits(435, 509);
        SetStam(68, 82);
        SetMana(16, 20);

        SetDamage(16, 20);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 50, 60);
        SetResistance(ResistanceType.Fire, 35, 40);
        SetResistance(ResistanceType.Cold, 10, 20);
        SetResistance(ResistanceType.Poison, 30, 40);
        SetResistance(ResistanceType.Energy, 30, 40);

        SetSkill(SkillName.Anatomy, 86.6, 88.8);
        SetSkill(SkillName.MagicResist, 69.7, 87.7);
        SetSkill(SkillName.Tactics, 83.3, 88.8);
        SetSkill(SkillName.Wrestling, 86.6, 87.9);

        Tamable = true;
        ControlSlots = 2;
        MinTameSkill = 19.1;

        Fame = 5000;
        Karma = -2500;
    }

    public override string DefaultName => "рыжая боура";

    public override int Meat => 10;
    public override int Hides => 20;
    public override int DragonBlood => 8;
    public override HideType HideType => HideType.Spined;
    public override FoodType FavoriteFood => FoodType.FruitsAndVeggies;

    public override int Fur => _gatheredFur ? 0 : 30;
    public override FurType FurType => FurType.LightBrown;

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

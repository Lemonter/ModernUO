using ModernUO.Serialization;
using Server.Items;
using Server.Network;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/Kepetch.cs). SetSpecialAbility dropped
/// (see Rotworm.cs). Fur, FurType and DragonBlood are back — see RuddyBoura.cs.</summary>
[SerializationGenerator(0, false)]
[CorpseName("a kepetch corpse")]
public partial class Kepetch : BaseCreature, ICarvable
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _gatheredFur;

    [Constructible]
    public Kepetch() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Body = 726;

        SetStr(337, 380);
        SetDex(184, 194);
        SetInt(30, 50);

        SetHits(300, 400);

        SetDamage(7, 17);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 55, 75);
        SetResistance(ResistanceType.Fire, 40, 60);
        SetResistance(ResistanceType.Cold, 40, 50);
        SetResistance(ResistanceType.Poison, 50, 70);
        SetResistance(ResistanceType.Energy, 60, 70);

        SetSkill(SkillName.Anatomy, 119.7, 124.1);
        SetSkill(SkillName.MagicResist, 89.9, 97.4);
        SetSkill(SkillName.Tactics, 117.4, 123.5);
        SetSkill(SkillName.Wrestling, 107.7, 113.9);
        SetSkill(SkillName.DetectHidden, 25.0);
        SetSkill(SkillName.Parry, 60.0, 70.0);

        Fame = 6000;
        Karma = -6000;
    }

    public override string DefaultName => "кепетч";

    public override int Meat => 5;
    public override int Hides => 14;
    public override int DragonBlood => 8;
    public override HideType HideType => HideType.Spined;
    public override FoodType FavoriteFood => FoodType.FruitsAndVeggies | FoodType.GrainsAndHay;

    public override int Fur => _gatheredFur ? 0 : 15;
    public override FurType FurType => FurType.Brown;

    public void Carve(Mobile from, Item item)
    {
        if (_gatheredFur)
        {
            // The Kepetch nimbly escapes your attempts to shear its mane.
            PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1112358, from.NetState);
            return;
        }

        if (FurShearing.TryShear(this, from, 1112359, 1112360))
        {
            GatheredFur = true;
        }
    }

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Average, 2);
    }

    public override int GetIdleSound() => 1545;
    public override int GetAngerSound() => 1542;
    public override int GetHurtSound() => 1544;
    public override int GetDeathSound() => 1543;
}

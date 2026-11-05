using System;
using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/Turkey.cs). The gobble deadline is a
/// serialized DateTime compared against Core.Now (the original used DateTime.UtcNow).</summary>
[SerializationGenerator(0, false)]
public partial class Turkey : BaseCreature
{
    [SerializableField(0)]
    private DateTime _nextGobble;

    [Constructible]
    public Turkey() : this(false)
    {
    }

    public Turkey(bool tamable) : base(AIType.AI_Animal, FightMode.Aggressor, 10, 1)
    {
        Body = 95;
        BaseSoundID = 0x66A;

        SetStr(5);
        SetDex(15);
        SetInt(5);

        SetHits(75);
        SetMana(0);

        SetDamage(5);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 1, 5);

        SetSkill(SkillName.MagicResist, 4.0);
        SetSkill(SkillName.Tactics, 5.0);
        SetSkill(SkillName.Wrestling, 5.0);

        Fame = 150;
        Karma = 0;

        VirtualArmor = 2;

        Tamable = tamable;
        ControlSlots = 1;
        MinTameSkill = -0.9;

        _nextGobble = Core.Now;
    }

    public override string CorpseName => "труп индейки";
    public override string DefaultName => "индейка";

    public override int Meat => 1;
    public override MeatType MeatType => MeatType.Bird;
    public override FoodType FavoriteFood => FoodType.GrainsAndHay;
    public override int Feathers => 25;

    public override int GetIdleSound() => 0x66A;
    public override int GetAngerSound() => 0x66A;
    public override int GetHurtSound() => 0x66B;
    public override int GetDeathSound() => 0x66B;

    public override void OnThink()
    {
        base.OnThink();

        if (Tamable && !Controlled && _nextGobble < Core.Now)
        {
            Say(1153511); // *gobble* *gobble*
            PlaySound(GetIdleSound());

            _nextGobble = Core.Now + TimeSpan.FromSeconds(Utility.RandomMinMax(20, 240));
        }
    }
}

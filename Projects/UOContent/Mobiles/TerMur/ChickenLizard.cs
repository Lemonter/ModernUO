using System;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/ChickenLizard.cs). LootPack.LootItem&lt;T&gt;
/// doesn't exist here — converted to a chance-based OnDeath drop (see WolfSpider.cs).</summary>
[SerializationGenerator(0, false)]
[CorpseName("a chicken lizard corpse")]
public partial class ChickenLizard : BaseCreature
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _nextEgg;

    [Constructible]
    public ChickenLizard() : base(AIType.AI_Melee, FightMode.Aggressor, 10, 1)
    {
        Body = 716;

        SetStr(74, 95);
        SetDex(78, 95);
        SetInt(6, 10);

        SetHits(74, 95);
        SetMana(6, 10);
        SetStam(78, 95);

        SetDamage(2, 5);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 15, 20);
        SetResistance(ResistanceType.Fire, 5, 15);

        SetSkill(SkillName.MagicResist, 25.1, 29.6);
        SetSkill(SkillName.Tactics, 30.1, 44.9);
        SetSkill(SkillName.Wrestling, 26.2, 38.2);

        Tamable = true;
        ControlSlots = 1;
        MinTameSkill = 0.0;
    }

    public override string DefaultName => "куриная ящерица";

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        if (Utility.RandomDouble() < 0.05)
        {
            c.DropItem(new ChickenLizardEgg());
        }
    }

    public override int Meat => 3;
    public override MeatType MeatType => MeatType.Bird;
    public override FoodType FavoriteFood => FoodType.Meat;

    public override int GetIdleSound() => 1511;
    public override int GetAngerSound() => 1508;
    public override int GetHurtSound() => 1510;
    public override int GetDeathSound() => 1509;

    public override bool CheckFeed(Mobile from, Item dropped)
    {
        if (from.Map == null || from.Map == Map.Internal)
        {
            return false;
        }

        var wasBonded = IsBonded;
        var fed = base.CheckFeed(from, dropped);

        if (!wasBonded && IsBonded)
        {
            _nextEgg = DateTime.UtcNow + TimeSpan.FromDays(1);
        }

        if (IsBonded && fed && DateTime.UtcNow >= _nextEgg)
        {
            if (Utility.RandomBool())
            {
                var egg = new ChickenLizardEgg();

                if (from.Backpack == null || from.Backpack.TryDropItem(from, egg, false))
                {
                    egg.MoveToWorld(from.Location, from.Map);
                }
            }

            _nextEgg = DateTime.UtcNow + TimeSpan.FromDays(7);
        }

        return fed;
    }
}

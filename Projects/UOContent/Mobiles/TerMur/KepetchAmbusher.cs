using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/KepetchAmbusher.cs). Fur, FurType and
/// DragonBlood are back — see RuddyBoura.cs. LootPack.LootItem&lt;T&gt; doesn't exist here —
/// converted to a chance-based OnDeath drop (see WolfSpider.cs). CanStealth/OnDamagedBySpell
/// fixed the same way as TrapdoorSpider.cs.</summary>
[SerializationGenerator(0, false)]
[CorpseName("a kepetch corpse")]
public partial class KepetchAmbusher : BaseCreature, ICarvable
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _gatheredFur;

    [Constructible]
    public KepetchAmbusher() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Body = 726;
        Hidden = true;

        SetStr(440, 446);
        SetDex(229, 254);
        SetInt(46, 46);

        SetHits(533, 544);

        SetDamage(7, 17);

        SetDamageType(ResistanceType.Physical, 80);
        SetDamageType(ResistanceType.Poison, 20);

        SetResistance(ResistanceType.Physical, 73, 95);
        SetResistance(ResistanceType.Fire, 57, 70);
        SetResistance(ResistanceType.Cold, 50, 60);
        SetResistance(ResistanceType.Poison, 55, 65);
        SetResistance(ResistanceType.Energy, 70, 95);

        SetSkill(SkillName.Anatomy, 104.3, 114.1);
        SetSkill(SkillName.MagicResist, 94.6, 97.4);
        SetSkill(SkillName.Tactics, 110.4, 123.5);
        SetSkill(SkillName.Wrestling, 107.3, 113.9);
        SetSkill(SkillName.Stealth, 125.0);
        SetSkill(SkillName.Hiding, 125.0);

        Fame = 2500;
        Karma = -2500;
    }

    public override string DefaultName => "кепетч-засадник";

    public override void OnDamage(int amount, Mobile from, bool willKill)
    {
        RevealingAction();
        base.OnDamage(amount, from, willKill);
    }

    public override void OnDamagedBySpell(Mobile from, int damage)
    {
        RevealingAction();
        base.OnDamagedBySpell(from, damage);
    }

    public override int Meat => 7;
    public override int Hides => 12;
    public override int DragonBlood => 8;
    public override HideType HideType => HideType.Horned;
    public override FoodType FavoriteFood => FoodType.FruitsAndVeggies | FoodType.GrainsAndHay;

    public override int Fur => _gatheredFur ? 0 : 15;
    public override FurType FurType => FurType.Brown;

    public void Carve(Mobile from, Item item)
    {
        if (_gatheredFur)
        {
            from.SendLocalizedMessage(1112358); // The Kepetch nimbly escapes your attempts to shear its mane.
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

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        if (Utility.RandomDouble() < 0.1)
        {
            c.DropItem(new KepetchWax());
        }

        if (Utility.RandomDouble() < 0.05)
        {
            c.DropItem(new RawRibs());
        }
    }

    public override void OnThink()
    {
        if (!Alive || Deleted)
        {
            return;
        }

        if (!Hidden)
        {
            var chance = 0.05;

            if (Hits < 20)
            {
                chance = 0.1;
            }

            if (Poisoned)
            {
                chance = 0.01;
            }

            if (Utility.RandomDouble() < chance)
            {
                HideSelf();
            }

            base.OnThink();
        }
    }

    private void HideSelf()
    {
        if (Core.TickCount < NextSkillTime)
        {
            return;
        }

        Effects.SendLocationParticles(EffectItem.Create(Location, Map, EffectItem.DefaultDuration), 0x3728, 10, 10, 2023);

        PlaySound(0x22F);
        Hidden = true;

        UseSkill(SkillName.Stealth);
    }
}

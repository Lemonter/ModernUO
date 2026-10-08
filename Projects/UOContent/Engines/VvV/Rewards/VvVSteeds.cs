using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.VvV;

// Ported from real OSI/ServUO content (Scripts/Services/ViceVsVirtue/Items/Rewards/VvVSteeds.cs).
// `BaseCreature.CanTransfer`/`CanFriend` overrides (block trading/befriending this pet) dropped
// — neither virtual member exists on this codebase's BaseCreature.
public enum SteedType
{
    Ostard,
    WarHorse
}

[SerializationGenerator(0, false)]
public partial class VvVSteedStatuette : BaseImprisonedMobile
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private SteedType _steedType;

    public override BaseCreature Summon =>
        SteedType == SteedType.WarHorse
            ? new VvVMount("боевой конь", 0xE2, 0x3EA0, Hue)
            : new VvVMount("боевой страус", 0xDA, 0x3EA4, Hue);

    [Constructible]
    public VvVSteedStatuette() : this(SteedType.Ostard, 0)
    {
    }

    [Constructible]
    public VvVSteedStatuette(SteedType mountType, int hue) : base(mountType == SteedType.Ostard ? 8501 : 8484)
    {
        Hue = hue;
        _steedType = mountType;
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add(1154937); // vvv item
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!ViceVsVirtueSystem.IsVvV(from) && from.AccessLevel == AccessLevel.Player)
        {
            from.SendLocalizedMessage(1155496); // This item can only be used by VvV participants!
            return;
        }

        base.OnDoubleClick(from);
    }
}

[SerializationGenerator(0, false)]
public partial class VvVMount : BaseMount
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _readiness;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _nextReadinessAtrophy;

    [CommandProperty(AccessLevel.GameMaster)]
    public int BattleReadiness
    {
        get => _readiness;
        set
        {
            var old = _readiness;
            _readiness = Math.Min(value, 20);

            if (old != value && ControlMaster?.NetState != null)
            {
                int cliloc;

                if (old > value)
                {
                    cliloc = _readiness < 5
                        ? 1155551 // *Your steed's battle readiness is dangerously low!*
                        : 1155549; // *Your steed's battle readiness is fading...*
                }
                else
                {
                    cliloc = _readiness == 20
                        ? 1155553 // *Your steed is at maximum battle readiness!*
                        : 1155552; // *Your steed's battle readiness has increased!*
                }

                Timer.DelayCall(
                    TimeSpan.FromSeconds(1),
                    () =>
                    {
                        if (!Deleted && ControlMaster != null)
                        {
                            ControlMaster.PrivateOverheadMessage(MessageType.Regular, 1154, cliloc, ControlMaster.NetState);
                        }
                    }
                );
            }

            if (_readiness <= 0)
            {
                GoPoof();
            }
        }
    }

    public override bool DeleteOnRelease => true;

    [Constructible]
    public VvVMount() : this("боевой конь", 0xE2, 0x3EA0, 0)
    {
    }

    public VvVMount(string name, int id, int itemId, int hue) : base(id, itemId, AIType.AI_Melee, FightMode.Aggressor, 10, 1)
    {
        Name = name;
        Hue = hue;

        BaseSoundID = id == 0xDA ? 0x275 : 0xA8;

        SetStr(400);
        SetDex(125);
        SetInt(51, 55);

        SetHits(240);
        SetMana(0);

        SetDamage(5, 8);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 40, 50);
        SetResistance(ResistanceType.Fire, 30, 40);
        SetResistance(ResistanceType.Cold, 30, 40);
        SetResistance(ResistanceType.Poison, 30, 40);
        SetResistance(ResistanceType.Energy, 30, 40);

        SetSkill(SkillName.MagicResist, 25.1, 30.0);
        SetSkill(SkillName.Tactics, 29.3, 44.0);
        SetSkill(SkillName.Wrestling, 29.3, 44.0);

        Fame = 300;
        Karma = 300;

        Tamable = true;
        ControlSlots = 1;
        MinTameSkill = 29.1;

        _readiness = 8;
        _nextReadinessAtrophy = Core.Now + TimeSpan.FromHours(24);

        Steeds.Add(this);
    }

    public void GoPoof()
    {
        Rider = null;

        ControlMaster?.PrivateOverheadMessage(MessageType.Regular, 1154, 1155550, ControlMaster.NetState); // *Your steed has depleted it's battle readiness!*

        Delete();
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!ViceVsVirtueSystem.IsVvV(from) && from.AccessLevel == AccessLevel.Player)
        {
            from.SendLocalizedMessage(1155561); // You are no longer in Vice vs Virtue!
        }
        else
        {
            base.OnDoubleClick(from);
        }
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        if (BattleReadiness > 1)
        {
            BattleReadiness--;
        }
    }

    public override bool OnDragDrop(Mobile from, Item dropped)
    {
        if (from == ControlMaster && dropped is EssenceOfCourage)
        {
            BattleReadiness += dropped.Amount;
            dropped.Delete();

            Animate(Body.IsMonster ? 17 : 3, 5, 1, true, false, 0);

            return true;
        }

        return base.OnDragDrop(from, dropped);
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();
        Steeds.Remove(this);
    }

    public override int Meat => 3;
    public override int Hides => 10;

    public override FoodType FavoriteFood => Body == 0xDA
        ? FoodType.Meat | FoodType.Fish | FoodType.Eggs | FoodType.FruitsAndVeggies
        : FoodType.FruitsAndVeggies | FoodType.GrainsAndHay;

    [AfterDeserialization]
    private void AfterDeserialization() => Steeds.Add(this);

    public static List<VvVMount> Steeds { get; private set; } = new();

    public static void Configure() => Steeds = new List<VvVMount>();

    public static void Initialize() =>
        Timer.DelayCall(
            TimeSpan.FromMinutes(10),
            TimeSpan.FromMinutes(10),
            () =>
            {
                foreach (var s in new List<VvVMount>(Steeds))
                {
                    if ((s.Map != Map.Internal || (s.Rider != null && s.Rider.Map != Map.Internal)) && s.NextReadinessAtrophy < Core.Now)
                    {
                        s.BattleReadiness--;

                        if (!s.Deleted)
                        {
                            s.NextReadinessAtrophy = Core.Now + TimeSpan.FromHours(24);
                        }
                    }
                }
            }
        );
}

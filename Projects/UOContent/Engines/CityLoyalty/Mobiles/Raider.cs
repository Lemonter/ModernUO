/// <summary>
///     Ported from ServUO (Scripts/Services/City Loyalty System/Mobiles/Raider.cs).
///
///     Substitutions:
///     <list type="bullet">
///         <item><c>SetWearable</c> became <c>AddItem(new X { Hue = hue })</c>.</item>
///         <item><c>NameList.RandomName</c> became <see cref="RussianNames" /> so raiders are Russian.</item>
///         <item>
///             The self-check that ran from <c>Serialize</c> in ServUO now runs from <c>OnThink</c> only;
///             re-arming a timer from inside a save is not allowed here.
///         </item>
///         <item><c>DateTime.UtcNow</c> for the arrest deadline became <c>Core.Now</c>.</item>
///     </list>
/// </summary>
using System;
using System.Linq;
using ModernUO.Serialization;
using Server.Items;
using Server.Network;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class Raider : BaseCreature
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _deleteTime;

    [Constructible]
    public Raider() : base(AIType.AI_Melee, FightMode.Closest)
    {
        Title = "налётчик";
        Hue = Race.Human.RandomSkinHue();
        Utility.AssignRandomHair(this);

        SetStr(150, 200);
        SetDex(125, 150);
        SetInt(95, 110);

        SetHits(400, 650);
        SetDamage(21, 28);

        if (Female = Utility.RandomBool())
        {
            Body = 0x191;
            Name = RussianNames.RandomFemale();
            AddItem(new Skirt { Hue = Utility.RandomNeutralHue() });
        }
        else
        {
            Body = 0x190;
            Name = RussianNames.RandomMale();
            AddItem(new ShortPants { Hue = Utility.RandomNeutralHue() });
        }

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 45, 55);
        SetResistance(ResistanceType.Fire, 45, 55);
        SetResistance(ResistanceType.Cold, 45, 55);
        SetResistance(ResistanceType.Poison, 45, 55);
        SetResistance(ResistanceType.Energy, 45, 55);

        SetSkill(SkillName.MagicResist, 50.0, 75.5);
        SetSkill(SkillName.Archery, 90.0, 105.5);
        SetSkill(SkillName.Tactics, 90.0, 105.5);
        SetSkill(SkillName.Anatomy, 90.0, 105.5);

        Fame = 7500;
        Karma = -7500;

        AddItem(new TricorneHat());
        AddItem(new LeatherArms());
        AddItem(new FancyShirt());
        AddItem(new Cutlass());
        AddItem(new Boots { Hue = Utility.RandomNeutralHue() });
        AddItem(new GoldEarrings());

        Item bow;

        switch (Utility.Random(4))
        {
            default:
            case 0:
                bow = new CompositeBow();
                PackItem(new Arrow(25));
                break;
            case 1:
                bow = new Crossbow();
                PackItem(new Bolt(25));
                break;
            case 2:
                bow = new Bow();
                PackItem(new Arrow(25));
                break;
            case 3:
                bow = new HeavyCrossbow();
                PackItem(new Bolt(25));
                break;
        }

        AddItem(bow);

        ControlSlots = 0;
    }

    public override bool Commandable => false;
    public override bool AlwaysMurderer => true;

    public bool TryArrest(Mobile m)
    {
        if (ControlMaster != null)
        {
            // That person is already under arrest.
            m.SendLocalizedMessage(1152247);
            return false;
        }

        if (m is PlayerMobile pm && pm.AllFollowers.Any(mob => mob is Raider))
        {
            // You already have a prisoner.
            m.SendLocalizedMessage(1152249);
            return false;
        }

        if (Hits > HitsMax / 10.0)
        {
            // That person won't sit still for it! A more aggressive approach is in order.
            m.SendLocalizedMessage(1152229);
            m.NonlocalOverheadMessage(MessageType.Regular, 0x3B2, 1152237, $"{m.Name}\t{Name}");
            return false;
        }

        _deleteTime = Core.Now + TimeSpan.FromHours(1);

        SetControlMaster(m);
        IsBonded = false;
        ControlTarget = m;
        ControlOrder = OrderType.Follow;

        // You arrest the ~1_name~. Take the criminal to the guard captain.
        m.SendLocalizedMessage(1152236, Name);
        m.NonlocalOverheadMessage(MessageType.Regular, 0x3B2, 1152238, $"{m.Name}\t{Name}");

        return true;
    }

    public override void OnThink()
    {
        base.OnThink();
        CheckDelete();
    }

    public void CheckDelete()
    {
        if (_deleteTime == DateTime.MinValue || Core.Now <= _deleteTime)
        {
            return;
        }

        if (ControlMaster?.NetState != null)
        {
            // You did not take your prisoner to the Guard Captain in time.
            ControlMaster.SendLocalizedMessage(1152248);
        }

        Delete();
    }

    public override void GenerateLoot() => AddLoot(LootPack.FilthyRich, 2);
}

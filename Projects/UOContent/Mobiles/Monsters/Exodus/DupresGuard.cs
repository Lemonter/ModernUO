using ModernUO.Serialization;
using Server.Items;
using Server.Misc;

namespace Server.Mobiles;

/// <summary>Dupre's guard, ported from ServUO
/// (Scripts/Services/Revamped Dungeons/TheExodusEncounter/Mobiles/Dupres*.cs). Three ranks, all
/// with identical numbers and only their kit differing: the squire in chain, the knight in
/// plate, the champion in gilded plate under a purple cloak.
///
/// They are innocent and cannot be made paragons — they are meant to be fought as a deliberate
/// choice, not stumbled into. Their gear is blessed and never drops; the original's SetWearable
/// is AddItem here, which is the same thing minus ServUO's pet-training plumbing.</summary>
[SerializationGenerator(0, false)]
public abstract partial class BaseDupresGuard : BaseCreature
{
    public BaseDupresGuard() : base(AIType.AI_Melee, FightMode.Aggressor, 10, 1)
    {
        Name = NameList.RandomName("male");
        Body = 0x190;
        Female = false;
        Hue = Race.Human.RandomSkinHue();

        SetStr(190, 200);
        SetDex(50, 75);
        SetInt(150, 250);

        SetHits(3900, 4100);

        SetDamage(22, 28);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 50, 70);
        SetResistance(ResistanceType.Fire, 50, 70);
        SetResistance(ResistanceType.Cold, 50, 70);
        SetResistance(ResistanceType.Poison, 50, 70);
        SetResistance(ResistanceType.Energy, 50, 70);

        SetSkill(SkillName.EvalInt, 195.0, 220.0);
        SetSkill(SkillName.Magery, 195.0, 220.0);
        SetSkill(SkillName.Meditation, 195.0, 200.0);
        SetSkill(SkillName.MagicResist, 100.0, 120.0);
        SetSkill(SkillName.Tactics, 195.0, 220.0);
        SetSkill(SkillName.Wrestling, 195.0, 220.0);

        VirtualArmor = 70;

        PackGold(400, 600);
    }

    public override bool CanBeParagon => false;
    public override Poison PoisonImmune => Poison.Lethal;
    public override int TreasureMapLevel => 5;

    public override void OnKilledBy(Mobile m)
    {
        base.OnKilledBy(m);

        if (Utility.RandomDouble() < 0.1)
        {
            ExodusChest.GiveRitualItem(m);
        }
    }

    protected void AddBlessed(Item item, int hue = -1)
    {
        item.LootType = LootType.Blessed;

        if (hue >= 0)
        {
            item.Hue = hue;
        }

        AddItem(item);
    }
}

[SerializationGenerator(0, false)]
[CorpseName("a human corpse")]
public partial class DupresSquire : BaseDupresGuard
{
    [Constructible]
    public DupresSquire()
    {
        Title = "The Squire";

        AddBlessed(new VikingSword());
        AddBlessed(new ChainChest());
        AddBlessed(new ChainLegs());
        AddBlessed(new CloseHelm());
        AddBlessed(new Boots(1));
        AddBlessed(new PlateGloves());
        AddBlessed(new MetalKiteShield(), 0x776);
        AddBlessed(new BodySash(0x794));
    }

    public override bool InitialInnocent => true;
}

[SerializationGenerator(0, false)]
[CorpseName("a human corpse")]
public partial class DupresKnight : BaseDupresGuard
{
    [Constructible]
    public DupresKnight()
    {
        Title = "The Knight";

        AddBlessed(new Longsword());
        AddBlessed(new PlateHelm());
        AddBlessed(new PlateArms());
        AddBlessed(new PlateGorget());
        AddBlessed(new PlateGloves());
        AddBlessed(new PlateLegs());
        AddBlessed(new PlateChest());
        AddBlessed(new MetalKiteShield(), 0x794);
        AddBlessed(new BodySash(0x794));
    }

    public override bool InitialInnocent => true;
}

/// <summary>The champion is the only one of the three the original does not mark innocent —
/// left as it stands.</summary>
[SerializationGenerator(0, false)]
[CorpseName("a human corpse")]
public partial class DupresChampion : BaseDupresGuard
{
    [Constructible]
    public DupresChampion()
    {
        Title = "The Champion";

        AddBlessed(new Cutlass());
        AddBlessed(new PlateHelm(), 0x8A5);
        AddBlessed(new PlateArms(), 0x8A5);
        AddBlessed(new PlateGorget(), 0x8A5);
        AddBlessed(new PlateGloves(), 0x8A5);
        AddBlessed(new PlateLegs(), 0x8A5);
        AddBlessed(new PlateChest(), 0x8A5);
        AddBlessed(new MetalKiteShield(), 0x776);
        AddBlessed(new BodySash(0x486));
        AddBlessed(new Cloak(0x486));
    }
}

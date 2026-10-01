using Server.Items;
using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Custom/GargishRouser.cs).
///
/// This port was cut back hard at first because three things it needed were missing. All three
/// have since been ported and it is whole: AIType.AI_Mystic (the original picks Mystic or Mage
/// at random), RisingColossusSpell, and the VoidManifestation it calls up — one chance in
/// twenty per summon, once per rouser, and again on its death.</summary>
[SerializationGenerator(0, false)]
public partial class GargishRouser : BaseCreature
{
    private const double ManifestChance = 0.05;

    private long _nextSummon;

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _crystalType;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _manifested;

    [Constructible]
    public GargishRouser(int crystalType = 0) : base(
        Utility.RandomBool() ? AIType.AI_Mystic : AIType.AI_Mage,
        FightMode.Closest,
        10,
        1
    )
    {
        _crystalType = crystalType;

        Race = Race.Gargoyle;
        Title = "the Gargish Rouser";

        SetStr(150);
        SetInt(150);
        SetDex(150);

        SetHits(1200, 1500);
        SetMana(700, 900);

        SetDamage(15, 19);

        if (Utility.RandomBool())
        {
            Name = NameList.RandomName("Gargoyle Male");
            Female = false;
            Body = 666;
        }
        else
        {
            Name = NameList.RandomName("Gargoyle Female");
            Female = true;
            Body = 667;
        }

        Utility.AssignRandomHair(this, true);
        if (!Female)
        {
            Utility.AssignRandomFacialHair(this, true);
        }

        Hue = Race.RandomSkinHue();

        AddImmovableItem(new GargishClothChestType1 { Hue = Utility.RandomNeutralHue() });
        AddImmovableItem(new GargishClothArmsType1 { Hue = Utility.RandomNeutralHue() });
        AddImmovableItem(new GargishClothLegsType1 { Hue = Utility.RandomNeutralHue() });
        AddImmovableItem(new GargishClothKiltType1 { Hue = Utility.RandomNeutralHue() });

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 15, 30);
        SetResistance(ResistanceType.Fire, 50, 65);
        SetResistance(ResistanceType.Cold, 50, 65);
        SetResistance(ResistanceType.Poison, 50, 65);
        SetResistance(ResistanceType.Energy, 50, 65);

        SetSkill(SkillName.MagicResist, 140.0);
        SetSkill(SkillName.Tactics, 130);
        SetSkill(SkillName.Anatomy, 0.0, 10.0);
        SetSkill(SkillName.Magery, 130.0);
        SetSkill(SkillName.EvalInt, 130.0);
        SetSkill(SkillName.Meditation, 120);
        SetSkill(SkillName.Wrestling, 90);

        SetSkill(SkillName.Necromancy, 120);
        SetSkill(SkillName.SpiritSpeak, 120);
        SetSkill(SkillName.Mysticism, 120);
        SetSkill(SkillName.Focus, 120);

        // Restored with the barding driver — these are what the CanDiscord/CanPeace/CanProvoke
        // gates below actually play with.
        SetSkill(SkillName.Musicianship, 100);
        SetSkill(SkillName.Discordance, 100);
        SetSkill(SkillName.Provocation, 100);
        SetSkill(SkillName.Peacemaking, 100);

        Fame = 12000;
        Karma = -12000;
    }

    private void AddImmovableItem(Item item)
    {
        item.LootType = LootType.Blessed;
        AddItem(item);
    }

    public override Poison PoisonImmune => Poison.Lethal;
    public override bool AlwaysMurderer => true;

    // The rousers sing their kin into a frenzy — the barding driver is in BaseCreature.OnThink.
    public override bool CanDiscord => true;
    public override bool CanPeace => true;
    public override bool CanProvoke => true;

    /// <summary>With mana to spare and room for followers it raises a colossus every half
    /// minute — but one summon in twenty tears open the Void instead, and that it only does
    /// once.</summary>
    public override void OnThink()
    {
        base.OnThink();

        if (Combatant == null || _nextSummon > Core.TickCount || Mana <= 40 || Followers + 5 > FollowersMax)
        {
            return;
        }

        if (!_manifested && Utility.RandomDouble() < ManifestChance)
        {
            Manifest(Combatant as Mobile);
            Manifested = true;
            _nextSummon = Core.TickCount + 600000;
            return;
        }

        new Spells.Mysticism.RisingColossusSpell(this).Cast();
        _nextSummon = Core.TickCount + 30000;
    }

    public override bool OnBeforeDeath()
    {
        if (Utility.RandomDouble() < ManifestChance)
        {
            Manifest(LastKiller);
        }

        return base.OnBeforeDeath();
    }

    /// <summary>Calls up a void manifestation and sets it on whoever the rouser was fighting —
    /// or on that creature's master, if it was fighting a pet.</summary>
    private void Manifest(Mobile target)
    {
        if (Map == null)
        {
            return;
        }

        if (target is BaseCreature { Summoned: true } or BaseCreature { Controlled: true })
        {
            target = ((BaseCreature)target).GetMaster();
        }

        FixedParticles(0x3709, 1, 30, 9904, 1108, 6, EffectLayer.RightFoot);

        var vm = new VoidManifestation(_crystalType);
        vm.MoveToWorld(Location, Map);
        vm.PlaySound(vm.GetAngerSound());

        if (target != null)
        {
            vm.Combatant = target;
        }
    }

    public override bool ReacquireOnMovement => true;
    public override bool AcquireOnApproach => true;
    public override int AcquireOnApproachRange => 8;

    public override WeaponAbility GetWeaponAbility()
    {
        if (Weapon is BaseWeapon weapon)
        {
            return Utility.RandomBool() ? weapon.PrimaryAbility : weapon.SecondaryAbility;
        }

        return WeaponAbility.WhirlwindAttack;
    }

    public override void GenerateLoot()
    {
        AddLoot(LootPack.UltraRich);
        AddLoot(LootPack.MedScrolls, 2);
        AddLoot(LootPack.HighScrolls, 2);
    }
}

using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Engines.Blackthorn;
using Server.Items;
using Server.Misc;
using Server.Spells;
using Server.Spells.Necromancy;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO's Blackthorn invasion spawner (InvasionSpawner/Creatures.cs) — the humanoid
///     invaders that accompany every wave: a randomly specialised fighter/mage/thief built from the same
///     stat block regardless of role, and its <see cref="InvaderCaptain" />, the blessed wave leader the
///     beacon waits on.
///
///     What this port left out, all deliberate:
///       - <c>HealChance</c>, <c>CanStealth</c> and <c>UseSmartAI</c> overrides — none of these knobs
///         exists on <see cref="BaseCreature" /> here (they belong to ServUO's AI ability profile), so
///         every invader keeps the stock AI behaviour.
///       - <c>AbilityProfile.HasAbility(SpecialAbility.Heal)</c> in <see cref="EquipSpecialty" /> — no
///         ability profiles here, so invaders no longer start with a bandage pack.
///       - <c>SetAreaEffect(AreaEffect.AuraOfEnergy)</c> on the invader is BaseCreature's built-in aura
///         (HasAura / AuraInterval / AuraRange / Aura*Damage / AuraEffect), configured for the same
///         30-second, five-tile, 20-30 energy burst.
///       - <c>CurseWeaponSpell.IsCursed</c> in <see cref="OnThink" /> — no counterpart; a sampire now
///         re-casts Curse Weapon on its own chance instead of skipping while the weapon is already cursed.
///       - <c>SetWearable(item[, hue])</c> — there is no such method; equipment goes on with
///         <c>AddItem(...)</c> and a hue in the initializer.
///       - <c>ColUtility.Free(list)</c> in <see cref="InvaderCaptain.OnDeath" /> — a ServUO pool return
///         with no equivalent; the list is simply left to the GC.
///       - ServUO's <c>AIType.AI_Ninja</c>, <c>AI_Samurai</c>, <c>AI_Paladin</c> and <c>AI_Spellweaving</c>
///         do not exist in this codebase's <see cref="AIType" />. Ninjitsu, Bushido and Chivalry map to
///         AI_Melee (the same "not a caster" behaviour the old types had) and Spellweaving maps to
///         AI_Mage.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Invader : BaseCreature
{
    private static readonly SkillName[] Specialties =
    {
        SkillName.Swords,
        SkillName.Fencing,
        SkillName.Macing,
        SkillName.Archery,
        SkillName.Magery,
        SkillName.Mysticism,
        SkillName.Spellweaving,
        SkillName.Bushido,
        SkillName.Bushido,
        SkillName.Ninjitsu,
        SkillName.Chivalry,
        SkillName.Necromancy,
        SkillName.Poisoning
    };

    [SerializableField(0)]
    private InvasionType _invasionType;

    [SerializableField(1)]
    private SkillName _specialty;

    [SerializableField(2)]
    private bool _sampire;

    private DateTime _nextSpecial;

    [Constructible]
    public Invader() : this(Server.Engines.Blackthorn.InvasionType.Dragons)
    {
    }

    public Invader(InvasionType type) : this(RandomSpecialty(), type)
    {
    }

    public Invader(SkillName specialty, InvasionType type) : base(GetAI(specialty), FightMode.Closest, 10, 1)
    {
        _specialty = specialty;
        _invasionType = type;

        if (_specialty == SkillName.Bushido && Utility.RandomBool())
        {
            _sampire = true;
        }

        Female = Utility.RandomBool();
        Name = RussianNames.RandomPerson(Female);

        SetBody();

        string title;

        if (_sampire)
        {
            title = "сампайр";
        }
        else if (specialty == SkillName.Magery)
        {
            title = "волшебник";
        }
        else
        {
            title = $"the {Skills[specialty].Info.Title}";

            if (Female && title.EndsWith("man"))
            {
                title = title[..^3] + "woman";
            }
        }

        Title = title;

        SetStr(120, 170);
        SetDex(SpellCaster ? 75 : 150);
        SetInt(SpellCaster ? 1800 : 500);

        SetHits(800, 1250);

        if (AI == AIType.AI_Melee)
        {
            SetDamage(15, 28);
        }
        else if (!SpellCaster)
        {
            SetDamage(12, 22);
        }
        else
        {
            SetDamage(8, 18);
        }

        Fame = 8000;
        Karma = -8000;

        SetResists();
        SetSkills();
        EquipSpecialty();

        _nextSpecial = Core.Now;

        if (_sampire)
        {
            Timer.DelayCall(TimeSpan.FromSeconds(1), EmbraceVampireForm);
        }
    }

    public static SkillName RandomSpecialty() => Specialties[Utility.Random(Specialties.Length)];

    public override bool AlwaysMurderer => true;

    // ServUO's SetAreaEffect(AreaEffect.AuraOfEnergy): a 30-second energy burst on everything within
    // five tiles, only while fighting. BaseCreature's built-in aura reproduces all three knobs.
    public override bool HasAura => Combatant != null;
    public override TimeSpan AuraInterval => TimeSpan.FromSeconds(30);
    public override int AuraRange => 5;
    public override int AuraBaseDamage => Utility.RandomMinMax(20, 30);
    public override int AuraFireDamage => 0;
    public override int AuraEnergyDamage => 100;

    public override void AuraEffect(Mobile m)
    {
        m.SendLocalizedMessage(1072073, false, Name); //  : The creature's aura of energy is damaging you!
        m.FixedParticles(0x374A, 10, 30, 5052, 1278, 0, EffectLayer.Waist);
        m.PlaySound(0x51D);
    }

    public override double WeaponAbilityChance => AI == AIType.AI_Melee ? 0.4 : 0.1;

    public virtual bool CanDoSpecial => SpellCaster;

    public virtual double MinSkill => 100.0;

    public virtual double MaxSkill => 120.0;

    public virtual int MinResist => 10;

    public virtual int MaxResist => 20;

    public bool SpellCaster => AI != AIType.AI_Melee;

    public override WeaponAbility GetWeaponAbility()
    {
        if (Weapon is BaseWeapon weapon)
        {
            return 0.6 > Utility.RandomDouble() ? weapon.PrimaryAbility : weapon.SecondaryAbility;
        }

        return null;
    }

    public static AIType GetAI(SkillName skill) => skill switch
    {
        SkillName.Magery         => AIType.AI_Mage,
        SkillName.Necromancy     => AIType.AI_NecroMage,
        SkillName.Mysticism      => AIType.AI_Mystic,
        SkillName.Spellweaving   => AIType.AI_Mage,
        SkillName.Ninjitsu       => AIType.AI_Melee,
        SkillName.Bushido        => AIType.AI_Melee,
        SkillName.Chivalry       => AIType.AI_Melee,
        _                        => AIType.AI_Melee
    };

    public virtual void SetBody()
    {
        switch (_specialty)
        {
            default:
                Race = 0.75 > Utility.RandomDouble() ? Race.Human : Race.Elf;
                break;
            case SkillName.Archery:
            case SkillName.Spellweaving:
                Race = Race.Elf;
                break;
        }

        HairItemID = Race.RandomHair(this);
        HairHue = Race.RandomHairHue();

        FacialHairItemID = Race.RandomFacialHair(this);
        FacialHairHue = Race.RandomHairHue();

        Hue = Race.RandomSkinHue();
    }

    public virtual void SetResists()
    {
        SetResistance(ResistanceType.Physical, MinResist, MaxResist);
        SetResistance(ResistanceType.Fire, MinResist, MaxResist);
        SetResistance(ResistanceType.Cold, MinResist, MaxResist);
        SetResistance(ResistanceType.Poison, MinResist, MaxResist);
        SetResistance(ResistanceType.Energy, MinResist, MaxResist);
    }

    public virtual void SetSkills()
    {
        SetSkill(SkillName.Fencing, MinSkill, MaxSkill);
        SetSkill(SkillName.Macing, MinSkill, MaxSkill);
        SetSkill(SkillName.MagicResist, MinSkill, MaxSkill);
        SetSkill(SkillName.Swords, MinSkill, MaxSkill);
        SetSkill(SkillName.Tactics, MinSkill, MaxSkill);
        SetSkill(SkillName.Wrestling, MinSkill, MaxSkill);
        SetSkill(SkillName.Archery, MinSkill, MaxSkill);
        SetSkill(SkillName.Parry, MinSkill, MaxSkill);

        if (SpellCaster)
        {
            SetSkill(SkillName.Magery, MinSkill, MaxSkill);
            SetSkill(SkillName.EvalInt, MinSkill, MaxSkill);
        }

        if (Skills[_specialty].Base < MinSkill)
        {
            SetSkill(_specialty, MinSkill, MaxSkill);
        }

        if (_sampire)
        {
            SetSkill(SkillName.Necromancy, MinSkill, MaxSkill);
        }

        if (_sampire || _specialty == SkillName.Necromancy)
        {
            SetSkill(SkillName.SpiritSpeak, MinSkill, MaxSkill);
        }

        if (_specialty == SkillName.Ninjitsu)
        {
            SetSkill(SkillName.Hiding, 100);
            SetSkill(SkillName.Stealth, 100);
        }
    }

    public virtual void EquipSpecialty()
    {
        AddItem(new ThighBoots());
        AddItem(new BodySash { Hue = Utility.RandomSlimeHue() });

        switch (_specialty)
        {
            case SkillName.Chivalry:
                AddItem(RandomSwordWeapon());
                PaladinEquip();
                break;
            case SkillName.Swords:
                AddItem(RandomSwordWeapon());
                StandardMeleeEquip();
                break;
            case SkillName.Fencing:
                AddItem(RandomFencingWeapon());
                StandardMeleeEquip();
                break;
            case SkillName.Macing:
                AddItem(RandomMaceWeapon());
                StandardMeleeEquip();
                break;
            case SkillName.Archery:
                AddItem(RandomArcheryWeapon());
                StandardMeleeEquip();
                break;
            case SkillName.Magery:
            case SkillName.Mysticism:
            case SkillName.Spellweaving:
            case SkillName.Necromancy:
                AddItem(RandomMageWeapon());
                StandardMageEquip();
                break;
            case SkillName.Bushido:
                var samuraiWeapon = RandomSamuraiWeapon() as BaseWeapon;

                if (samuraiWeapon != null)
                {
                    AddItem(samuraiWeapon);

                    if (_sampire)
                    {
                        samuraiWeapon.WeaponAttributes.HitLeechHits = 100;
                    }
                }

                AddItem(new LeatherSuneate());
                AddItem(new LeatherJingasa());
                AddItem(new LeatherDo());
                AddItem(new LeatherHiroSode());
                AddItem(new SamuraiTabi(Utility.RandomNondyedHue()));

                SetSkill(SkillName.Parry, 120);
                break;
            case SkillName.Ninjitsu:
                AddItem(RandomNinjaWeapon());

                var belt = new LeatherNinjaBelt
                {
                    UsesRemaining = 20,
                    Poison = Poison.Greater,
                    PoisonCharges = 20
                };
                AddItem(belt);

                for (var i = 0; i < 2; i++)
                {
                    var fukiya = new Fukiya
                    {
                        UsesRemaining = 10,
                        Poison = Poison.Greater,
                        PoisonCharges = 10,
                        Movable = false
                    };
                    PackItem(fukiya);
                }

                AddItem(new NinjaTabi());
                AddItem(new LeatherNinjaJacket());
                AddItem(new LeatherNinjaHood());
                AddItem(new LeatherNinjaPants());
                AddItem(new LeatherNinjaMitts());
                break;
            case SkillName.Poisoning:
                var assassinWeapon = RandomAssassinWeapon() as BaseWeapon;

                if (assassinWeapon != null)
                {
                    assassinWeapon.Poison = Poison.Lethal;
                    assassinWeapon.PoisonCharges = 100;

                    AddItem(assassinWeapon);
                }

                AddItem(new LeatherChest());
                AddItem(new LeatherLegs());
                AddItem(new LeatherGloves());
                AddItem(new LeatherGorget());
                break;
        }
    }

    private void PaladinEquip()
    {
        AddHuedItem(Loot.Construct(new[] { typeof(Bascinet), typeof(Helmet), typeof(PlateHelm) }), 1153);

        AddItem(new PlateChest());
        AddItem(new PlateLegs());
        AddItem(new PlateGloves());
        AddItem(new PlateGorget());
        AddItem(new PlateArms());
        AddItem(new MetalKiteShield());

        SetSkill(SkillName.Parry, 120);
    }

    /// <summary>ServUO's <c>SetWearable(item, hue)</c>: put the item on with a fixed hue.</summary>
    private void AddHuedItem(Item item, int hue)
    {
        if (item == null)
        {
            return;
        }

        item.Hue = hue;

        AddItem(item);
    }

    private void StandardMeleeEquip()
    {
        AddItem(Loot.Construct(new[] { typeof(Bascinet), typeof(Helmet), typeof(LeatherCap), typeof(RoyalCirclet) }));

        AddItem(new ChainChest());
        AddItem(new ChainLegs());
        AddItem(new RingmailGloves());
        AddItem(new LeatherGorget());
    }

    private void StandardMageEquip()
    {
        var mage = AI == AIType.AI_Mage;

        AddItem(new WizardsHat { Hue = mage ? Utility.RandomBlueHue() : Utility.RandomRedHue() });
        AddItem(new Robe { Hue = mage ? Utility.RandomBlueHue() : Utility.RandomRedHue() });
        AddItem(new LeatherGloves());
    }

    public Item RandomSwordWeapon()
    {
        if (Race == Race.Elf)
        {
            return Loot.Construct(new[] { typeof(ElvenMachete), typeof(RadiantScimitar) });
        }

        return Loot.Construct(
            new[] { typeof(Broadsword), typeof(Longsword), typeof(Katana), typeof(Halberd), typeof(Bardiche), typeof(VikingSword) }
        );
    }

    public Item RandomFencingWeapon()
    {
        if (Race == Race.Elf)
        {
            return Loot.Construct(new[] { typeof(Leafblade), typeof(WarCleaver), typeof(AssassinSpike) });
        }

        return Loot.Construct(new[] { typeof(Kryss), typeof(Spear), typeof(ShortSpear), typeof(Lance), typeof(Pike) });
    }

    public Item RandomMaceWeapon() => Loot.Construct(
        new[]
        {
            typeof(Mace), typeof(WarHammer), typeof(WarAxe), typeof(BlackStaff), typeof(QuarterStaff), typeof(WarMace),
            typeof(DiamondMace), typeof(Scepter)
        }
    );

    public Item RandomArcheryWeapon()
    {
        if (Race == Race.Elf)
        {
            return Loot.Construct(new[] { typeof(MagicalShortbow), typeof(ElvenCompositeLongbow) });
        }

        return Loot.Construct(
            new[] { typeof(Bow), typeof(Crossbow), typeof(HeavyCrossbow), typeof(CompositeBow), typeof(RepeatingCrossbow) }
        );
    }

    public Item RandomMageWeapon() => Loot.Construct(
        new[] { typeof(Spellbook), typeof(GnarledStaff), typeof(BlackStaff), typeof(QuarterStaff), typeof(WildStaff) }
    );

    public Item RandomSamuraiWeapon() => Loot.Construct(new[] { typeof(Lajatang), typeof(Wakizashi), typeof(NoDachi) });

    public Item RandomNinjaWeapon() => Loot.Construct(
        new[] { typeof(Wakizashi), typeof(Tessen), typeof(Nunchaku), typeof(Daisho), typeof(Sai), typeof(Tekagi), typeof(Kama), typeof(Katana) }
    );

    public Item RandomAssassinWeapon() => Loot.Construct(
        new[] { typeof(Cleaver), typeof(ButcherKnife), typeof(Kryss), typeof(Dagger) }
    );

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1154196 + (int)_invasionType);
    }

    public override void OnThink()
    {
        base.OnThink();

        if (Combatant == null)
        {
            return;
        }

        if (CanDoSpecial && InRange(Combatant, 4) && 0.1 > Utility.RandomDouble() && _nextSpecial < Core.Now)
        {
            DoSpecial();

            _nextSpecial = Core.Now + TimeSpan.FromSeconds(Utility.RandomMinMax(30, 60));
        }
        else if (_sampire)
        {
            if (0.1 > Utility.RandomDouble() && Weapon is BaseWeapon)
            {
                new CurseWeaponSpell(this).Cast();
            }
            else if (!TransformationSpellHelper.UnderTransformation(this, typeof(VampiricEmbraceSpell)))
            {
                new VampiricEmbraceSpell(this).Cast();
            }
        }
    }

    private void EmbraceVampireForm()
    {
        if (Deleted || !Alive)
        {
            return;
        }

        new VampiricEmbraceSpell(this).Cast();
    }

    private void DoSpecial()
    {
        var map = Map;

        if (map == null || map == Map.Internal)
        {
            return;
        }

        for (var i = 0; i < 4; i++)
        {
            var radius = i;

            Timer.DelayCall(
                TimeSpan.FromMilliseconds(i * 50),
                () => Geometry.Circle2D(
                    Location,
                    map,
                    radius,
                    (pnt, effectMap) => Effects.SendLocationEffect(
                        pnt,
                        effectMap,
                        Utility.RandomBool() ? 14000 : 14013,
                        14,
                        20,
                        2018,
                        0
                    )
                )
            );
        }

        Timer.DelayCall(TimeSpan.FromMilliseconds(200), ApplySpecialDamage);
    }

    private void ApplySpecialDamage()
    {
        var map = Map;

        if (map == null)
        {
            return;
        }

        var victims = new List<Mobile>();

        foreach (var mob in map.GetMobilesInRange(Location, 4))
        {
            if (mob.AccessLevel > AccessLevel.Player)
            {
                continue;
            }

            if (mob is PlayerMobile ||
                (mob is BaseCreature creature && creature.GetMaster() is PlayerMobile && CanBeHarmful(mob)))
            {
                victims.Add(mob);
            }
        }

        for (var i = 0; i < victims.Count; i++)
        {
            AOS.Damage(victims[i], this, Utility.RandomMinMax(80, 90), 0, 0, 0, 0, 0, 100);
        }
    }

    public override void GenerateLoot()
    {
        AddLoot(LootPack.UltraRich, 2);
    }
}

/// <summary>
///     The blessed wave leader. ServUO dropped two artifacts from the top damagers on death; that part is
///     unchanged apart from <c>ColUtility.Free</c> (no equivalent here) and the <c>GetLootingRights</c>
///     form — this codebase's is the static
///     <see cref="BaseCreature.GetLootingRights(in ValueLinkList{DamageEntry}, int)" />, not an instance
///     method.
/// </summary>
[SerializationGenerator(0, false)]
public partial class InvaderCaptain : Invader
{
    [Constructible]
    public InvaderCaptain() : this(Server.Engines.Blackthorn.InvasionType.Dragons)
    {
    }

    public InvaderCaptain(InvasionType type) : base(type)
    {
        SetStr(250);
        SetDex(SpellCaster ? 150 : 200);
        SetInt(SpellCaster ? 1000 : 5000);

        SetHits(8000, 12000);

        if (AI == AIType.AI_Melee)
        {
            SetDamage(22, 30);
        }
        else if (!SpellCaster)
        {
            SetDamage(20, 28);
        }
        else
        {
            SetDamage(10, 20);
        }

        Fame = 48000;
        Karma = -48000;
    }

    public override double MinSkill => 105.0;

    public override double MaxSkill => 130.0;

    public override int MinResist => 20;

    public override int MaxResist => 30;

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        var rights = GetLootingRights(DamageEntries, HitsMax);
        rights.Sort();

        var damagers = new List<Mobile>();

        for (var i = 0; i < rights.Count; i++)
        {
            var m = rights[i].m_Mobile;

            if (m != null && m.InRange(c.Location, 20))
            {
                damagers.Add(m);
            }
        }

        if (damagers.Count == 0)
        {
            return;
        }

        for (var i = 0; i < 2; i++)
        {
            var item = InvasionController.CreateItem(damagers[0]);

            if (item == null)
            {
                continue;
            }

            var drop = damagers.Count == 1 || i >= damagers.Count ? damagers[0] : damagers[i];

            drop.SendLocalizedMessage(1154530); // You notice the crest of Minax on your fallen foe's equipment and decide it may be of some value...

            if (drop.Backpack == null || !drop.Backpack.TryDropItem(drop, item, false))
            {
                drop.BankBox?.DropItem(item);
                drop.SendLocalizedMessage(1079730); // The item has been placed into your bank box.
            }
        }
    }

    public override void GenerateLoot()
    {
        AddLoot(LootPack.UltraRich, 2);
        AddLoot(LootPack.SuperBoss, 1);
    }
}

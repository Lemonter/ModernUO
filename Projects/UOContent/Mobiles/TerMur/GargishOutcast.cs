using Server.Items;
using Server.Spells;
using Server.Spells.Mysticism;
using System;
using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Custom/GargishOutcast.cs). AIType.AI_Mystic
/// doesn't exist here — the original's 50/50 AI_Mystic-vs-AI_Mage dynamic switch (via
/// ChangeAIType, which does exist) is simplified to always run AI_Mage, keeping the 50/50
/// Necromancy-vs-Mysticism skill-flavor branch. GargishClothChest/Arms/Legs/Kilt map to this
/// codebase's Type1 variants (see MantleOfTheFallen.cs in the Citadel port for the same
/// split). SetWearable converted to a local AddImmovableItem helper using AddItem.
/// PrimaryAbility/SecondaryAbility confirmed to exist on BaseWeapon.</summary>
[SerializationGenerator(0, false)]
public partial class GargishOutcast : BaseCreature
{
    [SerializableField(0)]
    private DateTime _nextSummon;

    [Constructible]
    public GargishOutcast() : base(AIType.AI_Mage, FightMode.Closest, 10, 1)
    {
        Race = Race.Gargoyle;
        Title = "the Gargish Outcast";

        SetStr(150);
        SetInt(150);
        SetDex(150);

        SetHits(1000, 1200);
        SetMana(450, 600);

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

        BaseWeapon wep = Utility.Random(3) switch
        {
            0 => new Cyclone(),
            1 => new SoulGlaive(),
            _ => new Boomerang()
        };

        wep.Attributes.SpellChanneling = 1;
        AddImmovableItem(wep);
        AddImmovableItem(new GargishClothChestType1 { Hue = Utility.RandomNeutralHue() });
        AddImmovableItem(new GargishClothArmsType1 { Hue = Utility.RandomNeutralHue() });
        AddImmovableItem(new GargishClothLegsType1 { Hue = Utility.RandomNeutralHue() });
        AddImmovableItem(new GargishClothKiltType1 { Hue = Utility.RandomNeutralHue() });

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 10, 25);
        SetResistance(ResistanceType.Fire, 40, 65);
        SetResistance(ResistanceType.Cold, 40, 65);
        SetResistance(ResistanceType.Poison, 40, 65);
        SetResistance(ResistanceType.Energy, 40, 65);

        SetSkill(SkillName.MagicResist, 120.0);
        SetSkill(SkillName.Tactics, 50.1, 60.0);
        SetSkill(SkillName.Throwing, 120.0);
        SetSkill(SkillName.Anatomy, 0.0, 10.0);
        SetSkill(SkillName.Magery, 50.0, 80.0);
        SetSkill(SkillName.EvalInt, 50.0, 80.0);
        SetSkill(SkillName.Meditation, 120);

        Fame = 12000;
        Karma = -12000;

        if (0.5 > Utility.RandomDouble())
        {
            SetSkill(SkillName.Necromancy, 90, 105);
            SetSkill(SkillName.SpiritSpeak, 90, 105);
        }
        else
        {
            SetSkill(SkillName.Mysticism, 90, 105);
            SetSkill(SkillName.Focus, 90, 105);
        }

        _nextSummon = DateTime.UtcNow;
    }

    private void AddImmovableItem(Item item)
    {
        item.LootType = LootType.Blessed;
        AddItem(item);
    }

    public override Poison PoisonImmune => Poison.Deadly;
    public override bool AlwaysMurderer => true;
    public override bool ReacquireOnMovement => true;
    public override TimeSpan AcquireOnApproachDelay => TimeSpan.Zero;
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

    public override void OnThink()
    {
        base.OnThink();

        if (Combatant == null || _nextSummon > DateTime.UtcNow)
        {
            return;
        }

        if (Mana > 40 && Followers + 4 <= FollowersMax)
        {
            var spell = new AnimatedWeaponSpell(this, null);
            spell.Cast();
            _nextSummon = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        }
    }
}

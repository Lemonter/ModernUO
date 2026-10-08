using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Collections;
using Server.Items;
using Server.Spells.Ninjitsu;

namespace Server.Mobiles;

/// <summary>The Citadel peerless. Ported from ServUO (Scripts/Mobiles/Bosses/Travesty.cs).
///
/// Its signature is that it steals a face: at 25% per damage event it copies a nearby player
/// whole — body, hue, name, gender, hair, and a mimicry of their equipment — and fights the
/// way that player fights for a minute.
///
/// Adaptations, all of them about AI types this codebase doesn't have (see
/// Mobiles/AI/BaseAI/AIType.cs, whose roster is Melee/Animal/Archer/Healer/Vendor/Mage/
/// Berserk/Predator/Thief plus the Mystic one added with Rising Colossus):
///   - Necromancy maps to AI_NecroMage (Mobiles/AI/NecroMageAI.cs, ported later) and
///     Spellweaving to AI_Mage; there is no spellweaver AI here.
///   - Ninjitsu and Bushido map to AI_Melee; there is no ninja or samurai AI here.
///   - Mysticism maps to AI_Mystic, Archery to AI_Archer, the weapon skills to AI_Melee, all
///     as in the original.
///
/// The CanDiscord / CanPeace / CanProvoke flags below turn on exactly when the original turns
/// them on, and the barding driver they feed lives in BaseCreature.OnThink, ported alongside
/// this — so a Travesty wearing a bard's face really does play at you.</summary>
[SerializationGenerator(0, false)]
public partial class Travesty : BasePeerless
{
    private static readonly Point3D[] _warpLocations =
    {
        new(84, 1954, 0),
        new(80, 1964, 0),
        new(80, 1949, 0),
        new(92, 1948, 0),
        new(92, 1962, 0),
        new(86, 1955, 0),
        new(88, 1958, 0)
    };

    private bool _canDiscord;
    private bool _canPeace;
    private bool _canProvoke;

    private long _nextBodyChange;
    private long _nextMirrorImage;
    private bool _spawnedHelpers;

    private TimerExecutionToken _restoreToken;
    private List<Item> _clonedItems;

    [Constructible]
    public Travesty() : base(AIType.AI_Mage, FightMode.Closest, 10, 1)
    {
        ActiveSpeed = 0.2;
        PassiveSpeed = 0.4;

        Name = "Travesty";
        Body = 0x108;
        BaseSoundID = 0x46E;

        SetStr(900, 950);
        SetDex(900, 950);
        SetInt(900, 950);

        SetHits(35000);

        SetDamage(11, 18);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 50, 70);
        SetResistance(ResistanceType.Fire, 50, 70);
        SetResistance(ResistanceType.Cold, 50, 70);
        SetResistance(ResistanceType.Poison, 50, 70);
        SetResistance(ResistanceType.Energy, 50, 70);

        SetSkill(SkillName.Wrestling, 300.0, 320.0);
        SetSkill(SkillName.Tactics, 100.0, 120.0);
        SetSkill(SkillName.MagicResist, 100.0, 120.0);
        SetSkill(SkillName.Anatomy, 100.0, 120.0);
        SetSkill(SkillName.Healing, 100.0, 120.0);
        SetSkill(SkillName.Poisoning, 100.0, 120.0);
        SetSkill(SkillName.DetectHidden, 100.0);
        SetSkill(SkillName.Hiding, 100.0);
        SetSkill(SkillName.Parry, 100.0, 110.0);
        SetSkill(SkillName.Magery, 100.0, 120.0);
        SetSkill(SkillName.EvalInt, 100.0, 120.0);
        SetSkill(SkillName.Meditation, 100.0, 120.0);
        SetSkill(SkillName.Necromancy, 100.0, 120.0);
        SetSkill(SkillName.SpiritSpeak, 100.0, 120.0);
        SetSkill(SkillName.Focus, 100.0, 120.0);
        SetSkill(SkillName.Spellweaving, 100.0, 120.0);
        SetSkill(SkillName.Discordance, 100.0, 120.0);
        SetSkill(SkillName.Bushido, 100.0, 120.0);
        SetSkill(SkillName.Ninjitsu, 100.0, 120.0);
        SetSkill(SkillName.Chivalry, 100.0, 120.0);
        SetSkill(SkillName.Musicianship, 100.0, 120.0);
        SetSkill(SkillName.Provocation, 100.0, 120.0);
        SetSkill(SkillName.Peacemaking, 100.0, 120.0);

        Fame = 30000;
        Karma = -30000;

        _nextBodyChange = Core.TickCount;
        _nextMirrorImage = Core.TickCount;
        _clonedItems = new List<Item>();
    }

    public override string CorpseName => "a travesty corpse";

    public override bool ShowFameTitle => false;
    public override bool AlwaysAttackable => true;

    public override bool CanSpawnHelpers => true;
    public override int MaxHelpersWaves => 1;

    public override bool CanDiscord => _canDiscord;
    public override bool CanPeace => _canPeace;
    public override bool CanProvoke => _canProvoke;

    public override double WeaponAbilityChance => BodyMod != 0 ? base.WeaponAbilityChance : 0.1;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.SuperBoss, 8);
        AddLoot(LootPack.ArcanistScrolls, Utility.RandomMinMax(1, 6));
        AddLoot(LootPack.PeerlessResource, 8);
        AddLoot(LootPack.Talisman, 5);
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        c.DropItem(new EyeOfTheTravesty());
        c.DropItem(new OrdersFromMinax());

        // One of the three keepsakes, always.
        Item keepsake = Utility.Random(3) switch
        {
            0 => new TravestysSushiPreparations(),
            1 => new TravestysFineTeakwoodTray(),
            _ => new TravestysCollectionOfShells()
        };

        c.DropItem(keepsake);
    }

    public override void OnDamage(int amount, Mobile from, bool willKill)
    {
        if (_nextMirrorImage <= Core.TickCount && Utility.RandomDouble() < 0.1)
        {
            DoMirrorImage();
        }

        if (_nextBodyChange <= Core.TickCount && Utility.RandomDouble() < 0.25)
        {
            ChangeBody();
        }

        base.OnDamage(amount, from, willKill);
    }

    private void DoMirrorImage()
    {
        if (Map == null)
        {
            return;
        }

        new MirrorImage(this, null).Cast();

        _nextMirrorImage = Core.TickCount + Utility.RandomMinMax(20000, 45000);
    }

    #region Body theft

    /// <summary>Picks a player within five tiles and becomes them for a minute.</summary>
    private void ChangeBody()
    {
        if (BodyMod != 0 || Map == null)
        {
            return;
        }

        using var candidates = PooledRefList<Mobile>.Create();
        foreach (var m in Map.GetMobilesInRange<PlayerMobile>(Location, 5))
        {
            if (m.Alive && m.AccessLevel == AccessLevel.Player)
            {
                candidates.Add(m);
            }
        }

        if (candidates.Count == 0)
        {
            return;
        }

        var target = candidates[Utility.Random(candidates.Count)];

        BodyMod = target.Body;
        HueMod = target.Hue;
        NameMod = target.Name;
        Female = target.Female;
        Title = target.Title;

        CloneEquipment(target);
        AdoptFightingStyle(target);

        _restoreToken.Cancel();
        Timer.StartTimer(TimeSpan.FromMinutes(1), RestoreBody, out _restoreToken);

        _nextBodyChange = Core.TickCount + 60000;
    }

    /// <summary>Copies the look of what the victim is wearing. Weapons try for a real crafted
    /// copy first so the mimicry actually swings; everything else is a hollow lookalike.</summary>
    private void CloneEquipment(Mobile target)
    {
        foreach (var item in target.Items)
        {
            if (item.Layer is Layer.Backpack or Layer.Mount or Layer.Bank or Layer.Hair
                or Layer.FacialHair)
            {
                continue;
            }

            Item clone = null;

            if (item is BaseWeapon)
            {
                clone = item.GetType().CreateInstance<Item>();

                if (clone != null)
                {
                    clone.Hue = item.Hue;
                    clone.Name = item.Name;
                }
            }

            clone ??= new ClonedItem(item);

            _clonedItems.Add(clone);
            AddItem(clone);
        }

        // Hair and beard are copied as appearance, not as items.
        HairItemID = target.HairItemID;
        HairHue = target.HairHue;
        FacialHairItemID = target.FacialHairItemID;
        FacialHairHue = target.FacialHairHue;
    }

    /// <summary>Fights the way the copied player fights, as far as the AI roster here allows.</summary>
    private void AdoptFightingStyle(Mobile target)
    {
        var skills = target.Skills;

        var ai = AIType.AI_Mage;

        if (skills[SkillName.Swords].Value >= 50.0 || skills[SkillName.Fencing].Value >= 50.0 ||
            skills[SkillName.Macing].Value >= 50.0)
        {
            ai = AIType.AI_Melee;
        }
        else if (skills[SkillName.Archery].Value >= 50.0)
        {
            ai = AIType.AI_Archer;
        }
        else if (skills[SkillName.Mysticism].Value >= 50.0)
        {
            ai = AIType.AI_Mystic;
        }
        else if (skills[SkillName.Necromancy].Value >= 50.0)
        {
            ai = AIType.AI_NecroMage;
        }
        else if (skills[SkillName.Ninjitsu].Value >= 50.0 || skills[SkillName.Bushido].Value >= 50.0)
        {
            // No ninja or samurai AI here; both fight in melee.
            ai = AIType.AI_Melee;
        }

        ChangeAIType(ai);

        _canDiscord = skills[SkillName.Discordance].Base > 50.0;
        _canPeace = skills[SkillName.Peacemaking].Base > 50.0;
        _canProvoke = skills[SkillName.Provocation].Base > 50.0;
    }

    private void RestoreBody()
    {
        _restoreToken.Cancel();

        BodyMod = 0;
        HueMod = -1;
        NameMod = null;
        Title = null;

        HairItemID = 0;
        FacialHairItemID = 0;

        _canDiscord = false;
        _canPeace = false;
        _canProvoke = false;

        for (var i = _clonedItems.Count - 1; i >= 0; i--)
        {
            _clonedItems[i]?.Delete();
        }

        _clonedItems.Clear();

        ChangeAIType(AIType.AI_Mage);
    }

    #endregion

    /// <summary>The one wave comes when it drops under two thousand hits, and only once.</summary>
    public override bool CanSpawnWave()
    {
        if (Hits > 2000)
        {
            _spawnedHelpers = false;
            return false;
        }

        if (_spawnedHelpers)
        {
            return false;
        }

        _spawnedHelpers = true;
        return true;
    }

    public override void SpawnHelpers()
    {
        var map = Map;

        if (map == null)
        {
            return;
        }

        SpawnNinjaGroup(80, 1964, 0);
        SpawnNinjaGroup(80, 1949, 0);
        SpawnNinjaGroup(92, 1948, 0);
        SpawnNinjaGroup(92, 1962, 0);

        // And it steps away from whoever was hitting it.
        MoveToWorld(_warpLocations.RandomElement(), map);
    }

    private void SpawnNinjaGroup(int x, int y, int z)
    {
        SpawnHelper(new DragonsFlameMage(), x, y, z);
        SpawnHelper(new SerpentsFangAssassin(), x, y, z);
        SpawnHelper(new TigersClawThief(), x, y, z);
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        _restoreToken.Cancel();
        _clonedItems?.Clear();
    }
}

/// <summary>A hollow copy of a player's gear, worn by Travesty while it is wearing their face.
/// Ported from ServUO, where it is an inner class of Travesty.cs. It has the look and nothing
/// else — and it goes away rather than becoming loot if it ends up off the corpse.</summary>
[SerializationGenerator(0, false)]
public partial class ClonedItem : Item
{
    [Constructible]
    public ClonedItem(Item item = null) : base(item?.ItemID ?? 0x1F03)
    {
        if (item == null)
        {
            return;
        }

        Name = item.Name;
        Weight = item.Weight;
        Hue = item.Hue;
        Layer = item.Layer;
    }

    public override bool OnDroppedInto(Mobile from, Container target, Point3D p)
    {
        Delete();
        return false;
    }
}

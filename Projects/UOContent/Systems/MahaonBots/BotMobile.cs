using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

public enum BotArchetype
{
    Warrior,
    Mage,
    Archer,
    Crafter,
    Trader
}

/// <summary>
///     A "fake player" for the Mahaon bot population: a real PlayerMobile (full skills,
///     stats, inventory, combat) but with no Account/NetState — driven entirely by
///     <see cref="Server.Systems.MahaonBots.BotController" /> instead of a client.
/// </summary>
[SerializationGenerator(0, false)]
public partial class BotMobile : PlayerMobile
{
    [SerializableField(0)]
    private BotArchetype _archetype;

    [SerializableField(1)]
    private bool _isPk;

    // Shared memory — every bot reads and writes the SAME pool of known spots, keyed by
    // map. One bot finding a good mine/tree/fish/hunting spot means every other bot
    // benefits immediately, instead of each one rediscovering the world from scratch.
    // Deliberately not [SerializableField] — in-memory only, same as the rest of the bot
    // system; a fresh pool after a restart is cheap to rebuild.
    private static readonly Dictionary<Map, List<Point3D>> SharedMineSpots = new();
    private static readonly Dictionary<Map, List<Point3D>> SharedTreeSpots = new();
    private static readonly Dictionary<Map, List<Point3D>> SharedFishSpots = new();
    private static readonly Dictionary<Map, List<Point3D>> SharedHuntingSpots = new();

    private const int MaxKnownSpots = 5000;

    public void RememberMineSpot(Point3D loc) => Remember(SharedMineSpots, Map, loc);
    public void RememberTreeSpot(Point3D loc) => Remember(SharedTreeSpots, Map, loc);
    public void RememberFishSpot(Point3D loc) => Remember(SharedFishSpots, Map, loc);
    public void RememberHuntingSpot(Point3D loc) => Remember(SharedHuntingSpots, Map, loc);

    public Point3D? PickKnownMineSpot() => PickKnown(SharedMineSpots, Map);
    public Point3D? PickKnownTreeSpot() => PickKnown(SharedTreeSpots, Map);
    public Point3D? PickKnownFishSpot() => PickKnown(SharedFishSpots, Map);
    public Point3D? PickKnownHuntingSpot() => PickKnown(SharedHuntingSpots, Map);

    private static void Remember(Dictionary<Map, List<Point3D>> pool, Map map, Point3D loc)
    {
        if (map == null)
        {
            return;
        }

        if (!pool.TryGetValue(map, out var list))
        {
            list = new List<Point3D>();
            pool[map] = list;
        }

        if (list.Contains(loc))
        {
            return;
        }

        list.Add(loc);

        if (list.Count > MaxKnownSpots)
        {
            list.RemoveAt(0);
        }
    }

    private static Point3D? PickKnown(Dictionary<Map, List<Point3D>> pool, Map map)
    {
        if (map == null || !pool.TryGetValue(map, out var list) || list.Count == 0)
        {
            return null;
        }

        return list[Utility.Random(list.Count)];
    }

    [Constructible]
    public BotMobile()
    {
    }

    public static BotMobile Create(string name, Point3D location, Map map, BotArchetype? archetype = null, bool forceThief = false)
    {
        var type = archetype ?? Utility.RandomList(
            BotArchetype.Warrior, BotArchetype.Mage, BotArchetype.Archer, BotArchetype.Crafter
        );

        var bot = new BotMobile
        {
            Name = name,
            Female = Utility.RandomBool(),
            Hue = Race.Human.RandomSkinHue(),
            Player = true,
            _archetype = type,
            _isPk = Utility.RandomDouble() < 0.12
        };

        bot.Body = bot.Female ? 0x191 : 0x190;

        bot.AddItem(new Backpack());
        bot.Backpack.DropItem(new Pickaxe());
        bot.Backpack.DropItem(new Hatchet());
        bot.Backpack.DropItem(new FishingPole());
        ApplyArchetype(bot, type);
        AssignProfession(bot, type, forceThief);
        AdaptWeaponToProfession(bot, type);
        GiveInstrumentIfBard(bot);
        Systems.MahaonSoulStones.TattooSystem.ApplyTattoo(bot, Systems.MahaonSoulStones.TattooType.SoulCatcher, TimeSpan.FromDays(3650));

        bot.Hits = bot.HitsMax;
        bot.Stam = bot.StamMax;
        bot.Mana = bot.ManaMax;

        bot.MoveToWorld(location, map);

        return bot;
    }

    private static void AssignProfession(BotMobile bot, BotArchetype type, bool forceThief = false)
    {
        var category = type switch
        {
            BotArchetype.Warrior => Systems.MahaonProfessions.ProfessionCategory.Warrior,
            BotArchetype.Mage    => Systems.MahaonProfessions.ProfessionCategory.Magic,
            BotArchetype.Archer  => Systems.MahaonProfessions.ProfessionCategory.Ranger,
            BotArchetype.Crafter => Systems.MahaonProfessions.ProfessionCategory.Craft,
            BotArchetype.Trader  => forceThief || Utility.RandomBool()
                ? Systems.MahaonProfessions.ProfessionCategory.Thief
                : Systems.MahaonProfessions.ProfessionCategory.Bard,
            _ => Systems.MahaonProfessions.ProfessionCategory.Warrior
        };

        var options = Systems.MahaonProfessions.ProfessionData.InCategory(category);
        var profession = options[Utility.Random(options.Count)];

        Systems.MahaonProfessions.ProfessionSystem.SetProfession(bot, profession);
    }

    /// <summary>Re-equips fresh basic gear and tools after resurrection — same gear a
    /// fresh bot of this archetype would start with.</summary>
    public static void RegearAfterDeath(BotMobile bot)
    {
        ApplyArchetype(bot, bot.Archetype);

        if (bot.Backpack?.FindItemByType<BaseInstrument>() == null)
        {
            GiveInstrumentIfBard(bot); // corpse got looted — replace it if this bot actually needs one
        }

        // Whatever's piled up in the bank from past deaths might beat the fresh roll —
        // check and upgrade. This is what makes repeated deaths a net gain over time
        // instead of a pure loss.
        var bank = bot.FindBankNoCreate();
        if (bank == null)
        {
            return;
        }

        foreach (var item in new List<Item>(bank.Items))
        {
            if (item is BaseArmor or BaseWeapon)
            {
                Systems.MahaonBots.BotController.TryEquipUpgrade(bot, item);
            }
        }
    }

    /// <summary>
    ///     Any profession that touches the Bard category (not just the pure Bard-primary
    ///     ones — Trickster, Scout, Musician etc. all dip into it too) gets one real
    ///     instrument. Without this, Provocation/Peacemaking/Discordance/Musicianship were
    ///     trained skills that could never actually be used — every one of those skills is
    ///     hard-gated on having an instrument in hand.
    /// </summary>
    private static void GiveInstrumentIfBard(BotMobile bot)
    {
        if (!Systems.MahaonProfessions.ProfessionSystem.TouchesCategory(bot, Systems.MahaonProfessions.ProfessionCategory.Bard))
        {
            return;
        }

        var instrument = RandomInstrument();
        bot.Backpack?.DropItem(instrument);

        // Pre-cache it as "the" instrument this bot plays — otherwise the very first bard
        // skill use would stop to prompt a target asking which instrument to play, the same
        // interactive prompt a real player gets, which a bot can never answer on its own.
        BaseInstrument.SetInstrument(bot, instrument);
    }

    private static BaseInstrument RandomInstrument()
    {
        var pool = new Func<BaseInstrument>[]
        {
            () => new Lute(), () => new Harp(), () => new LapHarp(),
            () => new Drums(), () => new Tambourine(), () => new BambooFlute()
        };

        return pool.RandomElement()();
    }

    private static Item RandomWarriorWeapon()
    {
        var pool = new Func<Item>[]
        {
            () => new Broadsword(), () => new Longsword(), () => new VikingSword(),
            () => new Scimitar(), () => new Cutlass(), () => new Katana(),
            () => new NoDachi(), () => new Daisho(),
            () => new WarMace(), () => new Maul(), () => new WarHammer(), () => new Hammer(),
            () => new BattleAxe(), () => new DoubleAxe(), () => new LargeBattleAxe(), () => new ExecutionersAxe(),
            () => new Halberd(), () => new WarFork(), () => new ShortSpear(), () => new Pike(),
            () => new Kryss(), () => new Kama(), () => new Nunchaku(), () => new Sai()
        };

        return pool.RandomElement()();
    }

    private static Item RandomMageStaff()
    {
        var pool = new Func<Item>[]
        {
            () => new GnarledStaff(), () => new QuarterStaff(), () => new BlackStaff(), () => new WildStaff()
        };

        return pool.RandomElement()();
    }

    private static Item RandomArcherWeapon()
    {
        var pool = new Func<Item>[]
        {
            () => new Bow(), () => new CompositeBow(), () => new Yumi(),
            () => new Crossbow(), () => new HeavyCrossbow(), () => new RepeatingCrossbow()
        };

        return pool.RandomElement()();
    }

    private static void EquipRandomArmorSet(BotMobile bot)
    {
        // Pick one material family and gear the whole set from it, instead of every
        // warrior looking like they raided the same plate rack.
        var material = Utility.Random(5);

        switch (material)
        {
            case 0:
                bot.EquipItem(new PlateChest());
                bot.EquipItem(new PlateArms());
                bot.EquipItem(new PlateLegs());
                bot.EquipItem(new PlateGorget());
                bot.EquipItem(RandomHelm());
                break;
            case 1:
                bot.EquipItem(new ChainChest());
                bot.EquipItem(new ChainLegs());
                bot.EquipItem(new RingmailArms());
                bot.EquipItem(new PlateGorget());
                bot.EquipItem(RandomHelm());
                break;
            case 2:
                bot.EquipItem(new RingmailChest());
                bot.EquipItem(new RingmailArms());
                bot.EquipItem(new RingmailLegs());
                bot.EquipItem(new PlateGorget());
                break;
            case 3:
                bot.EquipItem(new StuddedChest());
                bot.EquipItem(new StuddedArms());
                bot.EquipItem(new StuddedLegs());
                bot.EquipItem(new StuddedGorget());
                bot.EquipItem(new LeatherCap());
                break;
            default:
                bot.EquipItem(new BoneChest());
                bot.EquipItem(new BoneArms());
                bot.EquipItem(new BoneLegs());
                bot.EquipItem(new BoneGloves());
                break;
        }
    }

    private static Item RandomHelm()
    {
        var pool = new Func<Item>[]
        {
            () => new PlateHelm(), () => new Bascinet(), () => new CloseHelm(),
            () => new NorseHelm(), () => new Helmet()
        };

        return pool.RandomElement()();
    }

    private static void EquipRandomLeatherSet(BotMobile bot)
    {
        var variant = Utility.Random(3);

        switch (variant)
        {
            case 0:
                bot.EquipItem(new LeatherChest());
                bot.EquipItem(new LeatherArms());
                bot.EquipItem(new LeatherLegs());
                bot.EquipItem(new LeatherGloves());
                bot.EquipItem(new LeatherGorget());
                break;
            case 1:
                bot.EquipItem(new StuddedChest());
                bot.EquipItem(new StuddedArms());
                bot.EquipItem(new StuddedLegs());
                bot.EquipItem(new StuddedGloves());
                bot.EquipItem(new StuddedGorget());
                break;
            default:
                bot.EquipItem(bot.Female ? new FemaleLeatherChest() : new LeatherChest());
                bot.EquipItem(new LeatherArms());
                bot.EquipItem(new LeatherLegs());
                bot.EquipItem(new LeatherGloves());
                bot.EquipItem(new LeatherGorget());
                break;
        }
    }

    private static void AdaptWeaponToProfession(BotMobile bot, BotArchetype type)
    {
        // No-op now — warrior professions train the full Fencing/Macing/Swords/Wrestling/
        // Tactics kit to 120 (not one or two signature skills), so there's no longer a
        // single "correct" weapon skill to match against. Real per-profession weapon
        // restrictions (e.g. Mercenary can't use axes) from the source data aren't wired
        // in yet — worth a follow-up pass.
    }

    private static void ApplyArchetype(BotMobile bot, BotArchetype type)
    {
        switch (type)
        {
            case BotArchetype.Warrior:
                SetStr(bot, 80, 95);
                SetDex(bot, 65, 80);
                SetInt(bot, 25, 40);
                SetSkill(bot, SkillName.Swords, 60, 95);
                SetSkill(bot, SkillName.Tactics, 60, 95);
                SetSkill(bot, SkillName.Anatomy, 50, 85);
                SetSkill(bot, SkillName.Healing, 40, 75);
                SetSkill(bot, SkillName.Parry, 40, 75);
                SetSkill(bot, SkillName.Chivalry, 30, 65);
                SetSkill(bot, SkillName.Bushido, 30, 65);

                EquipRandomArmorSet(bot);
                bot.EquipItem(RandomWarriorWeapon());
                bot.Backpack?.DropItem(new Bandage(Utility.RandomMinMax(45, 55)));
                Systems.MahaonGuard.GuardSystem.SetRandomRank(bot);
                break;

            case BotArchetype.Mage:
                SetStr(bot, 30, 45);
                SetDex(bot, 45, 60);
                SetInt(bot, 80, 95);
                SetSkill(bot, SkillName.Magery, 60, 95);
                SetSkill(bot, SkillName.EvalInt, 60, 95);
                SetSkill(bot, SkillName.Meditation, 50, 85);
                SetSkill(bot, SkillName.MagicResist, 40, 75);
                SetSkill(bot, SkillName.Wrestling, 30, 60);
                SetSkill(bot, SkillName.Necromancy, 40, 75);
                SetSkill(bot, SkillName.SpiritSpeak, 30, 60);

                bot.EquipItem(new Robe(Utility.RandomNondyedHue()));
                bot.EquipItem(new Sandals());
                bot.EquipItem(RandomMageStaff());
                bot.Backpack?.DropItem(new Spellbook());
                bot.Backpack?.DropItem(new MahaonReagentPouch());
                bot.Backpack?.DropItem(new Bandage(Utility.RandomMinMax(15, 25)));
                break;

            case BotArchetype.Archer:
                SetStr(bot, 55, 70);
                SetDex(bot, 80, 95);
                SetInt(bot, 30, 45);
                SetSkill(bot, SkillName.Archery, 60, 95);
                SetSkill(bot, SkillName.Tactics, 50, 85);
                SetSkill(bot, SkillName.Anatomy, 40, 75);
                SetSkill(bot, SkillName.AnimalTaming, 20, 50);
                SetSkill(bot, SkillName.Tracking, 30, 60);
                SetSkill(bot, SkillName.Spellweaving, 30, 65);

                EquipRandomLeatherSet(bot);
                bot.EquipItem(RandomArcherWeapon());
                bot.Backpack?.DropItem(new Arrow(Utility.RandomMinMax(50, 200)));
                break;

            case BotArchetype.Crafter:
                SetStr(bot, 50, 65);
                SetDex(bot, 50, 65);
                SetInt(bot, 50, 65);

                var craftSkill = Utility.RandomList(
                    SkillName.Blacksmith, SkillName.Carpentry, SkillName.Tailoring, SkillName.Tinkering
                );
                SetSkill(bot, craftSkill, 60, 95);
                SetSkill(bot, SkillName.Mining, 40, 75);
                SetSkill(bot, SkillName.Lumberjacking, 40, 75);

                bot.EquipItem(new Shirt(Utility.RandomNeutralHue()));
                bot.EquipItem(new LongPants(Utility.RandomNeutralHue()));
                bot.EquipItem(new Sandals());
                bot.EquipItem(new LeatherGloves());
                bot.EquipItem(new LeatherCap());

                Item tool = craftSkill switch
                {
                    SkillName.Blacksmith => new SmithHammer(),
                    SkillName.Carpentry  => new Saw(),
                    SkillName.Tailoring  => new SewingKit(),
                    SkillName.Tinkering  => new TinkerTools(),
                    _                    => null
                };

                if (tool != null)
                {
                    bot.Backpack?.DropItem(tool);
                }

                break;

            case BotArchetype.Trader:
                SetStr(bot, 55, 70);
                SetDex(bot, 55, 70);
                SetInt(bot, 45, 60);
                SetSkill(bot, SkillName.Camping, 40, 75);
                SetSkill(bot, SkillName.ArmsLore, 30, 65);
                SetSkill(bot, SkillName.Fencing, 30, 60);
                SetSkill(bot, SkillName.Ninjitsu, 25, 55);
                SetSkill(bot, SkillName.Stealth, 30, 65);

                bot.EquipItem(new FancyShirt(Utility.RandomNeutralHue()));
                bot.EquipItem(new LongPants(Utility.RandomNeutralHue()));
                bot.EquipItem(new Boots());
                bot.EquipItem(new Cloak(Utility.RandomNeutralHue()));
                bot.EquipItem(new LeatherChest());
                bot.EquipItem(new LeatherArms());
                break;
        }
    }

    private static void SetStr(Mobile m, int min, int max) => m.RawStr = Utility.RandomMinMax(min, max);
    private static void SetDex(Mobile m, int min, int max) => m.RawDex = Utility.RandomMinMax(min, max);
    private static void SetInt(Mobile m, int min, int max) => m.RawInt = Utility.RandomMinMax(min, max);

    private static void SetSkill(Mobile m, SkillName skill, double min, double max) =>
        m.Skills[skill].Base = Utility.RandomMinMax((int)min, (int)max);
}

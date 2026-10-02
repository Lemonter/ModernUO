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
///     its <see cref="Server.Systems.Bots.BotBrain" /> instead of a client.
/// </summary>
[SerializationGenerator(1, false)]
public partial class BotMobile : PlayerMobile, IPathDoorOpener
{
    [SerializableField(0)]
    private BotArchetype _archetype;

    [SerializableField(1)]
    private bool _isPk;

    // The mind (Systems/Bots). Null only until the bot is first registered.
    [SerializableField(2, setter: "internal")]
    [SaveFlag(nameof(ShouldSerializeBrain))]
    private Systems.Bots.BotBrain _brain;

    private bool ShouldSerializeBrain() => _brain != null;

    private void MigrateFrom(V0Content content)
    {
        _archetype = content.Archetype;
        _isPk = content.IsPk;
    }

    [AfterDeserialization]
    private void AfterDeserialization() => Systems.Bots.BotSystem.OnBotLoaded(this);

    public bool OpensDoors => Systems.Bots.BotSystem.IsActive(this);

    // Bots keep the world awake around them the way a connected player does: away from the real
    // player there are otherwise no live monsters, spawners or AI for them to meet.
    public override bool ActivatesSectors => true;


    /// <summary>
    ///     Единственная надёжная точка для «бота больше нет»: через неё проходит любой
    ///     путь удаления ([ClearBots, [remove, удаление маяка).
    /// </summary>
    public override void OnDelete()
    {
        Systems.Bots.BotSystem.Unregister(this);
        Systems.Bots.BotHousing.OnOwnerDeleted(this);

        // Tamed pets would stay in the world with a master that no longer exists.
        if (AllFollowers is { Count: > 0 } followers)
        {
            foreach (var follower in new List<Mobile>(followers))
            {
                if (follower is BaseCreature { Controlled: true } pet && pet.ControlMaster == this)
                {
                    pet.Delete();
                }
            }
        }

        base.OnDelete();
    }

    /// <summary>A new crafter starts out knowing the weapon and armour patterns of its trade.</summary>
    private static void GrantStartingRecipes(PlayerMobile bot)
    {
        foreach (var recipe in Engines.Craft.Recipe.Recipes.Values)
        {
            var type = recipe.CraftItem?.ItemType;
            if (type != null && (typeof(BaseWeapon).IsAssignableFrom(type) || typeof(BaseArmor).IsAssignableFrom(type)) &&
                !bot.HasRecipe(recipe))
            {
                bot.AcquireRecipe(recipe);
            }
        }
    }

    public static bool IsProfessionCategory(Mobile m, Systems.MahaonProfessions.ProfessionCategory category) =>
        Systems.MahaonProfessions.ProfessionSystem.GetProfession(m) is { } profession &&
        Systems.MahaonProfessions.ProfessionData.All[profession].Category == category;

    // Mahaon: real players' Resurrect() (PlayerMobile.cs) grants a DeathRobe every single
    // time they come back to life — fine for an actual person who dies occasionally, but
    // bots die and resurrect constantly (sometimes thousands of times over a long test
    // session), and each robe that PlayerMobile.EquipItem couldn't equip cleanly (slot
    // already occupied by a leftover from the PREVIOUS death) gets pushed into the bot's
    // own backpack instead of being discarded — millions of orphaned DeathRobe items
    // accumulating in the world over time, all counted every single save even though
    // nothing about the world visually looks any different. Bots don't need the cosmetic
    // "you just died" indicator the way a real player does, so this skips granting one at
    // all — still calls the real PlayerMobile.Resurrect() first for everything else it
    // does (clearing poison, restoring Hits/Stam/Mana, closing the bank box, etc), just
    // immediately removes whatever robe that call just equipped.
    public override void Resurrect()
    {
        var wasAlive = Alive;
        base.Resurrect();

        if (Alive && !wasAlive && FindItemOnLayer(Layer.OuterTorso) is DeathRobe robe)
        {
            robe.Delete();
        }

        // Death clears the profession title.
        if (Alive && !wasAlive)
        {
            ApplyNameTemplate(this);
        }
    }

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

    /// <summary>
    ///     Вычёркивает из общей памяти всё, что лежит рядом с этой точкой.
    ///
    ///     Зовётся, когда до места так и не удалось дойти. Память общая, и попала туда
    ///     жила, возможно, от бота, стоявшего по ту сторону реки: без вычёркивания
    ///     недостижимое место оставалось бы в списке навсегда и раз за разом выпадало бы
    ///     следующему.
    /// </summary>
    public void ForgetSpotNear(Point3D loc)
    {
        Forget(SharedMineSpots, Map, loc);
        Forget(SharedTreeSpots, Map, loc);
        Forget(SharedFishSpots, Map, loc);
        Forget(SharedHuntingSpots, Map, loc);
    }

    private static void Forget(Dictionary<Map, List<Point3D>> pool, Map map, Point3D loc)
    {
        if (map == null || !pool.TryGetValue(map, out var list))
        {
            return;
        }

        list.RemoveAll(known => Math.Abs(known.X - loc.X) <= ForgetRadius && Math.Abs(known.Y - loc.Y) <= ForgetRadius);
    }

    /// <summary>Насколько широко вычёркивать вокруг недостижимой точки.</summary>
    private const int ForgetRadius = 6;

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

    // Share of outlaws among new bots.
    private const double PkChance = 0.12;

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
            _isPk = Utility.RandomDouble() < PkChance
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

        ApplyNameTemplate(bot);

        bot.Hits = bot.HitsMax;
        bot.Stam = bot.StamMax;
        bot.Mana = bot.ManaMax;

        bot.MoveToWorld(location, map);

        return bot;
    }

    /// <summary>
    ///     Имя бота по шаблону «Имя Профессия [Гильдия]».
    ///
    ///     Движок собирает эту строку сам (Mobile.AddNameProperties: имя, затем Title,
    ///     затем аббревиатура гильдии в скобках) — от нас требуются три вещи, каждой из
    ///     которых не хватало:
    ///
    ///     — Title. Его ставит ProfessionSystem.SetProfession, но воскрешение и
    ///       перевыдача снаряжения его затирали; поэтому проставляем заново здесь и после
    ///       смерти.
    ///     — Гильдия. Раньше в неё попадали только боты с маяка, у которого заполнено
    ///       GuildName; у остальных скобок в имени не было вовсе. Теперь безгильдейный бот
    ///       берёт гильдию из общего списка.
    ///     — DisplayGuildTitle. Без него движок скобки не рисует (BotGuilds.Join его
    ///       ставит, но только на своём пути).
    /// </summary>
    public static void ApplyNameTemplate(BotMobile bot)
    {
        if (bot?.Deleted != false)
        {
            return;
        }

        var profession = Systems.MahaonProfessions.ProfessionSystem.GetProfession(bot);

        if (profession != null)
        {
            bot.Title = Systems.MahaonProfessions.ProfessionData.GetName(profession.Value, bot.Female);
        }

        // Гильдию здесь больше не выбираем: этим занимается маяк (одна на всех своих
        // ботов). Случайная гильдия на каждого бота превращала точку возрождения в свалку
        // чужаков — см. BotGuilds.NameForBeacon.
        if (bot.Guild == null && bot.Brain?.OwnerBeacon is Items.MahaonBotBeacon { Deleted: false } beacon)
        {
            Systems.MahaonBots.BotGuilds.Join(beacon.GuildNameFor(bot), bot);
        }

        bot.DisplayGuildTitle = true;
        bot.InvalidateProperties();
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

    /// <summary>
    ///     Надевает вещь, а если не налезла — молча подсовывает кожаный аналог.
    ///
    ///     BotMobile наследует PlayerMobile, поэтому BaseArmor.CanEquip проверяет для бота
    ///     требования по силе так же, как для живого игрока. Латный доспех требует 95 силы,
    ///     а воину раскатывается SetStr(80, 95) — то есть у большинства воинов кираса и
    ///     поножи просто не надевались. Результат EquipItem никто не смотрел, так что бот
    ///     оставался с голым торсом, а сам предмет повисал в мире без владельца.
    ///
    ///     Кожа требует около 20 силы, её осилит любой бот, поэтому запасной вариант
    ///     срабатывает всегда.
    /// </summary>
    private static void Wear(BotMobile bot, Item item, Func<Item> fallback = null)
    {
        if (item == null)
        {
            return;
        }

        if (bot.EquipItem(item))
        {
            return;
        }

        // Не надел — уничтожаем, иначе предмет остаётся в мире ничьим.
        item.Delete();

        var spare = fallback?.Invoke();

        if (spare != null && !bot.EquipItem(spare))
        {
            spare.Delete();
        }
    }

    private static void EquipRandomArmorSet(BotMobile bot)
    {
        // Pick one material family and gear the whole set from it, instead of every
        // warrior looking like they raided the same plate rack.
        var material = Utility.Random(5);

        switch (material)
        {
            case 0:
                Wear(bot, new PlateChest(), () => new LeatherChest());
                Wear(bot, new PlateArms(), () => new LeatherArms());
                Wear(bot, new PlateLegs(), () => new LeatherLegs());
                Wear(bot, new PlateGorget(), () => new LeatherGorget());
                Wear(bot, RandomHelm(), () => new LeatherCap());
                break;
            case 1:
                Wear(bot, new ChainChest(), () => new LeatherChest());
                Wear(bot, new ChainLegs(), () => new LeatherLegs());
                Wear(bot, new RingmailArms(), () => new LeatherArms());
                Wear(bot, new PlateGorget(), () => new LeatherGorget());
                Wear(bot, RandomHelm(), () => new LeatherCap());
                break;
            case 2:
                Wear(bot, new RingmailChest(), () => new LeatherChest());
                Wear(bot, new RingmailArms(), () => new LeatherArms());
                Wear(bot, new RingmailLegs(), () => new LeatherLegs());
                Wear(bot, new PlateGorget(), () => new LeatherGorget());
                break;
            case 3:
                Wear(bot, new StuddedChest(), () => new LeatherChest());
                Wear(bot, new StuddedArms(), () => new LeatherArms());
                Wear(bot, new StuddedLegs(), () => new LeatherLegs());
                Wear(bot, new StuddedGorget(), () => new LeatherGorget());
                Wear(bot, new LeatherCap());
                break;
            default:
                Wear(bot, new BoneChest(), () => new LeatherChest());
                Wear(bot, new BoneArms(), () => new LeatherArms());
                Wear(bot, new BoneLegs(), () => new LeatherLegs());
                Wear(bot, new BoneGloves(), () => new LeatherGloves());
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
                Wear(bot, new LeatherChest());
                Wear(bot, new LeatherArms());
                Wear(bot, new LeatherLegs());
                Wear(bot, new LeatherGloves());
                Wear(bot, new LeatherGorget());
                break;
            case 1:
                Wear(bot, new StuddedChest());
                Wear(bot, new StuddedArms());
                Wear(bot, new StuddedLegs());
                Wear(bot, new StuddedGloves());
                Wear(bot, new StuddedGorget());
                break;
            default:
                Wear(bot, bot.Female ? new FemaleLeatherChest() : new LeatherChest());
                Wear(bot, new LeatherArms());
                Wear(bot, new LeatherLegs());
                Wear(bot, new LeatherGloves());
                Wear(bot, new LeatherGorget());
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
                Wear(bot, RandomWarriorWeapon());
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

                Wear(bot, new Robe(Utility.RandomNondyedHue()));
                Wear(bot, new Sandals());
                Wear(bot, new LeatherGloves());
                Wear(bot, new LeatherGorget());
                Wear(bot, new LeatherArms());
                Wear(bot, RandomMageStaff());
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
                Wear(bot, RandomArcherWeapon());
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

                Wear(bot, new LeatherChest());
                Wear(bot, new LeatherLegs());
                Wear(bot, new Sandals());
                Wear(bot, new LeatherGloves());
                Wear(bot, new LeatherCap());

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

                GrantStartingRecipes(bot);
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

                Wear(bot, new FancyShirt(Utility.RandomNeutralHue()));
                Wear(bot, new LongPants(Utility.RandomNeutralHue()));
                Wear(bot, new Boots());
                Wear(bot, new Cloak(Utility.RandomNeutralHue()));
                Wear(bot, new LeatherChest());
                Wear(bot, new LeatherArms());
                break;
        }
    }

    private static void SetStr(Mobile m, int min, int max) => m.RawStr = Utility.RandomMinMax(min, max);
    private static void SetDex(Mobile m, int min, int max) => m.RawDex = Utility.RandomMinMax(min, max);
    private static void SetInt(Mobile m, int min, int max) => m.RawInt = Utility.RandomMinMax(min, max);

    private static void SetSkill(Mobile m, SkillName skill, double min, double max) =>
        m.Skills[skill].Base = Utility.RandomMinMax((int)min, (int)max);
}

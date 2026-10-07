using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Guilds;
using Server.Items;
using Server.Systems.MahaonCities;

namespace Server.Mobiles;

[SerializationGenerator(1, false)]
public partial class CityGuard : BaseCreature
{
    // The city keys the guard registry, so it is set once.
    [SerializableField(0, setter: "private")]
    private string _city;

    [SerializableField(1)]
    private Guild _controllingGuild;

    // Where the guard keeps watch. A rally to the city's claim banner moves Home; this brings it back.
    [SerializableField(2, setter: "private")]
    private Point3D _post;

    private void MigrateFrom(V0Content content)
    {
        _city = content.City;
        _controllingGuild = content.ControllingGuild;
        _post = Home;
    }

    /// <summary>Posts the guard where it patrols around.</summary>
    public void StationAt(Point3D post, int patrolRadius)
    {
        Post = post;
        Home = post;
        RangeHome = patrolRadius;
    }

    /// <summary>Calls the guard to defend a spot: it walks there and stays close.</summary>
    public void RallyTo(Point3D spot)
    {
        Home = spot;
        RangeHome = 4;
    }

    /// <summary>Back to the post after a rally.</summary>
    public void ReturnToPost(int patrolRadius)
    {
        Home = _post;
        RangeHome = patrolRadius;
    }

    private const int GuardScanRange = 15;

    // Every city's living guards, wherever their posts are: a hand-marked post can be anywhere.
    private static readonly Dictionary<string, HashSet<CityGuard>> _byCity = new();

    /// <summary>The guards stationed in a city.</summary>
    public static IReadOnlyCollection<CityGuard> Of(string city) =>
        city != null && _byCity.TryGetValue(city, out var guards) ? guards : Array.Empty<CityGuard>();

    private void Register()
    {
        if (_city == null)
        {
            return;
        }

        if (!_byCity.TryGetValue(_city, out var guards))
        {
            _byCity[_city] = guards = [];
        }

        guards.Add(this);
    }

    [AfterDeserialization]
    private void AfterDeserialization() => Register();

    public override void OnDelete()
    {
        if (_city != null && _byCity.TryGetValue(_city, out var guards))
        {
            guards.Remove(this);
        }

        base.OnDelete();
    }

    public CityGuard(string city, Guild controllingGuild) : this(city, controllingGuild, AIType.AI_Melee)
    {
    }

    protected CityGuard(string city, Guild controllingGuild, AIType ai) : base(ai, FightMode.Aggressor)
    {
        _city = city;
        _controllingGuild = controllingGuild;
        Register();

        Name = $"{controllingGuild.Name} {Title}";
        Body = 0x190;
        Hue = Race.Human.RandomSkinHue();

        Outfit();
        ApplyLevel(0);
    }

    protected virtual string Title => "стражник";

    protected virtual void Outfit()
    {
        AddItem(new Longsword());
        AddItem(new PlateChest());
        AddItem(new PlateArms());
        AddItem(new PlateLegs());
        AddItem(new PlateGorget());
    }

    /// <summary>
    /// Trains and arms the guard to its city's level: each level adds strength, health and
    /// skill, and from the first one its gear is forged of the level's metal — the ingots the
    /// guild paid for it.
    /// </summary>
    public virtual void ApplyLevel(int level)
    {
        SetStr(100 + level * 3, 120 + level * 3);
        SetDex(80 + level * 2, 100 + level * 2);
        SetInt(50, 60);
        SetHits((int)(80 * (1 + level * 0.1)), (int)(100 * (1 + level * 0.1)));

        SetSkill(SkillName.Swords, Math.Min(120, 80.0 + level * 2), Math.Min(120, 100.0 + level * 2));
        SetSkill(SkillName.Tactics, Math.Min(120, 80.0 + level * 2), Math.Min(120, 100.0 + level * 2));
        SetSkill(SkillName.MagicResist, Math.Min(120, 60.0 + level * 4), Math.Min(120, 80.0 + level * 4));

        ForgeGear(level);
        Hits = HitsMax;
    }

    // Gear takes the colour of its metal: a guard's level shows on it at a glance.
    protected void ForgeGear(int level)
    {
        if (CityControlSystem.MetalFor(level) is not { } metal)
        {
            return;
        }

        foreach (var item in new List<Item>(Items))
        {
            if (item is BaseArmor or BaseWeapon)
            {
                Systems.MahaonMetals.MahaonMetalTracker.Forge(item, metal);
            }
        }
    }

    // A guard carries a few potions its guild keeps stocked (see CityGuardUpkeep) and drinks
    // them as a player would: a cure when poisoned, a heal when badly hurt.
    private void DrinkIfHurt()
    {
        if (Backpack is not { } pack || Core.TickCount - _nextDrink < 0)
        {
            return;
        }

        BasePotion potion = null;
        if (Poisoned)
        {
            potion = pack.FindItemByType<BaseCurePotion>();
        }
        else if (Hits < HitsMax / 2)
        {
            potion = pack.FindItemByType<BaseHealPotion>();
        }

        if (potion != null)
        {
            potion.OnDoubleClick(this);
            _nextDrink = Core.TickCount + DrinkDelayMs;
        }
    }

    public override bool AlwaysMurderer => false;

    public override bool IsEnemy(Mobile m) =>
        (_controllingGuild != null && CityControlSystem.IsHostileToCity(_city, m)) ||
        IsGuardTarget(m) ||
        base.IsEnemy(m);

    /// <summary>
    ///     Кого стража берёт сама, без всякого повода со своей стороны: убийц (красных) и
    ///     преступников (серых). Плюс боты с ролью PK — их флаг выдаётся при создании и к
    ///     счётчику убийств отношения не имеет.
    ///
    ///     Раньше здесь стояло ровно <c>m is BotMobile { IsPk: true }</c>. Это флаг роли,
    ///     живому игроку взяться ему неоткуда — то есть настоящий игрок мог набить сколько
    ///     угодно убийств и спокойно ходить мимо поста: стража его попросту не видела.
    ///     Ванильная стража городов у нас выключена целиком (см. DisableVanillaGuards),
    ///     так что этот метод — единственное место, где вообще решается, кого стража бьёт.
    ///
    ///     Карма сюда намеренно не входит. <see cref="Mobile.Murderer" /> — это
    ///     Kills >= 5, <see cref="Mobile.Criminal" /> — флаг за конкретное преступление;
    ///     ни то, ни другое от кармы не зависит, и низкая карма сама по себе ни цвета, ни
    ///     внимания стражи не даёт.
    /// </summary>
    private static bool IsGuardTarget(Mobile m)
    {
        if (m is BotMobile { IsPk: true })
        {
            return true;
        }

        // Соседняя стража, персонал, неуязвимые и благословлённые — мимо. Без этой
        // отсечки два поста разных городов, оба формально Criminal после драки, начали бы
        // резать друг друга.
        if (m is BaseGuard or CityGuard || m.AccessLevel > AccessLevel.Player || m.Blessed ||
            (m as BaseCreature)?.IsInvulnerable == true)
        {
            return false;
        }

        return m.Murderer || m.Criminal;
    }

    // Potions a guard drinks are paced like a player's, not one per think.
    private const long DrinkDelayMs = 10_000;
    private long _nextDrink;

    public override void OnThink()
    {
        base.OnThink();

        DrinkIfHurt();

        // Only bother scanning for trouble on an occasional think tick, not every single
        // one — cheap and still looks natural. Wandering/patrolling itself is handled
        // entirely by the stock Home/RangeHome walk-back AI (WalkRandomLogic.cs) — no
        // custom leash here, so no teleport-snapping back into view.
        if (Utility.RandomDouble() > 0.15)
        {
            return;
        }

        // Actively hunt down whoever's causing trouble in the city — reds and greys alike —
        // rather than waiting to be attacked first (FightMode.Aggressor alone wouldn't
        // trigger on sight).
        if ((Combatant?.Deleted != false || !Combatant.Alive) && Map != null)
        {
            foreach (var mobile in Map.GetMobilesInRange<Mobile>(Location, GuardScanRange))
            {
                if (mobile.Alive && !mobile.Deleted && !mobile.Hidden &&
                    (IsGuardTarget(mobile) || CityControlSystem.IsHostileToCity(_city, mobile)))
                {
                    Combatant = mobile;
                    Warmode = true;
                    break;
                }
            }
        }
    }
}

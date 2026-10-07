using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Guilds;
using Server.Items;
using Server.Systems.MahaonCities;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class CityGuard : BaseCreature
{
    // The city keys the guard registry, so it is set once.
    [SerializableField(0, setter: "private")]
    private string _city;

    [SerializableField(1)]
    private Guild _controllingGuild;

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

    public CityGuard(string city, Guild controllingGuild) : base(AIType.AI_Melee, FightMode.Aggressor)
    {
        _city = city;
        _controllingGuild = controllingGuild;
        Register();

        Name = $"{controllingGuild.Name} стражник";
        Body = 0x190;
        Hue = Race.Human.RandomSkinHue();

        SetStr(100, 120);
        SetDex(80, 100);
        SetInt(50, 60);
        SetHits(80, 100);

        SetSkill(SkillName.Swords, 80.0, 100.0);
        SetSkill(SkillName.Tactics, 80.0, 100.0);
        SetSkill(SkillName.MagicResist, 60.0, 80.0);

        AddItem(new Longsword());
        AddItem(new PlateChest());
        AddItem(new PlateArms());
        AddItem(new PlateLegs());
        AddItem(new PlateGorget());
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

    public override void OnThink()
    {
        base.OnThink();

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
                if (mobile.Alive && !mobile.Deleted && !mobile.Hidden && IsGuardTarget(mobile))
                {
                    Combatant = mobile;
                    Warmode = true;
                    break;
                }
            }
        }
    }
}

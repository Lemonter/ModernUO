using System;
using System.Collections.Generic;
using Server.Items;
using Server.Multis;
using Server.Regions;
using Server.Spells.Shamanism;

namespace Server.Systems.MahaonShamanism;

/// <summary>
///     Правила вершин — двух последних обрядов каждого раздела (метеорит, мор,
///     землетрясение, великая буря и др.). Согласованы с пользователем как «баланс вершин»:
///
///         • нужна специализация раздела ≥ 60 — иначе специализация ничего не решает;
///         • запрещены в охраняемых зонах (города) и рядом с домами — иначе первый же шаман
///           сносит рынок, а рельеф после вершин остаётся НАВСЕГДА;
///         • съедают редкий компонент раздела (ShamanComponent): седьмой обряд — один,
///           восьмой — два; списываются только при удачном срабатывании;
///         • долгий сбиваемый ритуал (ShamanSpell.RitualDuration / OnBeginCast);
///         • откат на ВЕСЬ сервер, а не на шамана: каждая вершина — событие, а не кнопка.
///           Откаты сохраняются, перезапуск их не сбрасывает;
///         • о начале ритуала узнаёт весь мир — у противника есть время найти шамана.
/// </summary>
public sealed class ShamanUltimates : GenericPersistence
{
    public const int HouseClearance = 15;

    private static ShamanUltimates _instance;
    private static readonly Dictionary<string, DateTime> NextAllowed = new();

    public ShamanUltimates() : base("MahaonShamanUltimates", 1)
    {
    }

    public static void Configure() => _instance = new ShamanUltimates();

    private static string Key(ShamanSpell spell) => spell.GetType().Name;

    public static TimeSpan CooldownLeft(ShamanSpell spell) =>
        NextAllowed.TryGetValue(Key(spell), out var next) && next > Core.Now ? next - Core.Now : TimeSpan.Zero;

    public static bool CanPerform(ShamanSpell spell, bool atStart, out string why)
    {
        var caster = spell.Caster;
        why = null;

        if (caster.AccessLevel >= AccessLevel.GameMaster)
        {
            return true;
        }

        var spec = ShamanismSchoolSystem.GetValue(caster, spell.Tradition);

        if (spec < ShamanismSchoolSystem.VertexRequirement)
        {
            why = $"{ShamanismSchoolSystem.RuName(spell.Tradition)}: для вершин нужна специализация " +
                  $"{ShamanismSchoolSystem.VertexRequirement:0}, у тебя {spec:0.0}.";
            return false;
        }

        if (!IsAllowedPlace(caster.Map, caster.Location, out why))
        {
            return false;
        }

        var left = CooldownLeft(spell);

        if (left > TimeSpan.Zero)
        {
            why = $"Мир ещё не оправился от прошлого такого обряда. Ждать: {Format(left)}.";
            return false;
        }

        var type = ShamanComponent.TypeFor(spell.Tradition);
        var have = caster.Backpack?.GetAmount(type) ?? 0;

        if (have < spell.ComponentCost)
        {
            why = $"Для обряда нужно: {ShamanComponent.NameFor(spell.Tradition)} ×{spell.ComponentCost}.";
            return false;
        }

        return true;
    }

    /// <summary>Можно ли здесь творить вершины: не в городе и не у домов.</summary>
    public static bool IsAllowedPlace(Map map, Point3D p, out string why)
    {
        why = null;

        if (map == null || map == Map.Internal)
        {
            why = "Здесь нет земли для обряда.";
            return false;
        }

        if (Region.Find(p, map)?.GetRegion<GuardedRegion>() != null)
        {
            why = "В охраняемых землях такие обряды не творят — стража не даст.";
            return false;
        }

        foreach (var _ in map.GetMultisInRange<BaseHouse>(p, HouseClearance))
        {
            why = "Слишком близко к жилью — отойди от домов.";
            return false;
        }

        return true;
    }

    public static void AnnounceBegin(ShamanSpell spell)
    {
        var where = spell.Caster.Region?.Name;
        var place = string.IsNullOrEmpty(where) ? "в диких землях" : $"в краях «{where}»";

        World.Broadcast(0x22, false, $"Земля стонет: {place} шаман начинает обряд «{spell.Name}».");
    }

    public static void OnPerformed(ShamanSpell spell)
    {
        var caster = spell.Caster;

        if (caster.AccessLevel < AccessLevel.GameMaster && spell.ComponentCost > 0)
        {
            caster.Backpack?.ConsumeTotal(ShamanComponent.TypeFor(spell.Tradition), spell.ComponentCost);
        }

        NextAllowed[Key(spell)] = Core.Now + spell.GlobalCooldown;
    }

    public static void Reset() => NextAllowed.Clear();

    private static string Format(TimeSpan t) =>
        t.TotalHours >= 1 ? $"{(int)t.TotalHours} ч {t.Minutes} мин" : $"{t.Minutes} мин {t.Seconds} с";

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version
        writer.WriteEncodedInt(NextAllowed.Count);

        foreach (var (key, time) in NextAllowed)
        {
            writer.Write(key);
            writer.Write(time);
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // version
        var count = reader.ReadEncodedInt();

        for (var i = 0; i < count; i++)
        {
            var key = reader.ReadString();
            var time = reader.ReadDateTime();

            if (time > Core.Now)
            {
                NextAllowed[key] = time;
            }
        }
    }
}

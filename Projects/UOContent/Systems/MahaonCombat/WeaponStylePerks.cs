using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.Systems.MahaonCombat;

/// <summary>Особая механика стиля — то, что не сводится к процентам в профиле удара.</summary>
public enum StyleSpecial : byte
{
    None,
    Counter,  // контрудар, когда бьют тебя (Айкидо)
    Disarm,   // выбить оружие (Оглушающий удар)
    Ambush,   // первый удар по свежей цели сильнее (Ассасин, Тёмный лучник, Теневой метатель)
    Distance, // чем дальше цель, тем больнее (Снайпер, Дальнобойный бросок)
    Cleave,   // задеть ещё одного врага рядом (Двуручный размах, Вихрь клинков, Смертельный вихрь)
    Revenge,  // чем меньше своего здоровья, тем больнее (Стиль возмездия)
    Execute,  // добивание: по цели с малым здоровьем больнее (Палач)
    Backstab, // со спины больнее (Ночной клинок)
    Headshot, // прицел в голову больнее (Охотник за головами)
    Bleed     // шанс пустить кровь (Отравленные лезвия)
}

/// <summary>Прибавки и цены одного стиля, в процентах и процентных пунктах.</summary>
public readonly record struct StyleMods(
    int Hit = 0, int Crit = 0, int Damage = 0, int Speed = 0, int Taken = 0, int Parry = 0
);

/// <summary>
///     Стиль: плюшка растёт с тренировкой стиля, цена — постоянная (раздел 7.1).
///     Special — особая механика, её сила (шанс или прибавка) тоже растёт с тренировкой.
/// </summary>
public sealed record StyleTrade(StyleMods Gain, StyleMods Price, StyleSpecial Special = StyleSpecial.None, int SpecialPower = 0);

/// <summary>
///     Этап 8: стиль как активный выбор с ценой.
///
///     ---- Было ----
///
///     Каждый стиль давал голую прибавку к урону (+5…+20%) — одинаковую у новичка и мастера и
///     бесплатную. Тренировка стиля ни на что не влияла, кроме «ширины» соседних стилей.
///
///     ---- Стало ----
///
///     У каждого стиля своя плюшка (скорость, точность, крит, урон, парирование, стойкость или
///     особая механика) и своя цена. Плюшка растёт с тренировкой стиля: на 0 — пятая часть, на
///     100 — полная. Цена не растёт: новичок в Берсерке платит ту же цену, но получает меньше
///     (раздел 7.1). Классический стиль — без плюшки и без цены.
///
///     ---- Где применяется ----
///
///     Попадание, крит, урон и особые прибавки к урону — вкладом в профиль удара (видно в чеке).
///     Скорость — BaseWeapon.GetDelay. Получаемый урон — PlayerMobile (как защитная стойка).
///     Парирование — BaseWeapon.CheckParry. Контрудар, выбивание оружия, задеть соседа, кровь —
///     после попадания (WeaponStyleSystem.OnAfterHit).
/// </summary>
public static class WeaponStylePerks
{
    /// <summary>Доля плюшки у нетренированного стиля.</summary>
    public const double MinScale = 0.2;

    private const int AmbushWindowSeconds = 30;

    private static readonly Dictionary<(WeaponCategory, byte), StyleTrade> Table = new()
    {
        // ---- Рукопашный бой ----
        [(WeaponCategory.Wrestling, 1)] = new(new(Speed: 20), new(Damage: -10)),                   // Бокс
        [(WeaponCategory.Wrestling, 2)] = new(new(Damage: 25), new(Taken: 10)),                    // Муай-тай
        [(WeaponCategory.Wrestling, 3)] = new(new(Crit: 8), new(Hit: -5)),                         // Карате
        [(WeaponCategory.Wrestling, 4)] = new(new(), new(Damage: -15), StyleSpecial.Counter, 25),  // Айкидо
        [(WeaponCategory.Wrestling, 5)] = new(new(Hit: 10), new(Speed: -10)),                      // Самбо
        [(WeaponCategory.Wrestling, 6)] = new(new(Taken: -12), new(Speed: -10)),                   // Греко-римская

        // ---- Мечи ----
        [(WeaponCategory.Swords, 1)] = new(new(Speed: 15), new(Hit: -5)),                          // Кендзюцу
        [(WeaponCategory.Swords, 2)] = new(new(Parry: 15), new(Damage: -10)),                      // Дуэльный
        [(WeaponCategory.Swords, 3)] = new(new(Damage: 25), new(Parry: -15, Taken: 10)),           // Берсерк
        [(WeaponCategory.Swords, 4)] = new(new(Crit: 10), new(Speed: -10)),                        // Клинок тени
        [(WeaponCategory.Swords, 5)] = new(new(), new(Speed: -15), StyleSpecial.Cleave, 40),       // Двуручный размах
        [(WeaponCategory.Swords, 6)] = new(new(), new(Parry: -10), StyleSpecial.Revenge, 30),      // Стиль возмездия

        // ---- Дробящее ----
        [(WeaponCategory.Macing, 1)] = new(new(Damage: 20), new(Speed: -15)),                      // Молот войны
        [(WeaponCategory.Macing, 2)] = new(new(Hit: 10), new(Parry: -10)),                         // Цепной
        [(WeaponCategory.Macing, 3)] = new(new(), new(Damage: -10), StyleSpecial.Disarm, 15),      // Оглушающий удар
        [(WeaponCategory.Macing, 4)] = new(new(Crit: 8), new(Taken: 5)),                           // Рунный кузнец
        [(WeaponCategory.Macing, 5)] = new(new(), new(Speed: -10), StyleSpecial.Execute, 40),      // Палач
        [(WeaponCategory.Macing, 6)] = new(new(Damage: 15, Hit: 5), new(Taken: 10)),               // Штурмовой таран

        // ---- Колющее ----
        [(WeaponCategory.Fencing, 1)] = new(new(Hit: 12), new(Damage: -10)),                       // Копейщик
        [(WeaponCategory.Fencing, 2)] = new(new(), new(Taken: 10), StyleSpecial.Ambush, 40),       // Ассасин
        [(WeaponCategory.Fencing, 3)] = new(new(Damage: 15), new(Speed: -10)),                     // Пронзающий
        [(WeaponCategory.Fencing, 4)] = new(new(), new(Hit: -5), StyleSpecial.Cleave, 25),         // Вихрь клинков
        [(WeaponCategory.Fencing, 5)] = new(new(Parry: 12, Hit: 5), new(Damage: -10)),             // Дуэлянт-фехтовальщик
        [(WeaponCategory.Fencing, 6)] = new(new(), new(Hit: -5), StyleSpecial.Backstab, 40),       // Ночной клинок

        // ---- Стрельба из лука ----
        [(WeaponCategory.Archery, 1)] = new(new(), new(Speed: -10), StyleSpecial.Distance, 25),    // Снайпер
        [(WeaponCategory.Archery, 2)] = new(new(Speed: 20), new(Hit: -10)),                        // Шквальный огонь
        [(WeaponCategory.Archery, 3)] = new(new(Crit: 10), new(Damage: -5)),                       // Следопыт
        [(WeaponCategory.Archery, 4)] = new(new(Hit: 12), new(Speed: -10)),                        // Эльфийская стрельба
        [(WeaponCategory.Archery, 5)] = new(new(), new(Hit: -5), StyleSpecial.Headshot, 30),       // Охотник за головами
        [(WeaponCategory.Archery, 6)] = new(new(), new(Taken: 10), StyleSpecial.Ambush, 40),       // Тёмный лучник

        // ---- Метание ----
        [(WeaponCategory.Throwing, 1)] = new(new(Hit: 12), new(Speed: -10)),                       // Меткий бросок
        [(WeaponCategory.Throwing, 2)] = new(new(), new(Damage: -10), StyleSpecial.Bleed, 20),     // Отравленные лезвия
        [(WeaponCategory.Throwing, 3)] = new(new(Speed: 20), new(Damage: -10)),                    // Шквал клинков
        [(WeaponCategory.Throwing, 4)] = new(new(), new(Speed: -10), StyleSpecial.Cleave, 30),     // Смертельный вихрь
        [(WeaponCategory.Throwing, 5)] = new(new(), new(Hit: -5), StyleSpecial.Distance, 20),      // Дальнобойный бросок
        [(WeaponCategory.Throwing, 6)] = new(new(), new(Taken: 10), StyleSpecial.Ambush, 35),      // Теневой метатель
    };

    public static StyleTrade TradeOf(WeaponCategory category, byte style) =>
        Table.TryGetValue((category, style), out var trade) ? trade : null;

    /// <summary>Доля плюшки по тренировке стиля: пятая часть на 0, полная на 100.</summary>
    public static double Scale(Mobile m, WeaponCategory category, byte style) =>
        Math.Max(MinScale, Math.Clamp(WeaponStyleSystem.GetValue(m, category, style), 0, 100) / 100.0);

    /// <summary>Активный стиль для оружия в руках этого бойца, если у стиля есть обмен.</summary>
    private static bool TryActive(Mobile m, Item weapon, out WeaponCategory category, out byte style, out StyleTrade trade)
    {
        trade = null;
        style = 0;
        category = default;

        if (m == null || WeaponStyleSystem.CategoryOf(weapon) is not { } c)
        {
            return false;
        }

        category = c;
        style = WeaponStyleSystem.GetActiveStyle(m, c);
        trade = TradeOf(c, style);
        return trade != null;
    }

    private static int Scaled(int gain, double scale) => (int)Math.Round(gain * scale);

    /// <summary>Итог поля: плюшка × доля + цена.</summary>
    private static int Net(Mobile m, Item weapon, Func<StyleMods, int> field)
    {
        if (!TryActive(m, weapon, out var c, out var s, out var trade))
        {
            return 0;
        }

        return Scaled(field(trade.Gain), Scale(m, c, s)) + field(trade.Price);
    }

    public static double SpeedScalar(Mobile m, Item weapon) => 1.0 + Net(m, weapon, x => x.Speed) / 100.0;

    public static double DamageTakenScalar(Mobile m) => DamageTakenScalar(m, m?.Weapon as Item);

    public static double DamageTakenScalar(Mobile m, Item weapon) => 1.0 + Net(m, weapon, x => x.Taken) / 100.0;

    public static int ParryModifier(Mobile m) => ParryModifier(m, m?.Weapon as Item);

    public static int ParryModifier(Mobile m, Item weapon) => Net(m, weapon, x => x.Parry);

    /// <summary>Сила особой механики активного стиля (шанс в % или прибавка в %), 0 — нет её.</summary>
    public static int SpecialPower(Mobile m, Item weapon, StyleSpecial special)
    {
        if (!TryActive(m, weapon, out var c, out var s, out var trade) || trade.Special != special)
        {
            return 0;
        }

        return Scaled(trade.SpecialPower, Scale(m, c, s));
    }

    // ---- Засада: свежая цель ------------------------------------------------------------

    private static readonly Dictionary<(Mobile, Mobile), DateTime> LastHit = new();

    public static bool IsFreshTarget(Mobile attacker, Mobile defender) =>
        defender != null && (!LastHit.TryGetValue((attacker, defender), out var last) ||
                             Core.Now - last >= TimeSpan.FromSeconds(AmbushWindowSeconds));

    /// <summary>После попадания: цель больше не свежая. Старые пары чистятся заодно.</summary>
    public static void MarkHit(Mobile attacker, Mobile defender)
    {
        if (LastHit.Count > 2048)
        {
            var cutoff = Core.Now - TimeSpan.FromSeconds(AmbushWindowSeconds);
            var stale = new List<(Mobile, Mobile)>();

            foreach (var (key, time) in LastHit)
            {
                if (time < cutoff)
                {
                    stale.Add(key);
                }
            }

            foreach (var key in stale)
            {
                LastHit.Remove(key);
            }
        }

        LastHit[(attacker, defender)] = Core.Now;
    }

    // ---- Вклад в профиль удара ----------------------------------------------------------

    private sealed class Contributor : IStrikeContributor
    {
        public void Contribute(in StrikeContext context, StrikeProfile profile)
        {
            var attacker = context.Attacker;

            if (!TryActive(attacker, context.Weapon, out var c, out var s, out var trade))
            {
                return;
            }

            var scale = Scale(attacker, c, s);
            var source = $"Стиль: {WeaponStyleSystem.StyleName(c, s).ToLowerInvariant()}";

            profile.Add(source, StrikeField.HitChance, Scaled(trade.Gain.Hit, scale) + trade.Price.Hit);
            profile.Add(source, StrikeField.CritChance, Scaled(trade.Gain.Crit, scale) + trade.Price.Crit);
            profile.Add(source, StrikeField.Damage, Scaled(trade.Gain.Damage, scale) + trade.Price.Damage);

            var power = Scaled(trade.SpecialPower, scale);
            var defender = context.Defender;

            switch (trade.Special)
            {
                case StyleSpecial.Ambush when defender != null && IsFreshTarget(attacker, defender):
                {
                    profile.Add($"{source}, засада", StrikeField.Damage, power);
                    break;
                }

                case StyleSpecial.Distance when defender != null:
                {
                    var distance = attacker.GetDistanceToSqrt(defender);
                    profile.Add($"{source}, дистанция", StrikeField.Damage, Math.Round(Math.Min(power, distance * power / 8.0)));
                    break;
                }

                case StyleSpecial.Revenge when attacker.HitsMax > 0:
                {
                    var missing = 1.0 - (double)attacker.Hits / attacker.HitsMax;
                    profile.Add($"{source}, возмездие", StrikeField.Damage, Math.Round(power * Math.Clamp(missing, 0, 1)));
                    break;
                }

                case StyleSpecial.Execute when defender is { HitsMax: > 0 } && defender.Hits * 4 <= defender.HitsMax:
                {
                    profile.Add($"{source}, добивание", StrikeField.Damage, power);
                    break;
                }

                case StyleSpecial.Backstab when defender != null && HitLocationSystem.IsBehind(attacker, defender):
                {
                    profile.Add($"{source}, со спины", StrikeField.Damage, power);
                    break;
                }

                case StyleSpecial.Headshot when HitLocationSystem.GetChosenLocation(attacker) == HitLocation.Head &&
                                                (defender == null || HitLocationSystem.AimApplies(attacker, defender, HitLocation.Head)):
                {
                    profile.Add($"{source}, в голову", StrikeField.Damage, power);
                    break;
                }
            }
        }
    }

    private static readonly Contributor Instance = new();

    public static void Configure() => Register();

    /// <summary>Повторный вызов безвреден — для тестов, где Configure не звали.</summary>
    public static void Register() => StrikeProfiles.Register(Instance);

    // ---- Описание для окна --------------------------------------------------------------

    /// <summary>«плюшка — цена», с учётом нынешней тренировки: что даёт сейчас и сколько на 100.</summary>
    public static string Describe(Mobile m, WeaponCategory category, byte style)
    {
        var trade = TradeOf(category, style);

        if (trade == null)
        {
            return "без плюшки и без цены";
        }

        var scale = m == null ? 1.0 : Scale(m, category, style);
        var gain = new List<string>();

        AddPart(gain, Scaled(trade.Gain.Speed, scale), "% скорости");
        AddPart(gain, Scaled(trade.Gain.Hit, scale), " к попаданию");
        AddPart(gain, Scaled(trade.Gain.Crit, scale), "% крит");
        AddPart(gain, Scaled(trade.Gain.Damage, scale), "% урона");
        AddPart(gain, Scaled(trade.Gain.Parry, scale), " к парированию");
        AddPart(gain, -Scaled(trade.Gain.Taken, scale), "% стойкости");

        if (trade.Special != StyleSpecial.None)
        {
            gain.Add(SpecialText(trade.Special, Scaled(trade.SpecialPower, scale)));
        }

        var price = new List<string>();

        AddPart(price, trade.Price.Speed, "% скорости");
        AddPart(price, trade.Price.Hit, " к попаданию");
        AddPart(price, trade.Price.Crit, "% крит");
        AddPart(price, trade.Price.Damage, "% урона");
        AddPart(price, trade.Price.Parry, " к парированию");
        AddPart(price, -trade.Price.Taken, "% стойкости");

        var full = trade.SpecialPower > 0 || scale < 1.0 ? $" (на 100 — {FullGain(trade)})" : "";

        return $"{string.Join(", ", gain)}{full}; цена: {string.Join(", ", price)}";
    }

    private static string FullGain(StyleTrade trade)
    {
        var parts = new List<string>();
        AddPart(parts, trade.Gain.Speed, "% скорости");
        AddPart(parts, trade.Gain.Hit, " к попаданию");
        AddPart(parts, trade.Gain.Crit, "% крит");
        AddPart(parts, trade.Gain.Damage, "% урона");
        AddPart(parts, trade.Gain.Parry, " к парированию");
        AddPart(parts, -trade.Gain.Taken, "% стойкости");

        if (trade.Special != StyleSpecial.None)
        {
            parts.Add(SpecialText(trade.Special, trade.SpecialPower));
        }

        return string.Join(", ", parts);
    }

    private static void AddPart(List<string> parts, int value, string unit)
    {
        if (value != 0)
        {
            parts.Add($"{value:+0;−0}{unit}");
        }
    }

    private static string SpecialText(StyleSpecial special, int power) => special switch
    {
        StyleSpecial.Counter  => $"{power}% шанс контрудара, когда бьют тебя",
        StyleSpecial.Disarm   => $"{power}% шанс выбить оружие",
        StyleSpecial.Ambush   => $"+{power}% урона первым ударом по свежей цели",
        StyleSpecial.Distance => $"до +{power}% урона с расстояния",
        StyleSpecial.Cleave   => $"{power}% урона задевает ещё одного врага рядом",
        StyleSpecial.Revenge  => $"до +{power}% урона по мере потери здоровья",
        StyleSpecial.Execute  => $"+{power}% урона по цели, у которой меньше четверти здоровья",
        StyleSpecial.Backstab => $"+{power}% урона со спины",
        StyleSpecial.Headshot => $"+{power}% урона прицелом в голову",
        StyleSpecial.Bleed    => $"{power}% шанс пустить кровь",
        _                     => ""
    };

    // ---- После попадания ----------------------------------------------------------------

    /// <summary>Особые механики после удачного удара. Зовётся из WeaponStyleSystem.OnAfterHit.</summary>
    public static void OnAfterHit(Mobile attacker, Mobile defender, BaseWeapon weapon, int damage)
    {
        TryCounter(attacker, defender);

        var disarm = SpecialPower(attacker, weapon, StyleSpecial.Disarm);

        if (disarm > 0 && Utility.Random(100) < disarm)
        {
            TryDisarm(attacker, defender);
        }

        var cleave = SpecialPower(attacker, weapon, StyleSpecial.Cleave);

        if (cleave > 0 && damage > 0)
        {
            TryCleave(attacker, defender, damage * cleave / 100);
        }

        var bleed = SpecialPower(attacker, weapon, StyleSpecial.Bleed);

        if (bleed > 0 && defender.Alive && Utility.Random(100) < bleed)
        {
            BleedingSystem.ApplyBleed(defender, attacker);
        }

        MarkHit(attacker, defender);
    }

    /// <summary>Айкидо: того, кто бьёт айкидоку, может настичь контрудар.</summary>
    private static void TryCounter(Mobile attacker, Mobile defender)
    {
        var chance = SpecialPower(defender, defender.Weapon as Item, StyleSpecial.Counter);

        if (chance <= 0 || !defender.Alive || Utility.Random(100) >= chance)
        {
            return;
        }

        attacker.Damage(Utility.RandomMinMax(4, 10), defender);
        defender.FixedParticles(0x3735, 1, 30, 0x26B8, EffectLayer.Waist);
        defender.PlaySound(0x23A);
        defender.PublicOverheadMessage(MessageType.Regular, 0x59, false, "Контрудар!");
    }

    private static void TryDisarm(Mobile attacker, Mobile defender)
    {
        if (defender.Weapon is not BaseWeapon || defender.Weapon is Fists)
        {
            return;
        }

        var weapon = defender.FindItemOnLayer(Layer.OneHanded) ?? defender.FindItemOnLayer(Layer.TwoHanded);

        if (weapon is not BaseWeapon || defender.Backpack == null || !defender.Backpack.TryDropItem(defender, weapon, false))
        {
            return;
        }

        attacker.PublicOverheadMessage(MessageType.Regular, 0x59, false, "Оглушающий удар выбивает оружие!");
        defender.PlaySound(0x3B9);
    }

    /// <summary>Задеть ещё одного врага рядом с целью — того, с кем боец и так в бою.</summary>
    private static void TryCleave(Mobile attacker, Mobile defender, int damage)
    {
        if (damage <= 0 || attacker.Map == null)
        {
            return;
        }

        foreach (var m in attacker.GetMobilesInRange(1))
        {
            if (m == attacker || m == defender || !m.Alive || !attacker.CanBeHarmful(m, false))
            {
                continue;
            }

            // Только тот, кто сам с тобой в бою, — размах не должен задевать прохожих и своих.
            if (m.Combatant != attacker && attacker.Aggressors.Find(a => a.Attacker == m) == null)
            {
                continue;
            }

            attacker.DoHarmful(m);
            AOS.Damage(m, attacker, damage, 100, 0, 0, 0, 0);
            m.FixedEffect(0x37B9, 10, 8);
            attacker.SendMessage(0x22, $"Размах задевает {m.Name} на {damage}.");
            return; // ровно один сосед
        }
    }
}

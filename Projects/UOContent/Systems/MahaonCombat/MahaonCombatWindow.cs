using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Spells;

namespace Server.Systems.MahaonCombat;

/// <summary>
///     Данные для боевого окна и доски мастерства в Unity-клиенте. Формат — в
///     Ultima-main/COMBAT-UI-TASK.md, здесь — ровно он.
///
///     ---- Сервер считает, клиент рисует ----
///
///     Окно ничего не решает: оно показывает то, что прислал сервер, и отправляет выборы
///     игрока. Проверки («хватает ли тактики») живут в MahaonCombatChoices — тех же, что зовёт
///     старый гамп, — и после любого выбора, удачного или нет, сюда уходит свежее состояние.
///
///     ---- Шлём только тому, кто попросил ----
///
///     Старый клиент (ClassicUO) на незнакомом подтипе 0xF9 не молчит, а проваливается в
///     разбор подмены графики статики — то есть подтип 7 или 8 он прочтёт как мусорную
///     правку карты. Поэтому подтипы 7 и 8 уходят только тем, кто хоть раз прислал любой из
///     входящих 0x10–0x18: такой клиент формат знает. Старый не пришлёт их никогда.
/// </summary>
public static class MahaonCombatWindow
{
    private const byte PacketId = 0xF9;

    public const byte SubtypeBoard = 7;
    public const byte SubtypeCombat = 8;

    private const byte InLocation = 0x10;
    private const byte InStance = 0x11;
    private const byte InStyle = 0x12;
    private const byte InArcheryStance = 0x13;
    private const byte InElement = 0x14;
    private const byte InFavoriteSpell = 0x15;
    private const byte InOpenCombat = 0x16;
    private const byte InOpenBoard = 0x17;
    private const byte InChannel = 0x18;

    /// <summary>Какие соединения знают новый формат. Слабая таблица: ушедшее соединение
    /// уносит свою запись с собой.</summary>
    private static readonly ConditionalWeakTable<NetState, object> Speakers = new();

    private static readonly object Yes = new();

    public static bool SpeaksNewUi(NetState ns) => ns != null && Speakers.TryGetValue(ns, out _);

    public static unsafe void Configure()
    {
        IncomingPackets.Register(PacketId, 0, true, &OnPacket);
    }

    /// <summary>Свежее боевое окно — если клиент его понимает.</summary>
    public static void Push(PlayerMobile pm)
    {
        var ns = pm?.NetState;

        if (SpeaksNewUi(ns))
        {
            SendCombatWindow(ns, pm, 255);
        }
    }

    /// <summary>Свежая доска — если клиент её понимает.</summary>
    public static void PushBoard(Mobile m)
    {
        if (m is PlayerMobile pm && SpeaksNewUi(pm.NetState))
        {
            SendBoard(pm.NetState, pm);
        }
    }

    // ---- Входящие ---------------------------------------------------------------------------

    private static void OnPacket(NetState state, SpanReader reader)
    {
        if (state.Mobile is not PlayerMobile pm || reader.Remaining < 1)
        {
            return;
        }

        Speakers.AddOrUpdate(state, Yes);

        var subtype = reader.ReadByte();

        switch (subtype)
        {
            case InLocation when reader.Remaining >= 1:
            {
                var id = reader.ReadByte();

                if (Enum.IsDefined(typeof(HitLocation), (int)id))
                {
                    MahaonCombatChoices.SetLocation(pm, (HitLocation)id);
                }

                break;
            }

            case InStance when reader.Remaining >= 1:
            {
                var id = reader.ReadByte();

                if (Enum.IsDefined(typeof(CombatStance), (int)id))
                {
                    MahaonCombatChoices.SetStance(pm, (CombatStance)id);
                }

                break;
            }

            case InStyle when reader.Remaining >= 2:
            {
                var category = reader.ReadByte();
                var style = reader.ReadByte();

                if (Enum.IsDefined(typeof(WeaponCategory), category))
                {
                    MahaonCombatChoices.SetStyle(pm, (WeaponCategory)category, style);
                }

                break;
            }

            case InArcheryStance when reader.Remaining >= 1:
            {
                var id = reader.ReadByte();

                if (Enum.IsDefined(typeof(ArcheryStance), id))
                {
                    MahaonCombatChoices.SetArcheryStance(pm, (ArcheryStance)id);
                }

                break;
            }

            case InElement when reader.Remaining >= 1:
            {
                MahaonCombatChoices.SetElement(pm, ElementFromWire(reader.ReadByte()));
                break;
            }

            case InFavoriteSpell when reader.Remaining >= 2:
            {
                MahaonCombatChoices.SetFavoriteSpell(pm, reader.ReadUInt16());
                break;
            }

            case InOpenCombat:
            {
                SendCombatWindow(state, pm, 255);
                break;
            }

            case InOpenBoard:
            {
                SendBoard(state, pm);
                break;
            }

            case InChannel when reader.Remaining >= 1:
            {
                var id = reader.ReadByte();

                if (Enum.IsDefined(typeof(CastingChannel), id))
                {
                    MahaonCombatChoices.SetChannel(pm, (CastingChannel)id);
                }

                break;
            }
        }
    }

    // Стихии на проводе: 0 нет, 1 огонь, 2 холод, 3 яд, 4 энергия. В PreferredElement порядок
    // другой (огонь, яд, энергия, холод), поэтому перевод явный, а не приведением.
    private static PreferredElement? ElementFromWire(byte b) => b switch
    {
        1 => PreferredElement.Fire,
        2 => PreferredElement.Cold,
        3 => PreferredElement.Poison,
        4 => PreferredElement.Energy,
        _ => null
    };

    private static byte ElementToWire(PreferredElement? e) => e switch
    {
        PreferredElement.Fire   => 1,
        PreferredElement.Cold   => 2,
        PreferredElement.Poison => 3,
        PreferredElement.Energy => 4,
        _                       => 0
    };

    // ---- Подтип 8: боевое окно -----------------------------------------------------------

    /// <summary>
    ///     Тело для куклы — тем же правилом, что у клиента (ClassicUO,
    ///     PaperDollInteractable): по графике тела мобиля, а не по полю «раса».
    /// </summary>
    public static ushort BodyGump(Mobile m) => m.Body.BodyID switch
    {
        0x0191 or 0x0193 => 0x000D,
        0x025D           => 0x000E,
        0x025E           => 0x000F,
        0x029A or 0x02B6 => 0x029A,
        0x029B or 0x02B7 => 0x0299,
        0x04E5           => 0xC835,
        _                => m.Female ? (ushort)0x000D : (ushort)0x000C
    };

    // Эффекты мест — те, что уже есть в бою: брешь в броне (HitLocationSystem), переломы
    // (BoneFractureSystem: рука — урон −30%, нога — не бегать, кисть — мешает колдовать) и
    // сбитый каст (CastingChannelSystem).
    private static string LocationEffect(HitLocation location) => location switch
    {
        HitLocation.Neck  => "×2 урона; сбивает каст голосом",
        HitLocation.Head  => "сбивает каст разумом",
        HitLocation.Hands => "больнее по слабой броне; может сломать кисть — мешает колдовать; сбивает каст руками",
        HitLocation.Arms  => "больнее по слабой броне; может сломать руку — цель бьёт на 30% слабее",
        HitLocation.Legs  => "больнее по слабой броне; может сломать ногу — цель не может бегать",
        HitLocation.Back  => "только со спины: +25% урона и больнее по слабой броне",
        _                 => "больнее по слабой броне"
    };

    private static string StanceTrade(CombatStance stance) => stance switch
    {
        CombatStance.Defensive    => "урон ×0.5, получаемый урон ×0.5",
        CombatStance.Aggressive2x => "удары на 25% чаще, но на 10% слабее и −10 к попаданию (тактика 60)",
        CombatStance.Aggressive3x => "удары на 45% чаще, но на 15% слабее и −15 к попаданию (тактика 100)",
        _                         => "без изменений"
    };

    private static string ChannelTrade(CastingChannel channel) => channel switch
    {
        CastingChannel.Voice => "сбивается ударом в шею",
        CastingChannel.Mind  => "сбивается ударом в голову",
        _                    => "сбивается ударом по кистям"
    };

    /// <summary>Одна строка чека: метка, итог и вклады под ним.</summary>
    private sealed class Row
    {
        public string Label;
        public string Value;
        public readonly List<(string Text, sbyte Sign)> Lines = new();

        public Row Line(string text, double value) =>
            Line(text, (sbyte)(value > 0 ? 1 : value < 0 ? -1 : 0));

        public Row Line(string text, sbyte sign)
        {
            Lines.Add((text, sign));
            return this;
        }
    }

    private sealed record Receipt(string Title, List<Row> Rows);

    private static string Signed(double v, string unit = "") =>
        v > 0 ? $"+{v:0.#}{unit}" : $"{v:0.#}{unit}";

    /// <summary>
    ///     Чек удара. Скорость, попадание и крит — живые: считаются сейчас, без броска. Урон —
    ///     по ПОСЛЕДНЕМУ настоящему удару: двадцать семь его слагаемых собираются только в самом
    ///     ударе, часть из них расходуется в момент удара, и пересобирать их здесь отдельно
    ///     значило бы получить второй подсчёт, который однажды разойдётся с первым.
    /// </summary>
    private static Receipt MeleeReceipt(PlayerMobile pm, BaseWeapon weapon, Mobile target)
    {
        var rows = new List<Row>();
        var live = weapon != null ? StrikeProfiles.Gather(pm, target, weapon) : new StrikeProfile();

        var speed = new Row { Label = "Скорость удара", Value = weapon != null ? $"{weapon.GetDelay(pm).TotalSeconds:0.00} с" : "—" };
        var stanceSpeed = CombatStanceSystem.GetSwingSpeedScalar(pm);

        if (stanceSpeed != 1.0)
        {
            speed.Line($"+{(stanceSpeed - 1) * 100:0}% стойка: {MahaonCombatChoices.StanceName(CombatStanceSystem.GetStance(pm)).ToLowerInvariant()}", +1);
        }

        rows.Add(speed);

        var hit = new Row { Label = "Шанс попадания" };

        if (weapon != null && target != null)
        {
            hit.Value = $"{Math.Clamp(weapon.ComputeHitChance(pm, target), 0.0, 1.0) * 100:0}%";
        }
        else
        {
            hit.Value = "—";
            hit.Line("нет цели — шанс зависит от противника", 0);
        }

        foreach (var line in live.Lines)
        {
            if (line.Field == StrikeField.HitChance)
            {
                hit.Line($"{Signed(line.Value)} {line.Source}", line.Value);
            }
        }

        rows.Add(hit);

        var crit = new Row { Label = "Шанс крита", Value = $"{live.CritChance:0.#}%" };

        foreach (var line in live.Lines)
        {
            if (line.Field == StrikeField.CritChance)
            {
                crit.Line($"{Signed(line.Value, "%")} {line.Source}", line.Value);
            }
        }

        rows.Add(crit);

        var damage = new Row { Label = "Урон" };
        var last = StrikeProfiles.LastOf(pm);

        if (last == null)
        {
            damage.Value = "—";
            damage.Line("ударь кого-нибудь — здесь появится разбор", 0);
        }
        else
        {
            damage.Value = Signed(Math.Min(last.Sum(StrikeField.Damage), 300), "%");
            damage.Line("по последнему удару:", 0);

            foreach (var line in last.Lines)
            {
                if (line.Field == StrikeField.Damage)
                {
                    damage.Line($"{Signed(line.Value, "%")} {line.Source}", line.Value);
                }
            }
        }

        rows.Add(damage);

        var aim = HitLocationSystem.GetChosenLocation(pm);

        rows.Add(
            aim != null
                ? new Row { Label = "Цель", Value = MahaonCombatChoices.LocationName(aim.Value) }.Line(LocationEffect(aim.Value), 0)
                : new Row { Label = "Цель", Value = "куда придётся" }
        );

        AddWoundRows(rows, pm, weapon, target);

        return new Receipt("ИТОГ УДАРА", rows);
    }

    /// <summary>Чек выстрела: дальность и скорость по стойке, попадание и крит — живые.</summary>
    private static Receipt RangedReceipt(PlayerMobile pm, BaseWeapon weapon, Mobile target)
    {
        var rows = new List<Row>();

        if (weapon is not { IsRangedWeapon: true })
        {
            rows.Add(new Row { Label = "Оружие", Value = "не стрелковое" });
            return new Receipt("ИТОГ ВЫСТРЕЛА", rows);
        }

        var live = StrikeProfiles.Gather(pm, target, weapon);
        var stance = ArcheryStanceSystem.Active(pm);
        var stanceName = ArcheryStanceSystem.Name(stance).ToLowerInvariant();

        var range = new Row { Label = "Дальность", Value = $"{ArcheryStanceSystem.CombatRange(pm, weapon)} тайлов" };

        foreach (var line in live.Lines)
        {
            if (line.Field == StrikeField.Range)
            {
                range.Line($"{Signed(line.Value)} {line.Source}", line.Value);
            }
        }

        rows.Add(range);

        var factor = ArcheryStanceSystem.DelayFactor(pm, weapon);
        var speed = new Row { Label = "Скорость", Value = $"{weapon.GetDelay(pm).TotalSeconds:0.00} с" };

        if (factor != 1.0)
        {
            speed.Line($"{Signed((1.0 / factor - 1.0) * 100, "%")} {stanceName}", factor < 1.0 ? 1 : -1);
        }

        rows.Add(speed);

        var hit = new Row
        {
            Label = "Шанс попадания",
            Value = target != null ? $"{Math.Clamp(weapon.ComputeHitChance(pm, target), 0.0, 1.0) * 100:0}%" : "—"
        };

        foreach (var line in live.Lines)
        {
            if (line.Field == StrikeField.HitChance)
            {
                hit.Line($"{Signed(line.Value)} {line.Source}", line.Value);
            }
        }

        rows.Add(hit);

        var crit = new Row { Label = "Шанс крита", Value = $"{live.CritChance:0.#}%" };

        foreach (var line in live.Lines)
        {
            if (line.Field == StrikeField.CritChance)
            {
                crit.Line($"{Signed(line.Value, "%")} {line.Source}", line.Value);
            }
        }

        rows.Add(crit);

        AddWoundRows(rows, pm, weapon, target);

        return new Receipt("ИТОГ ВЫСТРЕЛА", rows);
    }

    /// <summary>
    ///     Шансы ран при попадании: перелом (прицел в руки, ноги или кисти) и кровотечение
    ///     (стиль «Отравленные лезвия»). Шанс — на каждый удар, который прошёл (не парирован).
    /// </summary>
    private static void AddWoundRows(List<Row> rows, PlayerMobile pm, BaseWeapon weapon, Mobile target)
    {
        var aim = HitLocationSystem.GetChosenLocation(pm);
        var fracture = new Row { Label = "Перелом" };

        if (aim is { } location && BoneFractureSystem.CanFracture(location) && HitLocationSystem.CanUseLocation(pm, location))
        {
            fracture.Value = $"{BoneFractureSystem.FractureChance * 100:0}%";
            fracture.Line($"прицел: {MahaonCombatChoices.LocationName(location).ToLowerInvariant()}, за каждое прошедшее попадание", 0);
        }
        else
        {
            fracture.Value = "0%";
            fracture.Line("ломают только прицелом в руки, ноги или кисти", 0);
        }

        rows.Add(fracture);

        var bleedChance = weapon == null ? 0 : WeaponStylePerks.SpecialPower(pm, weapon, StyleSpecial.Bleed);
        var bleed = new Row { Label = "Кровотечение", Value = $"{bleedChance}%" };

        if (bleedChance > 0)
        {
            bleed.Line("стиль «Отравленные лезвия» — растёт с тренировкой стиля", +1);
        }
        else
        {
            bleed.Line("кровь пускает стиль «Отравленные лезвия» (метание)", 0);
        }

        rows.Add(bleed);
    }

    private static Receipt SpellReceipt(PlayerMobile pm)
    {
        var rows = new List<Row>
        {
            new() { Label = "Канал каста", Value = CastingChannelSystem.RuChannelName(CastingChannelSystem.GetChannel(pm)) }
        };

        var element = PreferredElementSystem.GetPreferred(pm);

        rows.Add(
            element != null
                ? new Row { Label = "Стихия", Value = PreferredElementSystem.RuElementName(element.Value) }
                    .Line("половина урона заклинаний уходит в эту стихию", 0)
                : new Row { Label = "Стихия", Value = "не выбрана" }
        );

        var favorite = FavoriteSpell(pm);

        if (favorite == null)
        {
            if (element != null)
            {
                rows[^1].Line(
                    $"цена: +{PreferredElementSystem.BasePremium * 100:0}% маны, школа заклинания сбивает до " +
                    $"+{(PreferredElementSystem.BasePremium - PreferredElementSystem.SchoolPremiumReduction) * 100:0}%",
                    -1
                );
            }

            rows.Add(new Row { Label = "Любимое заклинание", Value = "не выбрано" });
            return new Receipt("ИТОГ ЗАКЛИНАНИЯ", rows);
        }

        var effective = SpellMasterySystem.Effective(pm, favorite);
        var raw = SpellMasterySystem.Raw(pm, favorite);
        var school = SpellSchools.TryDescribe(pm, favorite, out var info) ? info : default;

        rows.Add(new Row { Label = "Любимое заклинание", Value = SpellSchools.RuName(favorite) });

        var mastery = new Row { Label = "Мастерство", Value = $"{Math.Floor(effective):0}" };

        if (effective > Math.Floor(raw) + 0.5)
        {
            mastery.Line($"своё {Math.Floor(raw):0}, +{effective - raw:0.#} от школы «{school.Name}»", +1);
        }

        rows.Add(mastery);

        var mana = new Row { Label = "Мана", Value = $"{favorite.ScaleMana(favorite.GetMana())}" };

        if (effective > 0)
        {
            mana.Line($"−{SpellMasterySystem.MaxManaDiscount * effective:0.#}% мастерство", +1);
        }

        if (element != null)
        {
            var premium = PreferredElementSystem.Premium(pm, favorite);
            mana.Line(
                $"+{premium * 100:0.#}% стихия — при уроне, школа «{school.Name}» {school.Value:0} сбивает с +30% до +10%",
                -1
            );
        }

        rows.Add(mana);

        var cast = new Row { Label = "Время каста", Value = $"{favorite.GetCastDelay().TotalSeconds:0.00} с" };

        if (effective > 0)
        {
            cast.Line($"−{SpellMasterySystem.MaxCastSpeedup * effective:0.#}% мастерство", +1);
        }

        rows.Add(cast);

        rows.Add(
            new Row { Label = "Сила", Value = $"+{SpellMasterySystem.MaxPower * effective:0.#}%" }
                .Line("урон и лечение — от мастерства", 0)
        );

        return new Receipt("ИТОГ ЗАКЛИНАНИЯ", rows);
    }

    /// <summary>Любимое заклинание как экземпляр с этим магом — чтобы считать его цену и время.</summary>
    private static Spell FavoriteSpell(PlayerMobile pm)
    {
        var id = MahaonCombatChoices.GetFavoriteSpell(pm);
        return id > 0 ? SpellRegistry.NewSpell(id, pm, null) : null;
    }

    /// <summary>Наполненность шара — действующее мастерство любимого заклинания, 0–100.</summary>
    private static double FavoriteMastery(PlayerMobile pm)
    {
        var spell = FavoriteSpell(pm);
        return spell == null ? 0 : Math.Clamp(SpellMasterySystem.Effective(pm, spell), 0, 100);
    }

    public static void SendCombatWindow(NetState ns, PlayerMobile pm, byte tab)
    {
        if (ns.CannotSendPackets() || pm == null)
        {
            return;
        }

        ns.Send(BuildCombatWindow(pm, tab));
    }

    /// <summary>
    ///     Пакет боевого окна целиком. Сборка отдельно от отправки: тесты разбирают именно эти
    ///     байты — большой пакет сетевой слой сразу выталкивает в сокет, и в буфере отправки
    ///     его уже не прочесть.
    /// </summary>
    internal static byte[] BuildCombatWindow(PlayerMobile pm, byte tab)
    {
        var weapon = pm.Weapon as BaseWeapon;
        var target = pm.Combatant;

        var writer = new SpanWriter(stackalloc byte[2048], resize: true);

        writer.Write(PacketId);
        writer.Write((ushort)0); // длина — WritePacketLength ниже
        writer.Write(SubtypeCombat);
        writer.Write(tab);
        writer.Write(BodyGump(pm));

        // места удара
        var chosen = HitLocationSystem.GetChosenLocation(pm);
        var locations = (HitLocation[])Enum.GetValues(typeof(HitLocation));

        writer.Write((byte)locations.Length);

        foreach (var location in locations)
        {
            var usable = HitLocationSystem.CanUseLocation(pm, location);

            writer.Write((byte)location);
            MahaonSkillTreePackets.WriteShortUtf8(ref writer, MahaonCombatChoices.LocationName(location));
            writer.Write((byte)(!usable ? 0 : chosen == location ? 2 : 1));
            writer.Write((ushort)Math.Round(HitLocationSystem.TacticsRequirement(location) * 10));
            writer.Write((sbyte)HitLocationSystem.HitModifier(location));
            MahaonSkillTreePackets.WriteLongUtf8(ref writer, LocationEffect(location));
        }

        // цель и то, что игрок о ней уже знает: слабые места (множитель больше 10) и крепкие
        // (меньше 10) — только открытые
        MahaonSkillTreePackets.WriteShortUtf8(ref writer, target?.Name ?? "");

        var known = MahaonWeakPoints.Known(pm, target);

        writer.Write((byte)known.Count);

        foreach (var point in known)
        {
            writer.Write((byte)point.Location);
            writer.Write((byte)Math.Round(point.Multiplier * 10));
        }

        // стойки
        var stance = CombatStanceSystem.GetStance(pm);
        var stances = (CombatStance[])Enum.GetValues(typeof(CombatStance));

        writer.Write((byte)stances.Length);

        foreach (var s in stances)
        {
            writer.Write((byte)s);
            MahaonSkillTreePackets.WriteShortUtf8(ref writer, MahaonCombatChoices.StanceName(s));
            writer.Write((byte)(s == stance ? 1 : 0));
            MahaonSkillTreePackets.WriteLongUtf8(
                ref writer,
                CombatStanceSystem.CanUseStance(pm, s) ? StanceTrade(s) : StanceTrade(s) + " — не хватает тактики"
            );
        }

        // стили вида оружия в руках
        var category = WeaponStyleSystem.CategoryOf(weapon);

        if (category == null)
        {
            writer.Write((byte)255);
            writer.Write((byte)0);
        }
        else
        {
            var active = WeaponStyleSystem.GetActiveStyle(pm, category.Value);

            writer.Write((byte)category.Value);
            writer.Write((byte)WeaponStyleSystem.StyleCount);

            for (byte style = 0; style < WeaponStyleSystem.StyleCount; style++)
            {
                writer.Write(style);
                MahaonSkillTreePackets.WriteShortUtf8(ref writer, WeaponStyleSystem.StyleName(category.Value, style));
                writer.Write((byte)(style == active ? 1 : 0));
                writer.Write((ushort)Math.Round(WeaponStyleSystem.GetValue(pm, category.Value, style) * 10));
                MahaonSkillTreePackets.WriteLongUtf8(ref writer, WeaponStylePerks.Describe(pm, category.Value, style));
            }
        }

        // стойки лучника
        var archery = ArcheryStanceSystem.Active(pm);
        var archeryStances = (ArcheryStance[])Enum.GetValues(typeof(ArcheryStance));

        writer.Write((byte)archeryStances.Length);

        foreach (var a in archeryStances)
        {
            writer.Write((byte)a);
            MahaonSkillTreePackets.WriteShortUtf8(ref writer, ArcheryStanceSystem.Name(a));
            writer.Write((byte)(a == archery ? 1 : 0));
            MahaonSkillTreePackets.WriteLongUtf8(
                ref writer,
                ArcheryStanceSystem.CanUse(pm, a) ? ArcheryStanceSystem.Trade(a) : ArcheryStanceSystem.Trade(a) + " — пока недоступно"
            );
        }

        writer.Write(ElementToWire(PreferredElementSystem.GetPreferred(pm)));
        writer.Write((ushort)Math.Clamp(MahaonCombatChoices.GetFavoriteSpell(pm), 0, ushort.MaxValue));

        // каналы каста
        var channel = CastingChannelSystem.GetChannel(pm);
        var channels = (CastingChannel[])Enum.GetValues(typeof(CastingChannel));

        writer.Write((byte)channels.Length);

        foreach (var c in channels)
        {
            writer.Write((byte)c);
            MahaonSkillTreePackets.WriteShortUtf8(ref writer, CastingChannelSystem.RuChannelName(c));
            writer.Write((byte)(!CastingChannelSystem.CanUseChannel(pm, c) ? 0 : c == channel ? 2 : 1));
            MahaonSkillTreePackets.WriteLongUtf8(ref writer, ChannelTrade(c));
        }

        // три чека
        WriteReceipt(ref writer, MeleeReceipt(pm, weapon, target));
        WriteReceipt(ref writer, RangedReceipt(pm, weapon, target));
        WriteReceipt(ref writer, SpellReceipt(pm));

        // шар: стихия — выбранная; наполненность считает этап 7, до тех пор пусто
        writer.Write(ElementToWire(PreferredElementSystem.GetPreferred(pm)));
        writer.Write((byte)Math.Floor(FavoriteMastery(pm)));

        writer.WritePacketLength();

        var bytes = writer.Span.ToArray();
        writer.Dispose();

        return bytes;
    }

    private static void WriteReceipt(ref SpanWriter writer, Receipt receipt)
    {
        MahaonSkillTreePackets.WriteShortUtf8(ref writer, receipt.Title);
        writer.Write((byte)receipt.Rows.Count);

        foreach (var row in receipt.Rows)
        {
            MahaonSkillTreePackets.WriteShortUtf8(ref writer, row.Label);
            MahaonSkillTreePackets.WriteShortUtf8(ref writer, row.Value ?? "");
            writer.Write((byte)row.Lines.Count);

            foreach (var (text, sign) in row.Lines)
            {
                MahaonSkillTreePackets.WriteLongUtf8(ref writer, text);
                writer.Write(sign);
            }
        }
    }

    // ---- Подтип 7: доска мастерства -------------------------------------------------------

    /// <summary>
    ///     Доска строится из того же снимка, что и нынешнее окно «Стили» (MahaonSkillTree), —
    ///     чтобы две картинки прокачки не разошлись, — плюс категория «Владение оружием» с
    ///     линейками мастерства.
    ///
    ///     Ключ ветки пока «name:имя ветки»: имя уникально (по нему же хранятся потолки), а
    ///     общий реестр с устойчивыми именами дорожек ещё впереди. Клиенту ключ непрозрачен —
    ///     годится, чтобы помнить, что раскрыто.
    /// </summary>
    public static void SendBoard(NetState ns, PlayerMobile pm)
    {
        if (ns.CannotSendPackets() || pm == null)
        {
            return;
        }

        ns.Send(BuildBoard(pm));
    }

    /// <summary>Пакет доски целиком — по той же причине, что и BuildCombatWindow.</summary>
    internal static byte[] BuildBoard(PlayerMobile pm)
    {
        var tree = MahaonSkillTree.GetTreeSnapshot(pm);
        var writer = new SpanWriter(stackalloc byte[8192], resize: true);

        writer.Write(PacketId);
        writer.Write((ushort)0);
        writer.Write(SubtypeBoard);
        writer.Write((byte)(tree.Length + 1));

        foreach (var category in tree)
        {
            MahaonSkillTreePackets.WriteShortUtf8(ref writer, category.Name);

            // Сводный список «прочих навыков» — это несколько навыков без веток: каждый идёт
            // своей строкой.
            var skills = new List<(int Skill, string Name, double Value, double Cap, MahaonSkillTree.SkillSubCategory? Sub)>();

            foreach (var sub in category.SubCategories)
            {
                if (sub.IsUsableSkillList)
                {
                    foreach (var entry in sub.Entries)
                    {
                        skills.Add((-1, entry.Name, entry.Value, entry.Cap > 0 ? entry.Cap : 100, null));
                    }

                    continue;
                }

                var value = sub.SkillId >= 0 ? pm.Skills[(SkillName)sub.SkillId].Value : 0.0;
                var cap = sub.SkillId >= 0 ? pm.Skills[(SkillName)sub.SkillId].Cap : 100.0;

                skills.Add((sub.SkillId, sub.Name, value, cap, sub));
            }

            writer.Write((byte)skills.Count);

            foreach (var (skill, name, value, cap, sub) in skills)
            {
                writer.Write((byte)(skill >= 0 ? skill : 255));
                MahaonSkillTreePackets.WriteShortUtf8(ref writer, name);
                writer.Write((ushort)Math.Round(value * 10));
                writer.Write((ushort)Math.Round(cap * 10));

                var entries = sub?.Entries ?? Array.Empty<MahaonSkillTree.SkillEntry>();

                writer.Write((byte)entries.Length);

                foreach (var entry in entries)
                {
                    MahaonSkillTreePackets.WriteShortUtf8(ref writer, "name:" + entry.Name);
                    MahaonSkillTreePackets.WriteShortUtf8(ref writer, entry.Name);
                    writer.Write((ushort)Math.Round(entry.Value * 10));
                    writer.Write((ushort)Math.Round((entry.Cap > 0 ? entry.Cap : 100) * 10));
                    writer.Write((byte)(sub.Value.IsSelectable && entry.EntryId == sub.Value.SelectedId ? 1 : 0));
                    // Перки по ступеням (этап 8): у школ магии и ремёсел; у прочих веток — пусто.
                    var schoolPerks = SchoolPerks.PerksOf(SchoolPerks.LineOf(skill, entry.Name));
                    var craftPerks = schoolPerks.Count > 0
                        ? Array.Empty<CraftPerk>()
                        : CraftPerks.PerksOf(CraftPerks.LineOf(skill, entry.Name));

                    writer.Write((byte)(schoolPerks.Count + craftPerks.Count));

                    foreach (var perk in schoolPerks)
                    {
                        writer.Write((byte)perk.Threshold);
                        writer.Write((byte)(entry.Value >= perk.Threshold ? 1 : 0));
                        MahaonSkillTreePackets.WriteLongUtf8(ref writer, SchoolPerks.Describe(perk));
                    }

                    foreach (var perk in craftPerks)
                    {
                        writer.Write((byte)perk.Threshold);
                        writer.Write((byte)(entry.Value >= perk.Threshold ? 1 : 0));
                        MahaonSkillTreePackets.WriteLongUtf8(ref writer, CraftPerks.Describe(perk));
                    }
                }

                WriteSpellLines(ref writer, pm, skill);
            }
        }

        WriteWeaponMasteryCategory(ref writer, pm);

        writer.WritePacketLength();

        var bytes = writer.Span.ToArray();
        writer.Dispose();

        return bytes;
    }

    /// <summary>
    ///     Линейки мастерства заклинаний (этап 7): у магического навыка линейки — его школы,
    ///     предметы — заклинания этой школы, все, а не только начатые: доска показывает, что
    ///     вообще можно развить. Иконка — из атласа гампов (iconKind = 1), как в книге.
    ///     У немагического навыка линеек нет.
    /// </summary>
    private static void WriteSpellLines(ref SpanWriter writer, PlayerMobile pm, int skill)
    {
        var lines = new List<(string Line, string Name, List<SpellSchools.Entry> Spells)>();

        if (skill >= 0)
        {
            foreach (var entry in SpellSchools.Catalog)
            {
                if ((int)entry.CastSkill != skill)
                {
                    continue;
                }

                var index = lines.FindIndex(l => l.Line == entry.Line);

                if (index < 0)
                {
                    lines.Add((entry.Line, entry.SchoolName, new List<SpellSchools.Entry>()));
                    index = lines.Count - 1;
                }

                lines[index].Spells.Add(entry);
            }
        }

        writer.Write((byte)lines.Count);

        foreach (var (line, name, spells) in lines)
        {
            MahaonSkillTreePackets.WriteShortUtf8(ref writer, "line:" + line);
            MahaonSkillTreePackets.WriteShortUtf8(ref writer, name);
            writer.Write((byte)spells.Count);

            foreach (var entry in spells)
            {
                var track = SpellMasterySystem.TrackOf(entry.Line, entry.Type);
                var raw = SpellMasterySystem.Raw(pm, track);
                var effective = SpellMasterySystem.Effective(pm, track);

                MahaonSkillTreePackets.WriteShortUtf8(ref writer, track);
                MahaonSkillTreePackets.WriteShortUtf8(ref writer, SpellSchools.RuName(entry.Id, entry.Type.Name));
                writer.Write((byte)1); // иконка — гамп заклинания
                writer.Write((ushort)SpellSchools.IconOf(entry.Id));
                writer.Write((byte)Math.Floor(raw));
                writer.Write((byte)Math.Floor(effective));
                writer.Write((byte)Math.Floor((raw - Math.Floor(raw)) * 255));
                MahaonSkillTreePackets.WriteLongUtf8(ref writer, SpellMasterySystem.BonusText(effective));
            }
        }
    }

    private static void WriteWeaponMasteryCategory(ref SpanWriter writer, PlayerMobile pm)
    {
        MahaonSkillTreePackets.WriteShortUtf8(ref writer, "Владение оружием");
        writer.Write((byte)WeaponMasterySystem.Lines.Count);

        foreach (var (lineId, lineName) in WeaponMasterySystem.Lines)
        {
            var best = 0.0;

            foreach (var kind in WeaponMasterySystem.KindsInLine(lineId))
            {
                best = Math.Max(best, WeaponMasterySystem.Raw(pm, kind));
            }

            // У линейки нет своего навыка: это отдельный блок, а его «значение» — лучшее
            // мастерство в линейке.
            writer.Write((byte)255);
            MahaonSkillTreePackets.WriteShortUtf8(ref writer, lineName);
            writer.Write((ushort)Math.Round(best * 10));
            writer.Write((ushort)(WeaponMasterySystem.MaxLevel * 10));
            writer.Write((byte)0); // ветвей нет
            writer.Write((byte)1); // одна линейка — она сама

            MahaonSkillTreePackets.WriteShortUtf8(ref writer, "line:" + lineId);
            MahaonSkillTreePackets.WriteShortUtf8(ref writer, lineName);

            var kinds = new List<WeaponMasterySystem.Kind>(WeaponMasterySystem.KindsInLine(lineId));

            writer.Write((byte)kinds.Count);

            foreach (var kind in kinds)
            {
                var raw = WeaponMasterySystem.Raw(pm, kind);
                var effective = WeaponMasterySystem.Effective(pm, kind);

                MahaonSkillTreePackets.WriteShortUtf8(ref writer, kind.Track);
                MahaonSkillTreePackets.WriteShortUtf8(ref writer, kind.Name);
                writer.Write((byte)0); // иконка — арт предмета
                writer.Write((ushort)kind.Icon);
                writer.Write((byte)Math.Floor(raw));
                writer.Write((byte)Math.Floor(effective));
                writer.Write((byte)Math.Floor((raw - Math.Floor(raw)) * 255));
                MahaonSkillTreePackets.WriteLongUtf8(
                    ref writer,
                    $"+{effective * WeaponMasterySystem.HitPerLevel:0.#} к попаданию, " +
                    $"+{effective * WeaponMasterySystem.CritPerLevel:0.#}% крит, " +
                    $"+{effective * WeaponMasterySystem.DamagePerLevel:0.#}% урона"
                );
            }
        }
    }
}

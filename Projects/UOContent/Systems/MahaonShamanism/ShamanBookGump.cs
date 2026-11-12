using System;
using System.Collections.Generic;
using Server.Gumps;
using Server.Items;
using Server.Network;
using Server.Spells;
using Server.Spells.Shamanism;

namespace Server.Systems.MahaonShamanism;

/// <summary>
///     Список обрядов книги и их номера в реестре заклинаний.
///
///     Номера 300–331 свободны у движка (там лежат Магия 0–63, Некромантия 100+, Рыцарство
///     200+, Бусидо/Ниндзюцу 400/500, Плетение 600+, Мистицизм 677+). Порядок — разделами
///     по восемь, внутри раздела по ступеням. Номер — это и ключ кнопки в гампах, поэтому
///     менять раскладку уже раздаваемых номеров не стоит.
/// </summary>
public static class ShamanSpellbookTable
{
    public const int FirstId = 300;

    public record Entry(int Id, Type Type, ShamanTradition Tradition, int Index, string Name, bool Posthumous);

    private static readonly Type[] Order =
    {
        // Орочье
        typeof(EmberSpell), typeof(WarCrySpell), typeof(BloodSacrificeSpell), typeof(FireRingSpell),
        typeof(AncestralRageSpell), typeof(FlameWaveSpell), typeof(AshRainSpell), typeof(MeteorSpell),

        // Гоблинское
        typeof(EvilEyeSpell), typeof(RotFogSpell), typeof(PhantomSpell), typeof(HexSpell),
        typeof(DivinationSpell), typeof(DanceManiaSpell), typeof(DelusionSpell), typeof(PestilenceSpell),

        // Огрское
        typeof(StoneSkinSpell), typeof(BoulderThrowSpell), typeof(StompSpell), typeof(RockWallSpell),
        typeof(EarthGripSpell), typeof(RockfallSpell), typeof(QuakeSpell), typeof(MountainAwakeningSpell),

        // Древнее
        typeof(AncestorWhisperSpell), typeof(WindGustSpell), typeof(GuardianSpiritSpell), typeof(BanishmentSpell),
        typeof(SoulSealSpell), typeof(ThunderCallSpell), typeof(GreatStormSpell), typeof(FuneralFeastSpell)
    };

    private static readonly List<Entry> All = new();
    private static readonly List<Entry> Posthumous = new();

    public static IReadOnlyList<Entry> Entries => All;
    public static IReadOnlyList<Entry> PosthumousSpells => Posthumous;

    public static void Configure()
    {
        for (var i = 0; i < Order.Length; i++)
        {
            SpellRegistry.Register(FirstId + i, Order[i]);
        }

        for (var i = 0; i < Order.Length; i++)
        {
            if (SpellRegistry.NewSpell(FirstId + i, null, null) is not ShamanSpell spell)
            {
                continue;
            }

            var entry = new Entry(FirstId + i, Order[i], spell.Tradition, i % 8, spell.Name, spell.Posthumous);
            All.Add(entry);

            if (entry.Posthumous)
            {
                Posthumous.Add(entry);
            }
        }
    }

    public static IEnumerable<Entry> Of(ShamanTradition tradition)
    {
        foreach (var entry in All)
        {
            if (entry.Tradition == tradition)
            {
                yield return entry;
            }
        }
    }
}

/// <summary>
///     Книга шамана: вкладки разделов, по восемь обрядов в каждой.
///
///     Страница строится заново на каждый щелчок вкладки, а не через страницы гампа: так
///     у каждой строки свежие цвета (хватает навыка/специализации/откат вершины) и не нужно
///     отсылать все 32 описания разом.
/// </summary>
public class ShamanBookGump : Gump
{
    private const int TabBase = 10;
    private const int CircleButton = 20;
    private const int SealInfoButton = 21;

    private static readonly Dictionary<Mobile, ShamanTradition> LastPages = new();

    private readonly Mobile _from;
    private readonly ShamanTradition _page;

    public static ShamanTradition LastPage(Mobile m) =>
        LastPages.TryGetValue(m, out var page) ? page : ShamanTradition.Orc;

    public ShamanBookGump(Mobile from, ShamanTradition page) : base(50, 40)
    {
        _from = from;
        _page = page;
        LastPages[from] = page;

        const int width = 560;
        const int height = 520;

        AddBackground(0, 0, width, height, 9380);
        AddHtml(30, 40, width - 60, 20, "<CENTER><BASEFONT COLOR=#3A1E00>Книга шамана</BASEFONT></CENTER>");

        // Вкладки разделов
        for (var t = ShamanTradition.Orc; t <= ShamanTradition.Ancient; t++)
        {
            var x = 40 + (int)t * 122;
            AddButton(x, 66, t == page ? 4006 : 4005, 4007, TabBase + (int)t);
            AddHtml(x + 34, 68, 90, 20, $"<BASEFONT COLOR={(t == page ? "#7A1500" : "#3A1E00")}>{ShortName(t)}</BASEFONT>");
        }

        var skill = from.Skills.Shamanism.Value;
        var spec = ShamanismSchoolSystem.GetValue(from, page);

        AddHtml(
            40, 94, width - 80, 20,
            $"<BASEFONT COLOR=#3A1E00>Шаманство {skill:0.0} · специализация раздела {spec:0.0} · {Condition(page)}</BASEFONT>"
        );

        var y = 120;

        foreach (var entry in ShamanSpellbookTable.Of(page))
        {
            if (SpellRegistry.NewSpell(entry.Id, from, null) is not ShamanSpell spell)
            {
                continue;
            }

            var available = skill >= spell.RequiredSkill &&
                            (!spell.IsVertex || spec >= ShamanismSchoolSystem.VertexRequirement);
            var color = available ? "#1E3A00" : "#7A7A7A";

            var meta = $"ступень {spell.RequiredSkill:0} · мана {spell.GetMana()}";

            if (spell.IsVertex)
            {
                meta += $" · {ShamanComponent.NameFor(page)} ×{spell.ComponentCost}";
                var left = ShamanUltimates.CooldownLeft(spell);

                if (left > TimeSpan.Zero)
                {
                    meta += $" · откат {(int)left.TotalMinutes} мин";
                }
            }

            if (spell.Posthumous)
            {
                meta += " · посмертный";
            }

            AddButton(40, y + 2, 2103, 2104, entry.Id + 1);
            AddHtml(58, y, 470, 18, $"<BASEFONT COLOR={color}><B>{spell.Name}</B> — {meta}</BASEFONT>");
            AddHtml(58, y + 17, 470, 30, $"<BASEFONT COLOR=#4A3A2A>{spell.Description}</BASEFONT>");
            y += 46;
        }

        if (page == ShamanTradition.Ancient)
        {
            AddButton(40, height - 52, 4005, 4007, CircleButton);
            AddHtml(74, height - 50, 240, 20, "<BASEFONT COLOR=#3A1E00>Начертить круг (1 кость)</BASEFONT>");
        }

        AddButton(320, height - 52, 4005, 4007, SealInfoButton);
        AddHtml(354, height - 50, 180, 20, "<BASEFONT COLOR=#3A1E00>Печать души</BASEFONT>");
    }

    private static string ShortName(ShamanTradition t) => t switch
    {
        ShamanTradition.Orc     => "Орочье",
        ShamanTradition.Goblin  => "Гоблинское",
        ShamanTradition.Ogre    => "Огрское",
        ShamanTradition.Ancient => "Древнее",
        _                       => t.ToString()
    };

    private static string Condition(ShamanTradition t) => t switch
    {
        ShamanTradition.Orc     => "нужен огонь рядом",
        ShamanTradition.Goblin  => "нужен инструмент",
        ShamanTradition.Ogre    => "нужна голая земля",
        ShamanTradition.Ancient => "нужен ритуальный круг",
        _                       => ""
    };

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var id = info.ButtonID;

        if (id == 0 || !_from.Alive)
        {
            return;
        }

        if (id is >= TabBase and < TabBase + 4)
        {
            _from.SendGump(new ShamanBookGump(_from, (ShamanTradition)(id - TabBase)));
            return;
        }

        if (id == CircleButton)
        {
            DrawCircle(_from);
            _from.SendGump(new ShamanBookGump(_from, _page));
            return;
        }

        if (id == SealInfoButton)
        {
            var anchor = SoulSeal.Find(_from);

            _from.SendMessage(
                0x482,
                anchor == null
                    ? "Печати нет. Её ставит обряд «Печать души» древнего раздела."
                    : $"Печать стоит: нитей {anchor.Threads}, якорь в {anchor.GetWorldLocation()}."
            );
            _from.SendGump(new ShamanBookGump(_from, _page));
            return;
        }

        var spell = SpellRegistry.NewSpell(id - 1, _from, null);

        if (spell is ShamanSpell)
        {
            spell.Cast();
        }

        _from.SendGump(new ShamanBookGump(_from, _page));
    }

    /// <summary>
    ///     Круг чертится костью: дёшево, но кость надо иметь при себе, а место — выбрать. Круг
    ///     стоит десять минут и мешает разве что тем, кто хочет вытолкать из него шамана.
    /// </summary>
    public static void DrawCircle(Mobile m)
    {
        if (m.Map == null || m.Map == Map.Internal)
        {
            return;
        }

        if (ShamanRitualCircle.FindAt(m) != null)
        {
            m.SendMessage(0x3B2, "Ты уже стоишь в круге.");
            return;
        }

        if (m.AccessLevel < AccessLevel.GameMaster && m.Backpack?.ConsumeTotal(typeof(Bone)) != true)
        {
            m.SendMessage(0x22, "Чтобы начертить круг, нужна кость.");
            return;
        }

        var circle = new ShamanRitualCircle();
        circle.MoveToWorld(m.Location, m.Map);
        m.PlaySound(0x24A);
        m.SendMessage(0x59, "Ты чертишь ритуальный круг. Он продержится десять минут.");
    }
}

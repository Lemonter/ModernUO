using System;
using System.Collections.Generic;
using ModernUO.CodeGeneratedEvents;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Spells;
using Server.Spells.Shamanism;

namespace Server.Systems.MahaonShamanism;

/// <summary>
///     Печать души — посмертные обряды шамана («как Неназываемый: умер, а всё равно гадит»).
///
///     ---- Как устроено ----
///
///     Живым шаман проводит обряд Печати (SoulSealSpell) и ставит в мире якорь. Пока якорь
///     цел, после смерти призрак может творить обряды с пометкой «посмертный» — но только в
///     AnchorRange от якоря и за нить души вместо маны. Нитей 3–6 по навыку; кончились —
///     печать рассыпается. Плюс один раз за смерть — Посмертная месть убийце, где бы тот ни был.
///
///     ---- Почему не сломан баланс смерти ----
///
///     Посмертно — только проклятия, видения и мелкие козни, вершины — никогда. Каждый каст
///     ПРОЯВЛЯЕТ призрака (Manifest): видно всем, откуда бьют. Якорь можно найти и разбить,
///     обряд Изгнания рвёт связь. И главное: воскрешение рвёт печать — либо вернуться к
///     жизни, либо мстить, не то и другое.
///
///     ---- Хранение ----
///
///     Всё состояние — в самом якоре (он сохраняется как обычный предмет). Индекс «шаман →
///     якорь» восстанавливается из якорей при загрузке.
/// </summary>
public static class SoulSeal
{
    public const int AnchorRange = 20;
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);

    private static readonly Dictionary<Mobile, SoulSealAnchor> ByOwner = new();
    private static readonly HashSet<Mobile> RevengeUsed = new();
    private static Timer _watch;

    public static void Configure()
    {
        _watch = Timer.DelayCall(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2), Watch);
    }

    public static int ThreadsFor(Mobile m) =>
        Math.Clamp(3 + (int)((m.Skills.Shamanism.Value - 60.0) / 20.0), 3, 6);

    public static SoulSealAnchor Find(Mobile m) =>
        m != null && ByOwner.TryGetValue(m, out var anchor) && !anchor.Deleted ? anchor : null;

    internal static void Register(SoulSealAnchor anchor)
    {
        if (anchor.Owner == null)
        {
            return;
        }

        if (ByOwner.TryGetValue(anchor.Owner, out var old) && old != anchor && !old.Deleted)
        {
            old.Crumble("Старая печать распадается: душу держит новая.");
        }

        ByOwner[anchor.Owner] = anchor;
    }

    internal static void Unregister(SoulSealAnchor anchor)
    {
        if (anchor.Owner != null && ByOwner.TryGetValue(anchor.Owner, out var current) && current == anchor)
        {
            ByOwner.Remove(anchor.Owner);
        }
    }

    public static bool CanCastFromBeyond(Mobile m, out string why)
    {
        var anchor = Find(m);

        if (anchor == null)
        {
            why = "Твоя душа ничем не привязана к миру.";
            return false;
        }

        if (anchor.Threads <= 0)
        {
            why = "Нити души исчерпаны.";
            return false;
        }

        if (m.Map != anchor.Map || !m.InRange(anchor.GetWorldLocation(), AnchorRange))
        {
            why = $"Ты слишком далеко от якоря — вернись к нему, не дальше {AnchorRange} шагов.";
            return false;
        }

        why = null;
        return true;
    }

    /// <summary>Платёж и проявление после удачного посмертного обряда.</summary>
    public static void OnCastFromBeyond(Mobile m, Spell spell)
    {
        var anchor = Find(m);

        if (anchor == null)
        {
            return;
        }

        Manifest(m);

        anchor.Threads--;
        anchor.PublicOverheadMessage(MessageType.Regular, 0x482, false, "*якорь дрожит*");
        m.SendMessage(0x482, anchor.Threads > 0 ? $"Нитей души осталось: {anchor.Threads}." : "Это была последняя нить.");

        if (anchor.Threads <= 0)
        {
            Timer.DelayCall(TimeSpan.FromSeconds(1), () => anchor.Crumble("Последняя нить порвалась — печать рассыпается."));
        }
    }

    /// <summary>Призрак становится видимым на несколько секунд: силуэт, свет, звук.</summary>
    public static void Manifest(Mobile ghost)
    {
        if (ghost.Map == null || ghost.Map == Map.Internal)
        {
            return;
        }

        var apparition = new SpiritApparition(ghost);
        apparition.MoveToWorld(ghost.Location, ghost.Map);

        Effects.SendLocationParticles(
            EffectItem.Create(ghost.Location, ghost.Map, EffectItem.DefaultDuration),
            0x3728, 8, 20, 0x482, 0, 5042, 0
        );
        Effects.PlaySound(ghost.Location, ghost.Map, 0x482);
    }

    /// <summary>
    ///     Рвёт печать, если шаман в её радиусе: обряд Изгнания. Возвращает число разорванных.
    /// </summary>
    public static int Banish(Map map, Point3D center, int range, Mobile by)
    {
        var torn = 0;

        foreach (var anchor in new List<SoulSealAnchor>(ByOwner.Values))
        {
            if (anchor.Deleted || anchor.Owner == by)
            {
                continue;
            }

            var owner = anchor.Owner;
            var anchorNear = anchor.Map == map && Utility.InRange(anchor.GetWorldLocation(), center, range);
            var ghostNear = owner is { Alive: false } && owner.Map == map && Utility.InRange(owner.Location, center, range);

            if (anchorNear || ghostNear)
            {
                if (ghostNear)
                {
                    Manifest(owner);
                }

                anchor.Crumble("Чужой обряд изгнания обрывает связь души с миром.");
                torn++;
            }
        }

        return torn;
    }

    [OnEvent(nameof(PlayerMobile.PlayerDeathEvent))]
    public static void OnPlayerDeathEvent(Mobile m)
    {
        var anchor = Find(m);

        if (anchor == null)
        {
            return;
        }

        anchor.Awakened = true;
        RevengeUsed.Remove(m);

        Timer.DelayCall(
            TimeSpan.FromSeconds(2),
            () =>
            {
                if (m.Alive || Find(m) == null)
                {
                    return;
                }

                m.SendMessage(0x482, "Печать держит твою душу. Пока ты не воскрес — ты ещё можешь колдовать.");
                m.CloseGump<PosthumousGump>();
                m.SendGump(new PosthumousGump(m));
            }
        );
    }

    /// <summary>Вернулся к жизни — значит выбрал жизнь: печать, проснувшаяся со смертью, рвётся.</summary>
    public static void OnResurrected(Mobile m)
    {
        var anchor = Find(m);

        if (anchor is { Awakened: true })
        {
            anchor.Crumble("Воскрешение разорвало печать: ты выбрал жизнь, а не месть.");
        }
    }

    private static void Watch()
    {
        if (ByOwner.Count == 0)
        {
            return;
        }

        foreach (var anchor in new List<SoulSealAnchor>(ByOwner.Values))
        {
            if (anchor.Deleted)
            {
                continue;
            }

            var owner = anchor.Owner;

            if (owner == null || owner.Deleted)
            {
                anchor.Delete();
            }
            else if (Core.Now >= anchor.Expires)
            {
                anchor.Crumble("Срок печати вышел — душа больше не привязана к миру.");
            }
            else if (anchor.Awakened && owner.Alive)
            {
                // Вернулся к жизни — значит выбрал жизнь. Мстить и жить одновременно нельзя.
                anchor.Crumble("Воскрешение разорвало печать: ты выбрал жизнь, а не месть.");
            }
        }
    }

    // ---- Посмертная месть ------------------------------------------------------------

    public static bool CanTakeRevenge(Mobile m, out Mobile killer, out string why)
    {
        killer = m.LastKiller;

        if (!CanCastFromBeyond(m, out why))
        {
            return false;
        }

        if (RevengeUsed.Contains(m))
        {
            why = "Месть за эту смерть уже свершилась.";
            return false;
        }

        if (killer is BaseCreature { Controlled: true } pet && pet.ControlMaster != null)
        {
            killer = pet.ControlMaster; // мстят хозяину, а не его псу
        }

        if (killer == null || killer.Deleted || killer == m || !killer.Alive)
        {
            why = "Некому мстить: убийца неизвестен или уже мёртв.";
            return false;
        }

        if (Find(m)?.Threads < 2)
        {
            why = "На месть нужно две нити души.";
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Проклятие убийце: на полчаса −10 ко всем сопротивлениям и −10% к характеристикам.
    ///     Расстояние не важно — в этом весь смысл.
    /// </summary>
    public static void TakeRevenge(Mobile m)
    {
        if (!CanTakeRevenge(m, out var killer, out var why))
        {
            m.SendMessage(0x22, why);
            return;
        }

        var anchor = Find(m);
        RevengeUsed.Add(m);
        Manifest(m);

        var duration = TimeSpan.FromMinutes(30);
        ShamanEffects.AddResistCurse(killer, "Посмертная месть", -10, duration);
        ShamanEffects.AddStatCurse(killer, "Посмертная месть", 0.1, duration);

        killer.FixedParticles(0x374A, 10, 30, 5054, 0x482, 0, EffectLayer.Head);
        killer.PlaySound(0x1FB);
        killer.SendMessage(0x22, $"Тебя настигает посмертное проклятие {m.Name}. Холод пробирает до костей.");
        m.SendMessage(0x482, $"Твоя месть настигла {killer.Name}.");

        anchor.Threads -= 2;

        if (anchor.Threads <= 0)
        {
            anchor.Crumble("Месть забрала последние нити — печать рассыпается.");
        }
    }
}

/// <summary>Якорь Печати души. Вид — по основному разделу шамана.</summary>
[SerializationGenerator(0, false)]
public partial class SoulSealAnchor : Item
{
    private const int MaxHits = 120;

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Mobile _owner;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _threads;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _expires;

    [SerializableField(3)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _awakened;

    [SerializableField(4)]
    private int _hitsLeft;

    private readonly Dictionary<Mobile, long> _nextStrike = new();

    public SoulSealAnchor(Mobile owner, ShamanTradition look) : base(GraphicFor(look))
    {
        Movable = false;
        Hue = 0x482;
        _owner = owner;
        _threads = SoulSeal.ThreadsFor(owner);
        _expires = Core.Now + SoulSeal.Lifetime;
        _hitsLeft = MaxHits;

        SoulSeal.Register(this);
    }

    public override string DefaultName => _owner != null ? $"якорь души ({_owner.Name})" : "якорь души";

    // Ни одна графика не помечена NoShoot: якорь стоит рядом с местом смерти, и призрак то и
    // дело оказывается за ним или прямо на нём. Тотем (0x4D19) с NoShoot закрывал призраку
    // линию взгляда, и посмертные обряды молча не находили цель — проверено вживую.
    private static int GraphicFor(ShamanTradition look) => look switch
    {
        ShamanTradition.Orc    => 0x1B17, // рёбра на кострище
        ShamanTradition.Goblin => 0x1853, // череп со свечой
        ShamanTradition.Ogre   => 0x134F, // валун
        _                      => 0x0ED4  // надгробие
    };

    public override void OnDoubleClick(Mobile from)
    {
        if (from == _owner)
        {
            var left = _expires - Core.Now;
            from.SendMessage(0x482, $"Твой якорь. Нитей души: {_threads}. Держится ещё {(int)left.TotalHours} ч {left.Minutes} мин.");
            return;
        }

        if (!from.Alive || !from.InRange(GetWorldLocation(), 2))
        {
            from.SendLocalizedMessage(500446); // That is too far away.
            return;
        }

        var now = Core.TickCount;

        if (_nextStrike.TryGetValue(from, out var next) && now < next)
        {
            return;
        }

        _nextStrike[from] = now + 1000;

        var damage = 8 + Utility.Random(8) + from.Str / 10 + (int)(from.Skills.SpiritSpeak.Value / 10);
        HitsLeft -= damage;

        from.Animate(9, 5, 1, true, false, 0);
        Effects.PlaySound(GetWorldLocation(), Map, 0x13E);
        PublicOverheadMessage(MessageType.Regular, 0x482, false, "*якорь трещит*");

        if (HitsLeft <= 0)
        {
            Crumble($"{from.Name} разбивает якорь твоей души.");
        }
    }

    public void Crumble(string message)
    {
        if (Deleted)
        {
            return;
        }

        _owner?.SendMessage(0x22, message);
        Effects.SendLocationParticles(
            EffectItem.Create(GetWorldLocation(), Map, EffectItem.DefaultDuration),
            0x36BD, 20, 10, 0x482, 0, 5044, 0
        );
        Effects.PlaySound(GetWorldLocation(), Map, 0x307);

        if (_owner is { Alive: false })
        {
            _owner.CloseGump<PosthumousGump>();
        }

        Delete();
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();
        SoulSeal.Unregister(this);
    }

    [AfterDeserialization]
    private void AfterDeserialization() => SoulSeal.Register(this);
}

/// <summary>Видимый силуэт проявившегося призрака. Живёт несколько секунд, не сохраняется.</summary>
[SerializationGenerator(0, false)]
public partial class SpiritApparition : BaseCreature
{
    public SpiritApparition(Mobile ghost) : base(AIType.AI_Animal, FightMode.None)
    {
        Body = ghost.Female ? 403 : 402;
        Hue = 0x4001;
        Name = $"дух {ghost.Name}";
        Blessed = true;
        CantWalk = true;
        Direction = ghost.Direction;

        Timer.DelayCall(TimeSpan.FromSeconds(6), Delete);
    }

    public override bool DeleteCorpseOnDeath => true;
    public override bool IsInvulnerable => true;
    public override bool ClickTitle => false;

    [AfterDeserialization]
    private void AfterDeserialization() => Timer.DelayCall(Delete);
}

/// <summary>
///     Посмертные обряды. Книга лежит в трупе (а у призрака нет рук её листать), поэтому
///     обряды приходят отдельным гампом сразу после смерти.
/// </summary>
public class PosthumousGump : Gump
{
    private const int RevengeButton = 100;
    private const int RefreshButton = 101;

    private readonly Mobile _ghost;

    public PosthumousGump(Mobile ghost) : base(60, 60)
    {
        _ghost = ghost;

        var anchor = SoulSeal.Find(ghost);
        var spells = ShamanSpellbookTable.PosthumousSpells;

        AddBackground(0, 0, 360, 120 + spells.Count * 26, 9270);
        AddHtml(20, 18, 320, 20, "<BASEFONT COLOR=#B0C8FF><CENTER>Посмертие</CENTER></BASEFONT>");
        AddHtml(
            20, 40, 320, 20,
            $"<BASEFONT COLOR=#FFFFFF>Нитей души: {anchor?.Threads ?? 0}. Якорь: {(anchor == null ? "нет" : "цел")}.</BASEFONT>"
        );

        var y = 66;

        for (var i = 0; i < spells.Count; i++)
        {
            var entry = spells[i];
            AddButton(20, y, 4005, 4007, entry.Id + 1);
            AddHtml(55, y + 2, 290, 20, $"<BASEFONT COLOR=#FFFFFF>{entry.Name}</BASEFONT>");
            y += 26;
        }

        AddButton(20, y, 4005, 4007, RevengeButton);
        AddHtml(55, y + 2, 290, 20, "<BASEFONT COLOR=#FF8080>Посмертная месть (2 нити)</BASEFONT>");
        y += 26;

        AddButton(20, y, 4011, 4013, RefreshButton);
        AddHtml(55, y + 2, 290, 20, "<BASEFONT COLOR=#C0C0C0>Обновить</BASEFONT>");
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (_ghost.Alive || SoulSeal.Find(_ghost) == null || info.ButtonID == 0)
        {
            return;
        }

        switch (info.ButtonID)
        {
            case RevengeButton:
                SoulSeal.TakeRevenge(_ghost);
                break;

            case RefreshButton:
                break;

            default:
                var spell = SpellRegistry.NewSpell(info.ButtonID - 1, _ghost, null);

                if (spell is ShamanSpell { Posthumous: true })
                {
                    spell.Cast();
                }

                break;
        }

        if (!_ghost.Alive && SoulSeal.Find(_ghost) != null)
        {
            _ghost.SendGump(new PosthumousGump(_ghost));
        }
    }
}

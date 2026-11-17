using System;
using System.Collections.Generic;
using Server.Commands;
using Server.Misc;
using Server.Mobiles;

namespace Server.Systems.MahaonCities;

/// <summary>
/// Торговцы между делом перекидываются словом с соседом по лавке или улице: о покупателях и ценах,
/// о налоге гильдии, что держит город, о набегах — своих и чужих, о слухах. Отвечающий нет-нет да
/// и засмеётся, вздохнёт или ахнет — со звуком, как игрок эмоцией. Говорят, только когда рядом
/// есть кому слушать, и в одном городе — изредка, а не наперебой.
/// </summary>
public static class VendorChatter
{
    private const double ChancePerThink = 0.01;
    private const int PartnerRange = 8;
    private const int AudienceRange = 14;
    private static readonly TimeSpan ReplyDelay = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan EmoteDelay = TimeSpan.FromSeconds(1.2);

    private static readonly Dictionary<string, long> _nextTalk = new(StringComparer.OrdinalIgnoreCase);

    // Как у стражи: тест слышит речь через это, клиентам она идёт обычным путём.
    internal static Action<Mobile, string> Said;

    // Пауза между разговорами в одном городе, минуты. Тест может её убрать.
    internal static int MinGapMinutes = 3, MaxGapMinutes = 6;

    // В тестах клиента рядом нет.
    internal static bool RequireAudience = true;

    private static readonly (string opener, string[] replies, Emote? emote)[] ShopTalk =
    [
        ("Покупателей сегодня — раз-два и обчёлся.", ["И у меня тишина.", "Ничего, к вечеру набегут."], Emote.Sigh),
        ("Цены на слитки опять скачут.", ["Кузнецы стонут, а нам что — продаём.", "Скупай, пока дёшево."], null),
        ("Вчера один торговался за свечку целый час.", ["Ха! И купил?", "Ха-ха, у меня такой же был."], Emote.Laugh),
        ("Подмастерье опять всё на полках перепутал.", ["Молодой ещё, научится.", "Гони его в шею!"], Emote.Laugh),
        ("Искатели приключений нынче — всё в долг да в долг.", ["Ни монеты в долг — вот моё правило.", "А потом ищи их на кладбище."], Emote.Laugh),
        ("Ноги гудят, весь день за прилавком.", ["А мне спину ломит.", "Скоро закрываться, потерпи."], Emote.Yawn),
        ("Крысы опять мешок зерна прогрызли.", ["Кота заведи, говорю же.", "У меня тоже. Напасть какая-то."], Emote.Groan),
        ("Видал, какой нынче товар с моря привезли?", ["Видал. Дорого просят.", "Ещё бы не видать — полгорода сбежалось."], Emote.Whistle),
        ("Кто-то опять пытался мне фальшивую монету всучить.", ["Кусай каждую, я так делаю.", "Стражу звать надо было!"], Emote.No),
        ("Дождь бы не пошёл — товар под навесом промокнет.", ["Небо чистое, не каркай.", "Накрой рогожей, и вся недолга."], null)
    ];

    public static void Configure() =>
        CommandSystem.Register("VendorChat", AccessLevel.GameMaster, VendorChat_OnCommand);

    [Usage("VendorChat")]
    [Description("Ближайший торговец сразу заговаривает с соседом — проверить, о чём болтают.")]
    private static void VendorChat_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;
        BaseVendor nearest = null;
        foreach (var v in from.Map.GetMobilesInRange<BaseVendor>(from.Location, 12))
        {
            if (nearest == null || from.GetDistanceToSqrt(v) < from.GetDistanceToSqrt(nearest))
            {
                nearest = v;
            }
        }

        if (nearest == null || !TryStart(nearest, true, true))
        {
            from.SendMessage("Рядом нет пары торговцев, готовых поболтать.");
        }
    }

    /// <summary>Торговец без дела может заговорить с соседом. True, если заговорил.</summary>
    public static bool TryStart(BaseVendor vendor, bool force = false, bool ignoreGap = false)
    {
        if (!force && Utility.RandomDouble() >= ChancePerThink)
        {
            return false;
        }

        if (vendor.Map == null || vendor.Map == Map.Internal || vendor.Combatant != null || !vendor.Alive || vendor.Deleted)
        {
            return false;
        }

        var key = vendor.Region?.Name ?? $"{vendor.Map}:{vendor.X / 64}:{vendor.Y / 64}";
        var now = Core.TickCount;
        if (!ignoreGap && _nextTalk.TryGetValue(key, out var next) && now - next < 0 || RequireAudience && !HasAudience(vendor) ||
            FindPartner(vendor) is not { } partner)
        {
            return false;
        }

        _nextTalk[key] = now + Utility.RandomMinMax(MinGapMinutes * 60, MaxGapMinutes * 60) * 1000L;

        var (opener, reply, emote) = Pick(CityOf(vendor));
        vendor.Direction = vendor.GetDirectionTo(partner);
        vendor.Say(opener);
        Said?.Invoke(vendor, opener);

        Timer.StartTimer(
            ReplyDelay,
            () =>
            {
                if (partner.Deleted || !partner.Alive || partner.Combatant != null || !partner.InRange(vendor, PartnerRange + 2))
                {
                    return;
                }

                partner.Direction = partner.GetDirectionTo(vendor);
                if (emote is { } e)
                {
                    MahaonEmotes.Do(partner, e);
                    Timer.StartTimer(EmoteDelay, () => SayReply(partner, reply));
                }
                else
                {
                    SayReply(partner, reply);
                }
            }
        );

        return true;
    }

    private static void SayReply(Mobile partner, string reply)
    {
        if (!partner.Deleted && partner.Alive)
        {
            partner.Say(reply);
            Said?.Invoke(partner, reply);
        }
    }

    private static bool HasAudience(Mobile vendor)
    {
        foreach (var m in vendor.Map.GetMobilesInRange<PlayerMobile>(vendor.Location, AudienceRange))
        {
            if (m.NetState != null)
            {
                return true;
            }
        }

        return false;
    }

    private static BaseVendor FindPartner(BaseVendor vendor)
    {
        BaseVendor best = null;
        var bestDistance = double.MaxValue;
        foreach (var other in vendor.Map.GetMobilesInRange<BaseVendor>(vendor.Location, PartnerRange))
        {
            if (other == vendor || !other.Alive || other.Combatant != null || !other.Body.IsHuman)
            {
                continue;
            }

            var distance = vendor.GetDistanceToSqrt(other);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = other;
            }
        }

        return best;
    }

    private static string CityOf(Mobile m) => CityControlSystem.Find(m.Region?.Name ?? "") ?? m.Region?.Name;

    // Свежий набег — главная новость; иначе налог, слух или лавочные заботы.
    private static (string opener, string reply, Emote? emote) Pick(string city)
    {
        var roll = Utility.RandomDouble();

        if (roll < 0.35 && TownTalk.TryRaidTalk(city, out var raidOpener, out var raidReply, out var raidEmote))
        {
            return (raidOpener, raidReply, raidEmote);
        }

        if (roll < 0.5 && city != null && CityControlSystem.Cities.ContainsKey(city))
        {
            return TaxTalk(city);
        }

        if (roll < 0.65 && MahaonBots.BotRumors.TryPick(out var rumor))
        {
            return (rumor, Any("Да ну?", "Брешут, поди.", "Вот это новость!", "Слыхал уже."), Emote.Huh);
        }

        var (opener, replies, emote) = ShopTalk[Utility.Random(ShopTalk.Length)];
        return (opener, replies[Utility.Random(replies.Length)], emote);
    }

    private static (string, string, Emote?) TaxTalk(string city)
    {
        var tax = CityControlSystem.TaxIn(city);
        if (tax > 0 && CityControlSystem.GetController(city) is { } holder)
        {
            return ($"Гильдия {holder.Name} дерёт {tax}% с каждой сделки на аукционе.",
                Any("Грабёж средь бела дня.", "Зато стража сытая.", "Ещё поднимут — уеду в другой город."), Emote.Groan);
        }

        return CityControlSystem.GetController(city) is { } owner
            ? ($"Гильдия {owner.Name} город держит, а налога не берёт. Чудеса.", Any("Пока не берёт.", "Хитрят, видать."), Emote.Huh)
            : ("Хорошо хоть налогов никто не дерёт.", Any("Пока город ничей — живём.", "Погоди, придёт гильдия — начнётся."), Emote.Sigh);
    }

    private static string Any(params string[] lines) => lines[Utility.Random(lines.Length)];
}

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Server.Spells;

namespace Server.Systems.MahaonCombat;

public enum PreferredElement : byte
{
    Fire = 0,
    Poison = 1, // "Природа" in the shard owner's own wording — same element, renamed
    Energy = 2,
    Cold = 3
}

/// <summary>
///     One "favored" element a caster can lock in from the Магия tab of
///     MahaonCombatMenuGump — only one active at a time, persists until changed. Converts
///     half of a spell's elemental damage split to the favored type (the physical share, if
///     any, is left untouched), letting a caster try to route around a specific enemy's
///     resist profile instead of always hitting with whatever a given spell's fixed
///     element is. No damage is lost in the conversion — but since stage 7 it costs mana:
///     +30% of the spell's cost, cut to +10% by the spell's own school (ApplyPaidConversion).
/// </summary>
public static class PreferredElementSystem
{
    private static readonly Dictionary<Mobile, PreferredElement> Preferred = new();

    private static Persistence _persistence;

    public static void Configure()
    {
        _persistence = new Persistence();
    }

    public static PreferredElement? GetPreferred(Mobile m) =>
        Preferred.TryGetValue(m, out var element) ? element : null;

    public static void SetPreferred(Mobile m, PreferredElement element) => Preferred[m] = element;

    public static void ClearPreferred(Mobile m) => Preferred.Remove(m);

    /// <summary>Called from SpellHelper.Damage right before AOS.Damage — halves every
    /// non-physical damage share and hands the remainder to the caster's favored element.
    /// A pure-physical spell (no elemental share at all) has nothing to convert and is left
    /// alone.</summary>
    public static void ApplyConversion(Mobile from, ref int fire, ref int cold, ref int pois, ref int nrgy)
    {
        if (from == null || !Preferred.TryGetValue(from, out var element))
        {
            return;
        }

        var totalElemental = fire + cold + pois + nrgy;

        if (totalElemental <= 0)
        {
            return;
        }

        fire /= 2;
        cold /= 2;
        pois /= 2;
        nrgy /= 2;

        var removed = totalElemental - (fire + cold + pois + nrgy);

        switch (element)
        {
            case PreferredElement.Fire:
                fire += removed;
                break;
            case PreferredElement.Cold:
                cold += removed;
                break;
            case PreferredElement.Poison:
                pois += removed;
                break;
            case PreferredElement.Energy:
                nrgy += removed;
                break;
        }
    }

    // ---- Цена стихии (этап 7) -----------------------------------------------------------

    public const double BasePremium = 0.30;
    public const double SchoolPremiumReduction = 0.20;

    private static readonly ConditionalWeakTable<Spell, object> Paid = new();
    private static readonly object Yes = new();
    private static readonly object No = new();

    /// <summary>
    ///     Наценка на ману за перевод урона в стихию: 30%, прокачанная школа самого заклинания
    ///     сбивает до 10% (раздел 9.2): наценка = 30% − 20% × школа / 100.
    /// </summary>
    public static double Premium(Mobile caster, ISpell spell)
    {
        var school = SpellSchools.TryDescribe(caster, spell, out var info) ? info.Value : 0.0;
        return BasePremium - SchoolPremiumReduction * Math.Clamp(school, 0.0, 100.0) / 100.0;
    }

    /// <summary>
    ///     Перевод половины урона в стихию — за ману. Зовётся из SpellHelper.Damage вместо
    ///     бесплатного ApplyConversion.
    ///
    ///     Почему наценка берётся здесь, а не в ScaleMana: стихия меняет только урон, а знать
    ///     заранее, нанесёт ли заклинание урон и есть ли что перекрашивать, при касте нельзя.
    ///     Платишь в тот момент, когда стихия реально сработала, и один раз за каст, сколько бы
    ///     целей ни задело. Не хватает маны — заклинание бьёт своей стихией, а маг узнаёт почему.
    ///     Если урон уже целиком в выбранной стихии, перекрашивать нечего и платить не за что.
    /// </summary>
    public static void ApplyPaidConversion(Mobile from, Spell spell, ref int fire, ref int cold, ref int pois, ref int nrgy)
    {
        if (from == null || spell == null || !Preferred.TryGetValue(from, out var element))
        {
            return;
        }

        var total = fire + cold + pois + nrgy;
        var inElement = element switch
        {
            PreferredElement.Fire   => fire,
            PreferredElement.Cold   => cold,
            PreferredElement.Poison => pois,
            _                       => nrgy
        };

        if (total <= 0 || inElement == total)
        {
            return;
        }

        if (!Paid.TryGetValue(spell, out var state))
        {
            var cost = (int)Math.Ceiling(spell.ScaleMana(spell.GetMana()) * Premium(from, spell));

            if (from.Mana < cost)
            {
                from.SendMessage(0x22, $"Не хватает маны на стихию ({cost}) — заклинание бьёт своей.");
                state = No;
            }
            else
            {
                from.Mana -= cost;
                state = Yes;
            }

            Paid.AddOrUpdate(spell, state);
        }

        if (!ReferenceEquals(state, Yes))
        {
            return; // в этом касте на стихию не хватило — остальные цели тоже своей стихией
        }

        ApplyConversion(from, ref fire, ref cold, ref pois, ref nrgy);
    }

    public static string RuElementName(PreferredElement element) => element switch
    {
        PreferredElement.Fire   => "Огонь",
        PreferredElement.Poison => "Природа",
        PreferredElement.Energy => "Энергия",
        PreferredElement.Cold   => "Холод",
        _                        => element.ToString()
    };

    private sealed class Persistence : GenericPersistence
    {
        public Persistence() : base("MahaonPreferredElement", 1)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version
            writer.WriteEncodedInt(Preferred.Count);

            foreach (var (mobile, element) in Preferred)
            {
                writer.Write(mobile);
                writer.Write((byte)element);
            }
        }

        public override void Deserialize(IGenericReader reader)
        {
            reader.ReadEncodedInt(); // version

            var count = reader.ReadEncodedInt();
            for (var i = 0; i < count; i++)
            {
                var mobile = reader.ReadEntity<Mobile>();
                var element = (PreferredElement)reader.ReadByte();

                if (mobile != null)
                {
                    Preferred[mobile] = element;
                }
            }
        }
    }
}

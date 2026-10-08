using System.Collections.Generic;

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
///     element is. No damage is lost in the conversion — see ApplyConversion.
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

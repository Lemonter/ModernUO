using System.Collections.Generic;
using Server.Items;

namespace Server.Systems.MahaonSoulStones;

/// <summary>
///     Tracks which Sakuro blueprints a player has learned. First pass only covers Sakuro
///     jewelry (tying into the system already built) — the same pattern (learn a scroll,
///     or hand-draw one) extends naturally to real vanilla craft recipes later; that's a
///     much bigger integration (hooking every DefBlacksmithy/DefCarpentry recipe) and out
///     of scope for this pass.
/// </summary>
public class SakuroBlueprintKnowledge : GenericPersistence
{
    private static SakuroBlueprintKnowledge _instance;

    private static readonly Dictionary<Mobile, HashSet<SakuroType>> Known = new();

    // Librarian prices — spread across the range the player remembered (5g to 60k g).
    public static readonly Dictionary<SakuroType, int> LibrarianPrice = new()
    {
        [SakuroType.Titan] = 5,
        [SakuroType.Balance] = 500,
        [SakuroType.Fox] = 2000,
        [SakuroType.Bear] = 2000,
        [SakuroType.Owl] = 2000,
        [SakuroType.Power] = 15000,
        [SakuroType.Agility] = 15000,
        [SakuroType.Wisdom] = 60000
    };

    public SakuroBlueprintKnowledge() : base("SakuroBlueprints", 1)
    {
    }

    public static void Configure()
    {
        _instance = new SakuroBlueprintKnowledge();
    }

    public static bool Knows(Mobile m, SakuroType type) =>
        Known.TryGetValue(m, out var set) && set.Contains(type);

    public static void Learn(Mobile m, SakuroType type)
    {
        if (!Known.TryGetValue(m, out var set))
        {
            Known[m] = set = new HashSet<SakuroType>();
        }

        set.Add(type);
    }

    public static IReadOnlyCollection<SakuroType> KnownTypes(Mobile m) =>
        Known.TryGetValue(m, out var set) ? set : System.Array.Empty<SakuroType>();

    public static List<SakuroType> UnknownTypes(Mobile m)
    {
        var result = new List<SakuroType>();

        foreach (SakuroType type in System.Enum.GetValues(typeof(SakuroType)))
        {
            if (!Knows(m, type))
            {
                result.Add(type);
            }
        }

        return result;
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version
        writer.WriteEncodedInt(Known.Count);

        foreach (var (mobile, set) in Known)
        {
            writer.Write(mobile);
            writer.WriteEncodedInt(set.Count);

            foreach (var type in set)
            {
                writer.WriteEncodedInt((int)type);
            }
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // version

        var mobileCount = reader.ReadEncodedInt();
        for (var i = 0; i < mobileCount; i++)
        {
            var mobile = reader.ReadEntity<Mobile>();
            var typeCount = reader.ReadEncodedInt();

            var set = new HashSet<SakuroType>();
            for (var j = 0; j < typeCount; j++)
            {
                set.Add((SakuroType)reader.ReadEncodedInt());
            }

            if (mobile != null)
            {
                Known[mobile] = set;
            }
        }
    }
}

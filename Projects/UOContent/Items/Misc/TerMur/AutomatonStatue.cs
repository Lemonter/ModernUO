using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Ported from ServUO (Scripts/Items/Functional/Automaton/AutomatonStatue.cs).
/// KotlAutomaton doesn't exist anywhere in this codebase (an Eodon-continent creature outside
/// this TerMur port's scope) — the entire "wakes up and attacks a passing player" mechanic
/// depends on it, so it's dropped; this is now a plain decorative statue.</summary>
[SerializationGenerator(0, false)]
public partial class AutomatonStatue : Item
{
    [Constructible]
    public AutomatonStatue() : base(Utility.RandomBool() ? 0x9DB3 : 0x9DB4)
    {
        Movable = false;
    }

    public override int LabelNumber => 1124395; // Automaton
}

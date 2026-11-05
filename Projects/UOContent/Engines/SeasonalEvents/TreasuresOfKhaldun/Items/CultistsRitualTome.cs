using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKhaldun/Items/CultistsRitualTome.cs).
///     ServUO rolled <c>SlayerGroup.RandomSuperSlayerTOL()</c>; that helper does not exist here, so the tome takes
///     a random super slayer from <see cref="SlayerGroup.Groups" /> (the same expression the Treasures of Tokuno
///     port uses).
/// </summary>
[SerializationGenerator(0, false)]
public partial class CultistsRitualTome : Spellbook
{
    public override int LabelNumber => 1158717; // Cultist's Ritual Tome

    [Constructible]
    public CultistsRitualTome() : base(0x3FFFFFFFF)
    {
        Hue = 2745;
        LootType = LootType.Blessed;

        Slayer = SlayerGroup.Groups[Utility.Random(SlayerGroup.Groups.Length - 1)].Super.Name;

        // Caddellite Infused
        Attributes.DefendChance = 5;
        Attributes.SpellDamage = 25;
        Attributes.CastRecovery = 2;
        Attributes.LowerManaCost = 4;
    }
}

namespace Server.Systems.MahaonCombat;

/// <summary>
///     Descriptive combat text ("Лич использовал заклинание Fireball и нанёс вам 20
///     урона", "Зомби ударил вас в шею на 3") sent to both sides of a hit via
///     SendMessage. This is the SERVER half of the request — a genuinely separate chat
///     window with its own damage tab is client-side UI work (ClassicUO), out of scope
///     here. What this DOES do: uses a distinct hue (0x22, the same "combat red" already
///     used elsewhere in Mahaon) so a client-side journal filter could split these into
///     their own tab by hue if/when that client work happens, without needing yet another
///     protocol change.
///
///     Spell names are the engine's own English names (Spell.Name) — a full Russian
///     translation table for every spell in the game is its own separate task, not
///     attempted here.
/// </summary>
public static class CombatLogSystem
{
    private const int Hue = 0x22;

    public static void LogSpellHit(Mobile caster, Mobile target, string spellName, int damage)
    {
        caster?.SendMessage(Hue, $"Вы использовали заклинание {spellName} и нанесли {target.Name} {damage} урона.");
        target?.SendMessage(Hue, $"{caster?.Name ?? "Кто-то"} использовал заклинание {spellName} и нанёс вам {damage} урона.");
    }

    public static void LogMeleeHit(Mobile attacker, Mobile defender, int damage, string locationRu = null, string armorNameRu = null)
    {
        var locationSuffix = locationRu != null ? $" в {locationRu}" : "";
        var armorSuffix = armorNameRu != null ? $" (по {armorNameRu})" : "";

        attacker?.SendMessage(Hue, $"Вы ударили {defender.Name}{locationSuffix}{armorSuffix} на {damage}.");
        defender?.SendMessage(Hue, $"{attacker?.Name ?? "Кто-то"} ударил вас{locationSuffix}{armorSuffix} на {damage}.");
    }

    public static void LogMiss(Mobile attacker, Mobile defender)
    {
        attacker?.SendMessage(Hue, $"Вы промахнулись мимо {defender.Name}.");
        defender?.SendMessage(Hue, $"{attacker?.Name ?? "Кто-то"} промахнулся мимо вас.");
    }

    public static void LogParry(Mobile attacker, Mobile defender)
    {
        attacker?.SendMessage(Hue, $"{defender.Name} парировал вашу атаку.");
        defender?.SendMessage(Hue, $"Вы парировали атаку {attacker?.Name ?? "кого-то"}.");
    }
}

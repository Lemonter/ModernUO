using Server.Mobiles;
using Server.Network;
using Server.Spells;
using Server.Systems.MahaonCombat;

namespace Server.Gumps;

/// <summary>
///     Was a single flat page of 11 rows that didn't fit any trainable info at all — no
///     ceiling for anything a Shepherd's Crook can raise, no combat-skill numbers, no
///     magic visibility whatsoever ("магия в вакууме" — the shard owner's own words).
///     Redesigned as 3 pages: attributes+resists (current/ceiling, not just current),
///     combat techniques, and magic (focus + real spellbook icons, known vs still-locked).
/// </summary>
public class MahaonAnimalLoreGump : DynamicGump
{
    private const int LabelColor = 0x480;
    private const int ValueColor = 0x59;
    private const int SpellIconBase = 0x1F2E; // see Spells/Initializer.cs — 0-indexed OSI numbering

    private readonly Mobile _creature;

    public override bool Singleton => true;

    public MahaonAnimalLoreGump(Mobile creature) : base(50, 50) => _creature = creature;

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        // AddPage(0) is the shared/permanent layer — anything added here shows on EVERY
        // page regardless of which one is selected. Only the background/frame/title belong
        // there; the earlier version also built the whole attributes page (and its own nav
        // buttons) while still on page 0, so that content rendered on top of pages 2 and 3
        // as well instead of being replaced by them — exactly the "everything stacks"
        // symptom reported. Each actual content page now gets its own explicit AddPage(N).
        builder.AddPage(0);
        builder.AddBackground(0, 0, 340, 400, 5054);
        builder.AddAlphaRegion(10, 10, 320, 380);
        builder.AddHtml(15, 15, 310, 20, $"<center>{_creature.Name}</center>");

        builder.AddPage(1);
        builder.AddButton(300, 375, 4014, 4016, 0, GumpButtonType.Page, 2);
        builder.AddHtml(240, 377, 55, 20, "Далее »");
        BuildAttributesPage(ref builder);

        builder.AddPage(2);
        builder.AddButton(15, 375, 4017, 4019, 0, GumpButtonType.Page, 1);
        builder.AddHtml(50, 377, 90, 20, "« Атрибуты");
        builder.AddButton(300, 375, 4014, 4016, 0, GumpButtonType.Page, 3);
        builder.AddHtml(240, 377, 55, 20, "Магия »");
        BuildCombatPage(ref builder);

        builder.AddPage(3);
        builder.AddButton(15, 375, 4017, 4019, 0, GumpButtonType.Page, 2);
        builder.AddHtml(50, 377, 90, 20, "« Техники");
        BuildMagicPage(ref builder);
    }

    // DynamicGumpBuilder is a ref struct — can't be captured by a local helper function
    // (the compiler forbids that outright), so every row is inlined instead of going
    // through a shared Row(label, value) helper the way this would normally be written.
    private void BuildAttributesPage(ref DynamicGumpBuilder builder)
    {
        var y = 45;

        if (_creature is BaseCreature bc)
        {
            var caps = AnimalTrainingSystem.GetPhysicalCaps(bc);

            builder.AddLabel(15, y, LabelColor, "Здоровье:");
            builder.AddLabel(150, y, ValueColor, $"{bc.Hits}/{caps.hits}  (потолок {caps.hitsCap})");
            y += 20;

            builder.AddLabel(15, y, LabelColor, "Стамина:");
            builder.AddLabel(150, y, ValueColor, $"{bc.Stam}/{caps.stam}  (потолок {caps.stamCap})");
            y += 20;

            builder.AddLabel(15, y, LabelColor, "Мана:");
            builder.AddLabel(150, y, ValueColor, $"{bc.Mana}/{caps.mana}  (потолок {caps.manaCap})");
            y += 20;

            builder.AddLabel(15, y, LabelColor, "Урон:");
            builder.AddLabel(150, y, ValueColor, $"{bc.DamageMin}-{bc.DamageMax}  (потолок {caps.dmgMaxCap})");
            y += 20;
        }
        else
        {
            builder.AddLabel(15, y, LabelColor, "Здоровье:");
            builder.AddLabel(150, y, ValueColor, $"{_creature.Hits}/{_creature.HitsMax}");
            y += 20;

            builder.AddLabel(15, y, LabelColor, "Стамина:");
            builder.AddLabel(150, y, ValueColor, $"{_creature.Stam}/{_creature.StamMax}");
            y += 20;

            builder.AddLabel(15, y, LabelColor, "Мана:");
            builder.AddLabel(150, y, ValueColor, $"{_creature.Mana}/{_creature.ManaMax}");
            y += 20;
        }

        y += 6;

        builder.AddLabel(15, y, LabelColor, "Физ. резист:");
        builder.AddLabel(150, y, ValueColor, $"{_creature.PhysicalResistance}%");
        y += 20;

        builder.AddLabel(15, y, LabelColor, "Огонь резист:");
        builder.AddLabel(150, y, ValueColor, $"{_creature.FireResistance}%");
        y += 20;

        builder.AddLabel(15, y, LabelColor, "Холод резист:");
        builder.AddLabel(150, y, ValueColor, $"{_creature.ColdResistance}%");
        y += 20;

        builder.AddLabel(15, y, LabelColor, "Яд резист:");
        builder.AddLabel(150, y, ValueColor, $"{_creature.PoisonResistance}%");
        y += 20;

        builder.AddLabel(15, y, LabelColor, "Энергия резист:");
        builder.AddLabel(150, y, ValueColor, $"{_creature.EnergyResistance}%");
        y += 20;

        if (_creature is BaseCreature bc2)
        {
            y += 6;

            builder.AddLabel(15, y, LabelColor, "Слава:");
            builder.AddLabel(150, y, ValueColor, bc2.Fame.ToString());
            y += 20;

            builder.AddLabel(15, y, LabelColor, "Приручён:");
            builder.AddLabel(150, y, ValueColor, bc2.Controlled ? "да" : "нет");
        }
    }

    private static readonly (SkillName skill, string ru)[] CombatSkillDisplay =
    {
        (SkillName.Wrestling, "Борьба"),
        (SkillName.Tactics, "Тактика"),
        (SkillName.Anatomy, "Анатомия"),
        (SkillName.Parry, "Парирование")
    };

    private void BuildCombatPage(ref DynamicGumpBuilder builder)
    {
        builder.AddHtml(15, 45, 290, 20, "Боевые техники");

        var y = 70;

        foreach (var (skill, ru) in CombatSkillDisplay)
        {
            var s = _creature.Skills[skill];
            builder.AddLabel(15, y, LabelColor, $"{ru}:");
            builder.AddLabel(180, y, ValueColor, $"{s.Value:F1}/{s.Cap:F1}");
            y += 22;
        }
    }

    private static int IconFor(System.Type spellType)
    {
        var id = SpellRegistry.GetRegistryNumber(spellType);
        return id >= 0 ? SpellIconBase + id : SpellIconBase;
    }

    private void BuildMagicPage(ref DynamicGumpBuilder builder)
    {
        builder.AddHtml(15, 45, 290, 20, "Магия");

        if (_creature is not BaseCreature bc || !AnimalTrainingSystem.IsMagicTrained(bc))
        {
            builder.AddHtml(15, 70, 290, 20, "Не обучен(а) магии.");
            return;
        }

        var focus = AnimalTrainingSystem.GetMagicFocus(bc);
        var magery = bc.Skills[SkillName.Magery].Value;
        var evalInt = bc.Skills[SkillName.EvalInt].Value;
        var manaMult = AnimalTrainingSystem.GetManaCostMultiplier(bc);

        builder.AddLabel(15, 70, LabelColor, "Специализация:");
        builder.AddLabel(180, 70, ValueColor, AnimalTrainingSystem.RuFocusName(focus));

        builder.AddLabel(15, 92, LabelColor, "Магия:");
        builder.AddLabel(180, 92, ValueColor, $"{magery:F1}/{bc.Skills[SkillName.Magery].Cap:F1}");

        builder.AddLabel(15, 114, LabelColor, "Оценка магии:");
        builder.AddLabel(180, 114, ValueColor, $"{evalInt:F1}/{bc.Skills[SkillName.EvalInt].Cap:F1}");

        builder.AddLabel(15, 136, LabelColor, "Стоимость маны:");
        builder.AddLabel(180, 136, ValueColor, $"x{manaMult:F1}");

        builder.AddHtml(15, 165, 290, 20, "Известные заклинания (тускло = ещё не выучено):");

        var x = 20;
        var y = 195;
        var col = 0;

        foreach (var (spellType, known) in AnimalTrainingSystem.GetFocusSpellKnowledge(bc))
        {
            // Full color once its circle is reachable at the pet's current Magery, a dark
            // grey hue while still locked — same icon either way, only the tint changes,
            // so the player can see the whole target list up front instead of discovering
            // spells one at a time as Magery happens to cross a threshold.
            builder.AddImage(x, y, IconFor(spellType), known ? 0 : 0x0451);

            col++;
            x += 44;

            if (col >= 6)
            {
                col = 0;
                x = 20;
                y += 44;
            }
        }
    }
}

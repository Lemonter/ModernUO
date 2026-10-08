using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Systems.MahaonCombat;

namespace Server.Gumps;

/// <summary>
///     Opened when a trainer picks "Магия" in AnimalTrainingGump — lets them choose (or
///     switch) which of the 4 curated spell groups the pet's magic training/auto-cast
///     draws from (see AnimalTrainingSystem.MagicFocus). One button per group, each shown
///     next to the real spell-book icon of that group's first spell (classic OSI spell
///     icon gump art starts at 0x1F2E for spell #1 and runs consecutively in
///     SpellRegistry order — same convention used by the real client spellbook).
/// </summary>
public class MagicFocusGump : DynamicGump
{
    private const int SpellIconBase = 0x1F2E;

    private readonly Mobile _trainer;
    private readonly BaseCreature _pet;
    private readonly BaseStaff _crook;

    public override bool Singleton => true;

    public MagicFocusGump(Mobile trainer, BaseCreature pet, BaseStaff crook) : base(50, 50)
    {
        _trainer = trainer;
        _pet = pet;
        _crook = crook;
    }

    // SpellRegistry IDs for Magery spells are 0-indexed OSI numbering (Clumsy=0 ...
    // Resurrection=63, see Spells/Initializer.cs) and classic spell-icon gump art runs
    // consecutively from 0x1F2E at id 0 — no off-by-one needed, unlike a 1-indexed scheme.
    private static int IconFor(System.Type spellType)
    {
        var id = Spells.SpellRegistry.GetRegistryNumber(spellType);
        return id >= 0 ? SpellIconBase + id : SpellIconBase;
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        var current = AnimalTrainingSystem.GetMagicFocus(_pet);
        var groups = new[] { MagicFocus.Support, MagicFocus.Debuff, MagicFocus.Damage, MagicFocus.Summon };

        var height = 60 + groups.Length * 60;

        builder.AddPage();
        builder.AddBackground(0, 0, 300, height, 5054);
        builder.AddAlphaRegion(10, 10, 280, height - 20);
        builder.AddHtml(15, 15, 270, 20, $"Магическая специализация: {_pet.Name}");

        for (var i = 0; i < groups.Length; i++)
        {
            var y = 45 + i * 60;
            var group = groups[i];
            var firstSpell = AnimalTrainingSystem.SpellGroup(group)[0];

            builder.AddImage(15, y, IconFor(firstSpell));
            builder.AddButton(60, y + 10, 4005, 4007, i + 1);
            builder.AddHtml(95, y + 8, 190, 20, AnimalTrainingSystem.RuFocusName(group) + (group == current ? " (текущая)" : ""));
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var index = info.ButtonID - 1;

        if (index is < 0 or > 3)
        {
            return;
        }

        var focus = (MagicFocus)index;
        AnimalTrainingSystem.StartTraining(_pet, _trainer, AnimalTrainingCategory.Magic, _crook, focus);
    }
}

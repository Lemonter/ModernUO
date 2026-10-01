using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Multis;
using Server.Spells;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SpellScroll : Item, ICommodity
{
    [SerializableField(0, setter: "private")]
    private int _spellID;

    [Constructible]
    public SpellScroll(int spellID, int itemID, int amount = 1) : base(itemID)
    {
        Stackable = true;
        Amount = amount;

        _spellID = spellID;
    }

    public override double DefaultWeight => 1.0;

    int ICommodity.DescriptionNumber => LabelNumber;
    bool ICommodity.IsDeedable => Core.ML;

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        var discipline = DisciplineRu(Spellbook.GetTypeForSpell(_spellID));

        if (discipline != null)
        {
            list.Add(discipline);
        }
    }

    // Mahaon: per the shard owner's ask — every scroll's tooltip names which discipline it
    // belongs to (свитки вперемешку иначе неотличимы на глаз), covering every SpellScroll
    // subclass from a single spot instead of touching each one individually. Reuses
    // Spellbook.GetTypeForSpell's own spellID-range mapping, so it can't drift out of sync
    // with which spellbook a scroll actually scribes into.
    private static string DisciplineRu(SpellbookType type) => type switch
    {
        SpellbookType.Regular     => "Магия",
        SpellbookType.Necromancer => "Некромантия",
        SpellbookType.Paladin     => "Рыцарство",
        SpellbookType.Samurai     => "Бусидо",
        SpellbookType.Ninja       => "Ниндзюцу",
        SpellbookType.Arcanist    => "Плетение заклинаний",
        SpellbookType.Mystic      => "Мистицизм",
        _                         => null
    };

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        if (from.Alive && Movable)
        {
            list.Add(new AddToSpellbookEntry());
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!DesignContext.Check(from))
        {
            return; // They are customizing
        }

        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return;
        }

        var spell = SpellRegistry.NewSpell(_spellID, from, this);

        if (spell != null)
        {
            spell.Cast();
        }
        else
        {
            from.SendLocalizedMessage(502345); // This spell has been temporarily disabled.
        }
    }
}

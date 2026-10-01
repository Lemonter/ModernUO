using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items;

/// <summary>The "Sacred Quest" key chain of the Underworld, ported from ServUO
/// (Scripts/Items/Quest/AbyssKey.cs and its siblings). Garamon explains it, Tyball's Shadow
/// guards the last piece: find the red and blue fragments in the Underworld's secret rooms,
/// take the yellow key from the shadow, and the three fuse into a Tripartite Key which the
/// stone guardians of the Tomb of Kings will accept as entry to the Stygian Abyss.
///
/// ServUO's own AbyssKey base is a line-for-line duplicate of its BaseDecayingItem; this
/// codebase already has that class, so AbyssKey here is the thin subclass it should have been.
/// Class names are the original's, verbatim, so ServUO's Data/objects.xml and decoration files
/// keep resolving against them.</summary>
[SerializationGenerator(0, false)]
public abstract partial class AbyssKey : BaseDecayingItem
{
    public AbyssKey(int itemID) : base(itemID)
    {
    }

    /// <summary>Any two fragments plus this one make the whole key. The original repeats this
    /// body in all three key classes; it is the same code three times.</summary>
    protected void TryAssemble(Mobile from, Item first, Item second)
    {
        if (first == null || second == null)
        {
            return;
        }

        from.AddToBackpack(new TripartiteKey());

        first.Delete();
        second.Delete();
        Delete();

        from.SendLocalizedMessage(1111649); // Tripartite Key
    }
}

[SerializationGenerator(0, false)]
public partial class RedKey1 : AbyssKey
{
    [Constructible]
    public RedKey1() : base(0x1012)
    {
        Weight = 1.0;
        Hue = 0x8F;
        LootType = LootType.Blessed;
        Movable = false;
    }

    public override int LabelNumber => 1111647; // Red Key Fragment
    public override int Lifespan => 21600;

    public override void OnDoubleClick(Mobile from)
    {
        if (from.Backpack == null)
        {
            return;
        }

        TryAssemble(
            from,
            from.Backpack.FindItemByType<BlueKey1>(),
            from.Backpack.FindItemByType<YellowKey1>()
        );
    }
}

[SerializationGenerator(0, false)]
public partial class BlueKey1 : AbyssKey
{
    [Constructible]
    public BlueKey1() : base(0x1012)
    {
        Weight = 1.0;
        Hue = 0x5D;
        LootType = LootType.Blessed;
        Movable = false;
    }

    public override int LabelNumber => 1111646; // Blue Key Fragment
    public override int Lifespan => 21600;

    public override void OnDoubleClick(Mobile from)
    {
        if (from.Backpack == null)
        {
            return;
        }

        TryAssemble(
            from,
            from.Backpack.FindItemByType<RedKey1>(),
            from.Backpack.FindItemByType<YellowKey1>()
        );
    }
}

/// <summary>Tyball's Shadow drops this one.</summary>
[SerializationGenerator(0, false)]
public partial class YellowKey1 : AbyssKey
{
    [Constructible]
    public YellowKey1() : base(0x1012)
    {
        Weight = 1.0;
        Hue = 0x489;
    }

    public override int LabelNumber => 1111648; // Yellow Key
    public override int Lifespan => 21600;

    public override void OnDoubleClick(Mobile from)
    {
        if (from.Backpack == null)
        {
            return;
        }

        TryAssemble(
            from,
            from.Backpack.FindItemByType<RedKey1>(),
            from.Backpack.FindItemByType<BlueKey1>()
        );
    }
}

/// <summary>A named, immovable variant of the yellow key. Kept because ServUO's objects.xml
/// places one by this exact type name.</summary>
[SerializationGenerator(0, false)]
public partial class TyballsKey : AbyssKey
{
    [Constructible]
    public TyballsKey() : base(0x1012)
    {
        Weight = 1.0;
        Hue = 0x489;
        Name = "Tyball's Key";
        Movable = false;
    }

    public override int Lifespan => 21600;

    public override void OnDoubleClick(Mobile from)
    {
        if (from.Backpack == null)
        {
            return;
        }

        TryAssemble(
            from,
            from.Backpack.FindItemByType<RedKey1>(),
            from.Backpack.FindItemByType<BlueKey1>()
        );
    }
}

/// <summary>The assembled key. Double-clicking it spends it for permanent Abyss entry.</summary>
[SerializationGenerator(0, false)]
public partial class TripartiteKey : Item
{
    [Constructible]
    public TripartiteKey() : base(0x1012)
    {
        Weight = 1.0;
        Hue = 1123;
        Movable = false;
    }

    public override int LabelNumber => 1111649; // Tripartite Key

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return;
        }

        if (from is not PlayerMobile pm)
        {
            return;
        }

        if (pm.AbyssEntry)
        {
            pm.SendMessage("Вы уже прошли Священный Поиск.");
        }
        else
        {
            pm.AbyssEntry = true;
            pm.SendMessage("Священный Поиск завершён — путь в Бездну открыт.");
        }

        Delete();
    }
}

/// <summary>The fragment fixed in place in one of the Underworld's secret rooms. It hands out
/// copies rather than being picked up, so a party can each take one.</summary>
[SerializationGenerator(0, false)]
public partial class Redkeyfragment : Item
{
    [Constructible]
    public Redkeyfragment() : base(0x1012)
    {
        Movable = false;
        Hue = 0x8F;
    }

    public override int LabelNumber => 1111647; // Red Key Fragment

    public override void OnDoubleClick(Mobile from)
    {
        from.SendMessage("Вы снимаете копию ключа себе в рюкзак.");

        var key = new RedKey1();

        if (!from.AddToBackpack(key))
        {
            key.Delete();
        }
    }
}

/// <summary>The blue fragment's dispenser; see <see cref="Redkeyfragment" />.</summary>
[SerializationGenerator(0, false)]
public partial class Bluekeyfragment : Item
{
    [Constructible]
    public Bluekeyfragment() : base(0x1012)
    {
        Movable = false;
        Hue = 0x5D;
    }

    public override int LabelNumber => 1111646; // Blue Key Fragment

    public override void OnDoubleClick(Mobile from)
    {
        from.SendMessage("Вы снимаете копию ключа себе в рюкзак.");

        var key = new BlueKey1();

        if (!from.AddToBackpack(key))
        {
            key.Delete();
        }
    }
}

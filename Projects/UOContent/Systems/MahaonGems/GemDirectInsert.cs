namespace Server.Items;

// Partial extensions — the original class files (Items/Gems/*.cs) are untouched. Each just
// adds "double-click the gem itself to insert it" on top of whatever it already does, per
// MahaonGemInsertion (see Systems/MahaonGems/MahaonGemInsertion.cs). GemEncrustingTool's
// separate 3-step flow still works too, this is just a shorter path straight from the gem.

public partial class Tourmaline
{
    public override void OnDoubleClick(Mobile from) => MahaonGemInsertion.StartInsert(from, this);
}

public partial class Amber
{
    public override void OnDoubleClick(Mobile from) => MahaonGemInsertion.StartInsert(from, this);
}

public partial class Amethyst
{
    public override void OnDoubleClick(Mobile from) => MahaonGemInsertion.StartInsert(from, this);
}

public partial class Ruby
{
    public override void OnDoubleClick(Mobile from) => MahaonGemInsertion.StartInsert(from, this);
}

public partial class Sapphire
{
    public override void OnDoubleClick(Mobile from) => MahaonGemInsertion.StartInsert(from, this);
}

public partial class Emerald
{
    public override void OnDoubleClick(Mobile from) => MahaonGemInsertion.StartInsert(from, this);
}

public partial class StarSapphire
{
    public override void OnDoubleClick(Mobile from) => MahaonGemInsertion.StartInsert(from, this);
}

public partial class Citrine
{
    public override void OnDoubleClick(Mobile from) => MahaonGemInsertion.StartInsert(from, this);
}

public partial class Diamond
{
    public override void OnDoubleClick(Mobile from) => MahaonGemInsertion.StartInsert(from, this);
}

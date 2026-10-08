using ModernUO.Serialization;

namespace Server.Items;

// Short, easy-to-type Spawner aliases — the real class names (TreasureChestLevel1-4) kept
// getting mistyped into the native Spawner's type-name field ("TreasureLevel3",
// "treasurechestlevel5"). Chest1-4 are plain subclasses of the real OSI-style itemized loot
// tables (TreasureChestLevel1-4) — same drops, just a shorter name to type. Chest5 has no
// itemized-loot ancestor to extend (this codebase's real per-item OSI loot tables were only
// ever ported up through level 4) — built on BaseTreasureChest's own simpler gold-scaling
// TreasureLevel.Level5 tier instead of inventing an "OSI level 5" item table with nothing
// real to check it against. Visual appearance deliberately not distinguished between these
// per the shard owner's own call — pick whichever tier's loot you want, the graphic doesn't
// matter.
//
// All five also drop our own blueprints (BaseTreasureChest.AddMahaonDrops) — blueprint tier
// matches the chest's own level, and the drop chance climbs with it. Chest5 already gets
// this for free from BaseTreasureChest's own GenerateTreasure(); Chest1-4 don't inherit that
// (TreasureChestLevel1-4 build their loot directly, no BaseTreasureChest in their chain), so
// each calls the same shared method explicitly with its own level.
//
// TreasureLevel1-4 below are a SECOND alias set for the exact same four classes — turns out
// Distribution/XmlSpawner/felucca.xml's own spawn entries already reference "TreasureLevel1"
// through "TreasureLevel4" (bare, no "Chest") thousands of times — confirmed via
// Distribution/badspawn.log, where these four names alone account for ~3200 of the log's
// ~7600 failed-to-resolve lines (42%), by far the single largest category. AssemblyHandler.
// FindTypeByName resolves case-insensitively, so these four classes also cover the log's
// lowercase "treasurelevel1".."treasurelevel4" variants for free.

[SerializationGenerator(0, false)]
public partial class Chest1 : TreasureChestLevel1
{
    [Constructible]
    public Chest1() => BaseTreasureChest.AddMahaonDrops(this, 1);
}

[SerializationGenerator(0, false)]
public partial class Chest2 : TreasureChestLevel2
{
    [Constructible]
    public Chest2() => BaseTreasureChest.AddMahaonDrops(this, 2);
}

[SerializationGenerator(0, false)]
public partial class Chest3 : TreasureChestLevel3
{
    [Constructible]
    public Chest3() => BaseTreasureChest.AddMahaonDrops(this, 3);
}

[SerializationGenerator(0, false)]
public partial class Chest4 : TreasureChestLevel4
{
    [Constructible]
    public Chest4() => BaseTreasureChest.AddMahaonDrops(this, 4);
}

[SerializationGenerator(0, false)]
public partial class Chest5 : BaseTreasureChest
{
    [Constructible]
    public Chest5() : base(0x9AB, TreasureLevel.Level5)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class TreasureLevel1 : TreasureChestLevel1
{
    [Constructible]
    public TreasureLevel1() => BaseTreasureChest.AddMahaonDrops(this, 1);
}

[SerializationGenerator(0, false)]
public partial class TreasureLevel2 : TreasureChestLevel2
{
    [Constructible]
    public TreasureLevel2() => BaseTreasureChest.AddMahaonDrops(this, 2);
}

[SerializationGenerator(0, false)]
public partial class TreasureLevel3 : TreasureChestLevel3
{
    [Constructible]
    public TreasureLevel3() => BaseTreasureChest.AddMahaonDrops(this, 3);
}

[SerializationGenerator(0, false)]
public partial class TreasureLevel4 : TreasureChestLevel4
{
    [Constructible]
    public TreasureLevel4() => BaseTreasureChest.AddMahaonDrops(this, 4);
}

using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/AcidSlug.cs). IAcidCreature dropped —
/// a pure marker interface with zero consumers found anywhere in this codebase (same
/// simplification as IBloodCreature in BloodWorm.cs). LootPack.LootItem&lt;T&gt; doesn't exist
/// (see WolfSpider.cs) — the AcidSac/CongealedSlugAcid drops are dropped entirely since
/// neither item exists anywhere in this codebase (ToL-specific resources never ported). The
/// Underworld movement-restriction check is kept verbatim — Region.IsPartOf(string) confirmed
/// to exist (Regions/Region.cs).</summary>
[SerializationGenerator(0, false)]
[CorpseName("an acid slug corpse")]
public partial class AcidSlug : BaseCreature
{
    [Constructible]
    public AcidSlug() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Hue = Utility.Random(4) switch
        {
            0 => 242,
            1 => 243,
            2 => 244,
            _ => 245
        };

        Body = 51;

        SetStr(213, 294);
        SetDex(80, 82);
        SetInt(18, 22);

        SetHits(333, 370);

        SetDamage(21, 28);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 10, 15);
        SetResistance(ResistanceType.Fire, 0);
        SetResistance(ResistanceType.Cold, 10, 15);
        SetResistance(ResistanceType.Poison, 60, 70);
        SetResistance(ResistanceType.Energy, 10, 15);

        SetSkill(SkillName.MagicResist, 25.0);
        SetSkill(SkillName.Tactics, 30.0, 50.0);
        SetSkill(SkillName.Wrestling, 30.0, 80.0);
    }

    public override string DefaultName => "кислотный слизень";

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Average);
    }

    public override int GetIdleSound() => 1499;
    public override int GetAngerSound() => 1496;
    public override int GetHurtSound() => 1498;
    public override int GetDeathSound() => 1497;

    public override bool CheckMovement(Direction d, out int newZ)
    {
        if (!base.CheckMovement(d, out newZ))
        {
            return false;
        }

        if (Region.IsPartOf("Underworld") && newZ > Location.Z)
        {
            return false;
        }

        return true;
    }
}

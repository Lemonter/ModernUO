using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/TreefellowGuardian.cs). AIType.AI_Mystic
/// substituted with AI_Mage (see Skree.cs). SetWeaponAbility converted to a GetWeaponAbility()
/// override. LootPack.LootItem&lt;T&gt; doesn't exist (see WolfSpider.cs) — the TreefellowWood/
/// Log drops are dropped entirely rather than converted, since TreefellowWood doesn't exist
/// anywhere in this codebase either (a ToL-specific resource item never ported).</summary>
[SerializationGenerator(0, false)]
[CorpseName("a treefellow guardian corpse")]
public partial class TreefellowGuardian : BaseCreature
{
    [Constructible]
    public TreefellowGuardian() : base(AIType.AI_Mage, FightMode.Evil, 10, 1)
    {
        Body = 301;

        SetStr(511, 695);
        SetDex(30, 55);
        SetInt(403, 491);

        SetHits(724, 900);

        SetDamage(12, 16);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 30, 35);
        SetResistance(ResistanceType.Cold, 50, 60);
        SetResistance(ResistanceType.Poison, 20, 30);
        SetResistance(ResistanceType.Energy, 80, 90);

        SetSkill(SkillName.MagicResist, 40.1, 55.0);
        SetSkill(SkillName.Tactics, 65.1, 90.0);
        SetSkill(SkillName.Wrestling, 65.1, 85.0);
        SetSkill(SkillName.Spellweaving, 120.0);

        Fame = 500;
        Karma = 1500;
    }

    public override string DefaultName => "древень-страж";

    public override bool BleedImmune => true;

    public override WeaponAbility GetWeaponAbility() => WeaponAbility.Dismount;

    public override int GetIdleSound() => 443;
    public override int GetDeathSound() => 31;
    public override int GetAttackSound() => 672;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Average);
    }
}

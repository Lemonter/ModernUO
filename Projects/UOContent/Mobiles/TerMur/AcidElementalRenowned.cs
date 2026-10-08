using System;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Renowned/AcidElementalRenowned.cs). Extends
/// the local BaseRenowned base. UniqueSAList/SharedSAList: BreastplateOfTheBerserker/
/// TerathanWarriorCostume/MysticsGarb don't exist anywhere in this codebase — dropped.
/// LootPack.LootItem&lt;T&gt; doesn't exist — Nightshade/LesserPoisonPotion converted to
/// always-on OnDeath drops (both items confirmed to exist).</summary>
[SerializationGenerator(0, false)]
[CorpseName("Acid Elemental [Renowned] corpse")]
public partial class AcidElementalRenowned : BaseRenowned
{
    [Constructible]
    public AcidElementalRenowned() : base(AIType.AI_Mage)
    {
        Title = "[Renowned]";
        Body = 0x9E;
        BaseSoundID = 278;

        SetStr(450, 600);
        SetDex(120, 185);
        SetInt(361, 435);

        SetHits(2000, 2400);

        SetDamage(9, 15);

        SetDamageType(ResistanceType.Physical, 25);
        SetDamageType(ResistanceType.Poison, 50);
        SetDamageType(ResistanceType.Energy, 25);

        SetResistance(ResistanceType.Physical, 40, 70);
        SetResistance(ResistanceType.Fire, 30, 50);
        SetResistance(ResistanceType.Cold, 20, 40);
        SetResistance(ResistanceType.Poison, 10, 30);
        SetResistance(ResistanceType.Energy, 20, 50);

        SetSkill(SkillName.EvalInt, 80.1, 100.0);
        SetSkill(SkillName.Magery, 80.1, 100.0);
        SetSkill(SkillName.MagicResist, 65.2, 100.0);
        SetSkill(SkillName.Tactics, 90.1, 100.0);
        SetSkill(SkillName.Wrestling, 80.1, 100.0);

        Fame = 12500;
        Karma = -12500;
    }

    public override string DefaultName => "кислотный элементаль";

    public override Type[] UniqueSAList => Array.Empty<Type>();
    public override Type[] SharedSAList => Array.Empty<Type>();
    public override bool BleedImmune => true;
    public override Poison PoisonImmune => Poison.Lethal;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Rich, 2);
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);
        c.DropItem(new Nightshade(4));
        c.DropItem(new LesserPoisonPotion());
    }
}

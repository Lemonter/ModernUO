using System;
using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Renowned/PixieRenowned.cs). Extends the local
/// BaseRenowned base. UniqueSAList/SharedSAList: DemonHuntersStandard/DragonJadeEarrings/
/// PillarOfStrength/SwordOfShatteredHopes don't exist anywhere in this codebase — dropped.
/// LootPack.Statue doesn't exist — dropped.</summary>
[SerializationGenerator(0, false)]
[CorpseName("Pixie [Renowned] corpse")]
public partial class PixieRenowned : BaseRenowned
{
    [Constructible]
    public PixieRenowned() : base(AIType.AI_Mage)
    {
        Title = "[Renowned]";
        Body = 128;
        BaseSoundID = 0x467;

        SetStr(350, 380);
        SetDex(450, 600);
        SetInt(700, 850);

        SetHits(9100, 9200);
        SetStam(450, 600);
        SetMana(700, 800);

        SetDamage(27, 38);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 70, 90);
        SetResistance(ResistanceType.Fire, 60, 70);
        SetResistance(ResistanceType.Cold, 70, 80);
        SetResistance(ResistanceType.Poison, 60, 70);
        SetResistance(ResistanceType.Energy, 60, 70);

        SetSkill(SkillName.EvalInt, 100.0, 100.0);
        SetSkill(SkillName.Magery, 90.1, 110.0);
        SetSkill(SkillName.Meditation, 100.0, 100.0);
        SetSkill(SkillName.MagicResist, 110.5, 150.0);
        SetSkill(SkillName.Tactics, 100.1, 120.0);
        SetSkill(SkillName.Wrestling, 100.1, 120.0);

        Fame = 7000;
        Karma = 7000;
    }

    public override string DefaultName => "пикси";

    public override Type[] UniqueSAList => Array.Empty<Type>();
    public override Type[] SharedSAList => Array.Empty<Type>();
    public override bool InitialInnocent => true;
    public override HideType HideType => HideType.Spined;
    public override int Hides => 5;
    public override int Meat => 1;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.UltraRich, 2);
    }
}

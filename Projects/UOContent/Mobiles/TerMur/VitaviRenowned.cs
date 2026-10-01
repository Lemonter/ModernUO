using System;
using ModernUO.Serialization;
using Server.Misc;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Renowned/VitaviRenowned.cs). Extends the
/// local BaseRenowned base. AIType.AI_Mystic substituted with AI_Mage (see Skree.cs).
/// AllureImmune dropped (no such virtual here). SharedSAList: AxeOfAbandon, DemonBridleRing,
/// VoidInfusedKilt all don't exist anywhere in this codebase — dropped, leaving an empty
/// list. LootPack.MageryRegs and LootPack.Statue don't exist either — dropped.</summary>
[SerializationGenerator(0, false)]
[CorpseName("Vitavi [Renowned] corpse")]
public partial class VitaviRenowned : BaseRenowned
{
    [Constructible]
    public VitaviRenowned() : base(AIType.AI_Mage)
    {
        Title = "[Renowned]";
        Body = 0x8F;
        BaseSoundID = 437;

        SetStr(300, 350);
        SetDex(250, 300);
        SetInt(300, 350);

        SetHits(45000, 50000);

        SetDamage(7, 14);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 50, 60);
        SetResistance(ResistanceType.Fire, 30, 50);
        SetResistance(ResistanceType.Cold, 60, 80);
        SetResistance(ResistanceType.Poison, 20, 30);
        SetResistance(ResistanceType.Energy, 30, 40);

        SetSkill(SkillName.EvalInt, 70.1, 80.0);
        SetSkill(SkillName.Magery, 70.1, 80.0);
        SetSkill(SkillName.MagicResist, 75.1, 100.0);
        SetSkill(SkillName.Tactics, 70.1, 75.0);
        SetSkill(SkillName.Wrestling, 50.1, 75.0);

        Fame = 7500;
        Karma = -7500;
    }

    public override string DefaultName => "Витави";

    public override Type[] UniqueSAList => Array.Empty<Type>();
    public override Type[] SharedSAList => Array.Empty<Type>();

    public override InhumanSpeech SpeechType => InhumanSpeech.Ratman;

    public override bool CanRummageCorpses => true;
    public override int Meat => 1;
    public override int Hides => 8;
    public override HideType HideType => HideType.Spined;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.FilthyRich, 3);
    }
}

using System;
using ModernUO.Serialization;
using Server.Misc;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Renowned/TikitaviRenowned.cs). Extends the
/// local BaseRenowned base. AllureImmune dropped (no such virtual here). UniqueSAList/
/// SharedSAList: BasiliskHideBreastplate, LegacyOfDespair, MysticsGarb all don't exist
/// anywhere in this codebase — dropped, leaving both lists empty.</summary>
[SerializationGenerator(0, false)]
[CorpseName("Tikitavi [Renowned] corpse")]
public partial class TikitaviRenowned : BaseRenowned
{
    [Constructible]
    public TikitaviRenowned() : base(AIType.AI_Melee)
    {
        Title = "[Renowned]";
        Body = 42;
        BaseSoundID = 437;

        SetStr(315, 354);
        SetDex(139, 177);
        SetInt(243, 288);

        SetHits(50000);
        SetMana(243, 288);
        SetStam(139, 177);

        SetDamage(7, 9);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 26, 28);
        SetResistance(ResistanceType.Fire, 22, 25);
        SetResistance(ResistanceType.Cold, 30, 38);
        SetResistance(ResistanceType.Poison, 14, 17);
        SetResistance(ResistanceType.Energy, 15, 18);

        SetSkill(SkillName.MagicResist, 40.4);
        SetSkill(SkillName.Tactics, 73.6);
        SetSkill(SkillName.Wrestling, 66.5);

        Fame = 1500;
        Karma = -1500;
    }

    public override string DefaultName => "Тикитави";

    public override Type[] UniqueSAList => Array.Empty<Type>();
    public override Type[] SharedSAList => Array.Empty<Type>();
    public override InhumanSpeech SpeechType => InhumanSpeech.Ratman;
    public override bool CanRummageCorpses => true;
    public override int Hides => 8;
    public override HideType HideType => HideType.Spined;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.FilthyRich, 3);
    }
}

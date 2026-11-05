using ModernUO.Serialization;
using Server.Misc;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO's Wrong Revamped dungeon (Mobile/LizardmanDefender.cs and
///     Mobile/LizardmanSquatter.cs) — the lizardmen that squat in the flooded tunnels.
///
///     Only <c>[CorpseName]</c> had to change: it became a <c>CorpseName</c> override because nothing in
///     this codebase reads the attribute.
/// </summary>
[SerializationGenerator(0, false)]
public partial class LizardmanDefender : BaseCreature
{
    [Constructible]
    public LizardmanDefender() : base(AIType.AI_Archer, FightMode.Closest, 10, 1)
    {
        Name = RussianNames.RandomLizardman();
        Title = "защитник";
        Body = Utility.RandomList(35, 36);
        BaseSoundID = 417;
        Hue = 1949;

        SetStr(157, 180);
        SetDex(105, 108);
        SetInt(50, 57);

        SetHits(1100, 1157);

        SetDamage(10, 15);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 50, 60);
        SetResistance(ResistanceType.Fire, 50, 60);
        SetResistance(ResistanceType.Cold, 50, 60);
        SetResistance(ResistanceType.Poison, 50, 60);

        SetSkill(SkillName.MagicResist, 65.1, 90.0);
        SetSkill(SkillName.Tactics, 95.1, 120.0);
        SetSkill(SkillName.Wrestling, 95.1, 120.0);

        Fame = 11000;
        Karma = -11000;

        VirtualArmor = 28;
    }

    public override string CorpseName => "труп ящера";

    public override InhumanSpeech SpeechType => InhumanSpeech.Lizardman;
    public override bool CanRummageCorpses => true;
    public override int TreasureMapLevel => 3;
    public override int Meat => 1;
    public override int Hides => 12;
    public override HideType HideType => HideType.Spined;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Meager);
    }
}

/// <summary>
///     The lizardman squatter; see <see cref="LizardmanDefender" /> for the corpse-name change.
/// </summary>
[SerializationGenerator(0, false)]
public partial class LizardmanSquatter : BaseCreature
{
    [Constructible]
    public LizardmanSquatter() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Name = RussianNames.RandomLizardman();
        Title = "сквоттер";
        Body = Utility.RandomList(35, 36);
        BaseSoundID = 417;

        SetStr(105, 138);
        SetDex(89, 114);
        SetInt(50, 66);

        SetHits(787, 869);

        SetDamage(10, 15);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 60, 70);
        SetResistance(ResistanceType.Fire, 50, 60);
        SetResistance(ResistanceType.Cold, 50, 60);
        SetResistance(ResistanceType.Poison, 50, 60);

        SetSkill(SkillName.MagicResist, 35.1, 90.0);
        SetSkill(SkillName.Tactics, 95.1, 120.0);
        SetSkill(SkillName.Wrestling, 95.1, 120.0);

        Fame = 10000;
        Karma = -10000;

        VirtualArmor = 28;
    }

    public override string CorpseName => "труп ящера-сквоттера";

    public override InhumanSpeech SpeechType => InhumanSpeech.Lizardman;
    public override bool CanRummageCorpses => true;
    public override int Meat => 1;
    public override int Hides => 12;
    public override int TreasureMapLevel => 2;
    public override HideType HideType => HideType.Spined;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Meager);
    }
}

using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO's Wrong Revamped dungeon (Mobile/BrigandCannibal.cs and
///     Mobile/BrigandCannibalMage.cs) — the cannibal brigands that guard the upper hall.
///
///     The mage's <c>SetSpecialAbility(SpecialAbility.LifeLeech)</c> comes through
///     <see cref="GetMonsterAbilities" /> as <see cref="MonsterAbilities.DrainLifeAttack" />. Corpse
///     names are <c>CorpseName</c> overrides rather than
///     ServUO's <c>[CorpseName]</c> attribute, which nothing in this codebase reads; the mage's is no
///     longer ServUO's copy-pasted "труп архимага".
/// </summary>
[SerializationGenerator(0, false)]
public partial class BrigandCannibal : Brigand
{
    [Constructible]
    public BrigandCannibal()
    {
        Title = "разбойник-каннибал";
        Hue = 33782;

        SetStr(68, 95);
        SetDex(81, 95);
        SetInt(110, 115);

        SetHits(2058, 2126);
        SetMana(552, 553);

        SetDamage(10, 23);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 65, 68);
        SetResistance(ResistanceType.Fire, 65, 66);
        SetResistance(ResistanceType.Cold, 62, 69);
        SetResistance(ResistanceType.Poison, 62, 67);
        SetResistance(ResistanceType.Energy, 64, 68);

        SetSkill(SkillName.MagicResist, 96.9, 96.9);
        SetSkill(SkillName.Tactics, 94.0, 94.0);
        SetSkill(SkillName.Swords, 54.3, 54.3);

        Fame = 14500;
        Karma = -14500;

        VirtualArmor = 16;
    }

    public override string CorpseName => "труп разбойника-каннибала";

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Average);
        AddLoot(LootPack.Meager);
    }
}

/// <summary>
///     The cannibal brigand mage; see <see cref="BrigandCannibal" /> for the shared substitutions.
///
///     Its ServUO <c>SetSpecialAbility(SpecialAbility.LifeLeech)</c> is
///     <see cref="MonsterAbilities.DrainLifeAttack" />; the mage keeps its <c>AI_NecroMage</c> switch
///     and its stat block alongside it.
/// </summary>
[SerializationGenerator(0, false)]
public partial class BrigandCannibalMage : EvilMage
{
    [Constructible]
    public BrigandCannibalMage()
    {
        Title = "разбойник-каннибал-маг";

        SetStr(68, 95);
        SetDex(81, 95);
        SetInt(110, 115);

        SetHits(2058, 2126);
        SetMana(552, 553);

        SetDamage(10, 23);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 50, 60);
        SetResistance(ResistanceType.Fire, 50, 60);
        SetResistance(ResistanceType.Cold, 50, 60);
        SetResistance(ResistanceType.Poison, 50, 60);
        SetResistance(ResistanceType.Energy, 50, 60);

        SetSkill(SkillName.MagicResist, 96.9, 96.9);
        SetSkill(SkillName.Tactics, 94.0, 94.0);
        SetSkill(SkillName.Wrestling, 54.3, 54.3);
        SetSkill(SkillName.Necromancy, 94.0, 94.0);
        SetSkill(SkillName.SpiritSpeak, 54.3, 54.3);

        Fame = 14500;
        Karma = -14500;

        if (Utility.RandomDouble() < 0.75)
        {
            PackItem(new SeveredHumanEars());
        }

        AI = AIType.AI_NecroMage;
    }

    public override string CorpseName => "труп разбойника-каннибала-мага";

    private static readonly MonsterAbility[] _abilities = [MonsterAbilities.DrainLifeAttack];

    public override MonsterAbility[] GetMonsterAbilities() => _abilities;
}

using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/MinotaurGeneral.cs). Loot.ArcanistScrollTypes
/// does not exist here, so the scroll pack uses the regular/necromancer random scroll instead.</summary>
[SerializationGenerator(0, false)]
public partial class MinotaurGeneral : MinotaurCaptain
{
    [Constructible]
    public MinotaurGeneral()
    {
        Body = 0x118;

        SetStr(602, 606);
        SetDex(137, 139);
        SetInt(83, 96);

        SetHits(1006, 1041);

        SetDamage(16, 22);

        SetDamageType(ResistanceType.Physical, 40);
        SetDamageType(ResistanceType.Cold, 20);
        SetDamageType(ResistanceType.Poison, 20);
        SetDamageType(ResistanceType.Energy, 20);

        SetResistance(ResistanceType.Physical, 77, 80);
        SetResistance(ResistanceType.Fire, 59, 64);
        SetResistance(ResistanceType.Cold, 63, 68);
        SetResistance(ResistanceType.Poison, 61, 66);
        SetResistance(ResistanceType.Energy, 63, 66);

        SetSkill(SkillName.Wrestling, 101.0, 129.7);
        SetSkill(SkillName.Tactics, 92.0, 104.8);
        SetSkill(SkillName.MagicResist, 86.0, 87.0);

        Fame = 18000;
        Karma = -18000;

        for (var i = 0; i < Utility.RandomMinMax(0, 1); i++)
        {
            PackItem(Loot.RandomScroll(0, Loot.ArcanistScrollTypes.Length - 1, SpellbookType.Arcanist));
        }
    }

    public override string CorpseName => "труп генерала минотавров";
    public override string DefaultName => "генерал минотавров";

    public override void GenerateLoot()
    {
        AddLoot(LootPack.UltraRich, 2);
    }

    public override int TreasureMapLevel => 4;
}

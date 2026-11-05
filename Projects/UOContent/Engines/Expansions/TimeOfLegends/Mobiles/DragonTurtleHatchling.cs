using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO (Scripts/Services/Expansions/Time Of Legends/Mobiles/DragonTurtleHatchling.cs).
///
///     Dropped: <c>OnAfterTame</c> (the skill-cap haircut on taming). This codebase has no
///     <c>OnAfterTame</c> hook — the taming transition is handled by <c>BaseCreature</c>'s own tame
///     path — so a tamed hatchling keeps its full skills rather than losing 55%.
///
///     ServUO's <c>SetSpecialAbility(SpecialAbility.DragonBreath)</c> becomes
///     <see cref="MonsterAbilities.FireBreath" />, the dragon breath of this codebase's
///     monster-ability framework; the bite keeps its bleed ability through
///     <see cref="GetWeaponAbility" />.
/// </summary>
[SerializationGenerator(0, false)]
public partial class DragonTurtleHatchling : BaseCreature
{
    private static readonly MonsterAbility[] _abilities = [MonsterAbilities.FireBreath];

    [Constructible]
    public DragonTurtleHatchling() : base(AIType.AI_Mage, FightMode.Aggressor)
    {
        Body = 1294;
        BaseSoundID = 362;
        Name = "детёныш драконьей черепахи";

        SetStr(550, 650);
        SetDex(55, 65);
        SetInt(550, 650);

        SetHits(550, 850);

        SetDamage(24, 33);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 65, 85);
        SetResistance(ResistanceType.Fire, 80, 90);
        SetResistance(ResistanceType.Cold, 50, 55);
        SetResistance(ResistanceType.Poison, 60);
        SetResistance(ResistanceType.Energy, 70, 75);

        SetSkill(SkillName.MagicResist, 130, 140);
        SetSkill(SkillName.Tactics, 110, 120);
        SetSkill(SkillName.Wrestling, 140, 150);
        SetSkill(SkillName.Magery, 130, 140);
        SetSkill(SkillName.EvalInt, 100, 119);

        Fame = 16000;
        Karma = -16000;

        Tamable = true;
        ControlSlots = 5;
        MinTameSkill = 104.7;
    }

    public override string CorpseName => "труп детёныша драконьей черепахи";

    public override WeaponAbility GetWeaponAbility() => WeaponAbility.BleedAttack;

    public override MonsterAbility[] GetMonsterAbilities() => _abilities;

    public override int Meat => 4;
    public override FoodType FavoriteFood => FoodType.Fish;

    public override void GenerateLoot() => AddLoot(LootPack.Average, 3);

    public override void OnCarve(Mobile from, Corpse corpse, Item with)
    {
        if (!Controlled && corpse != null && !corpse.Carved)
        {
            from.SendLocalizedMessage(1156198); // You cut away some pelts, but they remain on the corpse.
            corpse.DropItem(new DragonTurtleScute(4));
        }

        base.OnCarve(from, corpse, with);
    }
}

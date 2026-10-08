using System;
using ModernUO.Serialization;
using Server.Guilds;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
/// A battle mage a guild hires for a city it holds, on top of the sword guards: fights with spells
/// from range and grows with the city's guard level like the others.
/// </summary>
[SerializationGenerator(0, false)]
public partial class CityMageGuard : CityGuard
{
    public CityMageGuard(string city, Guild controllingGuild) : base(city, controllingGuild, AIType.AI_Mage)
    {
    }

    protected override string Title => "боевой маг";

    // Bone armour, dyed with the metal of the city's guard level like the others' plate.
    protected override void Outfit()
    {
        AddItem(new BoneChest());
        AddItem(new BoneArms());
        AddItem(new BoneLegs());
        AddItem(new BoneGloves());
        AddItem(new BoneHelm());
        AddItem(new Sandals());
        AddItem(new GnarledStaff());
    }

    public override void ApplyLevel(int level)
    {
        SetStr(70 + level * 2, 90 + level * 2);
        SetDex(70, 90);
        SetInt(100 + level * 4, 120 + level * 4);
        SetHits((int)(60 * (1 + level * 0.1)), (int)(80 * (1 + level * 0.1)));

        SetSkill(SkillName.Magery, Math.Min(120, 80.0 + level * 2), Math.Min(120, 100.0 + level * 2));
        SetSkill(SkillName.EvalInt, Math.Min(120, 80.0 + level * 2), Math.Min(120, 100.0 + level * 2));
        SetSkill(SkillName.Meditation, 70.0, 90.0);
        SetSkill(SkillName.Wrestling, 60.0, 80.0);
        SetSkill(SkillName.MagicResist, Math.Min(120, 70.0 + level * 4), Math.Min(120, 90.0 + level * 4));

        ApplyTeachers(level);
        ForgeGear(level);
        Hits = HitsMax;
        Mana = ManaMax;
    }
}

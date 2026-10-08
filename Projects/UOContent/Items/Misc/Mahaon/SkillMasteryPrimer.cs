using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Mobiles;
using Server.Spells.SkillMasteries;
using Server.Systems.MahaonMasteries;

namespace Server.Items;

// Ported from real OSI/ServUO content (Scripts/Spells/Skill Masteries/Core/
// SkillMasteryPrimer.cs) — a one-time-use item that raises a mastery skill's learned
// volume. `MasteryInfo.HasLearned`/`LearnMastery` already route through MasteryState (see
// that file), so this needed no logic changes, just the usual Serial-ctor/Serialize/
// Deserialize -> [SerializationGenerator] migration and [Constructable] -> [Constructible].
[SerializationGenerator(0, false)]
public partial class SkillMasteryPrimer : Item
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private SkillName _skill;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _volume;

    public override bool ForceShowProperties => true;

    [Constructible]
    public SkillMasteryPrimer(SkillName skill, int volume) : base(7714)
    {
        _skill = skill;
        _volume = volume;
        LootType = LootType.Cursed;
        Name = "том мастерства";
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            return;
        }

        if (MasteryInfo.HasLearned(from, _skill, _volume))
        {
            from.SendLocalizedMessage(1155884, $"#{MasteryInfo.GetLocalization(_skill)}"); // You are already proficient in this level of ~1_MasterySkill~
            return;
        }

        if (!MasteryInfo.LearnMastery(from, _skill, _volume))
        {
            return;
        }

        from.SendLocalizedMessage(1155885, $"#{MasteryInfo.GetLocalization(_skill)}"); // You have increased your proficiency in ~1_SkillMastery~!

        Effects.SendLocationParticles(EffectItem.Create(from.Location, from.Map, EffectItem.DefaultDuration), 0, 0, 0, 0, 0, 5060, 0);
        Effects.PlaySound(from.Location, from.Map, 0x243);
        Effects.SendTargetParticles(from, 0x375A, 35, 90, 0x00, 0x00, 9502, (EffectLayer)255, 0x100);

        Delete();
    }

    public override void AddNameProperty(IPropertyList list) => list.Add(1155882, $"#{MasteryInfo.GetLocalization(_skill)}"); // Primer on ~1_Skill~

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add(1155883, GetVolumeLabel(_volume)); // Volume ~1_Level~
    }

    private static string GetVolumeLabel(int volume) => volume switch
    {
        1 => "I",
        2 => "II",
        _ => "III"
    };

    /// <summary>Called from a real creature-death hook (see ImbuingMaterialDropSystem for
    /// the same pattern) — 10% chance on any kill, real drop, no quest gate (ServUO's own
    /// Hawkwind quest for the Book of Masteries itself isn't ported — see the session
    /// writeup).</summary>
    public static void CheckPrimerDrop(Mobile killer)
    {
        if (killer?.Backpack == null || Utility.RandomDouble() >= 0.10)
        {
            return;
        }

        var primer = GetRandom();

        if (killer.Backpack.TryDropItem(killer, primer, false))
        {
            killer.SendLocalizedMessage(1156209); // You have received a mastery primer!
        }
        else
        {
            primer.Delete();
        }
    }

    public static SkillMasteryPrimer GetRandom()
    {
        var skill = MasteryInfo.Skills[Utility.Random(MasteryInfo.Skills.Length)];
        var roll = Utility.RandomDouble();

        var volume = roll switch
        {
            <= 0.2 => 3,
            <= 0.5 => 2,
            _      => 1
        };

        return new SkillMasteryPrimer(skill, volume);
    }
}

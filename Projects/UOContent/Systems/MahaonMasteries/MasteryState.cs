using System.Collections.Generic;

namespace Server.Systems.MahaonMasteries;

/// <summary>
///     Real OSI/ServUO Skill Masteries are built on Mastery-awareness baked directly into
///     the engine's own `Skill`/`Skills` classes (`Skill.VolumeLearned`, `Skill.IsMastery`,
///     `Skill.HasLearnedMastery()`, `Skills.CurrentMastery`, etc.) — none of that exists in
///     this ModernUO codebase (confirmed: zero mastery-related members anywhere in
///     Projects/Server/Skills.cs). Rather than patch the engine's Skill class, this holds
///     the exact same state externally, GenericPersistence-backed, same pattern as
///     AuctionHouseSystem/BotController for "state that isn't itself a serialized Item/
///     Mobile field." Every call site that would have read `m.Skills[x].VolumeLearned` or
///     `m.Skills.CurrentMastery` in the original source reads through here instead.
/// </summary>
public sealed class MasteryState : GenericPersistence
{
    private static MasteryState _instance;

    // (Mobile, SkillName) -> highest volume (1-3) learned for that mastery skill.
    private static readonly Dictionary<(Mobile, SkillName), int> VolumeLearned = new();

    // Mobile -> the one mastery skill currently active (only one at a time, real OSI rule).
    private static readonly Dictionary<Mobile, SkillName> CurrentMastery = new();

    public MasteryState() : base("MahaonMasteries", 1)
    {
    }

    public static void Configure() => _instance = new MasteryState();

    public static int GetVolumeLearned(Mobile m, SkillName skill) => VolumeLearned.GetValueOrDefault((m, skill), 0);

    public static bool HasLearnedMastery(Mobile m, SkillName skill) => GetVolumeLearned(m, skill) > 0;

    public static bool HasLearnedVolume(Mobile m, SkillName skill, int volume) => GetVolumeLearned(m, skill) >= volume;

    /// <returns>true if this actually raised the learned volume (matches
    /// MasteryInfo.LearnMastery's real contract — false if already known at/above this
    /// volume, so a primer doesn't get consumed for nothing).</returns>
    public static bool LearnMastery(Mobile m, SkillName skill, int volume)
    {
        if (GetVolumeLearned(m, skill) >= volume)
        {
            return false;
        }

        VolumeLearned[(m, skill)] = volume;
        return true;
    }

    // SkillName has no "none" member usable here (SkillName.Alchemy is real OSI's own
    // sentinel value for "no mastery active" — see MasterySelectionGump.OnResponse) — same
    // sentinel convention kept here for a straight port of the switching logic.
    public static SkillName GetCurrentMastery(Mobile m) => CurrentMastery.GetValueOrDefault(m, SkillName.Alchemy);

    public static void SetCurrentMastery(Mobile m, SkillName skill) => CurrentMastery[m] = skill;

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version

        writer.WriteEncodedInt(VolumeLearned.Count);
        foreach (var ((m, skill), volume) in VolumeLearned)
        {
            writer.Write(m);
            writer.Write((int)skill);
            writer.WriteEncodedInt(volume);
        }

        writer.WriteEncodedInt(CurrentMastery.Count);
        foreach (var (m, skill) in CurrentMastery)
        {
            writer.Write(m);
            writer.Write((int)skill);
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // version

        var volumeCount = reader.ReadEncodedInt();
        for (var i = 0; i < volumeCount; i++)
        {
            var m = reader.ReadEntity<Mobile>();
            var skill = (SkillName)reader.ReadInt();
            var volume = reader.ReadEncodedInt();

            if (m != null)
            {
                VolumeLearned[(m, skill)] = volume;
            }
        }

        var masteryCount = reader.ReadEncodedInt();
        for (var i = 0; i < masteryCount; i++)
        {
            var m = reader.ReadEntity<Mobile>();
            var skill = (SkillName)reader.ReadInt();

            if (m != null)
            {
                CurrentMastery[m] = skill;
            }
        }
    }
}

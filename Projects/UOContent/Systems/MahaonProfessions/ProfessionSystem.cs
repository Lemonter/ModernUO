using System;
using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Systems.MahaonProfessions;

public class ProfessionSystem : GenericPersistence
{
    private static ProfessionSystem _instance;

    private const double BaseSkillCap = 100.0;
    private const double PrimarySkillCap = 120.0;

    private static readonly Dictionary<Mobile, MahaonProfession> Chosen = new();

    public ProfessionSystem() : base("MahaonProfessions", 1)
    {
    }

    public static void Configure()
    {
        _instance = new ProfessionSystem();
    }

    public static MahaonProfession? GetProfession(Mobile m) =>
        Chosen.TryGetValue(m, out var profession) ? profession : null;

    public static void SetProfession(Mobile m, MahaonProfession profession)
    {
        Chosen[m] = profession;

        var info = ProfessionData.All[profession];

        // Every skill starts at the flat 100 cap...
        foreach (var skillName in System.Enum.GetValues<SkillName>())
        {
            if ((int)skillName >= m.Skills.Length)
            {
                continue;
            }

            var skill = m.Skills[skillName];
            if (skill == null)
            {
                continue; // not available in this era/ruleset
            }

            skill.Cap = BaseSkillCap;
        }

        // ...except the primary category's full class kit, which can go to 120.
        if (ProfessionData.CategorySkills.TryGetValue(info.Category, out var primarySkills))
        {
            foreach (var skillName in primarySkills)
            {
                if ((int)skillName >= m.Skills.Length)
                {
                    continue;
                }

                var skill = m.Skills[skillName];
                if (skill != null)
                {
                    skill.Cap = PrimarySkillCap;
                }
            }
        }

        // Stats start low and grow toward the profession's cap the same way skills do —
        // see BotController.TryTrainSkill, which now also occasionally trains a stat.
        // Stats start with a real head start (40% of this profession's own cap) and grow
        // the rest of the way through real use — see TryGainStat, hooked into combat/
        // casting/gathering — not gold-bought training like skills are.
        m.RawStr = Math.Max(10, (int)(info.StrCap * 0.4));
        m.RawDex = Math.Max(10, (int)(info.DexCap * 0.4));
        m.RawInt = Math.Max(10, (int)(info.IntCap * 0.4));

        // StatCap is the vanilla "sum of all three" ceiling — set it generously above this
        // profession's own individual caps so it never becomes the binding constraint
        // instead of the profession's real per-stat numbers.
        m.StatCap = info.StrCap + info.DexCap + info.IntCap;

        // Stand-in for "icon above the name" — a real floating icon needs client-side
        // graphic work we can't do here, so the profession shows as the character's title
        // instead (visible on single-click / in the paperdoll), same idea, simpler to ship.
        var name = ProfessionData.GetName(profession, m.Female);
        if (m is PlayerMobile pm)
        {
            pm.DisplayChampionTitle = false; // avoid the title fighting with champion titles
        }

        m.Title = name;
    }

    /// <summary>Reset — per the profession stone's new 1000-gold reset option. Drops the
    /// profession identity, title, and the primary-category skill cap bonus (back to the
    /// flat 100 everyone else has), and widens StatCap back to the vanilla default so it
    /// isn't left stuck at whatever the old profession's tighter ceiling was. Doesn't
    /// reduce stats/skills already trained — a respec, not a punishment.</summary>
    public static void ClearProfession(Mobile m)
    {
        if (!Chosen.Remove(m))
        {
            return;
        }

        foreach (var skillName in System.Enum.GetValues<SkillName>())
        {
            if ((int)skillName >= m.Skills.Length)
            {
                continue;
            }

            var skill = m.Skills[skillName];
            if (skill != null)
            {
                skill.Cap = BaseSkillCap;
            }
        }

        m.StatCap = 225; // vanilla default
        m.Title = null;
    }

    public static bool TouchesCategory(Mobile m, ProfessionCategory category)
    {
        var profession = GetProfession(m);
        if (profession == null)
        {
            return false;
        }

        var info = ProfessionData.All[profession.Value];
        return info.Category == category || info.SecondaryCategory == category;
    }

    /// <summary>The profession's own per-stat ceiling — separate from the aggregate
    /// StatCap, which is set wide enough that this is the real binding limit.</summary>
    public static int GetStatCap(Mobile m, StatType stat)
    {
        var profession = GetProfession(m);
        if (profession == null)
        {
            return 100;
        }

        var info = ProfessionData.All[profession.Value];
        return stat switch
        {
            StatType.Str => info.StrCap,
            StatType.Dex => info.DexCap,
            StatType.Int => info.IntCap,
            _ => 100
        };
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version
        writer.WriteEncodedInt(Chosen.Count);

        foreach (var (mobile, profession) in Chosen)
        {
            writer.Write(mobile);
            writer.WriteEncodedInt((int)profession);
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // version

        var count = reader.ReadEncodedInt();
        for (var i = 0; i < count; i++)
        {
            var mobile = reader.ReadEntity<Mobile>();
            var profession = (MahaonProfession)reader.ReadEncodedInt();

            if (mobile != null)
            {
                Chosen[mobile] = profession;
            }
        }
    }
}

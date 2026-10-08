using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Spells.SkillMasteries;
using Server.Systems.MahaonMasteries;

namespace Server.Gumps;

// Ported from real OSI/ServUO content (Scripts/Spells/Skill Masteries/Core/
// SelectMasteryGump.cs). Rewritten against this codebase's plain Gump (no BaseGump
// convenience wrapper here — same mechanical rewrite as every other TOL gump this
// session). Reads learned volumes/current mastery through MasteryState instead of
// Skill.VolumeLearned/Skills.CurrentMastery (neither exists here).
public class MasterySelectionGump : Gump
{
    private const int Red = 0x8E2525;
    private const int Blue = 0x000066;

    private readonly PlayerMobile _user;

    public MasterySelectionGump(PlayerMobile user) : base(75, 25)
    {
        _user = user;

        AddBackground(0, 0, 404, 550, 9380);
        AddHtmlLocalized(0, 40, 404, 16, 1151948, "", 0, false, false); // Switch Mastery

        var y = 58;
        var current = MasteryState.GetCurrentMastery(user);

        foreach (var skName in MasteryInfo.Skills)
        {
            if (!MasteryState.HasLearnedMastery(user, skName))
            {
                continue;
            }

            AddButton(30, y, 4005, 4007, (int)skName + 1, GumpButtonType.Reply, 0);

            var color = skName == current ? Red : Blue;
            AddHtmlLocalized(72, y, 200, 16, MasteryInfo.GetLocalization(skName), color, false, false);
            AddHtmlLocalized(265, y, 100, 16, 1156052, MasteryState.GetVolumeLearned(user, skName).ToString(), 0, false, false);

            y += 24;
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID == 0)
        {
            return;
        }

        var n = (SkillName)(info.ButtonID - 1);
        var current = MasteryState.GetCurrentMastery(_user);

        if (n == current)
        {
            MasteryState.SetCurrentMastery(_user, SkillName.Alchemy);
            MasteryInfo.OnMasteryChanged(_user, current);
        }
        else if (_user.Skills[n].Value >= MasteryInfo.MinSkillRequirement)
        {
            _user.SendLocalizedMessage(1155886, _user.Skills[n].Name); // Your active skill mastery is now set to ~1_MasterySkill~!
            MasteryState.SetCurrentMastery(_user, n);

            MasteryInfo.OnMasteryChanged(_user, current);

            BookOfMasteries.AddToCooldown(_user);
        }
        else
        {
            _user.SendLocalizedMessage(1156236, $"{MasteryInfo.MinSkillRequirement}\t{_user.Skills[n].Name}"); // You need at least ~1_SKILL_REQUIREMENT~ ~2_SKILL_NAME~ skill to use that mastery.
        }
    }
}

using Server.Mobiles;
using Server.Network;

namespace Server.Gumps;

/// <summary>
///     ServUO's <c>ApplySkillBonusGump</c> (from the reward-store framework) does not exist here; this is a small
///     replacement used by the seasonal skill-bonus rings (Sterling Silver Ring, Katalkotl's Ring).
/// </summary>
public class SkillBonusGump : Gump
{
    private readonly AosSkillBonuses _bonuses;
    private readonly SkillName[] _skills;
    private readonly int _index;
    private readonly double _value;

    public SkillBonusGump(AosSkillBonuses bonuses, SkillName[] skills, int index, double value) : base(100, 100)
    {
        _bonuses = bonuses;
        _skills = skills;
        _index = index;
        _value = value;

        AddBackground(0, 0, 320, 60 + skills.Length * 25, 0x2454);
        AddHtml(20, 15, 280, 20, "<basefont color=#FFFFFF>Выберите навык для бонуса:</basefont>", false, false);

        for (var i = 0; i < skills.Length; i++)
        {
            AddButton(25, 50 + i * 25, 4005, 4007, i + 1);
            AddHtml(60, 50 + i * 25, 220, 20, $"<basefont color=#FFFFFF>{skills[i]} +{value}</basefont>", false, false);
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (sender.Mobile is not PlayerMobile pm || info.ButtonID < 1 || info.ButtonID > _skills.Length)
        {
            return;
        }

        _bonuses.SetValues(_index, _skills[info.ButtonID - 1], _value);

        pm.SendLocalizedMessage(1155609); // Double Click to Set Skill Bonus
    }
}

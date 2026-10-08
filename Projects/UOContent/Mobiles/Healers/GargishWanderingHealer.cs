using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>The gargoyle healer of Ter Mur. Ported from ServUO
/// (Scripts/Mobiles/NPCs/GargishWanderingHealer.cs) — the human wandering healer with a
/// gargoyle body, and Mysticism added to what she teaches.
///
/// The original's robe line is commented out upstream and stays that way: it names
/// GargishRobe, which takes no hue argument there either.</summary>
[SerializationGenerator(0, false)]
public partial class GargishWanderingHealer : BaseHealer
{
    [Constructible]
    public GargishWanderingHealer()
    {
        Title = "a Gargish wandering healer";
        Body = 666;

        AddItem(new GnarledStaff());

        SetSkill(SkillName.Camping, 80.0, 100.0);
        SetSkill(SkillName.Forensics, 80.0, 100.0);
        SetSkill(SkillName.SpiritSpeak, 80.0, 100.0);
    }

    public override bool CanTeach => true;

    public override bool ClickTitle => false; // Do not display title in OnSingleClick

    public override bool CheckTeach(SkillName skill, Mobile from)
    {
        if (!base.CheckTeach(skill, from))
        {
            return false;
        }

        return skill is SkillName.Anatomy or SkillName.Camping or SkillName.Forensics or SkillName.Healing
            or SkillName.SpiritSpeak or SkillName.Mysticism;
    }

    public override bool CheckResurrect(Mobile m)
    {
        if (m.Criminal)
        {
            Say(501222); // Thou art a criminal.  I shall not resurrect thee.
            return false;
        }

        if (m.Murderer)
        {
            Say(501223); // Thou'rt not a decent and good person. I shall not resurrect thee.
            return false;
        }

        return true;
    }
}

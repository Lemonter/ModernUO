using ModernUO.Serialization;
using Server.Gumps;
using Server.Mobiles;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKhaldun/Items/SterlingSilverRing.cs).
///     ServUO's <c>IsArtifact</c> and <c>ApplySkillBonusGump</c> (reward-store framework) are dropped; the skill
///     bonus is picked through the local <see cref="SkillBonusGump" />.
/// </summary>
[SerializationGenerator(0, false)]
public partial class SterlingSilverRing : SilverRing
{
    public static SkillName[] Skills { get; } =
    {
        SkillName.Archery,
        SkillName.Fencing,
        SkillName.Macing,
        SkillName.Swords,
        SkillName.Throwing,
        SkillName.Wrestling
    };

    public override int LabelNumber => 1155606; // Stirling Silver Ring

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public SterlingSilverRing()
    {
        Attributes.RegenHits = 3;
        Attributes.RegenMana = 5;
        Attributes.WeaponDamage = 75;
    }

    public bool HasSkillBonus => SkillBonuses.Skill_1_Value != 0;

    public override void OnDoubleClick(Mobile m)
    {
        if (IsChildOf(m.Backpack) && m is PlayerMobile && !HasSkillBonus)
        {
            m.SendGump(new SkillBonusGump(SkillBonuses, Skills, 0, 20));
        }
        else
        {
            base.OnDoubleClick(m);
        }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (!HasSkillBonus)
        {
            list.Add(1155609); // Double Click to Set Skill Bonus
        }
    }
}

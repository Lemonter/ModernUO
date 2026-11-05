using Server.Engines.Craft;
using Server.Items;
using Server.Mobiles;
using Server.Systems.MahaonCombat;
using Xunit;

namespace Server.Tests.Systems.MahaonCombat;

/// <summary>Этап 8: ступени ремесленных веток.</summary>
[Collection("Sequential UOContent Tests")]
public class CraftPerksTests
{
    [Fact]
    public void Blacksmith_PicksTheBranchByItemType()
    {
        var pm = new PlayerMobile();

        try
        {
            Assert.Equal("craft:blacksmith/Shield", CraftPerks.BranchFor(pm, DefBlacksmithy.CraftSystem, typeof(HeaterShield)).Line);
            Assert.Equal("craft:blacksmith/Armor", CraftPerks.BranchFor(pm, DefBlacksmithy.CraftSystem, typeof(PlateChest)).Line);
            Assert.Equal("craft:blacksmith/Blunt", CraftPerks.BranchFor(pm, DefBlacksmithy.CraftSystem, typeof(WarHammer)).Line);
            Assert.Equal("craft:blacksmith/Blade", CraftPerks.BranchFor(pm, DefBlacksmithy.CraftSystem, typeof(Katana)).Line);
            Assert.Null(CraftPerks.BranchFor(pm, DefBlacksmithy.CraftSystem, typeof(Shovel)).Line);
        }
        finally
        {
            pm.Delete();
        }
    }

    [Fact]
    public void Exceptional_OnlyOnTopOfWhatTheSkillAllows()
    {
        var pm = new PlayerMobile();

        try
        {
            MahaonProgress.Set(pm, "branch:blacksmith/Armor", 100.0);

            // навык не позволяет исключительное — перк ничего не открывает
            Assert.Equal(0.0, CraftPerks.ExceptionalBonus(pm, DefBlacksmithy.CraftSystem, typeof(PlateChest), 0.0));
            // позволяет — 50: +5, 100: +10
            Assert.Equal(0.15, CraftPerks.ExceptionalBonus(pm, DefBlacksmithy.CraftSystem, typeof(PlateChest), 0.2), 3);
        }
        finally
        {
            pm.Delete();
        }
    }

    [Fact]
    public void EveryCraftLine_HasFourSteps()
    {
        foreach (var line in new[]
                 {
                     "craft:blacksmith/Armor", "craft:tailoring/Clothier", "craft:carpentry", "craft:tinkering",
                     "craft:cooking", "craft:alchemy", "craft:inscription", "gather:mining/Обычный", "gather:lumber/RareWood",
                     "gather:fishing/Deep"
                 })
        {
            var perks = CraftPerks.PerksOf(line);
            Assert.Equal(4, perks.Count);

            foreach (var perk in perks)
            {
                Assert.False(string.IsNullOrEmpty(CraftPerks.Describe(perk)), line);
            }
        }
    }

    [Fact]
    public void Board_FindsCraftLinesByBranchName()
    {
        Assert.Equal("craft:alchemy", CraftPerks.LineOf((int)SkillName.Alchemy, AlchemySpecializationSystem.RuSpecializationName));
        Assert.Equal(
            "craft:tailoring/Clothier",
            CraftPerks.LineOf((int)SkillName.Tailoring, TailoringSpecializationSystem.RuSpecializationName(TailoringSpecialization.Clothier))
        );
        Assert.Null(CraftPerks.LineOf((int)SkillName.Alchemy, "что-то другое"));
    }
}

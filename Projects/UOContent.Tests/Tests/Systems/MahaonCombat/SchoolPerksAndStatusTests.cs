using Server.Mobiles;
using Server.Spells.Third;
using Server.Systems.MahaonCombat;
using Xunit;

namespace Server.Tests.Systems.MahaonCombat;

/// <summary>
///     Этап 8: перки школ на 25/50/75/100; состояния «горит / истекает кровью / перелом» для
///     клиента.
/// </summary>
[Collection("Sequential UOContent Tests")]
public class SchoolPerksAndStatusTests
{
    [Fact]
    public void FireSchool_PerksOpenByThreshold()
    {
        var pm = new PlayerMobile();

        try
        {
            var fireball = new FireballSpell(pm, null);

            Assert.Equal(1.0, SchoolPerks.PowerScalar(pm, fireball));
            Assert.Equal(1.0, SchoolPerks.CastScalar(pm, fireball));

            MahaonProgress.Set(pm, "branch:magery/Fire", 30.0);
            Assert.Equal(1.05, SchoolPerks.PowerScalar(pm, fireball), 3); // 25: +5% силы
            Assert.Equal(1.0, SchoolPerks.CastScalar(pm, fireball));      // 75 ещё не открыт

            MahaonProgress.Set(pm, "branch:magery/Fire", 100.0);
            Assert.Equal(0.90, SchoolPerks.CastScalar(pm, fireball), 3);  // 75: каст −10%
            Assert.True(SchoolPerks.Has(pm, fireball, SchoolPerkKind.Burn)); // 100: поджог
        }
        finally
        {
            pm.Delete();
        }
    }

    [Fact]
    public void EverySchoolInTheCatalog_HasFourPerks()
    {
        foreach (var entry in SpellSchools.Catalog)
        {
            var perks = SchoolPerks.PerksOf(entry.Line);
            Assert.Equal(4, perks.Count);
            Assert.Equal(new[] { 25, 50, 75, 100 }, new[] { perks[0].Threshold, perks[1].Threshold, perks[2].Threshold, perks[3].Threshold });

            foreach (var perk in perks)
            {
                Assert.False(string.IsNullOrEmpty(SchoolPerks.Describe(perk)), entry.Line);
            }
        }
    }

    [Fact]
    public void SchoolsWithoutReagents_HaveNoReagentPerks()
    {
        foreach (var line in new[] { "chivalry/Healing", "chivalry/Combat", "chivalry/Sacrifice", "spellweaving/Damage", "spellweaving/Summoning" })
        {
            Assert.DoesNotContain(SchoolPerks.PerksOf(line), p => p.Kind == SchoolPerkKind.Reagents);
        }
    }

    [Fact]
    public void Board_FindsTheSchoolByBranchName()
    {
        Assert.Equal("magery/Fire", SchoolPerks.LineOf((int)SkillName.Magery, "Школа огня"));
        Assert.Equal("necromancy/Summoning", SchoolPerks.LineOf((int)SkillName.Necromancy, "Школа призыва мёртвых"));
        Assert.Null(SchoolPerks.LineOf((int)SkillName.Necromancy, "Школа призыва")); // это имя теперь только у Магии
        Assert.Equal("magery/Summoning", SchoolPerks.LineOf((int)SkillName.Magery, "Школа призыва"));
        Assert.Null(SchoolPerks.LineOf((int)SkillName.Swords, "Берсерк"));
    }

    [Fact]
    public void Burning_IgnitesAndGoesOut()
    {
        var victim = new PlayerMobile();

        try
        {
            BurningSystem.Ignite(victim, null, 5, 3);
            Assert.True(BurningSystem.IsBurning(victim));
            Assert.Equal(3, BurningSystem.SecondsLeft(victim));

            // повторный поджог продлевает, а не складывает
            BurningSystem.Ignite(victim, null, 2, 5);
            Assert.Equal(5, BurningSystem.SecondsLeft(victim));

            BurningSystem.Extinguish(victim);
            Assert.False(BurningSystem.IsBurning(victim));
        }
        finally
        {
            BurningSystem.Extinguish(victim);
            victim.Delete();
        }
    }

    [Fact]
    public void StatusPacket_HasTheDocumentedLayout()
    {
        var bytes = MahaonStatusEffects.Build((Serial)0x00001234, MahaonStatus.Fracture, true, 45, 1);

        Assert.Equal(13, bytes.Length);
        Assert.Equal(0xF9, bytes[0]);
        Assert.Equal(13, (bytes[1] << 8) | bytes[2]);
        Assert.Equal(MahaonStatusEffects.Subtype, bytes[3]);
        Assert.Equal(0x1234u, (uint)((bytes[4] << 24) | (bytes[5] << 16) | (bytes[6] << 8) | bytes[7]));
        Assert.Equal((byte)MahaonStatus.Fracture, bytes[8]);
        Assert.Equal(1, bytes[9]);
        Assert.Equal(45, (bytes[10] << 8) | bytes[11]);
        Assert.Equal(1, bytes[12]); // ноги
    }

    [Fact]
    public void Fracture_OnlyLimbsBreak()
    {
        Assert.True(BoneFractureSystem.CanFracture(HitLocation.Arms));
        Assert.True(BoneFractureSystem.CanFracture(HitLocation.Legs));
        Assert.True(BoneFractureSystem.CanFracture(HitLocation.Hands));
        Assert.False(BoneFractureSystem.CanFracture(HitLocation.Neck));
        Assert.False(BoneFractureSystem.CanFracture(HitLocation.Chest));
    }
}

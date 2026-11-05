using System.Collections.Generic;
using System.Linq;
using Server.Mobiles;
using Server.Systems.MahaonCombat;
using Xunit;

namespace Server.Tests.Systems.MahaonCombat;

/// <summary>
///     Имя ветки — ключ её потолка (свитки мастерства) и строка на доске. Два одинаковых имени
///     значат общий потолок у двух разных веток: свиток поднимал сразу обе. Так было со
///     «Школой призыва» (Магия и Некромантия) и «Школой перевоплощения» (Некромантия и
///     Плетение чар).
/// </summary>
[Collection("Sequential UOContent Tests")]
public class BranchNamesTests
{
    [Fact]
    public void EveryBranchName_IsUnique()
    {
        var pm = new PlayerMobile();

        try
        {
            var seen = new Dictionary<string, string>();
            var duplicates = new List<string>();

            foreach (var category in MahaonSkillTree.GetTreeSnapshot(pm))
            {
                foreach (var sub in category.SubCategories.Where(s => !s.IsUsableSkillList))
                {
                    foreach (var entry in sub.Entries.Where(e => e.HasCap))
                    {
                        if (seen.TryGetValue(entry.Name, out var where) && where != sub.Name)
                        {
                            duplicates.Add($"«{entry.Name}»: {where} и {sub.Name}");
                        }

                        seen[entry.Name] = sub.Name;
                    }
                }
            }

            Assert.True(duplicates.Count == 0, string.Join("; ", duplicates));
        }
        finally
        {
            pm.Delete();
        }
    }
}

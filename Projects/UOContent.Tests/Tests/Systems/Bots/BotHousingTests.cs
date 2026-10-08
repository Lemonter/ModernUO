using Server.Systems.Bots;
using Xunit;

namespace Server.Tests.Systems.Bots;

[Collection("Sequential UOContent Tests")]
public class BotHousingTests
{
    [Fact]
    public void ChooseEntry_StaysWithinBudget_AndGuildsTakeTheBiggest()
    {
        Assert.Null(BotHousing.ChooseEntry(10_000, false));

        var small = BotHousing.ChooseEntry(60_000, false);
        Assert.NotNull(small);
        Assert.True(small.Cost <= 60_000);

        var guild = BotHousing.ChooseEntry(200_000, true);
        Assert.NotNull(guild);
        Assert.True(guild.Cost <= 200_000);
        Assert.True(guild.Cost > 150_000);
    }
}

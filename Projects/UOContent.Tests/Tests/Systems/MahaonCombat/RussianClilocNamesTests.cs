using Server.Items;
using Server.Systems.MahaonCombat;
using Xunit;

namespace Server.Tests.Systems.MahaonCombat;

/// <summary>
///     Журнал боя называл броню именем класса («по RingmailChest»): у стандартных предметов
///     Name пустой. Теперь имя берётся своё, иначе из cliloc — и никогда не пустое.
/// </summary>
[Collection("Sequential UOContent Tests")]
public class RussianClilocNamesTests
{
    [Fact]
    public void OwnName_Wins()
    {
        var chest = new RingmailChest { Name = "рубаха Ярослава" };

        try
        {
            Assert.Equal("рубаха Ярослава", RussianClilocNames.NameOf(chest));
        }
        finally
        {
            chest.Delete();
        }
    }

    [Fact]
    public void Unnamed_IsNeverEmpty()
    {
        var chest = new RingmailChest();

        try
        {
            Assert.False(string.IsNullOrEmpty(RussianClilocNames.NameOf(chest)));
        }
        finally
        {
            chest.Delete();
        }
    }

    [Fact]
    public void NoItem_NoName() => Assert.Null(RussianClilocNames.NameOf(null));
}

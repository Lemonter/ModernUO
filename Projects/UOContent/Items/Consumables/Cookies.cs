using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class BegCookies : Cookies
{
    [Constructible]
    public BegCookies()
    {
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1075129); // Acquired by begging
    }
}

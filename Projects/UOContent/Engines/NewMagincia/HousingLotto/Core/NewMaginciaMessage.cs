/// <summary>
///     Портировано из ServUO (Scripts/Services/New Magincia/Housing Lotto/Core/NewMaginciaMessage.cs).
///
///     Сообщение в почте Новой Маджинции: заголовок, тело, необязательные аргументы и срок жизни.
///
///     Замены:
///     <list type="bullet">
///         <item>
///             Статических <c>TextDefinition.Serialize/Deserialize</c> в этом репозитории нет, поэтому номер и
///             строка пишутся двумя полями (<see cref="TextDefinition.Number" />/<see cref="TextDefinition.String" />).
///         </item>
///         <item><c>DateTime.UtcNow</c> → <see cref="Core.Now" />.</item>
///     </list>
/// </summary>
using System;

namespace Server.Engines.NewMagincia;

public class NewMaginciaMessage
{
    public static readonly TimeSpan DefaultExpirePeriod = TimeSpan.FromDays(7);

    public NewMaginciaMessage(TextDefinition title, TextDefinition body) : this(title, body, DefaultExpirePeriod, null)
    {
    }

    public NewMaginciaMessage(TextDefinition title, TextDefinition body, string args)
        : this(title, body, DefaultExpirePeriod, args)
    {
    }

    public NewMaginciaMessage(TextDefinition title, TextDefinition body, TimeSpan expires)
        : this(title, body, expires, null)
    {
    }

    public NewMaginciaMessage(TextDefinition title, TextDefinition body, TimeSpan expires, string args)
    {
        Title = title;
        Body = body;
        Args = args;
        Expires = Core.Now + expires;
    }

    public TextDefinition Title { get; }

    public TextDefinition Body { get; }

    public string Args { get; }

    public DateTime Expires { get; private set; }

    public bool Expired => Expires < Core.Now;

    public void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // версия

        writer.Write(Title?.Number ?? 0);
        writer.Write(Title?.String);
        writer.Write(Body?.Number ?? 0);
        writer.Write(Body?.String);
        writer.Write(Expires);
        writer.Write(Args);
    }

    public static NewMaginciaMessage Deserialize(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // версия

        var titleNumber = reader.ReadInt();
        var titleString = reader.ReadString();
        var bodyNumber = reader.ReadInt();
        var bodyString = reader.ReadString();
        var expires = reader.ReadDateTime();
        var args = reader.ReadString();

        return new NewMaginciaMessage(
            TextDefinition.Of(titleNumber, titleString),
            TextDefinition.Of(bodyNumber, bodyString),
            TimeSpan.Zero,
            args
        )
        {
            Expires = expires
        };
    }
}

using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Портировано из ServUO. Остальные миски из этого файла в репозитории уже есть, здесь
///     только отсутствующие.
/// </summary>
[SerializationGenerator(0, false)]
public partial class BowlOfRotwormStew : Food
{
    [Constructible]
    public BowlOfRotwormStew() : base(0x2DBA)
    {
        Stackable = false;
        Weight = 2.0;
        FillFactor = 1;
    }

    public override int LabelNumber => 1031706; // bowl of rotworm stew
}

[SerializationGenerator(0, false)]
public partial class BowlOfBlackrockStew : Food
{
    [Constructible]
    public BowlOfBlackrockStew() : base(0x2DBA)
    {
        Stackable = false;
        Weight = 2.0;
        FillFactor = 1;

        Hue = 1954;
    }

    public override int LabelNumber => 1115752; // blackrock stew

    public override bool Eat(Mobile from)
    {
        // You don't want to eat this, it smells horrible. It looks like food for some kind of demon.
        from.SendLocalizedMessage(1115751);
        return false;
    }
}

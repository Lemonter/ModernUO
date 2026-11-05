using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Clothing/Pants.cs).
/// </summary>
#region Reward Clothing
[SerializationGenerator(0, false)]
public partial class LibraryFriendSkirt : Kilt
{
    public override int LabelNumber => 1073352; // Friends of the Library Kilt

    [Constructible]
    public LibraryFriendSkirt()
        : this(0)
    {
    }

    public LibraryFriendSkirt(int hue)
        : base(hue)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class LibraryFriendPants : LongPants
{
    public override int LabelNumber => 1073349; // Friends of the Library Pants

    [Constructible]
    public LibraryFriendPants()
        : this(0)
    {
    }

    public LibraryFriendPants(int hue)
        : base(hue)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class MalabellesDress : Skirt
{
    public override int LabelNumber => 1073251; // Malabelle's Dress - Museum of Vesper Replica

    [Constructible]
    public MalabellesDress()
        : this(0)
    {
    }

    public MalabellesDress(int hue)
        : base(hue)
    {
    }
}
#endregion


using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Decorative/CollectionsBritLibraryMisc.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class LibraryFriendLantern : Lantern
{
    [Constructible]
    public LibraryFriendLantern()
        : base()
    {
    }

    public override int LabelNumber => 1073339; // Friends of the Library Reading Lantern
}

[SerializationGenerator(0, false)]
public partial class LibraryFriendReadingChair : BigElvenChair
{
    [Constructible]
    public LibraryFriendReadingChair()
        : base()
    {
    }

    public override int LabelNumber => 1073340; // Friends of the Library Reading Chair
}

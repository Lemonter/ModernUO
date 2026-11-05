using ModernUO.Serialization;
using System;
using System.Collections.Generic;
using System.Collections;
using Server.Gumps;
using Server.Network;
using Server.Multis;
using Server.ContextMenus;

using Server.Collections;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Decorative/MapOfTheKnownWorld.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class MapOfTheKnownWorld : Item, ISecurable
{
    private SecureLevel _level;

    [CommandProperty( AccessLevel.GameMaster )]
    public SecureLevel Level
    {
        get{ return _level; }
        set{ _level = value; }
    }

    [Constructible]
    public MapOfTheKnownWorld() : base( 0x3BB6 )
    {
        Name = "карта известного мира";
        Weight = 1.0;
        LootType = LootType.Blessed;
    }

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);;

        SetSecureLevelEntry.AddTo(from, this, ref list);;
    }

    public override void OnDoubleClick( Mobile from )
    {
        if ( from.InRange( GetWorldLocation(), 2 ) )
        {
            from.CloseGump<InternalGump>();
            from.SendGump( new InternalGump() );
        }
        else
        {
            from.LocalOverheadMessage( MessageType.Regular, 0x3B2, 1019045 ); // I can't reach that.
        }
    }

    private class InternalGump : Gump
    {
        [Constructible]
        public InternalGump() : base( 50, 50 )
        {
            AddImage( 0, 0, 0x12B ); //299
        }
    }
}

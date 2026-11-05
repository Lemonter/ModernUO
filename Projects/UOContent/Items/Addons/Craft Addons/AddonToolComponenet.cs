using ModernUO.Serialization;
using System;
using System.Collections.Generic;
using Server.ContextMenus;
using Server.Engines.Craft;
using Server.Multis;
using Server.Network;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/Craft Addons/AddonToolComponenet.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class AddonToolComponent : BaseTool, IChoppable
{
    private CraftSystem _CraftSystem;
    private int _LabelNumber;
    private bool _TurnedOn;

    public override CraftSystem CraftSystem => _CraftSystem;
    public override bool BreakOnDepletion => false;
    public override bool ForceShowProperties => true;
    public override int LabelNumber => _LabelNumber;

    public virtual bool NeedsWall => false;
    public virtual int MaxUses => 5000;
    public virtual Point3D WallPosition => Point3D.Zero;

    [CommandProperty(AccessLevel.GameMaster)]
    public CraftAddon Addon { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public Point3D Offset { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int InactiveID { get; private set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int ActiveID { get; private set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int InactiveMessage { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int ActiveMessage { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool TurnedOn
    {
        get
        {
            return _TurnedOn;
        }
        set
        {
            if (_TurnedOn != value)
            {
                if (value)
                    ItemID = ActiveID;
                else
                    ItemID = InactiveID;
            }

            _TurnedOn = value;
        }
    }

    public AddonToolComponent(CraftSystem system, int inactiveid, int activeid, int cliloc, int uses, CraftAddon addon)
        : this(system, inactiveid, activeid, 0, 0, cliloc, uses, addon)
    {
    }

    public AddonToolComponent(CraftSystem system, int inactiveid, int activeid, int inactivemessage, int activemessage, int cliloc, int uses, CraftAddon addon)
        : base(0, inactiveid)
    {
        _CraftSystem = system;
        _LabelNumber = cliloc;
        Addon = addon;
        Movable = false;

        InactiveID = inactiveid;
        ActiveID = activeid;

        InactiveMessage = inactivemessage;
        ActiveMessage = activemessage;

        UsesRemaining = uses;
    }

    public void OnChop(Mobile from)
    {
        if (Addon != null && from.InRange(GetWorldLocation(), 3))
            Addon.OnChop(from);
        else
            from.SendLocalizedMessage(500446); // That is too far away.
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (Addon != null)
            Addon.OnCraftComponentUsed(from, this);
    }

    public void SetCraftSystem(CraftSystem system)
    {
        _CraftSystem = system;
    }

    public override void OnLocationChange(Point3D old)
    {
        if (Addon != null)
            Addon.Location = new Point3D(X - Offset.X, Y - Offset.Y, Z - Offset.Z);
    }

    public override void OnMapChange()
    {
        if (Addon != null)
            Addon.Map = Map;
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        if (Addon != null)
            Addon.Delete();
    }

    public override bool OnDragDrop(Mobile from, Item dropped)
    {
        BaseHouse house = BaseHouse.FindHouseAt(this);

        if (house != null && Addon != null && house.HasSecureAccess(from, Addon.Level))
        {
            if (dropped is BaseTool && !(dropped is BaseRunicTool))
            {
                var tool = dropped as BaseTool;

                if (tool.CraftSystem == _CraftSystem)
                {
                    if (UsesRemaining >= MaxUses)
                    {
                        from.SendLocalizedMessage(1155740); // Adding this to the power tool would put it over the max number of charges the tool can hold.
                    }
                    else
                    {
                        int toadd = Math.Min(tool.UsesRemaining, MaxUses - UsesRemaining);

                        UsesRemaining += toadd;
                        tool.UsesRemaining -= toadd;

                        if (tool.UsesRemaining <= 0 && !tool.Deleted)
                            tool.Delete();

                        from.SendLocalizedMessage(1155741); // Charges have been added to the power tool.

                        return true;
                    }
                }
                else
                {
                    from.SendLocalizedMessage(1074836); // The container cannot hold that type of object.
                }
            }
            else
            {
                from.SendLocalizedMessage(1074836); // The container cannot hold that type of object.
            }
        }
        else
        {
            from.SendLocalizedMessage(1074836); // The container cannot hold that type of object.
        }

        return false;
    }
}

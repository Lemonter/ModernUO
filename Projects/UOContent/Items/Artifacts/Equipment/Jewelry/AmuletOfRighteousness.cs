using ModernUO.Serialization;
using System;
using Server.Targeting;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Jewelry/AmuletOfRighteousness.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class AmuletOfRighteousness : SilverNecklace, IUsesRemaining
{
    private int _usesRemaining;

    [Constructible]
    public AmuletOfRighteousness()
        : this(100)
    {
    }

    public AmuletOfRighteousness(int uses)
        : base()
    {
        this.LootType = LootType.Blessed;
        this.Weight = 1.0;

        this._usesRemaining = uses;
    }

    public override int LabelNumber => 1075313; // Amulet of Righteousness

    public virtual bool ShowUsesRemaining
    {
        get
        {
            return true;
        }
        set
        {
        }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int UsesRemaining
    {
        get
        {
            return this._usesRemaining;
        }
        set
        {
            this._usesRemaining = value;
            this.InvalidateProperties();
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        base.OnDoubleClick(from);

        if (this.IsChildOf(from.Backpack))
        {
            from.Target = new InternalTarget(this);
        }
        else
        {
            from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
        }
    }

    private class InternalTarget : Target
    {
        private readonly AmuletOfRighteousness _amulet;

        public InternalTarget(AmuletOfRighteousness amulet)
            : base(12, false, TargetFlags.None)
        {
            this._amulet = amulet;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (this._amulet == null || this._amulet.Deleted)
            {
                return;
            }

            if (targeted is Mobile)
            {
                Mobile target = (Mobile)targeted;

                if (this._amulet.UsesRemaining <= 0)
                {
                    from.SendLocalizedMessage(1042544); // This item is out of charges.
                    return;
                }

                target.BoltEffect(0);
                this._amulet.UsesRemaining -= 1;
                this._amulet.InvalidateProperties();
            }
        }
    }
}

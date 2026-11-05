using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Gumps;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public abstract partial class ShrineHealer : BaseHealer
{
    [Constructible]
    public ShrineHealer()
    {
    }

    public override bool CheckResurrect(Mobile m) => false;

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        if (from is PlayerMobile pm)
        {
            list.Add(new LockKarmaEntry(pm));
            list.Add(new ResurrectEntry(from, this));
            list.Add(new TitheEntry(from));
        }
    }

    private class ResurrectEntry : ContextMenuEntry
    {
        private readonly Mobile _mobile;
        private readonly ShrineHealer _vendor;

        public ResurrectEntry(Mobile mobile, ShrineHealer vendor) : base(6195, 2)
        {
            _mobile = mobile;
            _vendor = vendor;

            Enabled = !_mobile.Alive;
        }

        public override void OnClick(Mobile from, IEntity target) => _vendor.OfferResurrection(_mobile);
    }

    private class LockKarmaEntry : ContextMenuEntry
    {
        private readonly PlayerMobile _mobile;

        public LockKarmaEntry(PlayerMobile mobile) : base(mobile.KarmaLocked ? 6197 : 6196, 2) => _mobile = mobile;

        public override void OnClick(Mobile from, IEntity target)
        {
            _mobile.KarmaLocked = !_mobile.KarmaLocked;

            if (_mobile.KarmaLocked)
            {
                // Your karma has been locked. Your karma can no longer be raised.
                _mobile.SendLocalizedMessage(1060192);
            }
            else
            {
                // Your karma has been unlocked. Your karma can be raised again.
                _mobile.SendLocalizedMessage(1060191);
            }
        }
    }

    private class TitheEntry : ContextMenuEntry
    {
        private readonly Mobile _mobile;

        public TitheEntry(Mobile mobile) : base(6198, 2)
        {
            _mobile = mobile;

            Enabled = _mobile.Alive;
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            if (_mobile.CheckAlive())
            {
                _mobile.SendGump(new TithingGump(_mobile));
            }
        }
    }
}

using System;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Engines.BuffIcons;
using Server.Gumps;
using Server.Mobiles;
using Server.Multis;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SpikedEggNog : Item, ISecurable
{
    private Mobile _bleedVictim;
    private int _bleedCount;
    private TimerExecutionToken _bleedToken;

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _nextUseTime;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private SecureLevel _level;

    [Constructible]
    public SpikedEggNog() : base(0x3BBA) => Hue = 2711;

    public override int LabelNumber => 1157647; // Spiked Egg Nog

    public bool CheckAccessible(Mobile from, Item item)
    {
        if (from.AccessLevel >= AccessLevel.GameMaster)
        {
            return true; // Staff can access anything
        }

        var house = BaseHouse.FindHouseAt(item);

        if (house == null)
        {
            return false;
        }

        return Level switch
        {
            SecureLevel.Owner    => house.IsOwner(from),
            SecureLevel.CoOwners => house.IsCoOwner(from),
            SecureLevel.Friends  => house.IsFriend(from),
            SecureLevel.Anyone   => true,
            SecureLevel.Guild    => house.IsGuildMember(from),
            _                    => false
        };
    }

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        SetSecureLevelEntry.AddTo(from, this, ref list);
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(GetWorldLocation(), 2))
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
        }
        else if (!IsLockedDown)
        {
            from.SendLocalizedMessage(1112573); // This must be locked down or secured in order to use it.
        }
        else if (CheckAccessible(from, this))
        {
            BeginBleed(from);
        }
    }

    public void BeginBleed(Mobile m)
    {
        m.LocalOverheadMessage(MessageType.Regular, 0x21, 1010571); // Ouch!

        if (Core.Now < _nextUseTime)
        {
            m.SendLocalizedMessage(1078497); // You cannot use that right now
            return;
        }

        if (m is PlayerMobile pm)
        {
            pm.AddBuff(
                new BuffInfo(BuffIcon.Bleed, 1075829, 1075830, TimeSpan.FromSeconds(12), $"{"1"}\t{"10"}\t{"2"}")
            );
        }

        _bleedVictim = m;
        _bleedCount = 0;

        _bleedToken.Cancel();
        Timer.StartTimer(TimeSpan.FromSeconds(2.0), TimeSpan.FromSeconds(2.0), OnBleedTick, out _bleedToken);

        _nextUseTime = Core.Now + TimeSpan.FromMinutes(1);

        m.LocalOverheadMessage(MessageType.Regular, 0x21, 1060757); // You are bleeding profusely

        m.PlaySound(555);
        m.FixedParticles(0x119A, 244, 25, 9950, 31, 0, EffectLayer.Waist);
    }

    public void EndBleed(Mobile m)
    {
        _bleedToken.Cancel();

        if (m is PlayerMobile pm)
        {
            pm.RemoveBuff(BuffIcon.Bleed);
        }

        m.SendLocalizedMessage(1060167); // The bleeding wounds have healed, you are no longer bleeding!
    }

    public void DoBleed(Mobile m)
    {
        if (!m.Alive)
        {
            EndBleed(m);
            return;
        }

        m.PlaySound(0x133);
        AOS.Damage(m, Utility.Random(1, 10), false, 0, 0, 0, 0, 100);

        var blood = new Blood
        {
            ItemID = Utility.Random(0x122A, 5)
        };

        blood.MoveToWorld(m.Location, m.Map);
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        _bleedToken.Cancel();
        _bleedVictim = null;
    }

    private void OnBleedTick()
    {
        var victim = _bleedVictim;

        if (victim == null || Deleted)
        {
            _bleedToken.Cancel();
            return;
        }

        if (!victim.Alive || victim.Deleted)
        {
            EndBleed(victim);
            return;
        }

        DoBleed(victim);

        if (++_bleedCount == 6)
        {
            EndBleed(victim);
        }
    }
}

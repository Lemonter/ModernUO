using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Gumps;
using Server.Multis;
using Server.Network;

namespace Server.Items;

/// <summary>
///     Вспомогательный класс SecretChestArray не сохранён как отдельный тип: список доступа
///     хранится четырьмя параллельными списками, которые умеет писать генератор сериализации.
/// </summary>
[Flippable(0x9707, 0x9706)]
[SerializationGenerator(0, false)]
public partial class SecretChest : LockableContainer
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Mobile _lockingPerson;

    [SerializableField(1)]
    private List<Mobile> _accessMobiles;

    [SerializableField(2)]
    private List<bool> _accessPermissions;

    [SerializableField(3)]
    private List<int> _accessTrials;

    [SerializableField(4)]
    private List<DateTime> _accessExpire;

    [SerializableField(5)]
    private int[] _secretKey;

    [Constructible]
    public SecretChest(int id = 0x9707) : base(id)
    {
        Weight = 5;
        _secretKey = [0, 0, 0, 0, 0];
        _accessMobiles = [];
        _accessPermissions = [];
        _accessTrials = [];
        _accessExpire = [];
    }

    public override int LabelNumber => 1151583; // Secret Chest

    public override int DefaultGumpID => 0x58E;

    public override bool DisplaysContent => false;

    public int IndexOf(Mobile m) => _accessMobiles?.IndexOf(m) ?? -1;

    public bool CheckPermission(Mobile from)
    {
        var index = IndexOf(from);

        return Locked && _lockingPerson?.Account != from.Account && index >= 0 && !_accessPermissions[index];
    }

    public override void Open(Mobile from)
    {
        if (!Locked || from.AccessLevel >= AccessLevel.GameMaster || _lockingPerson?.Account == from.Account)
        {
            DisplayTo(from);
            return;
        }

        var index = IndexOf(from);

        if (index < 0)
        {
            _accessMobiles.Add(from);
            _accessPermissions.Add(false);
            _accessTrials.Add(3);
            _accessExpire.Add(default);
            index = _accessMobiles.Count - 1;
        }

        if (_accessPermissions[index])
        {
            // You remember the key number of this chest, so you can open this now.
            from.SendLocalizedMessage(1151600);
            DisplayTo(from);
            return;
        }

        if (_accessTrials[index] == 0 && _accessExpire[index] < Core.Now)
        {
            _accessTrials[index] = 3;
        }

        if (_accessTrials[index] > 0)
        {
            from.SendLocalizedMessage(501747); // It appears to be locked.
            from.SendLocalizedMessage(1151527); // Enter the key number to open.
            from.SendLocalizedMessage(1152346, $"{_accessTrials[index]}"); // Number of tries left: ~1_times~
            from.SendGump(new SecretChestGump(this, false));
        }
        else
        {
            // You cannot access the chest now, having exhausted your number of attempts.
            from.SendLocalizedMessage(1151599);
        }
    }

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        if (_lockingPerson == null || _lockingPerson.Account == from.Account)
        {
            list.Add(new SetEditKeyNumber(this, from));
            list.Add(new ResetKeyNumber(this, from));
        }
    }

    private class SetEditKeyNumber : ContextMenuEntry
    {
        private readonly SecretChest _chest;
        private readonly Mobile _mobile;

        public SetEditKeyNumber(SecretChest c, Mobile m) : base(1151608, -1) // Set/Edit Key Number
        {
            _mobile = m;
            _chest = c;

            if (!c.IsChildOf(m.Backpack))
            {
                Flags |= CMEFlags.Disabled;
            }
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            if (_mobile == null || _chest == null)
            {
                return;
            }

            if (_chest.Locked)
            {
                // Edit your key number...
                _mobile.SendLocalizedMessage(1151588);
            }
            else
            {
                // Set your key number...
                _mobile.SendLocalizedMessage(1151525);
            }

            _mobile.SendGump(new SecretChestGump(_chest, true));
        }
    }

    private class ResetKeyNumber : ContextMenuEntry
    {
        private readonly SecretChest _chest;
        private readonly Mobile _mobile;

        public ResetKeyNumber(SecretChest c, Mobile m) : base(1151609, -1) // Reset Key Number
        {
            _mobile = m;
            _chest = c;

            if (!c.IsChildOf(m.Backpack) || !c.Locked)
            {
                Flags |= CMEFlags.Disabled;
            }
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            if (_mobile == null || _chest == null)
            {
                return;
            }

            _chest.Locked = false;
            _chest.LockingPerson = null;
            _chest.AccessMobiles.Clear();
            _chest.AccessPermissions.Clear();
            _chest.AccessTrials.Clear();
            _chest.AccessExpire.Clear();

            // You have reset the number key of this chest. This is unlocked now.
            _mobile.SendLocalizedMessage(1151598);
        }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (Locked)
        {
            list.Add(1151610); // Locked
        }

        // Contents: ~1_COUNT~/~2_MAXCOUNT~ items, ~3_WEIGHT~/~4_MAXWEIGHT~ stones
        list.Add(1072241, $"{TotalItems}\t{MaxItems}\t{TotalWeight}\t{MaxWeight}");
    }

    public override bool TryDropItem(Mobile from, Item dropped, bool sendFullMessage)
    {
        if (CheckPermission(from))
        {
            from.SendLocalizedMessage(1151591); // You cannot place items into a locked chest!
            return false;
        }

        if (!CheckHold(from, dropped, true, true))
        {
            return false;
        }

        var house = BaseHouse.FindHouseAt(this);

        if (house != null && IsLockedDown && !house.CheckAccessibility(this, from))
        {
            // You are not allowed to access this!
            PublicOverheadMessage(MessageType.Regular, 0x21, 1061637);
            from.SendLocalizedMessage(501727); // You cannot lock that down!
            return false;
        }

        DropItem(dropped);

        return true;
    }

    public override bool IsAccessibleTo(Mobile m) => true;

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        _accessMobiles ??= [];
        _accessPermissions ??= [];
        _accessTrials ??= [];
        _accessExpire ??= [];
        _secretKey ??= [0, 0, 0, 0, 0];
    }
}

public class SecretChestGump : DynamicGump
{
    private readonly SecretChest _chest;
    private readonly int[] _tempSecretKey;
    private readonly bool _setEdit;

    public SecretChestGump(SecretChest chest, bool setEdit) : this(chest, null, setEdit)
    {
    }

    public SecretChestGump(SecretChest chest, int[] secretKey, bool setEdit) : base(0, 0)
    {
        _chest = chest;
        _setEdit = setEdit;

        if (setEdit)
        {
            _tempSecretKey = chest.SecretKey;
        }
        else
        {
            _tempSecretKey = secretKey ?? [0, 0, 0, 0, 0];
        }
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();

        builder.AddImage(50, 50, 0x58D);

        builder.AddButton(133, 270, 0x81A, 0x81B, _setEdit ? 2 : 1); // OKAY
        builder.AddButton(320, 270, 0x819, 0x818, 0);                // CANCEL

        for (var i = 0; i < _tempSecretKey.Length; ++i)
        {
            builder.AddButton(192 + 24 * i, 190, 0x58C, 0x58C, 10 + i * 2);
            builder.AddImage(190 + 24 * i, 200, 0x58F + _tempSecretKey[i]);
            builder.AddButton(192 + 24 * i, 230, 0x599, 0x599, 11 + i * 2);
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;

        if (_chest == null || _chest.Deleted)
        {
            return;
        }

        switch (info.ButtonID)
        {
            case 0:
                from.SendLocalizedMessage(1042021); // Cancelled.
                break;
            case 1:
                TryOpen(from);
                break;
            case 2:
                SetKey(from);
                break;
            default:
                ChangeDigit(info.ButtonID, from);
                break;
        }
    }

    private void TryOpen(Mobile from)
    {
        var index = _chest.IndexOf(from);

        if (index < 0)
        {
            return;
        }

        if (_chest.SecretKey.AsSpan().SequenceEqual(_tempSecretKey))
        {
            // You succeed at entering the correct key number; you may open the chest now.
            from.SendLocalizedMessage(1151589);
            _chest.AccessPermissions[index] = true;
            _chest.DisplayTo(from);
            return;
        }

        var trials = _chest.AccessTrials[index] - 1;
        _chest.AccessTrials[index] = trials;

        if (trials > 0)
        {
            // The number which you have entered is wrong...
            from.SendLocalizedMessage(1151590);
            from.SendLocalizedMessage(1152346, $"{trials}"); // Number of tries left: ~1_times~
            Timer.StartTimer(TimeSpan.FromSeconds(0.2), () => from.SendGump(new SecretChestGump(_chest, _tempSecretKey, _setEdit)));
        }
        else
        {
            _chest.AccessExpire[index] = Core.Now + TimeSpan.FromDays(1);
            // You have exhausted your unlock attempts for this chest...
            from.SendLocalizedMessage(1151611);
        }
    }

    private void SetKey(Mobile from)
    {
        for (var i = 0; i < _chest.SecretKey.Length; i++)
        {
            _chest.SecretKey[i] = _tempSecretKey[i];
        }

        _chest.Locked = true;
        _chest.LockingPerson = from;
        _chest.AccessMobiles.Clear();
        _chest.AccessPermissions.Clear();
        _chest.AccessTrials.Clear();
        _chest.AccessExpire.Clear();

        from.SendLocalizedMessage(1151528); // The key numbers have been set; this chest is now locked.
    }

    private void ChangeDigit(int buttonID, Mobile from)
    {
        var index = (buttonID - 10) / 2;

        if (index < 0 || index >= _tempSecretKey.Length)
        {
            return;
        }

        if ((buttonID - 10) % 2 == 0)
        {
            _tempSecretKey[index] = _tempSecretKey[index] == 9 ? 0 : _tempSecretKey[index] + 1;
        }
        else
        {
            _tempSecretKey[index] = _tempSecretKey[index] == 0 ? 9 : _tempSecretKey[index] - 1;
        }

        Timer.StartTimer(TimeSpan.FromSeconds(0.2), () => from.SendGump(new SecretChestGump(_chest, _tempSecretKey, _setEdit)));
    }
}

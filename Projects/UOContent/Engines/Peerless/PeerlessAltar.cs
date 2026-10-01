using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Engines.PartySystem;
using Server.Gumps;
using Server.Mobiles;

namespace Server.Items;

/// <summary>Ported from ServUO (Scripts/Services/Peerless/PeerlessAltar.cs). The altar owns a
/// peerless encounter end to end: it takes the offering of keys, hands back master keys,
/// spawns the boss, teleports the party in, runs the clock, and sweeps the room afterwards.
///
/// Subclasses supply the five things that differ per dungeon — which keys, how many master
/// keys, which boss, where the arena is, and what a master key for this altar looks like.</summary>
[SerializationGenerator(0, false)]
public abstract partial class PeerlessAltar : Container
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private BasePeerless _peerless;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Mobile _owner;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Point3D _bossLocation;

    [SerializableField(3)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Point3D _teleportDest;

    [SerializableField(4)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Point3D _exitDest;

    [SerializableField(5)]
    private DateTime _deadline;

    [Tidy]
    [SerializableField(6)]
    private List<Mobile> _fighters;

    [Tidy]
    [SerializableField(7)]
    private List<Item> _masterKeys;

    [Tidy]
    [SerializableField(8)]
    private List<BaseCreature> _helpers;

    private TimerExecutionToken _slayToken;
    private TimerExecutionToken _deadlineToken;
    private TimerExecutionToken _keyResetToken;

    private List<KeySlot> _keyValidation;

    public PeerlessAltar(int itemID) : base(itemID)
    {
        Movable = false;

        _fighters = new List<Mobile>();
        _masterKeys = new List<Item>();
        _helpers = new List<BaseCreature>();
    }

    public abstract int KeyCount { get; }
    public abstract Type[] Keys { get; }
    public abstract BasePeerless Boss { get; }
    public abstract Rectangle2D[] BossBounds { get; }
    public abstract MasterKey MasterKey { get; }

    public virtual TimeSpan TimeToSlay => TimeSpan.FromMinutes(90);
    public virtual TimeSpan DelayAfterBossSlain => TimeSpan.FromMinutes(15);

    public virtual bool CanEnter(Mobile m) => true;

    #region Keys

    public override bool OnDragDrop(Mobile from, Item dropped)
    {
        if (!IsKey(dropped))
        {
            from.SendLocalizedMessage(1072682); // This is not the proper key.
            return false;
        }

        if (_owner != null && _owner != from)
        {
            // ~1_NAME~ has already activated the Prism, please wait...
            from.SendLocalizedMessage(1072683, _owner.Name);
            return false;
        }

        if (_peerless != null)
        {
            // The master of this realm has already been summoned and is engaged in combat.
            from.SendLocalizedMessage(1075213);
            return false;
        }

        if (!MarkKey(dropped))
        {
            from.SendLocalizedMessage(1072682); // This is not the proper key.
            return false;
        }

        if (_owner == null)
        {
            Owner = from;
            // The offering resets if it isn't finished in time; 30 seconds a key.
            Timer.StartTimer(TimeSpan.FromSeconds(30 * Keys.Length), ResetOffering, out _keyResetToken);
        }

        if (!base.OnDragDrop(from, dropped))
        {
            return false;
        }

        from.SendLocalizedMessage(1074575); // You have activated this object!

        if (KeysValidated())
        {
            ActivateEncounter(from);
        }

        return true;
    }

    public bool IsKey(Item item)
    {
        if (item == null)
        {
            return false;
        }

        var type = item.GetType();

        foreach (var key in Keys)
        {
            if (key.IsAssignableFrom(type))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Ticks the dropped key's slot. Refuses a type that has already been offered, so
    /// five of the same key don't stand in for five different ones.</summary>
    private bool MarkKey(Item item)
    {
        _keyValidation ??= BuildValidation();

        var type = item.GetType();

        for (var i = 0; i < _keyValidation.Count; i++)
        {
            var slot = _keyValidation[i];

            if (!slot.Type.IsAssignableFrom(type))
            {
                continue;
            }

            if (slot.Active)
            {
                return false;
            }

            _keyValidation[i] = slot with { Active = true };
            return true;
        }

        return false;
    }

    private List<KeySlot> BuildValidation()
    {
        var list = new List<KeySlot>(Keys.Length);

        foreach (var key in Keys)
        {
            list.Add(new KeySlot(key, false));
        }

        return list;
    }

    private bool KeysValidated()
    {
        if (_keyValidation == null)
        {
            return false;
        }

        foreach (var slot in _keyValidation)
        {
            if (!slot.Active)
            {
                return false;
            }
        }

        return true;
    }

    private void ResetOffering()
    {
        _keyResetToken.Cancel();
        _keyValidation = null;

        if (_owner != null)
        {
            // Your realm offering has reset. You will need to start over.
            _owner.SendLocalizedMessage(1072679);
        }

        Owner = null;

        ClearContainer();
    }

    protected virtual void ClearContainer()
    {
        for (var i = Items.Count - 1; i >= 0; i--)
        {
            Items[i].Delete();
        }
    }

    #endregion

    #region Encounter

    public virtual void ActivateEncounter(Mobile from)
    {
        _keyResetToken.Cancel();
        _keyValidation = null;

        BeginSequence();

        for (var i = 0; i < KeyCount; i++)
        {
            var key = MasterKey;

            if (key == null)
            {
                continue;
            }

            key.Altar = this;
            key.PeerlessMap = Map;

            AddToMasterKeys(key);

            if (!from.PlaceInBackpack(key))
            {
                key.MoveToWorld(from.Location, from.Map);
            }
        }

        from.SendLocalizedMessage(1072680); // You have been given the key to the boss.

        Timer.DelayCall(TimeSpan.FromSeconds(1), ClearContainer);

        StartSlayTimer();
    }

    public virtual void BeginSequence() => SpawnBoss();

    public virtual void SpawnBoss()
    {
        if (_peerless?.Deleted == false)
        {
            return;
        }

        var boss = Boss;

        if (boss == null)
        {
            return;
        }

        boss.Altar = this;
        boss.Home = _bossLocation;
        boss.RangeHome = 12;
        boss.MoveToWorld(_bossLocation, Map);

        Peerless = boss;
    }

    public virtual void StartSlayTimer()
    {
        _slayToken.Cancel();

        Deadline = Core.Now + TimeToSlay;

        Timer.StartTimer(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5), DeadlineCheck, out _slayToken);
    }

    /// <summary>Runs every five minutes: ends the encounter once the deadline passes, warns
    /// inside the last half hour, and drops anyone who logged out ten minutes ago.</summary>
    public virtual void DeadlineCheck()
    {
        if (Core.Now > _deadline)
        {
            SendMessageToFighters(1072258); // You failed to complete an objective in time!
            FinishSequence();
            return;
        }

        var remaining = _deadline - Core.Now;

        if (remaining < TimeSpan.FromMinutes(30))
        {
            foreach (var fighter in _fighters)
            {
                // ~1_val~ seconds remain.
                fighter.SendLocalizedMessage(1075611, ((int)remaining.TotalSeconds).ToString());
            }
        }

        for (var i = _fighters.Count - 1; i >= 0; i--)
        {
            var fighter = _fighters[i];

            // LastOnline lives on PlayerMobile here rather than on Mobile.
            if (fighter.NetState == null && fighter is PlayerMobile pm &&
                Core.Now - pm.LastOnline > TimeSpan.FromMinutes(10))
            {
                Exit(fighter);
            }
        }
    }

    public virtual void OnPeerlessDeath()
    {
        // The master of this realm has been slain! You may only stay here so long.
        SendMessageToFighters(1072681);

        _slayToken.Cancel();

        DeleteMasterKeys();

        _deadlineToken.Cancel();
        Timer.StartTimer(DelayAfterBossSlain, FinishSequence, out _deadlineToken);
    }

    public virtual void FinishSequence()
    {
        _slayToken.Cancel();
        _deadlineToken.Cancel();
        _keyResetToken.Cancel();

        if (_peerless?.Deleted == false)
        {
            _peerless.Delete();
        }

        Peerless = null;

        for (var i = _fighters.Count - 1; i >= 0; i--)
        {
            Exit(_fighters[i]);
        }

        DeleteMasterKeys();
        CleanupHelpers();

        ClearFighters();
        ClearMasterKeys();

        Deadline = DateTime.MinValue;
        Owner = null;
        _keyValidation = null;
    }

    private void DeleteMasterKeys()
    {
        for (var i = _masterKeys.Count - 1; i >= 0; i--)
        {
            _masterKeys[i]?.Delete();
        }

        ClearMasterKeys();
    }

    private void SendMessageToFighters(int cliloc)
    {
        foreach (var fighter in _fighters)
        {
            fighter.SendLocalizedMessage(cliloc);
        }
    }

    #endregion

    #region Movement

    /// <summary>Puts the confirmation to everyone in range who might come along, so nobody is
    /// yanked into a boss room without being asked.</summary>
    public virtual void SendConfirmations(Mobile from)
    {
        var party = Party.Get(from);

        if (party == null)
        {
            Enter(from);
            return;
        }

        foreach (var info in party.Members)
        {
            var member = info.Mobile;

            if (member == from)
            {
                Enter(from);
            }
            else if (member.InRange(from.Location, 25) && member.Map == from.Map)
            {
                ConfirmEntranceGump.DisplayTo(member, this);
            }
        }
    }

    public virtual void Enter(Mobile m)
    {
        if (m?.Deleted != false || !CanEnter(m))
        {
            return;
        }

        var map = Map;

        if (map == null)
        {
            return;
        }

        // Pets come too, as long as they're alive and not being ridden.
        foreach (var pet in m.Map.GetMobilesInRange<BaseCreature>(m.Location, 5))
        {
            if (pet.Controlled && pet.ControlMaster == m && pet.Alive && !pet.IsDeadPet &&
                pet.Body.IsAnimal && pet is not IMount { Rider: not null })
            {
                Teleport(pet, _teleportDest, map);
            }
        }

        Teleport(m, _teleportDest, map);

        AddToFighters(m);
    }

    public virtual void Exit(Mobile m)
    {
        if (m?.Deleted != false)
        {
            return;
        }

        if (m.NetState == null)
        {
            // Offline: move where they'll reappear rather than where they are.
            if (InBossArea(m.LogoutLocation))
            {
                m.LogoutLocation = _exitDest;
                m.LogoutMap = ExitMap;
            }
        }
        else if (InBossArea(m.Location))
        {
            if (m.Mount is Mobile mount)
            {
                Teleport(mount, _exitDest, ExitMap);
            }

            Teleport(m, _exitDest, ExitMap);
            m.SendLocalizedMessage(1072677); // You have been transported out of this room.
        }

        RemoveFromFighters(m);

        // Once the room is empty and no keys are outstanding, reset so the next party can go.
        if (_fighters.Count == 0 && _masterKeys.Count == 0)
        {
            FinishSequence();
        }
    }

    /// <summary>The facet players are returned to. All current altars send players back onto
    /// the map the altar itself sits on; The Citadel is the exception — its arena is on Malas
    /// but its entrance, and so its exit, is Tokuno.</summary>
    public virtual Map ExitMap => Map;

    private static void Teleport(Mobile m, Point3D location, Map map)
    {
        m.MoveToWorld(location, map);
        m.FixedParticles(0x376A, 9, 32, 5030, EffectLayer.Waist);
        m.PlaySound(0x1FE);
    }

    public bool InBossArea(Point3D p)
    {
        foreach (var bounds in BossBounds)
        {
            if (bounds.Contains(p))
            {
                return true;
            }
        }

        return false;
    }

    #endregion

    #region Helpers

    public void AddHelper(BaseCreature helper)
    {
        if (helper?.Deleted == false && helper.Alive)
        {
            AddToHelpers(helper);
        }
    }

    public bool AllHelpersDead()
    {
        foreach (var helper in _helpers)
        {
            if (helper?.Deleted == false && helper.Alive)
            {
                return false;
            }
        }

        return true;
    }

    public void CleanupHelpers()
    {
        for (var i = _helpers.Count - 1; i >= 0; i--)
        {
            var helper = _helpers[i];

            if (helper?.Deleted == false && helper.Alive)
            {
                helper.Delete();
            }
        }

        ClearHelpers();
    }

    #endregion

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        _slayToken.Cancel();
        _deadlineToken.Cancel();
        _keyResetToken.Cancel();

        _peerless = null;
        _owner = null;
        _keyValidation = null;
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        _fighters ??= new List<Mobile>();
        _masterKeys ??= new List<Item>();
        _helpers ??= new List<BaseCreature>();

        // A save can land mid-encounter, and nothing that drives one survives the restart —
        // the timers are gone and the deadline is meaningless. Sweep and let the room reopen.
        if (_peerless != null || _fighters.Count > 0 || _masterKeys.Count > 0)
        {
            Timer.DelayCall(FinishSequence);
        }
    }

    private record struct KeySlot(Type Type, bool Active);
}

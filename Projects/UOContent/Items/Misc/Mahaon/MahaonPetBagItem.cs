using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items;

/// <summary>
///     A free "shrink" for small pets (see BaseCreature.GetContextMenuEntries — the "В
///     мешок" entry, gated to ControlSlots &lt;= 1) — same end result as a real Shrink
///     Potion (the live creature is pulled off the map, this item carries it around in the
///     owner's pack, double-click to bring it back), just without needing an actual potion
///     for something small enough to plausibly fit in a bag. Reuses ShrinkTable for the
///     icon, same as the vanilla shrink item would.
/// </summary>
[SerializationGenerator(0, false)]
public partial class MahaonPetBagItem : Item
{
    /// <summary>
    ///     Кто сейчас сидит в мешке. Ссылка на зверя живёт только в самом предмете, поэтому
    ///     всем остальным системам (в первую очередь конюшне при выходе и входе в игру)
    ///     узнать об этом больше неоткуда. Проверка «а он не на карте ли» оказалась
    ///     недостаточной: в старых сохранениях питомец успел попасть в AutoStabled ещё до
    ///     того, как конюшня научилась пропускать мешки, и вход в игру честно вытаскивал
    ///     его в мир — при том что мешок с ним же оставался в рюкзаке.
    /// </summary>
    private static readonly HashSet<BaseCreature> _bagged = new();

    /// <summary>Сидит ли зверь в мешке. Для конюшни: такого не запирать и не выпускать.</summary>
    public static bool IsBagged(Mobile m) => m is BaseCreature bc && _bagged.Contains(bc);

    [SerializableField(0)]
    private BaseCreature _creature;

    public MahaonPetBagItem(BaseCreature creature) : base(ShrinkTable.Lookup(creature))
    {
        _creature = creature;
        _bagged.Add(creature);
        Hue = creature.Hue & 0x0FFF;
        Movable = true;
        Name = creature.Name;

        creature.ControlTarget = null;
        creature.Internalize();
    }

    /// <summary>
    ///     Мир загружается раньше, чем кто-либо успевает войти в игру, поэтому здесь же и
    ///     чиним последствия старых сохранений: возвращаем зверя обратно «в мешок» и
    ///     вычёркиваем его из конюшни, чтобы вход в игру его оттуда не выпустил.
    /// </summary>
    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (_creature?.Deleted != false)
        {
            Delete();
            return;
        }

        _bagged.Add(_creature);

        if (_creature.IsStabled || _creature.StabledBy != null)
        {
            _creature.IsStabled = false;

            if (_creature.StabledBy is PlayerMobile stabler)
            {
                stabler.Stabled?.Remove(_creature);
                stabler.AutoStabled?.Remove(_creature);
            }

            _creature.StabledBy = null;
        }

        if (_creature.ControlMaster is PlayerMobile owner)
        {
            owner.Stabled?.Remove(_creature);
            owner.AutoStabled?.Remove(_creature);
        }

        if (_creature.Map != Map.Internal)
        {
            _creature.Internalize();
        }
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        if (_creature != null)
        {
            _bagged.Remove(_creature);
        }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_creature?.Deleted == false)
        {
            list.Add(1041603); // This item represents a pet currently in consideration for trade
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (_creature?.Deleted != false)
        {
            from.SendMessage(0x22, "Этот питомец больше не существует.");
            Delete();
            return;
        }

        if (_creature.ControlMaster != from)
        {
            from.SendMessage(0x22, "Это не твой питомец.");
            return;
        }

        // Repair path for bags saved before AutoStablePets learned to leave bagged pets
        // alone: those pets were dragged back into the world on login while this item
        // stayed in the pack. If the creature is already out there, the bag is a leftover.
        if (_creature.Map != Map.Internal)
        {
            from.SendMessage(0x59, $"{_creature.Name} и так уже рядом с тобой.");
            Delete();
            return;
        }

        if (!IsChildOf(from.Backpack) && RootParent != from)
        {
            from.SendMessage(0x22, "Достань его из рюкзака, чтобы выпустить питомца.");
            return;
        }

        if (from.Followers + _creature.ControlSlots > from.FollowersMax)
        {
            from.SendLocalizedMessage(1049607); // You have too many followers to control that creature.
            return;
        }

        _creature.MoveToWorld(from.Location, from.Map);
        _creature.SetControlMaster(from);
        from.SendMessage(0x59, $"{_creature.Name} снова рядом с тобой.");

        Delete();
    }
}

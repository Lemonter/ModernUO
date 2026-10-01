using System.Collections.Generic;
using ModernUO.Serialization;

namespace Server.Items;

// Mahaon: FloorIndex добавлен (версия 0 -> 1) для поддержки многоэтажных данжей — боты
// умеют идти только по прямой видимой местности (A*/StepTowardPath), телепортеры между
// этажами для них "невидимы" сами по себе, поэтому GM размечает КАЖДУЮ точку перехода
// (обычно прямо на тайле телепортера) отдельным маркером с номером этажа: FloorIndex=1 —
// точка перехода С этажа 1 НА этаж 2, FloorIndex=2 — с этажа 2 на этаж 3, и т.д. Бот
// приходит на маркер и обычная механика телепортера (Teleporter.OnMoveOver) срабатывает
// сама, без специального кода — см. TryAdvanceDungeonFloor в BotHuntingDungeon.cs.
// FloorIndex=0 (значение по умолчанию, в том числе у всех маркеров, размещённых ДО этого
// изменения) означает "обычный, не привязанный к этажу" маркер — старое поведение
// (Find по имени, без учёта этажа) продолжает работать как раньше и не ломается этим
// добавлением.
[SerializationGenerator(1, false)]
public partial class MahaonDungeonMarker : Item
{
    [SerializableField(0)]
    private string _dungeonName;

    [SerializableField(1)]
    private int _floorIndex;

    private static readonly List<MahaonDungeonMarker> AllMarkers = new();

    [Constructible]
    public MahaonDungeonMarker(string dungeonName = "Despise", int floorIndex = 0) : base(0x1F14)
    {
        Movable = false;
        Visible = false; // invisible to regular players — GMs still see it via [ItemID etc
        _dungeonName = dungeonName;
        _floorIndex = floorIndex;
        Name = $"маркер данжа: {dungeonName}";

        AllMarkers.Add(this);
    }

    // Явный хук миграции обязателен здесь — ModernUO Serialization Generator требует его
    // при ЛЮБОМ повышении версии (проверено: сгенерированный Deserialize вызывает
    // Deserialize(reader, version), когда version < SerializationVersion, вне зависимости
    // от того, что новое поле добавлено просто как [SerializableField]). Старые данные
    // (версия 0) содержат только _dungeonName — _floorIndex у них по умолчанию 0
    // ("обычный, не привязанный к этажу маркер"), что и есть корректное поведение.
    private void Deserialize(IGenericReader reader, int version)
    {
        _dungeonName = reader.ReadString();
        _floorIndex = 0;
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        AllMarkers.Add(this);
    }

    public override void OnDelete()
    {
        AllMarkers.Remove(this);
        base.OnDelete();
    }

    /// <summary>Finds a placed marker matching this dungeon name (case-insensitive),
    /// regardless of floor — the original single-marker-per-dungeon lookup, still used for
    /// the full-party fast-teleport shortcut.</summary>
    public static MahaonDungeonMarker Find(string dungeonName)
    {
        foreach (var marker in AllMarkers)
        {
            if (!marker.Deleted && string.Equals(marker._dungeonName, dungeonName, System.StringComparison.OrdinalIgnoreCase))
            {
                return marker;
            }
        }

        return null;
    }

    /// <summary>Finds the marker for a specific floor-transition point of this dungeon
    /// (see the FloorIndex convention above) — null if a GM hasn't placed one for that
    /// floor (or at all), which is the normal, unbroken state for every dungeon that
    /// hasn't been given multi-floor markers yet.</summary>
    public static MahaonDungeonMarker FindByFloor(string dungeonName, int floorIndex)
    {
        foreach (var marker in AllMarkers)
        {
            if (!marker.Deleted && marker._floorIndex == floorIndex &&
                string.Equals(marker._dungeonName, dungeonName, System.StringComparison.OrdinalIgnoreCase))
            {
                return marker;
            }
        }

        return null;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from.AccessLevel < AccessLevel.GameMaster)
        {
            return;
        }

        from.SendMessage(0x59, $"Маркер данжа: {_dungeonName} (этаж {_floorIndex}) — ({X}, {Y}, {Z} — {Map})");
    }
}

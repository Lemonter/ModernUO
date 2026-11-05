using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Server.Items;

namespace Server.Regions;

/// <summary>
///     Портировано из ServUO (Services/Tomb of Kings/ToKBridgeRegion.cs) — мост в Гробнице Королей:
///     каменные блоки невидимы, пока рядом никого нет, и «проявляются» волной оттенков, когда игрок
///     подходит. Оригинал собирал блоки из области региона и крутил их оттенок таймером; здесь то же
///     самое, но таймером с токеном, который отменяется при выгрузке региона.
/// </summary>
public class ToKBridgeRegion : BaseRegion
{
    private static readonly int[] Hues =
    {
        0,
        0x807, 0x806, 0x805, 0x804, 0x803,
        0x7E3, 0x7E4, 0x7E5, 0x7E6, 0x7E7,
        0x7E8, 0x7E9, 0x3E3, 0x3E4, 0x380,
        0
    };

    private List<Item> _blocks;
    private TimerExecutionToken _fadeToken;
    private int _huePointer;

    [JsonConstructor] // Don't include parent, since it is special
    public ToKBridgeRegion(string name, Map map, int priority, params Rectangle3D[] area) : base(name, map, priority, area)
    {
        Timer.DelayCall(TimeSpan.Zero, Ensure);
    }

    public ToKBridgeRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area)
        : base(name, map, parent, priority, area)
    {
        Timer.DelayCall(TimeSpan.Zero, Ensure);
    }

    public ToKBridgeRegion(string name, Map map, Region parent, params Rectangle3D[] area) : base(name, map, parent, area)
    {
        Timer.DelayCall(TimeSpan.Zero, Ensure);
    }

    private void Ensure()
    {
        if (Map == null)
        {
            return;
        }

        _blocks = new List<Item>();

        foreach (var rect in Area)
        {
            var bounds = new Rectangle2D(
                new Point2D(rect.Start.X, rect.Start.Y),
                new Point2D(rect.End.X, rect.End.Y)
            );

            foreach (var item in Map.GetItemsInBounds(bounds))
            {
                if (item is Static)
                {
                    _blocks.Add(item);
                }
            }
        }

        if (_blocks.Count == 0)
        {
            _blocks = null;
            return;
        }

        foreach (var block in _blocks)
        {
            block.Hue = 0x807;
            block.Visible = false;
        }

        _huePointer = 0;

        _fadeToken.Cancel();
        Timer.StartTimer(TimeSpan.Zero, TimeSpan.FromSeconds(0.33), Tick, out _fadeToken);
    }

    private void Tick()
    {
        if (_blocks == null)
        {
            _fadeToken.Cancel();
            return;
        }

        _huePointer += GetPlayerCount() > 0 ? 1 : -1;
        _huePointer = Math.Clamp(_huePointer, 0, Hues.Length - 1);

        foreach (var block in _blocks)
        {
            block.Hue = Hues[_huePointer];
        }

        if (_huePointer == 0)
        {
            foreach (var block in _blocks)
            {
                block.Visible = false;
            }

            _fadeToken.Cancel();
        }
        else if (_huePointer == 1)
        {
            foreach (var block in _blocks)
            {
                block.Visible = true;
            }
        }
        else if (_huePointer == Hues.Length - 1)
        {
            _fadeToken.Cancel();
        }
    }

    private void WakeUp()
    {
        if (_blocks == null)
        {
            Ensure();
            return;
        }

        if (!_fadeToken.Running)
        {
            Timer.StartTimer(TimeSpan.FromSeconds(0.33), TimeSpan.FromSeconds(0.33), Tick, out _fadeToken);
        }
    }

    public override void OnEnter(Mobile m)
    {
        base.OnEnter(m);
        WakeUp();
    }

    public override void OnExit(Mobile m)
    {
        base.OnExit(m);
        WakeUp();
    }

    public override void OnUnregister()
    {
        base.OnUnregister();
        _fadeToken.Cancel();
    }
}

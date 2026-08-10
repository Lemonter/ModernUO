using System.Collections.Generic;
using Server.Gumps;
using Server.Network;

namespace Server.Engines.Spawners;

/// <summary>
///     Opened from SpawnerGump's new "browse" button per row — lets the player search/
///     scroll a list of every spawnable creature type instead of having to already know
///     and type the exact class name by hand. Picking a name writes it into that specific
///     spawner entry (updating it if the row already had something, or adding a new entry
///     if the row was empty) and reopens the normal SpawnerGump.
/// </summary>
public class MahaonCreaturePickerGump : Gump
{
    private const int PerPage = 15;

    private readonly BaseSpawner _spawner;
    private readonly int _entryIndex;
    private readonly int _spawnerPage;
    private readonly string _filter;
    private readonly int _listPage;
    private readonly List<string> _results;

    public MahaonCreaturePickerGump(BaseSpawner spawner, int entryIndex, int spawnerPage, string filter = "", int listPage = 0)
        : base(60, 60)
    {
        _spawner = spawner;
        _entryIndex = entryIndex;
        _spawnerPage = spawnerPage;
        _filter = filter ?? "";
        _listPage = listPage;
        _results = MahaonCreaturePicker.Search(_filter);

        var totalHeight = 70 + PerPage * 22 + 40;

        AddPage(0);
        AddBackground(0, 0, 300, totalHeight, 5054);
        AddAlphaRegion(10, 10, 280, totalHeight - 20);

        AddHtml(20, 15, 260, 20, "<BASEFONT COLOR=#F4F4F4>Выбор существа для спауна</BASEFONT>");

        AddImageTiled(20, 40, 200, 21, 0xBBC);
        AddTextEntry(22, 41, 196, 19, 0, 0, _filter);
        AddButton(225, 40, 0x15E1, 0x15E5, 1); // Search

        var startIndex = _listPage * PerPage;

        for (var i = 0; i < PerPage; i++)
        {
            var y = 70 + i * 22;
            var resultIndex = startIndex + i;

            if (resultIndex >= _results.Count)
            {
                break;
            }

            AddButton(20, y, 0x15E1, 0x15E5, 100 + i);
            AddHtml(45, y, 230, 20, $"<BASEFONT COLOR=#F4F4F4>{_results[resultIndex]}</BASEFONT>");
        }

        var bottomY = totalHeight - 30;

        if (_listPage > 0)
        {
            AddButton(20, bottomY, 0x15E3, 0x15E7, 2); // Prev page
        }

        if ((_listPage + 1) * PerPage < _results.Count)
        {
            AddButton(60, bottomY, 0x15E1, 0x15E5, 3); // Next page
        }

        AddHtml(100, bottomY, 150, 20, $"<BASEFONT COLOR=#F4F4F4>{_results.Count} найдено</BASEFONT>");

        AddButton(240, bottomY, 0xFB1, 0xFB3, 0); // Cancel
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        if (_spawner.Deleted)
        {
            return;
        }

        if (info.ButtonID == 0) // Cancel — back to the spawner gump, no change
        {
            state.Mobile.SendGump(new SpawnerGump(_spawner, null, _spawnerPage));
            return;
        }

        if (info.ButtonID == 1) // Search
        {
            var newFilter = info.GetTextEntry(0) ?? "";
            state.Mobile.SendGump(new MahaonCreaturePickerGump(_spawner, _entryIndex, _spawnerPage, newFilter, 0));
            return;
        }

        if (info.ButtonID == 2) // Prev page
        {
            state.Mobile.SendGump(new MahaonCreaturePickerGump(_spawner, _entryIndex, _spawnerPage, _filter, _listPage - 1));
            return;
        }

        if (info.ButtonID == 3) // Next page
        {
            state.Mobile.SendGump(new MahaonCreaturePickerGump(_spawner, _entryIndex, _spawnerPage, _filter, _listPage + 1));
            return;
        }

        if (info.ButtonID >= 100)
        {
            var resultIndex = _listPage * PerPage + (info.ButtonID - 100);

            if (resultIndex >= 0 && resultIndex < _results.Count)
            {
                var chosenName = _results[resultIndex];

                if (_entryIndex >= 0 && _spawner.Entries != null && _entryIndex < _spawner.Entries.Count)
                {
                    _spawner.Entries[_entryIndex].SpawnedName = chosenName;
                }
                else
                {
                    _spawner.AddEntry(chosenName);
                }
            }

            state.Mobile.SendGump(new SpawnerGump(_spawner, null, _spawnerPage));
        }
    }
}

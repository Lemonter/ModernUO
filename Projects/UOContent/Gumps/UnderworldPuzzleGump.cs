using Server.Items;
using Server.Network;

namespace Server.Gumps;

// Ported from real OSI/ServUO content (Scripts/Services/Dungeons/Underworld/Maze of
// Death/UnderworldPuzzle.cs, the UnderworldPuzzleGump class) — already a plain `Gump`
// subclass in the original (not ServUO's BaseGump wrapper), so the only real migration
// change is OnResponse's signature (`in RelayInfo`, not `RelayInfo`).
public class UnderworldPuzzleGump : Gump
{
    private readonly Mobile _from;
    private readonly UnderworldPuzzleItem _item;
    private readonly UnderworldPuzzleSolution _solution;
    private readonly UnderworldPuzzleSolution _currentSolution;
    private int _row;

    public UnderworldPuzzleGump(Mobile from, UnderworldPuzzleItem item) : this(from, item, 0)
    {
    }

    public UnderworldPuzzleGump(Mobile from, UnderworldPuzzleItem item, int row) : base(5, 30)
    {
        _from = from;
        _item = item;
        _row = System.Math.Clamp(row, 0, 3);

        _solution = item.Solution;
        _currentSolution = item.CurrentSolution;

        AddBackground(55, 45, 500, 200, 0x2422);
        AddImage(75, 83, 0x2423);
        AddImage(65, 118, 0x2423);
        AddImage(75, 153, 0x2423);
        AddImage(65, 188, 0x2423);
        AddImage(108, 55, 0x2427);
        AddImage(86, 65, 0x2427);
        AddBackground(75, 65, 86, 153, 0x2422);
        AddBackground(192, 65, 137, 153, 0x2422);
        AddBackground(397, 65, 137, 153, 0x2422);
        AddBackground(55, 270, 195, 110, 0x2422);
        AddImage(205, 77, 0x52);
        AddImage(205, 110, 0x52);
        AddImage(205, 143, 0x52);
        AddImage(410, 77, 0x52);
        AddImage(410, 110, 0x52);
        AddImage(410, 143, 0x52);
        AddImage(5, 5, 0x28C8);

        AddButton(160, 320, 0xF2, 0xF1, 8, GumpButtonType.Reply, 0); // Cancel
        AddButton(80, 320, 0xEF, 0xF0, 7, GumpButtonType.Reply, 0);  // Apply
        AddButton(120, 345, 0x7DB, 0x7DB, 0, GumpButtonType.Reply, 0); // Log out

        AddHtmlLocalized(72, 285, 170, 20, 1150180, false, false); // Command Functions:
        AddHtml(200, 285, 100, 20, $"{_item.Attempts}/{_solution.MaxAttempts}", false, false);

        if (from.Skills[SkillName.Lockpicking].Base >= 100.0)
        {
            var locked = _solution.GetMatches(_currentSolution);
            AddHtmlLocalized(72, 300, 170, 20, 1150179, false, false); // Crystals Locked  :
            AddHtml(200, 300, 100, 20, locked.ToString(), false, false);
        }

        AddButton(108, 82, _row == 0 ? 208 : 209, _row == 0 ? 209 : 208, 1, GumpButtonType.Reply, 0);
        AddButton(108, 115, _row == 1 ? 208 : 209, _row == 0 ? 209 : 208, 2, GumpButtonType.Reply, 0);
        AddButton(108, 148, _row == 2 ? 208 : 209, _row == 0 ? 209 : 208, 3, GumpButtonType.Reply, 0);
        AddButton(108, 181, _row == 3 ? 208 : 209, _row == 0 ? 209 : 208, 4, GumpButtonType.Reply, 0);

        AddPiece(0, true, _solution.First);
        AddPiece(1, true, _solution.Second);
        AddPiece(2, true, _solution.Third);
        AddPiece(3, true, _solution.Fourth);

        AddPiece(0, false, _currentSolution.First);
        AddPiece(1, false, _currentSolution.Second);
        AddPiece(2, false, _currentSolution.Third);
        AddPiece(3, false, _currentSolution.Fourth);

        AddButton(88, 82 + _row * 35, 2650, 2650, 5, GumpButtonType.Reply, 0); // Up
        AddButton(128, 82 + _row * 35, 2648, 2648, 6, GumpButtonType.Reply, 0); // Down
    }

    private void AddPiece(int row, bool right, PuzzlePiece piece)
    {
        var id = GetPuzzlePieceID(piece);
        var x = right ? 410 : 205;
        var y = 76 + 33 * row;

        switch (piece)
        {
            case PuzzlePiece.None:
                break;
            case PuzzlePiece.RedSingle:
            case PuzzlePiece.BlueSingle:
            case PuzzlePiece.GreenSingle:
                AddImage(x + 40, y, id);
                break;
            case PuzzlePiece.RedDouble:
            case PuzzlePiece.BlueDouble:
            case PuzzlePiece.GreenDouble:
                AddImage(x, y, id);
                AddImage(x + 80, y, id);
                break;
            case PuzzlePiece.RedTriple:
            case PuzzlePiece.BlueTriple:
            case PuzzlePiece.GreenTriple:
                AddImage(x, y, id);
                AddImage(x + 40, y, id);
                AddImage(x + 80, y, id);
                break;
            case PuzzlePiece.RedBar:
            case PuzzlePiece.BlueBar:
            case PuzzlePiece.GreenBar:
                AddImage(x, y, id);
                break;
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (_item.Deleted || info.ButtonID == 0 || !_from.CheckAlive())
        {
            return;
        }

        if (_from.AccessLevel == AccessLevel.Player && !_item.IsChildOf(_from.Backpack))
        {
            _from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 500446); // That is too far away.
            return;
        }

        switch (info.ButtonID)
        {
            case 5:
            {
                var nextRow = _row - 1;
                if (nextRow < 0)
                {
                    nextRow = 3;
                }

                var movingPiece = _currentSolution.Rows[_row];
                var movingToPiece = _currentSolution.Rows[nextRow];

                if (movingPiece == PuzzlePiece.None || _item.Attempts >= _solution.MaxAttempts)
                {
                    break;
                }

                SplitPiecesUp(ref movingPiece, ref movingToPiece);

                if (movingPiece != _currentSolution.Rows[_row] || movingToPiece != _currentSolution.Rows[nextRow])
                {
                    _currentSolution.Rows[_row] = movingPiece;
                    _currentSolution.Rows[nextRow] = movingToPiece;

                    _item.CurrentSolution = _currentSolution;
                    _item.Attempts++;
                }

                break;
            }
            case 6:
            {
                var nextRow = _row + 1;
                if (nextRow > 3)
                {
                    nextRow = 0;
                }

                var movingPiece = _currentSolution.Rows[_row];
                var movingToPiece = _currentSolution.Rows[nextRow];

                if (movingPiece == PuzzlePiece.None || _item.Attempts >= _solution.MaxAttempts)
                {
                    break;
                }

                SplitPiecesDown(ref movingPiece, ref movingToPiece);

                if (movingPiece != _currentSolution.Rows[_row] || movingToPiece != _currentSolution.Rows[nextRow])
                {
                    _currentSolution.Rows[_row] = movingPiece;
                    _currentSolution.Rows[nextRow] = movingToPiece;

                    _item.Attempts++;
                    _item.CurrentSolution = _currentSolution;
                }

                break;
            }
            case 7:
                if (_item.SubmitSolution(_from, _currentSolution))
                {
                    return;
                }

                break;
            case 8:
                _item.CurrentSolution = new UnderworldPuzzleSolution(_item, _item.Solution.Index);
                _item.Attempts = 0;
                break;
            default:
                _row = info.ButtonID - 1;
                break;
        }

        _from.SendGump(new UnderworldPuzzleGump(_from, _item, _row));
    }

    private static int GetTotalPieces(PuzzlePiece piece) => piece switch
    {
        PuzzlePiece.RedSingle or PuzzlePiece.BlueSingle or PuzzlePiece.GreenSingle => 1,
        PuzzlePiece.RedDouble or PuzzlePiece.BlueDouble or PuzzlePiece.GreenDouble => 2,
        PuzzlePiece.RedTriple or PuzzlePiece.BlueTriple or PuzzlePiece.GreenTriple => 3,
        PuzzlePiece.RedBar or PuzzlePiece.BlueBar or PuzzlePiece.GreenBar          => 4,
        _                                                                          => 0
    };

    private static void SplitPiecesUp(ref PuzzlePiece movingPiece, ref PuzzlePiece movingToPiece)
    {
        var movingAmount = GetTotalPieces(movingPiece);
        var moveToAmount = GetTotalPieces(movingToPiece);

        if (movingToPiece == PuzzlePiece.None)
        {
            if (movingAmount is 2 or 4)
            {
                (movingToPiece, movingPiece) = movingPiece switch
                {
                    PuzzlePiece.RedDouble  => (PuzzlePiece.BlueSingle, PuzzlePiece.GreenSingle),
                    PuzzlePiece.BlueDouble => (PuzzlePiece.GreenSingle, PuzzlePiece.RedSingle),
                    PuzzlePiece.GreenDouble => (PuzzlePiece.RedSingle, PuzzlePiece.BlueSingle),
                    PuzzlePiece.RedBar      => (PuzzlePiece.BlueDouble, PuzzlePiece.GreenDouble),
                    PuzzlePiece.BlueBar     => (PuzzlePiece.GreenDouble, PuzzlePiece.RedDouble),
                    PuzzlePiece.GreenBar    => (PuzzlePiece.RedDouble, PuzzlePiece.BlueDouble),
                    _                       => (movingToPiece, movingPiece)
                };
            }

            return;
        }

        if (movingAmount + moveToAmount > 4)
        {
            (movingPiece, movingToPiece) = (movingToPiece, movingPiece);
            return;
        }

        movingToPiece = CombinePieces(movingPiece, movingToPiece);
        movingPiece = PuzzlePiece.None;
    }

    private static void SplitPiecesDown(ref PuzzlePiece movingPiece, ref PuzzlePiece movingToPiece)
    {
        var movingAmount = GetTotalPieces(movingPiece);
        var moveToAmount = GetTotalPieces(movingToPiece);

        if (movingToPiece == PuzzlePiece.None)
        {
            if (movingAmount is 2 or 4)
            {
                (movingToPiece, movingPiece) = movingPiece switch
                {
                    PuzzlePiece.RedDouble   => (PuzzlePiece.BlueSingle, PuzzlePiece.GreenSingle),
                    PuzzlePiece.BlueDouble  => (PuzzlePiece.GreenSingle, PuzzlePiece.RedSingle),
                    PuzzlePiece.GreenDouble => (PuzzlePiece.RedSingle, PuzzlePiece.BlueSingle),
                    PuzzlePiece.RedBar      => (PuzzlePiece.BlueDouble, PuzzlePiece.GreenDouble),
                    PuzzlePiece.BlueBar     => (PuzzlePiece.GreenDouble, PuzzlePiece.RedDouble),
                    PuzzlePiece.GreenBar    => (PuzzlePiece.RedDouble, PuzzlePiece.BlueDouble),
                    _                       => (movingToPiece, movingPiece)
                };
            }

            return;
        }

        int t;

        if (moveToAmount > movingAmount)
        {
            t = (int)movingToPiece - movingAmount;
        }
        else if (movingAmount > moveToAmount)
        {
            t = (movingAmount, moveToAmount) switch
            {
                (4, 3) => (int)movingToPiece - 2,
                (4, 2) => (int)movingToPiece,
                (4, 1) => (int)movingToPiece + 2,
                (2, 1) => (int)movingToPiece,
                (3, 2) => (int)movingToPiece - 1,
                (3, 1) => (int)movingToPiece + 1,
                _      => 0
            };
        }
        else
        {
            movingPiece = PuzzlePiece.None;
            movingToPiece = PuzzlePiece.None;
            return;
        }

        movingToPiece = (PuzzlePiece)t;
        movingPiece = PuzzlePiece.None;
    }

    private static PuzzlePiece CombinePieces(PuzzlePiece moving, PuzzlePiece movingInto)
    {
        var movingAmount = GetTotalPieces(moving);
        var moveToAmount = GetTotalPieces(movingInto);

        if (movingAmount != moveToAmount)
        {
            var t = (int)movingInto + movingAmount;
            return (PuzzlePiece)t;
        }

        var combined = movingAmount + moveToAmount - 1;

        var movingCol = GetColor(moving);
        var movingIntoCol = GetColor(movingInto);

        return (movingCol, movingIntoCol) switch
        {
            (PuzzleColor.Red, PuzzleColor.Red)     => PuzzlePiece.RedSingle + combined,
            (PuzzleColor.Red, PuzzleColor.Blue)    => PuzzlePiece.GreenSingle + combined,
            (PuzzleColor.Red, PuzzleColor.Green)   => PuzzlePiece.BlueSingle + combined,
            (PuzzleColor.Blue, PuzzleColor.Red)    => PuzzlePiece.GreenSingle + combined,
            (PuzzleColor.Blue, PuzzleColor.Blue)   => PuzzlePiece.BlueSingle + combined,
            (PuzzleColor.Blue, PuzzleColor.Green)  => PuzzlePiece.RedSingle + combined,
            (PuzzleColor.Green, PuzzleColor.Red)   => PuzzlePiece.BlueSingle + combined,
            (PuzzleColor.Green, PuzzleColor.Blue)  => PuzzlePiece.RedSingle + combined,
            (PuzzleColor.Green, PuzzleColor.Green) => PuzzlePiece.GreenSingle + combined,
            _                                       => moving
        };
    }

    private static PuzzleColor GetColor(PuzzlePiece piece) => piece switch
    {
        PuzzlePiece.BlueSingle or PuzzlePiece.BlueDouble or PuzzlePiece.BlueTriple or PuzzlePiece.BlueBar => PuzzleColor.Blue,
        PuzzlePiece.GreenSingle or PuzzlePiece.GreenDouble or PuzzlePiece.GreenTriple or PuzzlePiece.GreenBar => PuzzleColor.Green,
        _ => PuzzleColor.Red
    };

    private static int GetPuzzlePieceID(PuzzlePiece piece) => piece switch
    {
        PuzzlePiece.None => 0x0,
        PuzzlePiece.RedSingle or PuzzlePiece.RedDouble or PuzzlePiece.RedTriple => 0x2A62,
        PuzzlePiece.BlueSingle or PuzzlePiece.BlueDouble or PuzzlePiece.BlueTriple => 0x2A3A,
        PuzzlePiece.GreenSingle or PuzzlePiece.GreenDouble or PuzzlePiece.GreenTriple => 0x2A4E,
        PuzzlePiece.RedBar => 0x2A58,
        PuzzlePiece.BlueBar => 0x2A30,
        PuzzlePiece.GreenBar => 0x2A44,
        _ => 0x0
    };
}

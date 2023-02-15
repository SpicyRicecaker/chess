using System;
// using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

public class GameManager : MonoBehaviour
{
    private Player _turn = Player.Me;
    private Vector2Int _selectedPieceCoord = new Vector2Int(-1, -1);
    private GridManager _gridManager;

    [FormerlySerializedAs("availableMoveSquareDot")] [FormerlySerializedAs("_available_move_square_dot")] [SerializeField]
    private Transform _availableMoveSquareDot;

    [SerializeField] private Transform _gText;
    [SerializeField] private Transform _replayButton;


    private readonly Dictionary<Vector2Int, Transform> _availableSquares = new Dictionary<Vector2Int, Transform>();

    // Start is called before the first frame update
    private ListManager _listManager;
    private PromptManager _promptManager;
    private bool _isMidPrompt;
    private Vector2Int _promotedPieceLocation;

    private Dictionary<Player, bool> _isKingMoved = new Dictionary<Player, bool>();

    private void Start()
    {
        _gridManager = GameObject.FindGameObjectWithTag("GridManager").GetComponent<GridManager>();
        _listManager = GameObject.FindGameObjectWithTag("ListManager").GetComponent<ListManager>();
        _promptManager = GameObject.FindGameObjectWithTag("PromptManager").GetComponent<PromptManager>();
    }

    private void TurnCleanup()
    {
        switch (YouWon(_gridManager.Board, _turn))
        {
            case WinState.Win:
                Object.Instantiate(_gText, new Vector3(8.0f / 2.0f - 0.5f, 8.0f / 2.0f - 0.5f, 0), Quaternion.identity);
                Object.Instantiate(_gText, new Vector3(8.0f / 2.0f + 0.5f, 8.0f / 2.0f - 0.5f, 0), Quaternion.identity);
                Object.Instantiate(_replayButton, new Vector3(8.0f / 2.0f, 8.0f / 2.0f + 0.5f, 0), Quaternion.identity);
                break;
            case WinState.Stalemate:
                Object.Instantiate(_replayButton, new Vector3(8.0f / 2.0f, 8.0f / 2.0f + 0.5f, 0), Quaternion.identity);
                break;
            case WinState.None:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        _turn = (Player)((int)_turn * -1);
    }

    public void SquareClicked(Vector2Int e)
    {
        if (_isMidPrompt)
        {
            return;
        }

        if (_availableSquares.ContainsKey(e))
        {
            MoveToThisSquare(_selectedPieceCoord, e);

            if (ShouldPromote(_gridManager.Board, e))
            {
                _isMidPrompt = true;
                _promptManager.SetVisible(true);
                _promotedPieceLocation = e;
            }
            else
            {
                // In turn cleanup we check for win and increment turn
                TurnCleanup();
            }

            RemovePreviousSelectedPiece();
        }
        else
        {
            var piece = _gridManager.Board[e.y, e.x];

            RemovePreviousSelectedPiece();

            if (piece == null)
            {
                return;
            }

            if (piece.Owner != _turn)
            {
                return;
            }


            MakePieceSelected(e);
        }
    }

    private static bool AnyPieceCanMove(Board board, Player player)
    {
        for (var y = 0; y < 8; y++)
        {
            for (var x = 0; x < 8; x++)
            {
                var p = board[y, x];
                if (p == null || p.Owner != player)
                {
                    continue;
                }

                if (GetAvailableSquaresWithKingCheck(board, new Vector2Int(x, y)).Count != 0)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public void FinishPrompt(PieceType pieceType)
    {
        _promptManager.SetVisible(false);
        // update our piece in question with whatever piece the user selected
        _gridManager.OverrideNewPieceAtLocation(_promotedPieceLocation, _turn, pieceType);
        _isMidPrompt = false;

        // don't forget to check for win with cleanup
        TurnCleanup();
    }

    // the king must be both in check and the opponent must have a move that
    // removes them from the check
    private static WinState YouWon(Board board, Player you)
    {
        var opponent = (Player)((int)you * -1);
        var opponentKingRef = GetKingRef(board, opponent);

        bool opponentKingInCheck = PieceCanGetCapturedByOpponent(board, opponentKingRef);

        return AnyPieceCanMove(board, opponent) switch {
            false when !opponentKingInCheck => WinState.Stalemate,
            false => WinState.Win,
            _ => WinState.None
        };
    }


    private void MoveToThisSquare(Vector2Int from, Vector2Int to)
    {
        var p = _gridManager.Board[to.y, to.x];
        if (p != null)
        {
            _listManager.PlaceNewCapturedPiece(p, _turn);
        }

        // attempt to move our current piece
        // move the taken piece to the list
        p = _gridManager.Board[from.y, from.x];
        if (p.PieceType is PieceType.King)
        {
            _isKingMoved[p.Owner] = true;
        }

        _gridManager.Board[to.y, to.x] = _gridManager.Board[from.y, from.x];
        _gridManager.Board[from.y, from.x] = null;
        _gridManager.Board[to.y, to.x].Transform.position = new Vector3(to.x, to.y, 0);
    }

    private static bool IsWithinRange(Vector2Int v)
    {
        int x = v.x, y = v.y;
        return x is >= 0 and < 8 && y is >= 0 and < 8;
    }

    // We need this function just for the case that we're moving a pawn to promotion
    private static bool ShouldPromote(Board board, Vector2Int to)
    {
        var p = board[to.y, to.x]!;

        return p.PieceType == PieceType.Pawn && (p.Owner == Player.Me && to.y == 7);
    }

    private static IEnumerable<Vector2Int> GetAvailableSquaresFromDirectionVectors(Board board, Piece p, Vector2Int pRef,
        IEnumerable<Vector2Int> directionVectors)
    {
        var newAvailableSquares = new List<Vector2Int>();

        foreach (var direction in directionVectors)
        {
            var currentLoc = pRef;
            while (true)
            {
                currentLoc += direction;
                if (!IsWithinRange(currentLoc))
                {
                    break;
                }

                var pieceAtLocation = board[currentLoc.y, currentLoc.x];
                if (pieceAtLocation != null && pieceAtLocation.Owner == p.Owner)
                {
                    break;
                }
                else if (pieceAtLocation != null)
                {
                    newAvailableSquares.Add(currentLoc);
                    break;
                }
                newAvailableSquares.Add(currentLoc);
            }
        }
        return newAvailableSquares;
    }

    // Returns the possible squares something can move to, not counting if they expose the king or not
    private static List<Vector2Int> GetAvailableSquares(Board board, Vector2Int pRef)
    {
        var newAvailableSquares = new List<Vector2Int>();
        var p = board[pRef.y, pRef.x];

        switch (p.PieceType)
        {
            case PieceType.Pawn:
            {
                var directionVector = new Vector2Int(0, 1) * (int)p.Owner;
                var magnitude = 1;

                switch (p.Owner)
                {
                    case Player.Me:
                        if (pRef.y == 1)
                        {
                            magnitude = 2;
                        }

                        break;
                    case Player.Other:
                        if (pRef.y == 6)
                        {
                            magnitude = 2;
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                Vector2Int[] captureDirections = {
                    new Vector2Int(1, 1) * (int)p.Owner,
                    new Vector2Int(-1, 1) * (int)p.Owner
                };
                foreach (var direction in captureDirections)
                {
                    var newLocation = pRef + direction;
                    if (!IsWithinRange(newLocation)) continue;
                    var pieceToCapture = board[newLocation.y, newLocation.x];
                    if (pieceToCapture != null && pieceToCapture.Owner != p.Owner)
                    {
                        newAvailableSquares.Add(newLocation);
                    }
                }

                // Debug.LogFormat("our direction coord {0}, {1}", directionVector.x, directionVector.y);
                // Debug.LogFormat("our current coord {0}, {1}", pRef.x, pRef.y);

                // now handle front case
                var current = pRef;
                for (var i = 1; i <= magnitude; i++)
                {
                    current += directionVector;

                    if (!IsWithinRange(current))
                    {
                        break;
                    }

                    if (board[current.y, current.x] != null)
                    {
                        break;
                    }

                    newAvailableSquares.Add(current);
                }

                break;
            }
            case PieceType.Knight:
            {
                // The knight can move in an l shape
                Vector2Int[] dxDyList = {
                    new Vector2Int(1, 2),
                    new Vector2Int(-1, 2),
                    new Vector2Int(2, 1),
                    new Vector2Int(-2, 1)
                };

                int[] factorsToMultiplyToFlipY = {
                    -1,
                    1
                };

                foreach (var dxDy in dxDyList)
                {
                    foreach (int factor in factorsToMultiplyToFlipY)
                    {
                        var localDxDy = dxDy;
                        localDxDy.y *= factor;

                        var newLoc = new Vector2Int(pRef.x + localDxDy.x, pRef.y + localDxDy.y);
                        if (!IsWithinRange(newLoc))
                        {
                            continue;
                        }

                        var pieceAtNewLoc = board[newLoc.y, newLoc.x];
                        if (pieceAtNewLoc != null && p.Owner == pieceAtNewLoc.Owner)
                        {
                            continue;
                        }

                        newAvailableSquares.Add(newLoc);
                    }
                }
                break;
            }
            case PieceType.Bishop:
            {
                Vector2Int[] directionVectors = {
                    new Vector2Int(1, 1),
                    new Vector2Int(-1, 1),
                    new Vector2Int(-1, -1),
                    new Vector2Int(1, -1),
                };

                newAvailableSquares.AddRange(GetAvailableSquaresFromDirectionVectors(board, p, pRef, directionVectors));

                break;
            }
            case PieceType.Rook:
            {
                Vector2Int[] directionVectors = {
                    new Vector2Int(1, 0),
                    new Vector2Int(-1, 0),
                    new Vector2Int(0, 1),
                    new Vector2Int(0, -1),
                };

                newAvailableSquares.AddRange(GetAvailableSquaresFromDirectionVectors(board, p, pRef, directionVectors));
                break;
            }
            case PieceType.Queen:
            {
                // Debug.Log("HI");
                Vector2Int[] directionVectors = {
                    new Vector2Int(1, 1),
                    new Vector2Int(-1, 1),
                    new Vector2Int(-1, -1),
                    new Vector2Int(1, -1),
                    new Vector2Int(1, 0),
                    new Vector2Int(-1, 0),
                    new Vector2Int(0, 1),
                    new Vector2Int(0, -1),
                };

                newAvailableSquares.AddRange(GetAvailableSquaresFromDirectionVectors(board, p, pRef, directionVectors));
                break;
            }
            // hardest, since we can't move into a check
            case PieceType.King:
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0)
                        {
                            continue;
                        }
                        var newLoc = new Vector2Int(pRef.x + dx, pRef.y + dy);
                        if (!IsWithinRange(newLoc))
                        {
                            continue;
                        }
                        var pieceAtLoc = board[newLoc.y, newLoc.x];
                        if (pieceAtLoc != null && pieceAtLoc.Owner == p.Owner)
                        {
                            continue;
                        }
                        newAvailableSquares.Add(new Vector2Int(pRef.x + dx, pRef.y + dy));
                    }
                }
                // // also allow for castles, if the king hasn't moved yet and is not in check
                // if (!board.IsKingMoved[p.Owner] && !PieceCanGetCapturedByOpponent(board, pRef))
                // {
                //     // PROBLEM TODO: We need to keep track of what rook is moved (how do we handle promotions?)
                //     // Calling the previous method also recurses forever yay!
                //
                // }

                break;
            }
            default:
                // unreachable
                throw new ArgumentOutOfRangeException();
        }

        return newAvailableSquares;
    }

    private static List<Vector2Int> GetAvailableSquaresWithKingCheck(Board board, Vector2Int pRef)
    {
        var squares = GetAvailableSquares(board, pRef);

        return squares.Where(square => !AfterMoveKingIsExposed(board, pRef, square)).ToList();
    }

    private void MakePieceSelected(Vector2Int pieceRef)
    {
        // TODO: none of the pieces which result in the king moved into check
        // should be allowed

        // Debug.Log("working" + square.x + square.y);
        foreach (var square in GetAvailableSquaresWithKingCheck(_gridManager.Board, pieceRef))
        {
            // Debug.LogFormat("({0},{1})", square.x, square.y);
            var t = Object.Instantiate(_availableMoveSquareDot, new Vector3(square.x, square.y, 0),
                Quaternion.identity);
            _availableSquares.Add(square, t);
        }

        // set current square to the selected square
        _selectedPieceCoord = pieceRef;
    }

    private static bool AfterMoveKingIsExposed(Board board, Vector2Int from, Vector2Int to)
    {
        var newBoard = (Board)board.Clone();

        var p = newBoard[from.y, from.x];
        var o = p.Owner;

        newBoard[to.y, to.x] = newBoard[from.y, from.x];
        newBoard[from.y, from.x] = null;

        var kRef = GetKingRef(newBoard, o);
        return PieceCanGetCapturedByOpponent(newBoard, kRef);
    }

    private static bool PieceCanGetCapturedByOpponent(Board board, Vector2Int pieceInDangerRef)
    {
        var pieceInDanger = board[pieceInDangerRef.y, pieceInDangerRef.x];

        for (var y = 0; y < 8; y++)
        {
            for (var x = 0; x < 8; x++)
            {
                var capturer = board[y, x];
                if (capturer == null)
                {
                    continue;
                }

                // only get pieces that can capture this piece
                if (capturer.Owner == pieceInDanger.Owner)
                {
                    continue;
                }

                var capturerRef = new Vector2Int(x, y);
                if (GetAvailableSquares(board, capturerRef).Contains(pieceInDangerRef))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static Vector2Int GetKingRef(Board board, Player o)
    {
        for (var y = 0; y < 8; y++)
        {
            for (var x = 0; x < 8; x++)
            {
                var p = board[y, x];
                if (p == null)
                {
                    continue;
                }

                if (p.PieceType == PieceType.King && p.Owner == o)
                {
                    return new Vector2Int(x, y);
                }
            }
        }

        // unreachable
        return new Vector2Int();
    }

    private void RemovePreviousSelectedPiece()
    {
        _selectedPieceCoord = new Vector2Int(-1, -1);
        // // we have to check here because the initialization of x and y could be any int
        // if (!isWithinRange(v))
        // {
        //     return;
        // }

        foreach (var entry in _availableSquares)
        {
            Object.Destroy(entry.Value.gameObject);
        }

        _availableSquares.Clear();
    }
}

public enum Player
{
    Me = 1,
    Other = -1
}

public enum PieceType
{
    Pawn,
    Bishop,
    Knight,
    Rook,
    Queen,
    King
}

public class Piece
{
    public readonly Player Owner;
    public readonly PieceType PieceType;
    public readonly Transform Transform;

    public Piece(Player owner, PieceType pieceType, Transform transform)
    {
        this.Owner = owner;
        this.PieceType = pieceType;
        this.Transform = transform;
    }
}

public enum WinState
{
    Win,
    Stalemate,
    None
}

public class Board : ICloneable
{
    public Piece[,] Pieces;
    public Dictionary<Player, bool> IsKingMoved;

    public Board(Piece[,] pieces, Dictionary<Player, bool> isKingMoved)
    {
        this.Pieces = pieces;
        this.IsKingMoved = isKingMoved;
    }
    public object Clone()
    {
        return new Board((Piece[,])Pieces.Clone(), new Dictionary<Player, bool>(IsKingMoved));
    }

    public Piece this[int y, int x]
    {
        get => Pieces[y, x];
        set => Pieces[y, x] = value;
    }
}
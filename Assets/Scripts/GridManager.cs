// using System.Collections;
// using System.Collections.Generic;

using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;


public class GridManager : MonoBehaviour
{
    // represent a chess grid using an array of 8x8 holding chesspieces
    // spawn squares on the tilegrid depending on the piece positions.

    [SerializeField] private Tile _tile;

    [SerializeField] private Transform _r;
    [SerializeField] private Transform _n;
    [SerializeField] private Transform _b;
    [SerializeField] private Transform _q;
    [SerializeField] private Transform _k;
    [SerializeField] private Transform _p;
    [SerializeField] private Transform _cam;


    private const int _WIDTH = 8;
    private const int _HEIGHT = 8;
    public Board Board = new Board(new Piece[_HEIGHT, _WIDTH], new Dictionary<Player, bool>() {
        {
            Player.Me, false
        }, {
            Player.Other, false
            
        }
    });

    private static readonly string[] _INITIAL_BOARD_CONFIG = {
        "rnbqkbnr",
        "pppppppp",
        "        ",
        "        ",
        "        ",
        "        ",
        "pppppppp",
        "rnbqkbnr",
    };

    // place a new piece at a given location
    public void OverrideNewPieceAtLocation(Vector2Int coord, Player owner, PieceType pieceType)
    {
        // check if the piece already exists there
        var p = Board[coord.y, coord.x];
        if (p != null)
        {
            Object.Destroy(p.Transform.gameObject);
        }
        var chessPieceFab = GetPieceFabFromPieceType(pieceType);
        var chessPieceTransform = Object.Instantiate(chessPieceFab, new Vector3(coord.x, coord.y, 0), Quaternion.identity);
        var newP = new Piece(owner, pieceType, chessPieceTransform);
        Board[coord.y, coord.x] = newP;
    }

    private Transform GetPieceFabFromPieceType(PieceType pieceType)
    {
        var pieceFab = pieceType switch {
            PieceType.Bishop => _b,
            PieceType.Knight => _n,
            PieceType.Pawn => _p,
            PieceType.Rook => _r,
            PieceType.King => _k,
            PieceType.Queen => _q,
            _ => throw new NotSupportedException()
        };
        return pieceFab;
    }

    private static PieceType? GetPieceTypeFromChar(char c)
    {
        // Debug.Log(c);
        return c switch {
            'r' => PieceType.Rook,
            'n' => PieceType.Knight,
            'b' => PieceType.Bishop,
            'q' => PieceType.Queen,
            'k' => PieceType.King,
            'p' => PieceType.Pawn,
            _ => null
        };
    }

    // Start is called before the first frame update
    private void Start()
    {
        // Based off of our readonly string, generate the chess pieces
        for (var y = 0; y < _INITIAL_BOARD_CONFIG.Length; y++)
        {
            for (var x = 0; x < _INITIAL_BOARD_CONFIG[y].Length; x++)
            {
                var tile = Object.Instantiate(_tile, new Vector3(x, y, 0), Quaternion.identity);
                tile.Init(x, y, (x + y) % 2 == 0);

                var pieceTypeOp = GetPieceTypeFromChar(_INITIAL_BOARD_CONFIG[y][x]);
                if (!pieceTypeOp.HasValue)
                {
                    continue;
                }
                var pieceType = pieceTypeOp ?? 0;

                var chessPieceFab = GetPieceFabFromPieceType(pieceType!);
                var pieceTransform = Object.Instantiate(chessPieceFab, new Vector3(x, y, 0), Quaternion.identity);
                var owner = y <= 1 ? Player.Me : Player.Other;
                Board[y, x] = new Piece(owner, pieceType, pieceTransform);
            }
        }

        const float MIDDLE_OFFSET = 0.5f;
        var transform1 = _cam.transform;
        var centeredCameraPosition = new Vector3(_WIDTH / 2.0f - MIDDLE_OFFSET, _HEIGHT / 2.0f - MIDDLE_OFFSET, transform1.position.z);
        transform1.position = centeredCameraPosition;
    }
}
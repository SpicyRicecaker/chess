using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ListManager : MonoBehaviour
{
    // two lists will be implemented as a stack that grows from top to the
    // bottom for me, and bottom to top for the other
    public Dictionary<Player, List<Piece>> Stack = new Dictionary<Player, List<Piece>>();
    private readonly Dictionary<Player, Stack> _locationAndDirectionByPlayer = new Dictionary<Player, Stack>() {
        {Player.Me, new Stack(new Vector2Int(-1, 7), new Vector2Int(-1, -1))},
        {Player.Other, new Stack(new Vector2Int(8, 0), new Vector2Int(1, 1))}
    };

    public void PlaceNewCapturedPiece(Piece p, Player o)
    {
        var stack = _locationAndDirectionByPlayer[o];

        var pieceLocation = p.Transform.position;

        pieceLocation.x = stack.Location.x;
        pieceLocation.y = stack.Location.y;

        p.Transform.position = pieceLocation;

        int maybeInvalidY = stack.Location.y + stack.Direction.y;
        if (maybeInvalidY is < 0 or > 7)
        {
            stack.Location.x = stack.Location.x + stack.Direction.x;
            stack.Location.y = stack.Start.y;
        } else {
            stack.Location.y = maybeInvalidY;
        }
    }
}

public class Stack
{
    public Vector2Int Start;
    public Vector2Int Location;
    public Vector2Int Direction;
    public Stack(Vector2Int location, Vector2Int direction)
    {
        this.Start = location;
        this.Location = location;
        this.Direction = direction;
    }
}
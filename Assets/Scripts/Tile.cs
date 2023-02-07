using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Tile : MonoBehaviour
{
    private int _x;
    private int _y;
    bool _isLightColoredSquare;
    [SerializeField] private Color _lightColor, _darkColor;
    [SerializeField] private SpriteRenderer _renderer;
    [FormerlySerializedAs("_adjustment_layer")] [SerializeField] private GameObject _adjustmentLayer;
    private GameManager _game;
    public void Init(int x, int y, bool isLightColoredSquare)
    {
        this._x = x;
        this._y = y;
        this._isLightColoredSquare = isLightColoredSquare;

        _renderer.color = this._isLightColoredSquare ? _lightColor : _darkColor;
    }
    // Start is called before the first frame update
    void Start()
    {
        _game = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
    }

    // option 1: utilize this tile object, and enable an "is prompt" argument
    // with the piece type it is attached to for the prompt.
    // option 2: create a new tile object, but this time when it is clicked se

    void OnMouseEnter()
    {
        _adjustmentLayer.SetActive(true);
    }

    void OnMouseExit()
    {
        _adjustmentLayer.SetActive(false);
    }

    void OnMouseDown()
    {
        _game.SquareClicked(new Vector2Int(_x, _y));
    }

    // Update is called once per frame
    void Update()
    {

    }
}

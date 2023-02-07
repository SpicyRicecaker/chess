using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class PromptTile : MonoBehaviour
{
    private PieceType _piecePieceType;
    bool _isLightColoredSquare;
    [SerializeField] private Color _lightColor, _darkColor;
    [SerializeField] private SpriteRenderer _renderer;
    [FormerlySerializedAs("_adjustment_layer")][SerializeField] private GameObject _adjustmentLayer;
    private PromptManager _promptManager;
    
    public void Init(PieceType piecePieceType)
    {
        this._isLightColoredSquare = (int)piecePieceType % 2 == 0;
        _renderer.color = this._isLightColoredSquare ? _lightColor : _darkColor;
        _piecePieceType = piecePieceType;
    }
    // Start is called before the first frame update
    void Start()
    {
        _promptManager = GameObject.FindGameObjectWithTag("PromptManager").GetComponent<PromptManager>();
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
        _promptManager.PieceSelected(_piecePieceType);
    }

    // Update is called once per frame
    void Update()
    {

    }
}

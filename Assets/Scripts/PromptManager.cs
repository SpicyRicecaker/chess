using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Object = UnityEngine.Object;

public class PromptManager : MonoBehaviour
{
    private static readonly PieceType[] _PROMOTION_LIST = {
        PieceType.Queen,
        PieceType.Rook,
        PieceType.Bishop,
        PieceType.Knight
    };

    private GameManager _gameManager;
    [SerializeField] private Transform _tile;
    [SerializeField] private Transform _q;
    [SerializeField] private Transform _r;
    [SerializeField] private Transform _b;
    [SerializeField] private Transform _n;
    [SerializeField] private PromptTile _promptTile;

    private readonly List<Transform> _gameObjects = new List<Transform>();
    // Start is called before the first frame update
    void Start()
    {
        _gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();

        const float LEFT = 4.0f - (4.0f / 2.0f);
        const float TOP = 4.0f - 0.5f;

        for (var x = 0; x < _PROMOTION_LIST.Length; x++)
        {
            var chessPieceTransform = _PROMOTION_LIST[x] switch {
                PieceType.Queen => _q,
                PieceType.Rook => _r,
                PieceType.Bishop => _b,
                PieceType.Knight => _n,
                // should be unreachable
                _ => throw new ArgumentOutOfRangeException()
            };
            
            var chessPieceObject = GameObject.Instantiate(chessPieceTransform, new Vector3(LEFT + x, TOP, 0), Quaternion.identity);
            _gameObjects.Add(chessPieceObject);
            chessPieceObject.gameObject.SetActive(false);
            // also add a tile for each one
            var promptTile = GameObject.Instantiate(_promptTile, new Vector3(LEFT + x, TOP), Quaternion.identity);
            promptTile.Init(_PROMOTION_LIST[x]);
            _gameObjects.Add(promptTile.transform);
            promptTile.gameObject.SetActive(false);
        }
    }

    public void SetVisible(bool visible)
    {
        foreach (var o in _gameObjects)
        {
            o.gameObject.SetActive(visible);
        }
    }
    public void PieceSelected(PieceType pieceType)
    {
        _gameManager.FinishPrompt(pieceType);
    }

}
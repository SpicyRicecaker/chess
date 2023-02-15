using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Replay : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _renderer;
    [SerializeField] private Color _preColor, _postColor;
    // Start is called before the first frame update
    private void Start()
    {
        _renderer.color = _preColor;

    }

    private void OnMouseEnter()
    {
        _renderer.color = _postColor;
    }

    private void OnMouseExit()
    {
        _renderer.color = _preColor;
    }

    private void OnMouseDown()
    {
        SceneManager.LoadScene("SampleScene");
    }
}

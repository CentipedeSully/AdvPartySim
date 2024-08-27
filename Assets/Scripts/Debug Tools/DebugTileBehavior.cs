using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebugTileBehavior : MonoBehaviour
{
    [SerializeField] private Vector2 xyPosition;
    [SerializeField] private Canvas _canvas;
    [SerializeField] private Text _valueText;

    public void SetValue(string newValue)
    {
        _valueText.text = newValue;
    }

    public void SetIndex(int x,int y)
    {
        xyPosition.x = x;
        xyPosition.y = y;
    }

    public void SetCamera(Camera cam)
    {
        _canvas.worldCamera = cam;

    }

}

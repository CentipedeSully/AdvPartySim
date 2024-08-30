using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UI;





public class DebugTileBehavior : SerializedMonoBehaviour
{
    [TabGroup("Default","Data")]
    [SerializeField] private Vector2Int _index;
    [TabGroup("Default", "Data")]
    [SerializeField] private bool _isWalkable;
    [TabGroup("Default", "Data")]
    [SerializeField] private bool _isLabel;
    [TabGroup("Default", "Data")]
    [SerializeField] private Vector2Int _visibleParentArrow;



    [TabGroup("Default", "References")]
    [SerializeField] private Canvas _canvas;
    [TabGroup("Default", "References")]
    [SerializeField] private SpriteRenderer _spriteRenderer;

    [Space(20)]
    [TabGroup("Default", "References")]
    [SerializeField] private GameObject _defaultUiObject;
    [TabGroup("Default", "References")]
    [SerializeField] private Text _defaultUiText;

    [Space(20)]
    [TabGroup("Default", "References")]
    [SerializeField] private GameObject _costDisplay;
    [TabGroup("Default", "References")]
    [SerializeField] private Text _gLabelText;
    [TabGroup("Default", "References")]
    [SerializeField] private Text _gText;
    [TabGroup("Default", "References")]
    [SerializeField] private Text _hLabelText;
    [TabGroup("Default", "References")]
    [SerializeField] private Text _hText;
    [TabGroup("Default", "References")]
    [SerializeField] private Text _fLabelText;
    [TabGroup("Default", "References")]
    [SerializeField] private Text _fText;

    [Space(20)]
    [TabGroup("Default", "References")]
    [SerializeField] private GameObject _parentDisplay;
    [TabGroup("Default", "References")]
    [SerializeField] private Dictionary<Vector2Int,Image> _parentArrows = new();







    


    //Setup
    public void SetupBehavior(GridNode node, Camera mainCam)
    {
        //convert the index tuple into a vector2Int 
        Vector2Int index = new Vector2Int(node._index.Item1, node._index.Item2);

        //set index
        _index = index;

        //set the default ui text to show this cell's index
        _defaultUiText.text = $"{index.x.ToString()},{index.y.ToString()}";

        //set renderCam
        _canvas.worldCamera = mainCam;

        //setWalkability
        _isWalkable = node._isWalkable;

        //assume this tile isn't a label
        _isLabel = false;
    }

    public void SetupBehaviorAsLabel(Vector2Int index, string uiText, Camera mainCam)
    {
        //set index
        _index = index;

        //set the default ui text as the given uiText param
        _defaultUiText.text = uiText;

        //set renderCam
        _canvas.worldCamera = mainCam;

        //set the tile as a Label
        _isLabel = true;

        //labels aren't walkable, nor will they be checked for walkability
        _isWalkable = false;
    }

    public void SetTileColor(Color newColor)
    {
        _spriteRenderer.color = newColor;
    }

    public void SetTextColor(Color  newColor)
    {
        _defaultUiText.color = newColor;

        _gText.color = newColor;
        _hText.color = newColor;
        _fText.color = newColor;

        _gLabelText.color = newColor;
        _hLabelText.color = newColor;
        _fLabelText.color = newColor;
    }

    public void SetArrowColor(Color newColor)
    {
        foreach (KeyValuePair<Vector2Int, Image> arrowObject in _parentArrows)
        {
            arrowObject.Value.color = newColor;
        }
    }

    public bool IsLabel() { return _isLabel; }

    public void SetPathDataText(PathNode node)
    {
        _gText.text = node._gCost.ToString();

        _hText.text = node._hCost.ToString();
        _fText.text = node._fCost.ToString();


        //is our parent's index valid
        if (node._parentIndex != new Vector2Int(-1, -1))
        {
            //calculate parent's direction relative to this node
            int relativeX = node._parentIndex.x - node._index.x;
            int relativeY = node._parentIndex.y - node._index.y;

            //clarify the new index
            Vector2Int parentRelativeDirection = new Vector2Int(relativeX, relativeY);


            //activate the directional arrow that matches our parent's relative direction
            _parentArrows[parentRelativeDirection].gameObject.SetActive(true);

            //Set this relative index as showing
            _visibleParentArrow = parentRelativeDirection;
        }
    }

    public void ClearPathDataText()
    {
        _gText.text = "---";
        _hText.text = "---";
        _fText.text = "---";

        if (_visibleParentArrow != Vector2Int.zero)
        {
            //hide the visible parent arrow
            _parentArrows[_visibleParentArrow].gameObject.SetActive(false);

            //clear the visibleArrow data
            _visibleParentArrow -= Vector2Int.zero;
        }
    }

    public Vector2Int GetIndex()
    {
        return _index;
    }

    public bool IsWalkable() { return _isWalkable; }


    //UI management
    public void ShowDefaultUi()
    {
        _defaultUiObject.SetActive(true);
    }

    public void HideDefaultUi()
    {
        _defaultUiObject.SetActive(false);
    }

    public void ShowPathingData()
    {
        _costDisplay.SetActive(true);
        _parentDisplay.SetActive(true);
    }

    public void HidePathingData()
    {
        _costDisplay.SetActive(false);
        _parentDisplay.SetActive(false);
    }

}









using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;





public class DebugTileBehavior : MonoBehaviour
{
    [TabGroup("Default","Data")]
    [SerializeField] private Vector2Int _index;
    [TabGroup("Default", "Data")]
    [SerializeField] private bool _isWalkable;
    [TabGroup("Default", "Data")]
    [SerializeField] private bool _isLabel;



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
    [SerializeField] private Text _gText;
    [TabGroup("Default", "References")]
    [SerializeField] private Text _hText;
    [TabGroup("Default", "References")]
    [SerializeField] private Text _fText;

    [Space(20)]
    [TabGroup("Default", "References")]
    [SerializeField] private GameObject _parentDisplay;
    [TabGroup("Default", "References")]
    [SerializeField] private Text _parentIndexText;







    


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

    public void SetColor(Color newColor)
    {
        _spriteRenderer.color = newColor;
    }

    public bool IsLabel() { return _isLabel; }

    public void SetPathDataText(PathNode node)
    {
        _gText.text = node._gCost.ToString();

        _hText.text = node._hCost.ToString();
        _fText.text = node._fCost.ToString();

        _parentIndexText.text = $"{node._parentIndex.x},{node._parentIndex.y}";
    }



    //UI management
    public void ShowCostData()
    {
        _defaultUiObject.SetActive(false);

        _costDisplay.SetActive(true);
        _parentDisplay.SetActive(false);
    }

    public void ShowParentData()
    {
        _defaultUiObject.SetActive(false);

        _parentDisplay.SetActive(true);
        _costDisplay.SetActive(false);
    }

    public void ShowIndex()
    {
        _defaultUiObject.SetActive(true);

        _costDisplay.SetActive(false);
        _parentDisplay.SetActive(false);
    }

}



[CreateAssetMenu]
public class DebugGridManager : SerializedScriptableObject, IQuickLoggable
{

    //Declarations ===================================================================
    [BoxGroup("Setup")]
    [TabGroup("Setup/Tabgroup", "References")]
    [SerializeField] private Grid _unityGrid;

    [TabGroup("Setup/Tabgroup", "References")]
    [SerializeField] private Camera _mainCamera;

    private Vector2Int _gridSize;
    private Vector3 _gridOffset;

    [TabGroup("Setup/Tabgroup", "References")]
    [SerializeField] private Transform _debugGridParent;

    [TabGroup("Setup/Tabgroup", "References")]
    [SerializeField] private GameObject _debugTilePrefab;

    [TabGroup("Setup/Tabgroup", "References")]
    [SerializeField] private GameObject _xLabelDebugTilePrefab;

    [TabGroup("Setup/Tabgroup", "References")]
    [SerializeField] private GameObject _yLabelDebugTilePrefab;


    [TabGroup("Setup/Tabgroup","Tile Colors")]
    [SerializeField] private Color _defaultColor;

    [TabGroup("Setup/Tabgroup", "Tile Colors")]
    [SerializeField] private Color _xLabelColor;

    [TabGroup("Setup/Tabgroup", "Tile Colors")]
    [SerializeField] private Color _yLabelColor;

    [TabGroup("Setup/Tabgroup", "Tile Colors")]
    [SerializeField] private Color _walkableColor;

    [TabGroup("Setup/Tabgroup", "Tile Colors")]
    [SerializeField] private Color _unwalkableColor;


    [BoxGroup("Management")]
    [TabGroup("Management/Tabgroup", "Grid Nodes")]
    [SerializeField] private Dictionary<Vector2Int, GridNode> _gridNodes = new();

    [TabGroup("Management/Tabgroup", "Debug Tiles")]
    [SerializeField] private Dictionary<Vector2Int, DebugTileBehavior> _debugTileBehaviors = new();








    //Internals ================================================================================
    private void ShowDefaultColors()
    {
        foreach (KeyValuePair<Vector2Int, DebugTileBehavior> entry in _debugTileBehaviors)
        {
            entry.Value.SetColor(_defaultColor);
        }

    }

    [BoxGroup("Debug")]
    [Button("Destroy Debug Grid")]
    private void DestroyDebugGrid()
    {
        //delete each visual debug object of the grid
        if (_debugGridParent.childCount > 0)
        {
            //note the number of child objects we have
            int childCount = _debugGridParent.childCount;

            //destroy all child objects, stepping from the last to the first
            for (int i = childCount - 1; i >= 0; i--)
            {
                //Destroy the objects at the end of the frame if we're in play mode
                if (Application.isPlaying)
                    Destroy(_debugGridParent.GetChild(i).gameObject);

                //Otherwise, destroy them NOW if we're working in edit mode
                else DestroyImmediate(_debugGridParent.GetChild(i).gameObject);
            }
        }

        //Clear our references to the deleted object's behaviors
        _debugTileBehaviors.Clear();
    }


    [BoxGroup("Management")]
    [Button("Generate Debug Grid")]
    private void GenerateDebugGrid()
    {

        //if either any grid objects exist or if any tile behaviors exist, then destroy everything
        if (_debugGridParent.childCount > 0 || _debugTileBehaviors.Count > 0)
        {
            //Destroy all preexisting debug grid data
            DestroyDebugGrid();
        }


        //declare our temp object
        GameObject newDebugTile = null;
        DebugTileBehavior newBehavior = null;


        //Use our collection of gridNodes to create and setup our debug tiles
        foreach (KeyValuePair<Vector2Int, GridNode> nodeEntry in _gridNodes)
        {
            //Create a new debug tile
            newDebugTile = Instantiate(_debugTilePrefab, _debugGridParent);

            //Capture the new tile's debugTileBehavior
            newBehavior = newDebugTile.GetComponent<DebugTileBehavior>();

            //Add the behavior to our collection
            _debugTileBehaviors.Add(nodeEntry.Key, newBehavior);

            //Setup DebugTile Behavior
            newBehavior.SetupBehavior(nodeEntry.Value, _mainCamera);

            //Set the color
            newBehavior.SetColor(_defaultColor);

            //Place the tile at its respective unityGrid position
            newDebugTile.transform.localPosition = nodeEntry.Value._localPosition;
        }



        //delcare the variables for our x & y indicator tiles
        GameObject newXTile = null;
        DebugTileBehavior xTileBehavior = null;

        GameObject newYTile = null;
        DebugTileBehavior yTileBehavior = null;


        //Label the x rows
        for (int i = 0; i < _gridSize.x; i++)
        {
            // Create an x DebugTile to label the current row 
            newXTile = Instantiate(_xLabelDebugTilePrefab, _debugGridParent);

            //Capture this label's debugTileBehavior
            xTileBehavior = newXTile.GetComponent<DebugTileBehavior>();

            //create this tile's 'offGrid' index 
            Vector2Int index = new Vector2Int(i, -1);

            //Add this debug tile behavior to our collection
            _debugTileBehaviors.Add(index, xTileBehavior);

            //Setup the DebugTile Behavior
            xTileBehavior.SetupBehaviorAsLabel(index,  i.ToString(), _mainCamera);

            //Set the color
            xTileBehavior.SetColor(_xLabelColor);

            //calculate the object's local position
            Vector3 localPosition = _unityGrid.CellToLocal(new Vector3Int(i, -1, 0));

            //offset the object
            localPosition += _gridOffset;

            //place the tile at the unityGrid's index position
            newXTile.transform.localPosition = localPosition;
            
        }



        //Label the y columns
        for (int j = 0; j < _gridSize.y; j++)
        {
            // Create a y DebugTile to label the current column
            newYTile = Instantiate(_yLabelDebugTilePrefab, _debugGridParent);

            //Capture this label's debugTileBehavior
            yTileBehavior = newYTile.GetComponent<DebugTileBehavior>();

            //create this tile's 'offGrid' index 
            Vector2Int index = new Vector2Int(-1, j);

            //Add this debug tile behavior to our collection
            _debugTileBehaviors.Add(index, yTileBehavior);

            //Setup the DebugTile Behavior
            yTileBehavior.SetupBehaviorAsLabel(index, j.ToString(), _mainCamera);

            //Set Color
            yTileBehavior .SetColor(_yLabelColor);

            //calculate the object's local position
            Vector3 localPosition = _unityGrid.CellToLocal(new Vector3Int(-1, j, 0));

            //offset the object
            localPosition += _gridOffset;

            //place the tile at the unityGrid's index position
            newYTile.transform.localPosition = localPosition;
        }
    }






    //externals ==========================================================================
    public void SetGridNodes(Dictionary<Vector2Int, GridNode> newNodeCollection, Vector2Int gridSize, Vector3 gridOffset)
    {
        //Make sure we're told how big the grid is
        _gridSize = gridSize;

        //Also pickup any offset data for later tile positioning
        _gridOffset = gridOffset;

        //overrite the old node collection
        _gridNodes = newNodeCollection;

        //create a new grid based off the new data. Any old grid data is discarded in the process
        GenerateDebugGrid();
    }


    //public void UpdateGridNode(GridNode preexistingNode) {}




    public int GetScriptID()
    {
        return GetInstanceID();
    }

    public string GetScriptName()
    {
        return name;
    }



}

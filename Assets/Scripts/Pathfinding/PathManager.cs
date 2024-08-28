using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System;
using System.Dynamic;
using UnityEditorInternal;



public interface IPathAgent
{

}


[Serializable]
public struct PathNode
{
    public (int,int) _index;
    public Vector3 _localPosition;
    public int _traversalCost;
    public bool _isWalkable;
    public Dictionary<int, IPathAgent> _occupants;

    public PathNode(bool defaultWalkability = true)
    {
        _index = (-1,-1);
        _localPosition = Vector3.zero;
        _traversalCost = 0;
        _isWalkable = defaultWalkability;
        _occupants = new Dictionary<int, IPathAgent>();
    }
}




public class PathGrid
{
    private PathNode[,] _grid;
    private Grid _unityGrid;
    private Vector3 _gridOffset;
    private int _width;
    private int _height;

    public PathGrid(int width, int height, Grid unityGrid, Vector3 offset)
    {
        _width = width; 
        _height = height;
        _unityGrid = unityGrid;
        _gridOffset = offset;

        _grid = new PathNode[width,height];

        for (int i =0; i< width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                //Set each index
                _grid[i, j]._index = (i, j);

                //Set each world position
                _grid[i, j]._localPosition = _unityGrid.CellToLocal(new Vector3Int(i,j,0));

                //Add gridOffset
                _grid[i, j]._localPosition += _gridOffset;

            }
        }
    }

    public int Width() { return _width; }

    public int Height() {return _height; }
    
    public PathNode GetCell(int x, int y)
    {
        bool xPositionValid = (x >= 0) && (x < _width);
        bool yPositionValid = (y >= 0) && (y < _height);

        if (!xPositionValid)
        {
            Debug.LogError($"x index {x} out of grid path grid range");
            return default;
        }

        else if (!yPositionValid)
        {
            Debug.LogError($"y index {y} out of grid path grid range");
            return default;
        }

        else
            return _grid[x, y];
    }

    public PathNode GetCell((int,int) xyPair)
    {
        return GetCell(xyPair.Item1, xyPair.Item2);
    }

    public void UpdateCell(int x,int y, PathNode updatedNode)
    {
        bool xPositionValid = (x >= 0) && (x < _width);
        bool yPositionValid = (y >= 0) && (y < _height);

        if (!xPositionValid)
        {
            Debug.LogError($"x index {x} out of grid path grid range. Ignoring update request on index ({x},{y})");
        }

        else if (!yPositionValid)
        {
            Debug.LogError($"y index {y} out of grid path grid range. Ignoring update request on index ({x},{y})");
        }

        else 
            _grid[x, y] = updatedNode;
    }

    public void UpdateCell((int,int) xyPair, PathNode updatedNode)
    {
        UpdateCell(xyPair.Item1, xyPair.Item2, updatedNode);
    }

    public void UpdateCell(Vector2Int xyPair, PathNode updatedNode)
    {
        UpdateCell(xyPair.x, xyPair.y, updatedNode);
    }
}



[Serializable]
public struct DebugNode
{
    public GameObject _visualTile;
    public int _xPosition;
    public int _yPosition;

    public DebugNode(PathNode trueNode, GameObject visualTileObject)
    {
        //Reflect the node's data
        _xPosition = trueNode._index.Item1;
        _yPosition = trueNode._index.Item2;

        //Set this debug node's visual object
        _visualTile = visualTileObject;

        //Place the visual at the true node's location
        _visualTile.transform.localPosition = trueNode._localPosition;

    }

    public DebugNode(int x, int y, GameObject visualTileObject, Vector3 localPosition)
    {
        _xPosition = x;
        _yPosition = y;
        _visualTile = visualTileObject;

        //place the node at it's proper position
        _visualTile.transform.localPosition = localPosition;
    }

    
}



public class PathManager : SerializedMonoBehaviour, IQuickLoggable
{
    //Declarations
    [BoxGroup("Setup")]
    [TabGroup("Setup/Tabgroup", "Parameters")]
    [SerializeField] private int _gridWidth;

    [TabGroup("Setup/Tabgroup", "Parameters")]
    [SerializeField] private int _gridHeight;

    [TabGroup("Setup/Tabgroup", "Parameters")]
    [SerializeField] private Vector3 _gridOffset = new Vector3(0, 1.5f, 0);




    [TabGroup("Setup/Tabgroup", "References")]
    [SerializeField] private Grid _unityGrid;

    [TabGroup("Setup/Tabgroup", "References")]
    [SerializeField] private TileBehaviorManager _tileBehaviorManager;

    [TabGroup("Setup/Tabgroup", "References")]
    [SerializeField] private Transform _DebugGridObjectTransform;

    [TabGroup("Setup/Tabgroup", "References")]
    [SerializeField] private GameObject _positionDebugTilePrefab;

    [TabGroup("Setup/Tabgroup", "References")]
    [SerializeField] private GameObject _xCounterTilePrefab;

    [TabGroup("Setup/Tabgroup", "References")]
    [SerializeField] private GameObject _yCounterTilePrefab;

    [TabGroup("Setup/Tabgroup", "References")]
    [SerializeField] private Camera _eventCamera;



    [BoxGroup("Grid")]
    [SerializeField]
    private PathGrid _pathGrid;

    [TabGroup("Grid/Tabgroup", "Nodes")]
    [SerializeField] Dictionary<Vector2Int, PathNode> _pathNodes = new();



    [TabGroup("Grid/Tabgroup", "Debug Nodes")]
    [SerializeField] private Dictionary<Vector2Int, DebugNode> _debugNodes = new();




    //[SerializeField] private Dictionary<int,IPathAgent> _agents;
    [BoxGroup("Debug")]
    [SerializeField] private bool _isDebugActive = true;






    //Monobehaviours




    //Internals


    [TabGroup("Debug/Tabgroup","Functions")]
    [Button("Generate Debug Grid")]
    private void GenerateDebugGrid()
    {
        //if any debug nodes OR any debug grid objects exist, destroy everything
        if (_debugNodes.Count > 0 || _DebugGridObjectTransform.childCount > 0)
        {
            //Destroy all preexisting debug grid data
            DestroyDebugGrid();
        }



        //declare our temp object
        GameObject newDebugTile = null;



        //Use our collection of gridNodes to populate our debugNode Collection
        foreach(KeyValuePair<Vector2Int,PathNode> nodeEntry in _pathNodes)
        {
            //Create a new debug tile
            newDebugTile = Instantiate(_positionDebugTilePrefab, _DebugGridObjectTransform);

            //declare index for clarity
            Vector2Int index = nodeEntry.Key;

            //Setup DebugTile Behavior
            newDebugTile.GetComponent<DebugTileBehavior>().SetCamera(_eventCamera);
            newDebugTile.GetComponent<DebugTileBehavior>().SetIndex(index.x, index.y);
            newDebugTile.GetComponent<DebugTileBehavior>().SetValue($"{index.x},{index.y}");

            //Create a new DebugNode and add it to our debugNode collection 
            _debugNodes.Add(index, new DebugNode(nodeEntry.Value, newDebugTile));
        }



        //delcare the variables for our x & y indicator tiles
        GameObject newXTile = null;
        GameObject newYTile = null;



        //Label the x rows
        for (int i = 0; i < _gridWidth; i++)
        {
            // Create an x DebugTile to label the current row 
            newXTile = Instantiate(_xCounterTilePrefab, _DebugGridObjectTransform);

            //Setup DebugTile Behavior
            newXTile.GetComponent<DebugTileBehavior>().SetCamera(_eventCamera);
            newXTile.GetComponent<DebugTileBehavior>().SetIndex(i, -1);
            newXTile.GetComponent<DebugTileBehavior>().SetValue(i.ToString());

            //calculate the node's local position
            Vector3 localPosition = _unityGrid.CellToLocal(new Vector3Int(i, -1, 0));

            //offset the node
            localPosition += _gridOffset;

            //Create a new DebugNode and add it to our debugNode collection 
            _debugNodes.Add(new Vector2Int(i, -1), new DebugNode(i, -1, newXTile, localPosition));
        }



        //Label the y columns
        for (int j = 0; j < _gridHeight; j++)
        {
            // Create a y DebugTile to label the current y column
            newYTile = Instantiate(_yCounterTilePrefab, _DebugGridObjectTransform);

            //Setup DebugTile Behavior
            newYTile.GetComponent<DebugTileBehavior>().SetCamera(_eventCamera);
            newYTile.GetComponent<DebugTileBehavior>().SetIndex(-1, j);
            newYTile.GetComponent<DebugTileBehavior>().SetValue(j.ToString());

            //calculate the node's local position
            Vector3 localPosition = _unityGrid.CellToLocal(new Vector3Int(-1, j, 0));

            //offset the node
            localPosition += _gridOffset;

            //Create a new DebugNode and add it to our debugNode collection 
            _debugNodes.Add(new Vector2Int(-1, j), new DebugNode(-1, j, newYTile, localPosition));
        }
    }



    [TabGroup("Debug/Tabgroup", "Functions")]
    [BoxGroup("Debug")]
    [Button("Destroy Debug Grid")]
    private void DestroyDebugGrid()
    {
        //clear the debug node data
        _debugNodes.Clear();

        //delete each visual debug object of the previous grid
        if (_DebugGridObjectTransform.childCount > 0)
        {
            //note the number of child objects we have
            int childCount = _DebugGridObjectTransform.childCount;

            //create our childCollection
            List<GameObject> childObject = new List<GameObject>();

            //destroy all child objects, stepping from the last to the first
            for (int i = childCount - 1; i >= 0; i--)
            {
                //Destroy the objects at the end of the frame if we're in play mode
                if (Application.isPlaying)
                    Destroy(_DebugGridObjectTransform.GetChild(i).gameObject);

                //Otherwise, destroy them NOW if we're working in edit mode
                else DestroyImmediate(_DebugGridObjectTransform.GetChild(i).gameObject);
            }
        }
    }



    [BoxGroup("Setup")]
    [Button("Build Grid")]
    private void BuildPathGrid()
    {
        //Clear any old pathNode data
        if (_pathNodes.Count > 0)
        {
            //clear all grid utils
            _pathNodes.Clear();
        }

        //Build a new pathing grid
        _pathGrid = new PathGrid(_gridWidth, _gridHeight, _unityGrid, _gridOffset);


        //Now cache each new gridNode (nodes are value data types)...
        for (int i = 0; i < _gridWidth; i++)
        {
            for (int j = 0; j < _gridHeight; j++)
            {
                //cache the current node for clarity
                PathNode currentNode = _pathGrid.GetCell(i, j);

                //Generate the current vector2 index for clarity
                Vector2Int dictionaryIndex = new Vector2Int(i, j);

                //Add the node to the pathNode collection (for easier iteration later)
                _pathNodes.Add(dictionaryIndex, currentNode);
            }
        }

        //Update the tileManager's data with the newly generated grid
        _tileBehaviorManager.UpdateTileData(this);

        //Update the grid's walkability data
        ReadTileWalkability();

        //Build the debug utilites associated with this new grid
        GenerateDebugGrid();
    }




    //[BoxGroup("Setup")]
    //[Button("Read Walkability Data")]
    private void ReadTileWalkability()
    {
        Dictionary<Vector2Int, TileBehavior> tileBehaviors = _tileBehaviorManager.GetTileData();

        foreach (KeyValuePair<Vector2Int, TileBehavior> tileEntry in tileBehaviors)
        {
            //setup a temp node that's a copy of the node we're about to update
            PathNode node = _pathNodes[tileEntry.Key];

            Debug.Log($"Behavior's Walkability: {tileEntry.Value.IsWalkable()}");

            //apply the update to the copy
            node._isWalkable = tileEntry.Value.IsWalkable();

            //replace the old node with the updated copy
            _pathNodes[tileEntry.Key] = node;

            Debug.Log($"new node's Walkability: {node._isWalkable}");

            //Also be certain to reflect the change on our grid
            _pathGrid.UpdateCell(tileEntry.Key, node);
        }
    }




    //Externals
    [TabGroup("Debug/Tabgroup", "Functions")]
    [Button("GetLocalCellPosition")]
    public Vector3 GetLocalCellPosition(int x, int y)
    {
        Vector3Int cellPosition = new Vector3Int(x, y, 0);
        Vector3 localPosition = _unityGrid.CellToLocal(cellPosition) + _gridOffset;

        //log position
        QuickLogger.ConditionalLog(_isDebugActive, this, $"Cell ({x},{y}) Local: {localPosition}");

        return localPosition;
    }

    public Vector3 GetLocalCellPosition((int,int) xy)
    {
        return GetLocalCellPosition(xy.Item1, xy.Item2);
    }


    [TabGroup("Debug/Tabgroup", "Functions")]
    [Button("Debug/GetWorldCellPosition")]
    public Vector3 GetWorldCellPositon(int x, int y)
    {
        Vector3Int cellPosition = new Vector3Int(x, y, 0);
        Vector3 worldPosition = _unityGrid.CellToWorld(cellPosition) + _gridOffset;

        //log position
        QuickLogger.ConditionalLog(_isDebugActive, this, $"Cell ({x},{y}) World: {worldPosition}");

        return worldPosition;
    }
    
    public Vector3 GetWorldCellPositon((int,int) xy)
    {
        return GetWorldCellPositon(xy.Item1, xy.Item2);
    }


    public Dictionary<Vector2Int, PathNode> GetPathNodes()
    {
        return _pathNodes;
    }



    //Debugging
    public int GetScriptID()
    {
        return GetInstanceID();
    }

    public string GetScriptName()
    {
        return name;
    }



}

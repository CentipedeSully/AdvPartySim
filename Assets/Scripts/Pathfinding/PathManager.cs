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



[Serializable]
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
    [TabGroup("Setup", "Parameters")]
    [SerializeField] private int _gridWidth;

    [TabGroup("Setup", "Parameters")]
    [SerializeField] private int _gridHeight;

    [TabGroup("Setup", "Parameters")]
    [SerializeField] private Vector3 _gridOffset = new Vector3(0, 1.5f, 0);

    [TabGroup("Setup","References")]
    [SerializeField] private Grid _unityGrid;

    [TabGroup("Setup", "References")]
    [SerializeField] private Transform _DebugGridObjectTransform;

    [TabGroup("Setup", "References")]
    [SerializeField] private GameObject _positionDebugTilePrefab;

    [TabGroup("Setup", "References")]
    [SerializeField] private GameObject _xCounterTilePrefab;

    [TabGroup("Setup", "References")]
    [SerializeField] private GameObject _yCounterTilePrefab;

    [TabGroup("Setup", "References")]
    [SerializeField] private Camera _eventCamera;

    [TabGroup("Grid", "Nodes")]
    [SerializeField] List<PathNode> _pathNodes = new();
    private PathGrid _pathGrid;

    [TabGroup("Grid", "Debug Nodes")]
    [SerializeField] private List<DebugNode> _debugNodes = new();

    //[SerializeField] private Dictionary<int,IPathAgent> _agents;
    [Space]
    [SerializeField] private bool _isDebugActive = true; 

    [Button("Build Grid")]
    private void DefaultSizedButton()
    {
        BuildPathGrid();
    }

    //Monobehaviours




    //Internals
    private void BuildDebugUtilities()
    {
        //declare our temp object
        GameObject newDebugTile = null;


        //For each node on the grid...
        for (int i = 0; i < _gridWidth; i++)
        {
            for (int j = 0; j < _gridHeight; j++)
            {
                //cache the current node for clarity
                PathNode currentNode = _pathGrid.GetCell(i, j);

                //Add the node to the pathNode collection (for easier iteration later)
                _pathNodes.Add(currentNode);

                //Create a new debug tile
                newDebugTile = Instantiate(_positionDebugTilePrefab, _DebugGridObjectTransform);

                //Setup DebugTile Behavior
                newDebugTile.GetComponent<DebugTileBehavior>().SetCamera(_eventCamera);
                newDebugTile.GetComponent<DebugTileBehavior>().SetIndex(i,j);
                newDebugTile.GetComponent<DebugTileBehavior>().SetValue($"({i},{j})");

                //Create a new DebugNode and add it to our debugNode collection 
                _debugNodes.Add(new DebugNode(currentNode, newDebugTile));
            }
        }

        //delcare the variables for our x & y indicator tiles
        GameObject newXTile = null;
        GameObject newYTile = null;

        //Label the x rows
        for (int i=0; i < _gridWidth; i++)
        {
            // Create an x DebugTile to label the current row 
            newXTile = Instantiate(_xCounterTilePrefab, _DebugGridObjectTransform);

            //Setup DebugTile Behavior
            newXTile.GetComponent<DebugTileBehavior>().SetCamera(_eventCamera);
            newXTile.GetComponent<DebugTileBehavior>().SetIndex(i, -1);
            newXTile.GetComponent<DebugTileBehavior>().SetValue(i.ToString());

            //calculate the node's local position
            Vector3 localPosition = _unityGrid.CellToLocal( new Vector3Int(i,-1,0));

            //offset the node
            localPosition += _gridOffset;

            //Create a new DebugNode and add it to our debugNode collection 
            _debugNodes.Add(new DebugNode(i, -1, newXTile, localPosition));
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
            _debugNodes.Add(new DebugNode(-1, j, newYTile, localPosition));
        }
    }




    //Externals
    public void BuildPathGrid()
    {
        if (_pathGrid != null)
        {
            //clear all grid utils
            _pathNodes.Clear();

            //delete each visual debug object of the previous grid
            foreach (DebugNode node in _debugNodes)
            {
                //if NOT in play mode, destory immediately (for editor debugging)
                if (!Application.isPlaying)
                    DestroyImmediate(node._visualTile);

                //else destroy on the game's own time
                else
                    Destroy(node._visualTile);
            }

            //clear all the debug nodes
            _debugNodes.Clear();

        }

        //Build the pathing grid
        _pathGrid = new PathGrid(_gridWidth, _gridHeight, _unityGrid, _gridOffset);

        //Build the debug utilites associated with this new grid
        BuildDebugUtilities();
    }


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

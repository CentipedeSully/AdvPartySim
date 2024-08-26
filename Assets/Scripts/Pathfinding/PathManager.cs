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
    private int _width;
    private int _height;

    public PathGrid(int width, int height, Grid unityGrid)
    {
        _width = width; 
        _height = height;
        _unityGrid = unityGrid;

        _grid = new PathNode[width,height];

        for (int i =0; i< width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                //Set each index
                _grid[i, j]._index = (i, j);

                //Set each world position
                _grid[i, j]._localPosition = _unityGrid.CellToLocal(new Vector3Int(i,j,0));

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

    
} 

public class PathManager : SerializedMonoBehaviour
{
    //Declarations
    [SerializeField] private Grid _unityGrid;
    [SerializeField] private Transform _DebugGridObjectTransform;
    [SerializeField] private GameObject _positionDebugTilePrefab;
    [SerializeField] List<PathNode> _pathNodes = new();
    private PathGrid _pathGrid;
    private List<DebugNode> _debugNodes = new();
    [SerializeField] private int _gridWidth;
    [SerializeField] private int _gridHeight;
    //[SerializeField] private Dictionary<int,IPathAgent> _agents;

    [Button("Build Grid")]
    private void DefaultSizedButton()
    {
        Debug.Log("button Activated");
        BuildPathGrid();
    }

    //Monobehaviours




    //Internals
    private void BuildDebugUtilities()
    {
        //declare our temp object
        GameObject newDebugTile = null;

        //declare


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

                //Create a new DebugNode and add it to our debugNode collection 
                _debugNodes.Add(new DebugNode(currentNode, newDebugTile));
            }
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

        }

        //Build the pathing grid
        _pathGrid = new PathGrid(_gridWidth, _gridHeight, _unityGrid);

        //Build the debug utilites associated with this new grid
        BuildDebugUtilities();
    }



    //Debugging




}

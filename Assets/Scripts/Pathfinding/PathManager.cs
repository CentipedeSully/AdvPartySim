using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System;
using System.Dynamic;
using UnityEditorInternal;
using Unity.VisualScripting;



public interface IPathAgent
{

}


[Serializable]
public struct GridNode
{
    public (int,int) _index;
    public Vector3 _localPosition;
    public int _terrainTraversalCost;
    public bool _isWalkable;
    public Dictionary<int, IPathAgent> _occupants;

    public GridNode(bool defaultWalkability = false)
    {
        _index = (-1,-1);
        _localPosition = Vector3.zero;
        _terrainTraversalCost = 0;
        _isWalkable = defaultWalkability;
        _occupants = new Dictionary<int, IPathAgent>();
    }
}




public class PathingGrid
{
    private GridNode[,] _grid;
    private Grid _unityGrid;
    private Vector3 _gridOffset;
    private int _width;
    private int _height;

    public PathingGrid(int width, int height, Grid unityGrid, Vector3 offset)
    {
        _width = width; 
        _height = height;
        _unityGrid = unityGrid;
        _gridOffset = offset;

        _grid = new GridNode[width,height];

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
    
    public GridNode GetCell(int x, int y)
    {
        if (!IsValidX(x))
        {
            Debug.LogError($"x index {x} out of grid path grid range");
            return default;
        }

        else if (!IsValidY(y))
        {
            Debug.LogError($"y index {y} out of grid path grid range");
            return default;
        }

        else
            return _grid[x, y];
    }

    public GridNode GetCell((int,int) xyPair)
    {
        return GetCell(xyPair.Item1, xyPair.Item2);
    }

    public void UpdateCell(int x,int y, GridNode updatedNode)
    {

        if (!IsValidX(x))
            Debug.LogError($"x index {x} out of grid path grid range. Ignoring update request on index ({x},{y})");

        else if (!IsValidY(y))
        {
            Debug.LogError($"y index {y} out of grid path grid range. Ignoring update request on index ({x},{y})");
        }

        else 
            _grid[x, y] = updatedNode;
    }

    public void UpdateCell((int,int) xyPair, GridNode updatedNode)
    {
        UpdateCell(xyPair.Item1, xyPair.Item2, updatedNode);
    }

    public void UpdateCell(Vector2Int xyPair, GridNode updatedNode)
    {
        UpdateCell(xyPair.x, xyPair.y, updatedNode);
    }


    public bool IsIndexValid(int x, int y)
    {
        return IsValidX(x) && IsValidY(y);
    }

    public bool IsIndexValid((int,int) xypair)
    {
        return IsIndexValid(xypair.Item1, xypair.Item2);
    }

    public bool IsIndexValid(Vector2Int xyPair)
    {
        return IsIndexValid(xyPair.x, xyPair.y);
    }


    private bool IsValidY(int y)
    {
        return (y >= 0) && (y < _height);
    }

    private bool IsValidX(int x)
    {
        return (x >= 0) && (x < _width);
    }
}



[Serializable]
public struct DebugNode
{
    public GameObject _visualTile;
    public int _xPosition;
    public int _yPosition;

    public DebugNode(GridNode trueNode, GameObject visualTileObject)
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


[Serializable]
public struct PathNode
{
    public Vector2Int _index;
    public Vector2Int _parentIndex;

    public int _gCost;
    public int _hCost;
    public int _fCost;

    public PathNode(Vector2Int index, Vector2Int parentIndex, int gCost, int hCost)
    {
        _index = index;
        _parentIndex = parentIndex;

        _gCost = gCost;
        _hCost = hCost;
        _fCost = _gCost + _hCost;
    }

    public void UpdateParentAndTraversalCost(Vector2Int newParent, int gCost, int hCost)
    {
        _gCost = gCost;
        _hCost = hCost;
        _fCost = _gCost + _hCost;

        _parentIndex = newParent;
    }
}



public static class Cardinal
{
    //Declarations
    private static Vector2Int _north = new Vector2Int(0, 1);

    private static Vector2Int _northEast = new Vector2Int(1, 1);

    private static Vector2Int _east = new Vector2Int(1, 0);

    private static Vector2Int _southEast = new Vector2Int(1, -1);

    private static Vector2Int _south = new Vector2Int(0, -1);

    private static Vector2Int _southWest = new Vector2Int(-1, -1);

    private static Vector2Int _west = new Vector2Int(-1, 0);

    private static Vector2Int _northWest = new Vector2Int(-1, 1);


    //Externals
    public static Vector2Int N() { return _north; }

    public static Vector2Int NE() { return _northEast; }

    public static Vector2Int E() { return _east; }

    public static Vector2Int SE() { return _southEast; }

    public static Vector2Int S() { return _south; }

    public static Vector2Int SW() { return _southWest; }

    public static Vector2Int W() { return _west; }

    public static Vector2Int NW() { return _northWest; }

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

    [TabGroup("Setup/Tabgroup", "Parameters")]
    [SerializeField] private int _straightTraveralCost= 10;

    [TabGroup("Setup/Tabgroup", "Parameters")]
    [SerializeField] private int _diagonalTraversalCost = 14; 




    [TabGroup("Setup/Tabgroup", "References")]
    [SerializeField] private Grid _unityGrid;

    [TabGroup("Setup/Tabgroup", "References")]
    [SerializeField] private TileBehaviorManager _tileBehaviorManager;

    



    [BoxGroup("Grid")]
    [SerializeField]
    private PathingGrid _pathingGrid;

    [TabGroup("Grid/Tabgroup", "Nodes")]
    [SerializeField] Dictionary<Vector2Int, GridNode> _gridNodes = new();

    [BoxGroup("Debug Grid")]
    [SerializeField] private DebugGridManager _debugGridManager;


    //[SerializeField] private Dictionary<int,IPathAgent> _agents;
    [BoxGroup("Debug")]
    [SerializeField] private bool _isDebugActive = true;






    //Monobehaviours
    private void Start()
    {
        BuildPathGrid();
    }



    //Internals
    [BoxGroup("Setup")]
    [Button("Build Grid")]
    private void BuildPathGrid()
    {
        //Clear any old pathNode data
        if (_gridNodes.Count > 0)
        {
            //clear all grid utils
            _gridNodes.Clear();
        }

        //Build a new pathing grid
        _pathingGrid = new PathingGrid(_gridWidth, _gridHeight, _unityGrid, _gridOffset);


        //Now cache each new gridNode (nodes are value data types)...
        for (int i = 0; i < _gridWidth; i++)
        {
            for (int j = 0; j < _gridHeight; j++)
            {
                //cache the current node for clarity
                GridNode currentNode = _pathingGrid.GetCell(i, j);

                //Generate the current vector2 index for clarity
                Vector2Int dictionaryIndex = new Vector2Int(i, j);

                //Add the node to the pathNode collection (for easier iteration later)
                _gridNodes.Add(dictionaryIndex, currentNode);
            }
        }

        //Update the tileManager's data with the newly generated grid
        _tileBehaviorManager.UpdateTileData(this);

        //Update the grid's walkability data
        ReadTileWalkability();

        //Send the new grid data to the debugGridManager to render a new debug grid
        _debugGridManager.SetGridNodes(_gridNodes, new Vector2Int(_gridWidth,_gridHeight),_gridOffset);
    }


    private void ReadTileWalkability()
    {
        Dictionary<Vector2Int, TileBehavior> tileBehaviors = _tileBehaviorManager.GetTileData();

        foreach (KeyValuePair<Vector2Int, TileBehavior> tileEntry in tileBehaviors)
        {
            //setup a temp node that's a copy of the node we're about to update
            GridNode node = _gridNodes[tileEntry.Key];

            Debug.Log($"Behavior's Walkability: {tileEntry.Value.IsWalkable()}");

            //apply the update to the copy
            node._isWalkable = tileEntry.Value.IsWalkable();

            //replace the old node with the updated copy
            _gridNodes[tileEntry.Key] = node;

            Debug.Log($"new node's Walkability: {node._isWalkable}");

            //Also be certain to reflect the change on our grid
            _pathingGrid.UpdateCell(tileEntry.Key, node);
        }
    }


    [TabGroup("Debug/Tabgroup", "Pathing")]
    [Button("Calculate Cell Distance")]
    private int CalculateCellDistance(Vector2Int start, Vector2Int destination)
    {
        //create our distance variablbe
        int distance = 0;

        //Get the differences of each index element
        int xDiff = destination.x - start.x;
        int yDiff = destination.y - start.y;

        /*Long Solution
        //While each element BOTH hold at least 1 remaining distance step
        while (xDiff != 0 && yDiff != 0)
        {
            //add a diagonal step to our distance
            distance += _diagonalTraversalCost;

            //Calculate a step in the xDifference's opposite direction
            int xOpposite = -1 * (int)Mathf.Sign(xDiff);

            //step x towards 0
            xDiff += xOpposite;

            //Calculate a step in the yDifference's opposite direction
            int yOpposite = -1 * (int)Mathf.Sign(yDiff);

            //step y towards 0
            yDiff += yOpposite;
        }
        */


        //convert our elements into positive values. 
        xDiff = Math.Abs(xDiff);
        yDiff = Math.Abs(yDiff);

        //Find the smallest value of the two 
        int diagonalSteps = Mathf.Min(xDiff, yDiff);

        //apply the diagonal steps values 
        distance += diagonalSteps * _diagonalTraversalCost;

        //reduce both differences by the diagonal steps taken 
        xDiff -= diagonalSteps;
        yDiff -= diagonalSteps;

        //at least one of these is zero. Get the largest of the two
        int remainingDistanceSteps = Mathf.Max(xDiff, yDiff);

        //Add the remaining Steps as straight distance steps
        distance += remainingDistanceSteps * _straightTraveralCost;

        //return our result ^_^
        return distance;

    }


    [TabGroup("Debug/Tabgroup", "Pathing")]
    [Button("Find Path")]
    private List<Vector2Int> FindPath(Vector2Int start, Vector2Int destination)
    {
        List<Vector2Int> returnPath = new();
        Dictionary<Vector2Int,PathNode> avaiableNodes = new();
        Dictionary<Vector2Int, PathNode> traversedNodes = new();


        //Stop if the start is off the grid
        if (!_pathingGrid.IsIndexValid(start))
        {
            QuickLogger.ConditionalLog(_isDebugActive, this, $"Starting Index ({start}) is out of grid range. Returning empty path");
            return returnPath;
        }

        //stop if the destination is off grid
        else if (!_pathingGrid.IsIndexValid(destination))
        {
            QuickLogger.ConditionalLog(_isDebugActive, this, $"Destination ({destination}) is out of grid range. Returning empty path");
            return returnPath;
        }

        //stop if the start isn't on a walkable cell
        else if (!_gridNodes[start]._isWalkable)
        {
            QuickLogger.Warn(this, $"Caution. Attempted to path from an unwalkable starting point ${start}. Returning empty path");
            return returnPath;
        }

        //stop if the destination isn't on a walkable cell
        else if (!_gridNodes[destination]._isWalkable)
        {
            QuickLogger.Warn(this, $"Caution. Attempted to path into an unwalkable destination point ${destination}. Returning empty path");
            return returnPath;

            //Possible Feature: Adjust destination into nearest walkable point
            //...
        }


        //Create an invalid index to represent a parentless node
        Vector2Int invalidIndex = new Vector2Int(-1, -1);

        //create the first node
        PathNode startNode = new PathNode(start, invalidIndex, 0, CalculateCellDistance(start, destination));

        //Add the first node to our closedNode list
        traversedNodes.Add(start,startNode);

        //Begin the recursive trace. Return a path if it exists
        returnPath = BuildPath(startNode, destination, avaiableNodes, traversedNodes);

        //return whatever came back
        return returnPath;

    }

    private List<Vector2Int> BuildPath(PathNode currentNode, Vector2Int destination, Dictionary<Vector2Int, PathNode> openNodes, Dictionary<Vector2Int, PathNode> closedNodes)
    {
        //Check if we're at the destination
        if (currentNode._index == destination)
        {
            //get the path
            List<Vector2Int> pathList = TraceBackPath(currentNode, closedNodes);

            //sort the path list into a "start-to-destination" format
            pathList.Reverse();

            //Visualize our hard work to the gridManager
            VisualizeDebugPathData(pathList, openNodes, closedNodes);

            //return our hard work
            return pathList;
        }

        //Setup utils for easier neighbor iteration
        List<PathNode> neighbors = new();
        Vector2Int[] possibleNeighborIndexes = new Vector2Int[8];

        //Calculate all possible directional neighbor indexes
        possibleNeighborIndexes[0] = currentNode._index + Cardinal.N();
        possibleNeighborIndexes[1] = currentNode._index + Cardinal.NE();
        possibleNeighborIndexes[2] = currentNode._index + Cardinal.E();
        possibleNeighborIndexes[3] = currentNode._index + Cardinal.SE();
        possibleNeighborIndexes[4] = currentNode._index + Cardinal.S();
        possibleNeighborIndexes[5] = currentNode._index + Cardinal.SW();
        possibleNeighborIndexes[6] = currentNode._index + Cardinal.W();
        possibleNeighborIndexes[7] = currentNode._index + Cardinal.NW();


        //detect all walkable, nontraversed, cells around the currentNode
        foreach (Vector2Int neighborIndex in possibleNeighborIndexes)
        {
            //ignore the cell at this index if it's off the grid
            if (!_pathingGrid.IsIndexValid(neighborIndex))
                continue;

            //ignore the cell at this index if it's not walkable
            if (!_gridNodes[neighborIndex]._isWalkable)
                continue;

            //ignore the cell at this index if it's already within our CLOSED Nodes
            if (closedNodes.ContainsKey(neighborIndex))
                continue;


            //calculate the gCost from our current node To this neighbor
            int newPathToNeighborCostG = currentNode._gCost + CalculateCellDistance(currentNode._index, neighborIndex);


            //is this neighbor already in our openNodes collection
            if (openNodes.ContainsKey(neighborIndex))
            {
                //did we find a shorter path to this neighbor?
                if (newPathToNeighborCostG < openNodes[neighborIndex]._gCost)
                {
                    //create a new node with updated pathCosts 
                    PathNode newUpdatedNode = new PathNode(neighborIndex, currentNode._index, newPathToNeighborCostG, CalculateCellDistance(neighborIndex,destination));

                    //replace the preexisting neighbor's node entry with this new node
                    openNodes[neighborIndex] = newUpdatedNode;
                }
            }

            //otherwise, add it to the openNodes collection
            else
            {
                //create the new neighbor node. 
                PathNode newNeighborNode = new PathNode(neighborIndex, currentNode._index, newPathToNeighborCostG, CalculateCellDistance(neighborIndex, destination));

                //add this new node into our openNodes collection
                openNodes.Add(neighborIndex, newNeighborNode);
            }

        }


        //Stop looking if we're out of options, since we still haven't reached our destination
        if (openNodes.Count == 0)
        {
            QuickLogger.ConditionalLog(_isDebugActive, this, "No path found");
            return null;
        }

        //We still have nodes to search. 
        else
        {

            //setup a place to hold our cheapest node
            PathNode cheapestNode = default;
            bool isFirstIteration = true;

            //Look at each node that's available for traversal
            foreach (KeyValuePair<Vector2Int,PathNode> entry in openNodes)
            {
                //the first node we see is by default the cheapest
                if (isFirstIteration)
                {
                    cheapestNode = entry.Value;
                    isFirstIteration = false;
                }
                    

                else
                {
                    //change our selection of we've found a cheaper node
                    if (cheapestNode._fCost > entry.Value._fCost)
                        cheapestNode = entry.Value;
                }
            }

            //Remove our node from the openNode collection
            openNodes.Remove(cheapestNode._index);

            //add our node to the closedNode collection
            closedNodes.Add(cheapestNode._index, cheapestNode);


            //We've found the cheapest node that's currently available to us. 
            //return a new iteration of this function with our updated utilities
            return BuildPath(cheapestNode, destination, openNodes, closedNodes);

        }
    }

    private List<Vector2Int> TraceBackPath(PathNode currentNode, Dictionary<Vector2Int,PathNode> traversedNodes)
    {
        //start the return list
        List<Vector2Int> path = new();

        //add our current node to the list
        path.Add(currentNode._index);

        //set the next index to our current node's parent
        Vector2Int nextIndex = currentNode._parentIndex;
        

        //Keep tracing until we reach the node that has no valid parent
        while (_pathingGrid.IsIndexValid(nextIndex))
        {
            //Add the parent's index to our path, since it's a valid cell
            path.Add(nextIndex);

            //step our index to this valid node's parentIndex
            nextIndex = traversedNodes[nextIndex]._parentIndex;
        }

        return path;

    }

    private void VisualizeDebugPathData(List<Vector2Int> foundPath, Dictionary<Vector2Int, PathNode> inspectedNodes, Dictionary<Vector2Int, PathNode> exploredNodes)
    {
        //update the inspected nodes' ui
        foreach (KeyValuePair<Vector2Int, PathNode> entry in inspectedNodes)
            _debugGridManager.UpdatePathingVisual(entry.Value, PathingVisualState.Inspected);


        //update the explored nodes' ui
        foreach (KeyValuePair<Vector2Int, PathNode> entry in exploredNodes)
            _debugGridManager.UpdatePathingVisual(entry.Value, PathingVisualState.Explored);


        //check our explored nodes
        foreach (KeyValuePair<Vector2Int, PathNode> entry in exploredNodes)
        {
            //if the explored node is a part of our path, update its debug visual as InPath
            if (foundPath.Contains(entry.Key))
                _debugGridManager.UpdatePathingVisual(entry.Value, PathingVisualState.InPath);
        }

    }


    [TabGroup("Debug/Tabgroup", "Pathing")]
    [Button("Clear Debug Pathing Data")]
    private void ClearDebugPathingGrid()
    {
        foreach (KeyValuePair<Vector2Int, GridNode> entry in _gridNodes)
            _debugGridManager.ClearPathNodeUi(entry.Key);
    }






    //Externals
    [TabGroup("Debug/Tabgroup", "Grid")]
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


    [TabGroup("Debug/Tabgroup", "Grid")]
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


    public Dictionary<Vector2Int, GridNode> GetPathNodes()
    {
        return _gridNodes;
    }

    public bool IsWalkable(Vector2Int index)
    {
        return _gridNodes[index]._isWalkable;
    }

    public List<Vector2Int> CreatePath(Vector2Int start, Vector2Int destination)
    {
        return FindPath(start, destination);
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;



public enum GridDisplayMode
{
    Unset,
    Default,
    Pathing
}

public enum PathingVisualState
{
    Reset,
    Inspected,
    Explored,
    InPath
}


[CreateAssetMenu(fileName = "Debug Grid Manager", menuName = "Scriptable Objects/Debug Grid Manager")]
[InlineEditor]
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

    /*
    [TabGroup("Setup/Tabgroup", "References")]
    [TabGroup("Setup/Tabgroup", "References")]
    [TabGroup("Setup/Tabgroup", "References")]
    [TabGroup("Setup/Tabgroup", "References")]
    [TabGroup("Setup/Tabgroup", "References")]
    [TabGroup("Setup/Tabgroup", "References")]
    [TabGroup("Setup/Tabgroup", "References")]
    [TabGroup("Setup/Tabgroup", "References")]
    */



    [TabGroup("Setup/Tabgroup", "Default UI")]
    [SerializeField] private Color _defaultTileColor;

    [TabGroup("Setup/Tabgroup", "Default UI")]
    [SerializeField] private Color _xLabelColor;

    [TabGroup("Setup/Tabgroup", "Default UI")]
    [SerializeField] private Color _yLabelColor;

    [TabGroup("Setup/Tabgroup", "Default UI")]
    [SerializeField] private Color _defaultTextColor;


    [TabGroup("Setup/Tabgroup", "Pathing UI")]
    [SerializeField] private Color _walkableTileColor;

    [TabGroup("Setup/Tabgroup", "Pathing UI")]
    [SerializeField] private Color _unwalkableTileColor;


    [TabGroup("Setup/Tabgroup", "Pathing UI")]
    [SerializeField] private Color _tileInspectedColor;

    [TabGroup("Setup/Tabgroup", "Pathing UI")]
    [SerializeField] private Color _tileExploredColor;

    [TabGroup("Setup/Tabgroup", "Pathing UI")]
    [SerializeField] private Color _tileInReturnPathColor;


    [TabGroup("Setup/Tabgroup", "Pathing UI")]
    [SerializeField] private Color _pathingTextColor;

    [TabGroup("Setup/Tabgroup", "Pathing UI")]
    [SerializeField] private Color _arrowColor;




    [BoxGroup("Grid")]
    [ReadOnly]
    [SerializeField] private GridDisplayMode _gridDisplayMode = GridDisplayMode.Unset;
    [TabGroup("Grid/Tabgroup", "Grid Nodes")]
    [SerializeField] private Dictionary<Vector2Int, GridNode> _gridNodes = new();

    [TabGroup("Grid/Tabgroup", "Debug Tiles")]
    [SerializeField] private Dictionary<Vector2Int, DebugTileBehavior> _debugTileBehaviors = new();








    //Internals ================================================================================
    private void ShowDefaultTileDisplay()
    {
        //look at each tile's debugBehavior
        foreach (KeyValuePair<Vector2Int, DebugTileBehavior> entry in _debugTileBehaviors)
        {
            //show the object's default ui
            entry.Value.ShowDefaultUi();

            //reset the text colors to default
            entry.Value.SetTextColor(_defaultTextColor);

            //is the current tile a label?
            if (entry.Value.IsLabel())
            {
                //Set Text Color
                entry.Value.SetTextColor(_defaultTextColor);

                //if the x index is off the grid, then this tile is an xLabel
                if (entry.Value.GetIndex().x < 0)
                    entry.Value.SetTileColor(_xLabelColor);

                //otherwise, its a yLabel
                else
                    entry.Value.SetTileColor(_yLabelColor);
            }

            //if the tile isn't a label, default it's color
            else
                entry.Value.SetTileColor(_defaultTileColor);
        }
    }

    private void HideDefaultTileDisplay()
    {
        foreach (KeyValuePair<Vector2Int, DebugTileBehavior> entry in _debugTileBehaviors)
        {
            //ignore labels-- tiles that exist outside of the grid
            if (_gridNodes.ContainsKey(entry.Key))
            {
                //hide the behavior's default object
                entry.Value.HideDefaultUi();
            }

        }
    }

    private void ShowPathingDisplay()
    {
        //look at each tile's debugBehavior
        foreach (KeyValuePair<Vector2Int, DebugTileBehavior> entry in _debugTileBehaviors)
        {
            //ignore labels that exist outside of the grid
            if (_gridNodes.ContainsKey(entry.Key))
            {

                //if the tile is walkable,
                if (_gridNodes[entry.Key]._isWalkable)
                {
                    //visually color the tile as walkable
                    entry.Value.SetTileColor(_walkableTileColor);

                    //Set the tile's text color
                    entry.Value.SetTextColor(_pathingTextColor);

                    //Set the tile's arrow Color
                    entry.Value.SetArrowColor(_arrowColor);

                    //show the tile's pathing ui data
                    entry.Value.ShowPathingData();
                }

                //otherwise if the tile isn't walkable
                else
                {
                    //only visually color the tile as unwalkable
                    entry.Value.SetTileColor(_unwalkableTileColor);
                }
            }
        }
    }

    private void HidePathingDisplay()
    {
        //look at each tile's debugBehavior
        foreach (KeyValuePair<Vector2Int, DebugTileBehavior> entry in _debugTileBehaviors)
        {
            //ignore labels that exist outside of the grid
            if (_gridNodes.ContainsKey(entry.Key))
            {
                //Hide each tile's costs and step node ui
                entry.Value.HidePathingData();
            }

        }
    }





    [BoxGroup("Grid")]
    [Button("Regenerate Debug Grid")]
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
            newBehavior.SetTileColor(_defaultTileColor);

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
            xTileBehavior.SetupBehaviorAsLabel(index, i.ToString(), _mainCamera);

            //Set the color
            xTileBehavior.SetTileColor(_xLabelColor);

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
            yTileBehavior.SetTileColor(_yLabelColor);

            //calculate the object's local position
            Vector3 localPosition = _unityGrid.CellToLocal(new Vector3Int(-1, j, 0));

            //offset the object
            localPosition += _gridOffset;

            //place the tile at the unityGrid's index position
            newYTile.transform.localPosition = localPosition;
        }


        //Lasts, enter the default mode
        SetDisplayMode(GridDisplayMode.Default);


    }


    [BoxGroup("Grid")]
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


    [BoxGroup("Grid")]
    [Button("Set Display Mode")]
    public void SetDisplayMode(GridDisplayMode newMode)
    {
        switch (newMode)
        {
            case GridDisplayMode.Default:
                //Hide any other mode data
                if (_gridDisplayMode == GridDisplayMode.Pathing)
                    HidePathingDisplay();

                //update the mode
                _gridDisplayMode = newMode;

                //show the graphical data
                ShowDefaultTileDisplay();
                break;


            case GridDisplayMode.Pathing:

                //Hide any other mode data
                if (_gridDisplayMode == GridDisplayMode.Default)
                    HideDefaultTileDisplay();

                //update mode
                _gridDisplayMode = newMode;

                //show graphical data
                ShowPathingDisplay();
                break;
        }


    }


    public void UpdatePathingVisual(PathNode node, PathingVisualState selectedVisual)
    {
        //Clarify the index
        Vector2Int index = node._index;

        //If the node exists on the grid
        if (_debugTileBehaviors.ContainsKey(index))
        {
            //clarify our selected behavior
            DebugTileBehavior behavior = _debugTileBehaviors[index];

            //Update this cell's ui if we aren't resetting the cell
            if (selectedVisual != PathingVisualState.Reset)
                behavior.SetPathDataText(node);
            
            //Otherwise, reset the cell's ui data
            else behavior.ClearPathDataText();



            //Only recolor the cell if we're in the Pathing visual mode
            if (_gridDisplayMode == GridDisplayMode.Pathing)
            {
                //then Recolor the cell based on the given visual state
                switch (selectedVisual)
                {
                    //Recolor the tile to walkable
                    case PathingVisualState.Reset:
                        behavior.SetTileColor(_walkableTileColor);
                        break;

                    //Recolor the tile as inspected
                    case PathingVisualState.Inspected:
                        behavior.SetTileColor(_tileInspectedColor);
                        break;

                    //Recolor the tile as explored
                    case PathingVisualState.Explored:
                        behavior.SetTileColor(_tileExploredColor);
                        break;

                    //recolor the tile as within the return path
                    case PathingVisualState.InPath:
                        behavior.SetTileColor(_tileInReturnPathColor);
                        break;
                }
            }
        }
    }

    public void ClearPathNodeUi(Vector2Int index)
    {
        //clarify the debug tile
        DebugTileBehavior behavior = _debugTileBehaviors[index];

        //chear the ui text
        behavior.ClearPathDataText();

        //if the tile isn't a label
        if (!behavior.IsLabel())
        {
            //color the tile based on its walkability
            if (behavior.IsWalkable())
                behavior.SetTileColor(_walkableTileColor);
            else
                behavior.SetTileColor(_unwalkableTileColor);
        }
            
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

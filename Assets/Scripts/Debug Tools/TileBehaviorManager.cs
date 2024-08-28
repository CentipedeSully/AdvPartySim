using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;


public class TileBehaviorManager : SerializedMonoBehaviour
{
    [BoxGroup("Setup")]
    [TabGroup("Setup/Tabgroup","References")]
    [SerializeField] private PathManager _pathManager;


    [TabGroup("Manage Tiles", "Manage Collection")]
    [SerializeField] private Dictionary<Vector2Int, TileBehavior> _behaviorTiles = new();


    [InlineButton("DestroyChildrenTiles", "Destroy Tiles")]
    [TabGroup("Manage Tiles", "Destroy Tiles")]
    [SerializeField] private bool _enableDestruction = false;



    //Monobehaviours





    //Internals
    private void DestroyChildrenTiles()
    {
        if (!Application.isPlaying && _enableDestruction)
        {
            List<GameObject> children = new List<GameObject>();
            int childCount = transform.childCount;

            //Collect all child objects
            for (int i = 0; i < childCount; i++)
                children.Add(transform.GetChild(i).gameObject);


            //Destroy them, starting from the latest entry to the earliest
            for (int i = childCount - 1; i >= 0; i--)
            {
                //mark the selection fro destruction
                DestroyImmediate(children[i]);
            }
        }
        else if (Application.isPlaying)
        {
            Debug.LogWarning("Caution. 'DestoryChildren' may only be called in editmode.");
        }
        else
            Debug.LogWarning($"Attempted to Destroy all children of {this.name} while safetly is on. Be careful.");

    }



    [BoxGroup("Setup")]
    [Button("Detect Tiles")]
    private void CollectChildrenTiles()
    {

        //declare a new temporary collection
        List<GameObject> children = new List<GameObject>();

        //Note the amount of children detected
        int childCount = transform.childCount;

        //Fetch this object's children. All BehaviorTiles lie here as children
        for (int i = 0; i < childCount; i++)
            children.Add(transform.GetChild(i).gameObject);


        //Get a reference to all nodes on our grid
        Dictionary<Vector2Int, GridNode> pathNodeCollection = _pathManager.GetPathNodes();


        //Look at each node on our grid...
        foreach(KeyValuePair<Vector2Int,GridNode> nodeEntry in pathNodeCollection)
        {
            //Get the position of the current node
            Vector3 localNodePosition = nodeEntry.Value._localPosition;

            //Look for a tile that holds a similar local position to our current node.
            //Our tilemap used to build the tileBehavior tiles should align with our unityGrid
            foreach (GameObject child in children)
            {
                if (child.transform.localPosition == localNodePosition)
                {
                    TileBehavior behavior = child.GetComponent<TileBehavior>();

                    //Save this tile, and pair the object to our node's grid index 
                    _behaviorTiles.Add(nodeEntry.Key, behavior);

                    //stop looking for a matching child
                    break;
                }
            }
        }
    }



    [TabGroup("Manage Tiles", "Manage Collection")]
    [Button("Clear Tile Data")]
    private void ClearCollectedTileData()
    {
        _behaviorTiles.Clear();
    }





    //Externals
    public void UpdateTileData(PathManager pathManager)
    {
        //Reset this reference, just in case
        _pathManager = pathManager;

        //Clear any pre existing tile data
        ClearCollectedTileData();

        //Detect all aligning tilebehaviors
        CollectChildrenTiles();
    }

    public Dictionary<Vector2Int,TileBehavior> GetTileData()
    {
        return _behaviorTiles;
    }

    public TileBehavior GetTileData(Vector2Int index)
    {
        return _behaviorTiles[index];
    }



    //Debugging






    //[TabGroup("Manage Tiles", "Clear Tiles")]

}

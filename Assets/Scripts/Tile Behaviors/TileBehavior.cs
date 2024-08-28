using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public enum TileType
{
    Undefined,
    Grass,
    Dirt,
    Water
}

public class TileBehavior : MonoBehaviour
{
    //Declarations
    [SerializeField] private TileType _tileType;
    [SerializeField] private bool _isWalkable;
    [SerializeField] private GameObject _debugVisualObject;
    [SerializeField] private bool _hideVisualOnPlay = true;


    //Monobehaviours
    private void Start()
    {
        if (_hideVisualOnPlay)
            _debugVisualObject.SetActive(false);
    }



    //Internals




    //Externals
    public TileType GetTileType()
    {
        return _tileType; 
    }

    public bool IsWalkable()
    {
        return _isWalkable;
    }



    //Debugging




}

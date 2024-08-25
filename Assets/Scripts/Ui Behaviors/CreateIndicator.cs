using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;


public  class CreateIndicator: SerializedMonoBehaviour
{
    //Declarations
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private Dictionary<IndicatorType, GameObject> _indicatorPrefabs = new();



    //monobehaviours
    private void Start()
    {
        InitializeCamera();
    }



    //Internals
    private void InitializeCamera()
    {
        _mainCamera= GameObject.Find("Main Camera").GetComponent<Camera>();
    }



    //Externals
    public GameObject CreateNewIndicator(Transform parent, IndicatorType type)
    {
        GameObject returnObject = null;
        if (_indicatorPrefabs.ContainsKey(type))
        {
            //create a new object from the matching prefab
            returnObject = GameObject.Instantiate(_indicatorPrefabs[type], parent, false);

            //Get the new object's indicator component
            IndicatorBehavior _indicatorBehavior = returnObject.GetComponent<IndicatorBehavior>();

            //Set the main camera of the indicator before sending it away
            _indicatorBehavior.SetupIndicator(_mainCamera);

            return returnObject;
        }

        else
        {
            Debug.LogError($"Failed to find Indicator prefab of type '{type}'. returning empty gameObject");
            return returnObject;
        }
    }



    //Debugging
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IndicatorBehavior : MonoBehaviour
{
    //Declarations
    [SerializeField] private Canvas _canvas;
    [SerializeField] private Animator _animator;

    [SerializeField] private Text _text;
    [SerializeField] private Color _textColor;

    [SerializeField] private Text _shadowText;
    [SerializeField] private Color _shadowColor;

    
    [SerializeField] private float _maxLifetime;
    private float _currentLifetime;
    private bool _showIndicator = false;


    //Monobehaviours
    private void Update()
    {
        TickLifetime();
    }




    //Internals
    private void TickLifetime()
    {
        if (_showIndicator)
        {
            _currentLifetime += Time.deltaTime;

            if (_currentLifetime >= _maxLifetime)
                Destroy(gameObject);
        }
    }




    //Externals
    public void SetupIndicator(Camera mainCamera)
    {
        //set the indicator's render camera
        _canvas.worldCamera = mainCamera;
    }

    public void SetText(string text)
    {
        //Set the text
        _text.text = text;
        _shadowText.text = text;
    }

    public void SetColors(Color mainColor, Color shadowColor)
    {
        //Set colors
        _textColor = mainColor;
        _shadowColor = shadowColor;
    }

    public void ShowIndicator()
    {
        _animator.SetBool("isShowing", true);
        _showIndicator = true;
    }




    //Debugging



}

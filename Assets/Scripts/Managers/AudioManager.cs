using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class AudioManager : MonoBehaviour
{
    //Declarations
    [Header("Sounds & Music Sources")]
    [SerializeField] private List<AudioSource> _ambientSoundSources = new();
    [SerializeField] private AudioSource _calmMusic;

    [Header("Debug Utils")]
    [SerializeField] private bool _isDebugActive = false;
    [SerializeField] private bool _DEBUG_playAmbience_cmd = false;
    [SerializeField] private bool _DEBUG_stopAmbience_cmd = false;
    [SerializeField] private bool _DEBUG_playCalmMusic_cmd = false;
    [SerializeField] private bool _DEBUG_stopCalmMusic_cmd = false;


    //Monobehaviours
    private void Update()
    {
        ListenForDebugCommands();
    }


    //Internals




    //Externals
    public void PlayAmbientSounds()
    {
        foreach (AudioSource source in _ambientSoundSources) { source.Play(); }
    }

    public void StopAmbientSounds() 
    {
        foreach (AudioSource source in _ambientSoundSources) { source.Stop(); }
    }

    public void PlayCalmMusic()
    {
        _calmMusic.Play();
    }

    public void StopCalmMusic()
    {
        _calmMusic.Stop();
    }



    //Debugging
    private void ListenForDebugCommands()
    {
        if (_isDebugActive)
        {
            if (_DEBUG_playAmbience_cmd)
            {
                _DEBUG_playAmbience_cmd = false;
                PlayAmbientSounds();
            }

            if (_DEBUG_stopAmbience_cmd)
            {
                _DEBUG_stopAmbience_cmd = false;
                StopAmbientSounds();
            }

            if (_DEBUG_playCalmMusic_cmd)
            {
                _DEBUG_playCalmMusic_cmd = false;
                PlayCalmMusic();
            }

            if (_DEBUG_stopCalmMusic_cmd)
            {
                _DEBUG_stopCalmMusic_cmd = false;
                StopCalmMusic();
            }
        }
    }
}

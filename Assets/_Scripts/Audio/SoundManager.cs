using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : Singleton<SoundManager>
{
    [SerializeField] private AudioSource effectsSource;

    private void Start()
    {
        effectsSource = GetComponent<AudioSource>();
    }

    public void PlaySound(AudioClip clip)
    {
        if (effectsSource.isPlaying) effectsSource.Stop();
        effectsSource.PlayOneShot(clip);
    }
}
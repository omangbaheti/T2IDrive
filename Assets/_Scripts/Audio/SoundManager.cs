using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : Singleton<SoundManager>
{
    public AudioSource EffectsSource => effectsSource;
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

    public void ResetPitch(float duration)
    {
        StartCoroutine(ResetPitchAfterDelay(duration));
    }
    
    private IEnumerator ResetPitchAfterDelay(float duration) 
    {
        yield return new WaitForSeconds(0.1f);
        effectsSource.pitch = 1;
    }
}
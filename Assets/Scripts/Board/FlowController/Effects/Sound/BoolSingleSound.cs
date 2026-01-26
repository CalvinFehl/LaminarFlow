using SmallHedge.SoundManager;
using System;
using UnityEngine;

public class BoolSingleSound : FloatFlexibleSound
{
    [Tooltip("Can be null, it will then use the default audio source")]
    [SerializeField] AudioSource audioSource;

    [Header("Settings")]
    [SerializeField] string soundName = "RocketEngage";
    [SerializeField] float volume = 0.2f;

    public override void HandleSound(float power, float deltaTime)
    {
        if (power > 0.001f)
        {
            PlaySound(soundName, audioSource, volume);
        }
    }

    void PlaySound(string sound, AudioSource audioSource = null, float volume = 1f)
    {
        if (sound == null) return;
        SoundType soundType;
        if (Enum.TryParse(sound, true, out soundType))
        {
            SoundManager.PlaySound(soundType, audioSource, volume);
        }
    }
}
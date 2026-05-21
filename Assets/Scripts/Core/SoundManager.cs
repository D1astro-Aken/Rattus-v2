using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    private AudioSource source;
    public static SoundManager instance { get; private set; }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        source = GetComponent<AudioSource>();
    }

    private void Start()
    {
        UpdateVolume();
    }

    public void UpdateVolume()
    {
        if (SettingsManager.instance != null)
        {
            source.volume = SettingsManager.instance.sfxVolume;
        }
    }

    public void PlaySound(AudioClip _sound)
    {
        if (SettingsManager.instance != null)
        {
            source.volume = SettingsManager.instance.sfxVolume;
        }
        source.PlayOneShot(_sound);
    }

    public void PlayOneOf(params AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0 || source == null) return;
        List<AudioClip> valid = new List<AudioClip>(clips.Length);
        foreach (var c in clips)
        {
            if (c != null) valid.Add(c);
        }
        if (valid.Count == 0) return;
        int idx = Random.Range(0, valid.Count);
        if (SettingsManager.instance != null)
        {
            source.volume = SettingsManager.instance.sfxVolume;
        }
        source.PlayOneShot(valid[idx]);
    }
}

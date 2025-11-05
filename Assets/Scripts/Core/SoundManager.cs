using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
private AudioSource source;
public static SoundManager instance { get; private set; }
    private void Awake()
    {
        instance = this;
        source = GetComponent<AudioSource>();
    }

    public void PlaySound(AudioClip _sound)
    {
        source.PlayOneShot(_sound);
    }

    // Přehrání jednoho náhodného klipu z více variant
    public void PlayOneOf(params AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0 || source == null) return;
        // Filtrovat null hodnoty
        List<AudioClip> valid = new List<AudioClip>(clips.Length);
        foreach (var c in clips)
        {
            if (c != null) valid.Add(c);
        }
        if (valid.Count == 0) return;
        int idx = Random.Range(0, valid.Count);
        source.PlayOneShot(valid[idx]);
    }
}

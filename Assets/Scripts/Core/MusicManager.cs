using UnityEngine;

public class MusicManager : MonoBehaviour
{
    private AudioSource source;
    public static MusicManager instance { get; private set; }

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
            source.volume = SettingsManager.instance.musicVolume;
        }
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (source == null || clip == null) return;
        source.clip = clip;
        source.loop = loop;
        if (SettingsManager.instance != null)
        {
            source.volume = SettingsManager.instance.musicVolume;
        }
        source.Play();
    }

    public void StopMusic()
    {
        if (source != null)
        {
            source.Stop();
        }
    }

    public void PauseMusic()
    {
        if (source != null)
        {
            source.Pause();
        }
    }

    public void UnpauseMusic()
    {
        if (source != null)
        {
            source.UnPause();
        }
    }
}

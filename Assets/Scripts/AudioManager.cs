// Create: Assets/Scripts/Audio/AudioManager.cs
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;
    
    [Header("Audio Sources")]
    public AudioSource sfxSource;
    public AudioSource musicSource;
    
    [Header("SFX Clips")]
    public AudioClip plankGrow;
    public AudioClip plankLand;
    public AudioClip successSound;
    public AudioClip failSound;
    public AudioClip buttonClick;
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void PlayPlankGrow()
    {
        if (sfxSource != null && plankGrow != null)
        {
            if (!sfxSource.isPlaying || sfxSource.clip != plankGrow)
            {
                sfxSource.clip = plankGrow;
                sfxSource.loop = true;
                sfxSource.Play();
            }
        }
    }
    
    public void StopPlankGrow()
    {
        if (sfxSource != null && sfxSource.clip == plankGrow)
        {
            sfxSource.Stop();
            sfxSource.loop = false;
        }
    }
    
    public void PlayPlankLand()
    {
        PlaySFX(plankLand);
    }
    
    public void PlaySuccess()
    {
        PlaySFX(successSound);
    }
    
    public void PlayFail()
    {
        PlaySFX(failSound);
    }
    
    public void PlayButtonClick()
    {
        PlaySFX(buttonClick);
    }
    
    void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }
}

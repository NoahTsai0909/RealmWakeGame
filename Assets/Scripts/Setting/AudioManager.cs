using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Mixer")]
    [SerializeField] private AudioMixer mainMixer;


    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource jingleSource;

    private Dictionary<AudioClip, float> clipLastPlayedTime = new Dictionary<AudioClip, float>();
    private float soundCooldown = 0.05f;
    private Coroutine currentMusicRoutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        SetMasterVolume(PlayerPrefs.GetFloat("MasterVolume", 0.75f));
        SetMusicVolume(PlayerPrefs.GetFloat("MusicVolume", 0.75f));
        SetSFXVolume(PlayerPrefs.GetFloat("SFXVolume", 0.75f));
    }

    public void PlayJingle(AudioClip clip, float volumeMultiplier = 1f)
    {
        if (clip != null && jingleSource != null)
        {
            jingleSource.PlayOneShot(clip, volumeMultiplier);
        }
    }

    public void PlaySFX(AudioClip clip, float volumeMultiplier = 1f)
    {
        if (clip != null && sfxSource != null)
        {
            float currentTime = Time.unscaledTime;

            if (clipLastPlayedTime.TryGetValue(clip, out float lastTime))
            {
                if (currentTime - lastTime < soundCooldown)
                {
                    return;
                }
            }
            clipLastPlayedTime[clip] = currentTime;
            sfxSource.PlayOneShot(clip, volumeMultiplier);
        }
    }

    public void PlayMusicWithFade(AudioClip clip, float fadeDuration = 2f)
    {
        if (clip == null || musicSource == null) return;

        // Don't restart the routine if the exact same song is already playing
        if (musicSource.clip == clip && musicSource.isPlaying) return;

        if (currentMusicRoutine != null)
        {
            StopCoroutine(currentMusicRoutine);
        }

        currentMusicRoutine = StartCoroutine(MusicLoopRoutine(clip, fadeDuration));
    }

    private IEnumerator MusicLoopRoutine(AudioClip clip, float fadeDuration)
    {
        if (musicSource.isPlaying && musicSource.volume > 0)
        {
            float startVolume = musicSource.volume;
            float transitionElapsed = 0f;

            while (transitionElapsed < fadeDuration)
            {
                transitionElapsed += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, transitionElapsed / fadeDuration);
                yield return null;
            }
            musicSource.Stop();
        }

        musicSource.loop = false;
        musicSource.clip = clip;

        float playDuration = clip.length - (fadeDuration * 2);
        if (playDuration < 0) playDuration = 0.1f;

        while (true)
        {
            musicSource.volume = 0f;
            musicSource.Play();

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
                yield return null;
            }
            musicSource.volume = 1f;
            yield return new WaitForSecondsRealtime(playDuration);

            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                yield return null;
            }

            musicSource.volume = 0f;
            musicSource.Stop();
        }
    }

    public void StopMusicWithFade(float fadeDuration = 2f)
    {
        if (musicSource == null || !musicSource.isPlaying) return;

        if (currentMusicRoutine != null)
        {
            StopCoroutine(currentMusicRoutine);
        }

        currentMusicRoutine = StartCoroutine(FadeOutAndStopRoutine(fadeDuration));
    }

    private System.Collections.IEnumerator FadeOutAndStopRoutine(float fadeDuration)
    {
        float startVolume = musicSource.volume;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeDuration);
            yield return null;
        }

        musicSource.volume = 0f;
        musicSource.Stop();
        musicSource.clip = null;
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null || musicSource == null) return;
        if (musicSource.clip == clip) return; 

        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void SetMasterVolume(float sliderValue)
    {
        float clampValue = Mathf.Clamp(sliderValue, 0.0001f, 1f);
        mainMixer.SetFloat("MasterVolume", Mathf.Log10(clampValue) * 20f);
        PlayerPrefs.SetFloat("MasterVolume", sliderValue);
    }

    public void SetMusicVolume(float sliderValue)
    {
        float clampValue = Mathf.Clamp(sliderValue, 0.0001f, 1f);
        mainMixer.SetFloat("MusicVolume", Mathf.Log10(clampValue) * 20f);
        PlayerPrefs.SetFloat("MusicVolume", sliderValue);
    }

    public void SetSFXVolume(float sliderValue)
    {
        float clampValue = Mathf.Clamp(sliderValue, 0.0001f, 1f);
        mainMixer.SetFloat("SFXVolume", Mathf.Log10(clampValue) * 20f);
        PlayerPrefs.SetFloat("SFXVolume", sliderValue);
    }
}
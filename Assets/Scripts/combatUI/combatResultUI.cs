using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CombatResultUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private RectTransform victoryBannerRect;
    [SerializeField] private RectTransform defeatBannerRect;
    [SerializeField] private ParticleSystem sparksParticles;

    [Header("Atmosphere References")]
    [SerializeField] private Volume globalVolume; //Global Volume here

    [Header("Animation Settings")]
    [SerializeField] private float popDuration = 0.2f;
    [SerializeField] private float lingerDuration = 1.0f;
    [SerializeField] private float floatSpeed = 3f;
    [SerializeField] private float floatAmplitude = 10f;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip victorySound;
    [SerializeField] private AudioClip defeatSound;
    [SerializeField] private float volume = 1f;

    private RectTransform activeBannerRect;
    private Vector2 originalBannerPos;
    private bool isFloating = false;
    private bool isVictoryResult = false;

    private ColorAdjustments colorAdjustments;

    private void Awake()
    {
        panelCanvasGroup.alpha = 0f;
        victoryBannerRect.gameObject.SetActive(false);
        defeatBannerRect.gameObject.SetActive(false);

        // Cache the Color Adjustments override if the volume exists
        if (globalVolume != null && globalVolume.profile.TryGet(out colorAdjustments))
        {
            colorAdjustments.saturation.value = 0f;
        }
    }

    public void ShowResult(bool isVictory)
    {
        gameObject.SetActive(true);
        isVictoryResult = isVictory;

        victoryBannerRect.gameObject.SetActive(isVictory);
        defeatBannerRect.gameObject.SetActive(!isVictory);

        activeBannerRect = isVictory ? victoryBannerRect : defeatBannerRect;

        // Start defeat banner massive, start victory banner at 0
        activeBannerRect.localScale = isVictory ? Vector3.zero : Vector3.one * 3f;
        originalBannerPos = activeBannerRect.anchoredPosition;
        if (isVictory && victorySound != null)
        {
            AudioManager.Instance.PlayJingle(victorySound, volume);
        }
        else if (!isVictory && defeatSound != null)
        {
            AudioManager.Instance.PlayJingle(defeatSound, volume);
        }

        StartCoroutine(AnimateBannerSequence());
    }

    private IEnumerator AnimateBannerSequence()
    {
        // 1. Fast Background Fade
        float elapsed = 0f;
        while (elapsed < 0.15f)
        {
            elapsed += Time.deltaTime;
            panelCanvasGroup.alpha = Mathf.Lerp(0f, 0.8f, elapsed / 0.15f);
            yield return null;
        }

        // Only play sparks on Victory
        if (isVictoryResult && sparksParticles != null) sparksParticles.Play();

        // 2. The Pop or Slam Animation
        elapsed = 0f;
        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popDuration;

            if (isVictoryResult)
            {
                // Bouncy Pop
                float scale = Mathf.Sin(t * Mathf.PI * 0.6f) * 1.15f;
                if (t > 0.6f) scale = Mathf.Lerp(1.15f, 1.0f, (t - 0.6f) * 2.5f);
                activeBannerRect.localScale = new Vector3(scale, scale, 1f);
            }
            else
            {
                // Heavy Slam (from 3.0 scale down to 1.0)
                if (GameplayManager.Instance.EnableScreenShake)
                {
                    float easeOut = 1f - (1f - t) * (1f - t) * (1f - t);
                    float scale = Mathf.Lerp(3f, 1f, easeOut);
                    activeBannerRect.localScale = new Vector3(scale, scale, 1f);
                }
            }

            yield return null;
        }
        activeBannerRect.localScale = Vector3.one;

        // 3. Linger and Color Drain
        isFloating = true;
        float lingerElapsed = 0f;
        while (lingerElapsed < lingerDuration)
        {
            lingerElapsed += Time.deltaTime;

            // Plunge saturation to -100 over the first 0.5 seconds of the linger
            if (!isVictoryResult && colorAdjustments != null)
            {
                colorAdjustments.saturation.value = Mathf.Lerp(0f, -100f, lingerElapsed / 0.5f);
            }

            yield return null;
        }
        isFloating = false;

        // 4. Vanish and Restore Color
        elapsed = 0f;
        while (elapsed < 0.15f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.15f;
            float easeIn = t * t;

            activeBannerRect.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, easeIn);
            panelCanvasGroup.alpha = Mathf.Lerp(0.8f, 0f, t);

            // Snap the color back to reality as the banner vanishes
            if (!isVictoryResult && colorAdjustments != null)
            {
                colorAdjustments.saturation.value = Mathf.Lerp(-100f, 0f, t);
            }

            yield return null;
        }

        // Safety check to ensure color is perfectly reset
        if (colorAdjustments != null) colorAdjustments.saturation.value = 0f;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (isFloating && activeBannerRect != null)
        {
            float newY = originalBannerPos.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            activeBannerRect.anchoredPosition = new Vector2(originalBannerPos.x, newY);
        }
    }
}
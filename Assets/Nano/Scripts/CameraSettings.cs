using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI; // for Image
using DG.Tweening;   // add this at the top




public class CameraSettings : MonoBehaviour
{
    public static CameraSettings Instance { get; private set; }
    
    /// death thing
    [Header("Post FX - Death")]
    [SerializeField] private Volume deathVolume;     // assign your DeathFX Global Volume here
    [SerializeField] private float deathFadeDuration = 1f;
    private Coroutine deathFxRoutine;

    [Header("Crosshair FX")]
    [SerializeField] private Image crosshair;                 // drag your crosshair Image here
    [SerializeField] private Color crosshairNormal = Color.white;
    [SerializeField] private Color crosshairTarget = Color.red;
    [SerializeField] private Vector3 crosshairNormalScale = Vector3.one;
    [SerializeField] private Vector3 crosshairTargetScale = Vector3.one * 1.25f;
    [SerializeField] private float crosshairLerpSpeed = 10f;

    private Coroutine crosshairCo;
    private bool crosshairIsTargetable = false;


    private Vector3 originalPos;
    private Camera mainCamera;
    private float originalFOV;

    private Coroutine shakeRoutine;
    private Coroutine zoomRoutine;
    private Coroutine slowMotionRoutine;

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (deathVolume == null)
        {
            deathVolume = GetComponentInChildren<Volume>();
        }
        mainCamera = Camera.main;
        originalPos = transform.localPosition;
        originalFOV = mainCamera.fieldOfView;
    }

    // 🔹 Shake
    public void StartShake(float duration, float magnitude)
    {
        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(Shake(duration, magnitude));
    }

    private IEnumerator Shake(float duration, float magnitude)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float strength = Mathf.Lerp(magnitude, 0f, elapsed / duration); // ease out

            float x = Random.Range(-1f, 1f) * strength;
            float y = Random.Range(-1f, 1f) * strength;

            transform.localPosition = originalPos + new Vector3(x, y, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPos;
        shakeRoutine = null;
    }

    // 🔹 Zoom
    public void StartZoom(float targetFOV, float duration)
    {
        if (zoomRoutine != null) StopCoroutine(zoomRoutine);
        zoomRoutine = StartCoroutine(Zoom(targetFOV, duration));
    }

    private IEnumerator Zoom(float targetFOV, float duration)
    {
        float elapsed = 0f;
        float startFOV = mainCamera.fieldOfView;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float smoothT = t * t * (3f - 2f * t); // smoothstep easing

            mainCamera.fieldOfView = Mathf.Lerp(startFOV, targetFOV, smoothT);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Snap back after effect
        mainCamera.fieldOfView = originalFOV;
        zoomRoutine = null;
    }

    // 🔹 Slow Motion (safe, never stuck)
    public void StartSlowMotion(float targetScale, float duration)
    {
        if (slowMotionRoutine != null) StopCoroutine(slowMotionRoutine);
        slowMotionRoutine = StartCoroutine(SlowMotionRoutine(targetScale, duration));
    }

    private IEnumerator SlowMotionRoutine(float targetScale, float duration)
    {
        float originalTimeScale = 1f; // always return to normal speed
        Time.timeScale = targetScale;

        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale = originalTimeScale;
        slowMotionRoutine = null;
    }
    //get called when the player is zero hp
     public void OnPlayerDeath()
    {
        StartDeathFxFade(1f);
    }

    private void StartDeathFxFade(float target)
    {
        if (deathVolume == null)
        {
            Debug.LogWarning("[CameraSettings] Death Volume not assigned.");
            return;
        }

        if (deathFxRoutine != null) StopCoroutine(deathFxRoutine);
        deathFxRoutine = StartCoroutine(FadeDeathFx(deathVolume.weight, target, deathFadeDuration));
    }

    private IEnumerator FadeDeathFx(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime; // works even if Time.timeScale = 0
            deathVolume.weight = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        deathVolume.weight = to;
        deathFxRoutine = null;
    }
    /////./////


    //////crosshair + outline//////
    private RectTransform _crosshairRect;
    private Tween crosshairTween;

    // Optional defaults (tweak in Inspector or here)
    [SerializeField] private Outline.Mode outlineMode = Outline.Mode.OutlineAll;
    [SerializeField] private Color outlineColor = Color.yellow;
    [SerializeField] private float outlineWidth = 5f;

    private Outline _currentOutline;

    private void Start()
    {
        if (crosshair != null)
        {
            _crosshairRect = crosshair.rectTransform;
            _crosshairRect.localScale = crosshairNormalScale;
        }
    }

    /// <summary>
    /// Scale crosshair and toggle enemy outline. Pass `outlineTarget` when hittable is under cursor.
    /// </summary>
    public void SetCrosshairTargetable(bool isTargetable, Outline outlineTarget = null)
    {
        // Crosshair scale (DOTween)
        if (_crosshairRect != null)
        {
            Vector3 target = isTargetable ? crosshairTargetScale : crosshairNormalScale;
            crosshairTween?.Kill();
            crosshairTween = _crosshairRect.DOScale(target, 0.15f).SetEase(Ease.OutBack);
        }

        // Disable old outline if target changed
        if (_currentOutline != null && _currentOutline != outlineTarget)
        {
            _currentOutline.enabled = false;
            _currentOutline = null;
        }

        // Enable/refresh new outline when targetable
        if (isTargetable && outlineTarget != null)
        {
            _currentOutline = outlineTarget;

            // configure (cheap, runs once per change)
            _currentOutline.OutlineMode = outlineMode;
            _currentOutline.OutlineColor = outlineColor;
            _currentOutline.OutlineWidth = outlineWidth;

            _currentOutline.enabled = true;
        }
        else if (!isTargetable && _currentOutline != null)
        {
            _currentOutline.enabled = false;
            _currentOutline = null;
        }
    }

}

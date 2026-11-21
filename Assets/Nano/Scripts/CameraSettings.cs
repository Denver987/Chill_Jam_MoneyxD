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
    private float slowMotionBaseScale = 1f;
    [SerializeField] private bool enableCameraShake = false;
    [SerializeField] private float crosshairShakeScale = 2f;
    private Vector2 crosshairRestPosition;

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
        if (Time.timeScale == 0f) return;
        if (_crosshairRect == null && crosshair != null)
        {
            _crosshairRect = crosshair.rectTransform;
            crosshairRestPosition = _crosshairRect.anchoredPosition;
        }
        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(Shake(Mathf.Min(duration, 0.15f), magnitude * 0.5f));
    }

    private IEnumerator Shake(float duration, float magnitude)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float strength = Mathf.Lerp(magnitude, 0f, elapsed / duration); // ease out

            float x = Random.Range(-1f, 1f) * strength;
            float y = Random.Range(-1f, 1f) * strength;

            if (enableCameraShake) transform.localPosition = originalPos + new Vector3(x, y, 0f);
            if (_crosshairRect != null)
                _crosshairRect.anchoredPosition = crosshairRestPosition + new Vector2(x, y) * crosshairShakeScale;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        transform.localPosition = originalPos;
        if (_crosshairRect != null) _crosshairRect.anchoredPosition = crosshairRestPosition;
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
        if (Time.timeScale <= 0f || IsModalOpen()) return;
        if (slowMotionRoutine == null) slowMotionBaseScale = Time.timeScale;
        if (slowMotionRoutine != null) StopCoroutine(slowMotionRoutine);
        slowMotionRoutine = StartCoroutine(SlowMotionRoutine(targetScale, duration));
    }

    private IEnumerator SlowMotionRoutine(float targetScale, float duration)
    {
        Time.timeScale = targetScale;

        yield return new WaitForSecondsRealtime(duration);

        // Do not un-freeze a card selection or pause menu that opened while
        // the slow motion was playing.
        if (!IsModalOpen() && Mathf.Approximately(Time.timeScale, targetScale))
        {
            Time.timeScale = slowMotionBaseScale;
        }
        slowMotionRoutine = null;
    }

    private static bool IsModalOpen()
    {
        var canvas = CanvasSingleton.Instance;
        if (canvas == null) return false;
        if (canvas.CardsSelection != null && canvas.CardsSelection.activeSelf) return true;
        if (canvas.PausePanel != null && canvas.PausePanel.activeSelf) return true;
        if (canvas.die != null && canvas.die.activeSelf) return true;

        if (canvas.player != null)
        {
            var input = canvas.player.GetComponent<InputSystem>();
            if (input != null && input.IsPaused) return true;
        }

        return false;
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
            crosshairRestPosition = _crosshairRect.anchoredPosition;
            _crosshairRect.localScale = crosshairNormalScale;
        }
    }

    /// <summary>
    /// Scale crosshair and toggle enemy outline. Pass `outlineTarget` when hittable is under cursor.
    /// </summary>
    public void SetCrosshairTargetable(bool isTargetable, Outline outlineTarget = null)
    {
        if (crosshairIsTargetable == isTargetable && _currentOutline == outlineTarget) return;
        bool targetabilityChanged = crosshairIsTargetable != isTargetable;
        crosshairIsTargetable = isTargetable;
        // Crosshair scale (DOTween)
        if (_crosshairRect != null && targetabilityChanged)
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

    /// <summary>
    /// Subtle pulse on crosshair when hitting an enemy or bouncing a shell
    /// </summary>
    public void TriggerHitFeedback()
    {
        if (_crosshairRect == null && crosshair != null)
        {
            _crosshairRect = crosshair.rectTransform;
            crosshairRestPosition = _crosshairRect.anchoredPosition;
        }
        if (_crosshairRect != null)
        {
            crosshairTween?.Kill();
            _crosshairRect.localScale = crosshairNormalScale * 1.35f;
            crosshairTween = _crosshairRect.DOScale(crosshairNormalScale, 0.12f).SetEase(Ease.OutQuad).SetUpdate(true);
        }
    }

    private void OnDestroy()
    {
        crosshairTween?.Kill();
        if (Instance == this) Instance = null;
    }

}

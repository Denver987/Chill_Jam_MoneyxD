using System.Collections;
using UnityEngine;

public class CameraSettings : MonoBehaviour
{
    public static CameraSettings Instance { get; private set; }

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

    // 🔹 Slow Motion
    public void StartSlowMotion(float targetScale, float duration)
    {
        if (slowMotionRoutine != null) StopCoroutine(slowMotionRoutine);
        slowMotionRoutine = StartCoroutine(SlowMotion(targetScale, duration));
    }

    private IEnumerator SlowMotion(float targetScale, float duration)
    {
        float originalTimeScale = Time.timeScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float smoothT = t * t * (3f - 2f * t);
            Time.timeScale = Mathf.Lerp(originalTimeScale, targetScale, smoothT);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        Time.timeScale = originalTimeScale;
        slowMotionRoutine = null;
    }
}

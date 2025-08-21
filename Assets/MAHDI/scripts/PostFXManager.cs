using UnityEngine;
using UnityEngine.Rendering;

public class PostFXManager : MonoBehaviour
{
    [SerializeField] private Volume deathVolume;   // drag your DeathFX volume here
    [SerializeField] private float fadeDuration = 1f;

    private Coroutine currentRoutine;

    public void OnPlayerDeath()
    {
        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(FadeVolume(deathVolume, deathVolume.weight, 1f));
    }

    public void OnPlayerRespawn()
    {
        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(FadeVolume(deathVolume, deathVolume.weight, 0f));
    }

    private System.Collections.IEnumerator FadeVolume(Volume vol, float from, float to)
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime; // still works if you pause on death
            vol.weight = Mathf.Lerp(from, to, t / fadeDuration);
            yield return null;
        }
        vol.weight = to;
    }
}

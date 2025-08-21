using UnityEngine;

public class KnockbackScreenshake : MonoBehaviour
{
    [Header("Shake Settings")]
    [SerializeField] private float minShakeMagnitude = 0.02f;
    [SerializeField] private float maxShakeMagnitude = 0.15f; 
    public float shakeThreshold = 0.8f; 
    public float autoReleaseDelay = 1f;
    [SerializeField] private AnimationCurve shakeIntensityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private bool isShaking = false;
    private float shakeStartTime = -1f;
    private float dynamicShakeRampUpTime = 2f;

    // Update is called once per frame
    void Update()
    {
        if (isShaking)
        {
            float timeSinceShakeStart = Time.time - shakeStartTime;
            float shakeProgress = Mathf.Clamp01(timeSinceShakeStart / dynamicShakeRampUpTime);

            float intensityMultiplier = shakeIntensityCurve.Evaluate(shakeProgress);
            float currentShakeMagnitude = Mathf.Lerp(minShakeMagnitude, maxShakeMagnitude, intensityMultiplier);
            
            CameraSettings.Instance?.StartShake(0.1f, currentShakeMagnitude);
        }
    }

    public void StartProgressiveShake(float totalDuration)
    {
        if (!isShaking)
        {
            isShaking = true;
            shakeStartTime = Time.time;
            dynamicShakeRampUpTime = totalDuration; 
        }
    }

    public void StopShake()
    {
        isShaking = false;
        shakeStartTime = -1f;
    }


    public bool IsShaking => isShaking;
}

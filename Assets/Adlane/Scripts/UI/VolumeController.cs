using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the master audio volume via AudioListener.volume and persists it in PlayerPrefs.
/// Can be attached directly to any Slider GameObject, or finds Sliders named "Volume Slider" automatically.
/// </summary>
public class VolumeController : MonoBehaviour
{
    public const string VOLUME_PREF_KEY = "MasterVolume";
    private Slider volumeSlider;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitVolume()
    {
        float savedVolume = PlayerPrefs.GetFloat(VOLUME_PREF_KEY, 1f);
        AudioListener.volume = Mathf.Clamp01(savedVolume);
    }

    private void Awake()
    {
        volumeSlider = GetComponent<Slider>();
        float savedVolume = PlayerPrefs.GetFloat(VOLUME_PREF_KEY, 1f);
        AudioListener.volume = Mathf.Clamp01(savedVolume);

        if (volumeSlider != null)
        {
            volumeSlider.value = savedVolume;
            volumeSlider.onValueChanged.RemoveListener(SetVolume);
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }
    }

    private void OnEnable()
    {
        if (volumeSlider != null)
        {
            float savedVolume = PlayerPrefs.GetFloat(VOLUME_PREF_KEY, 1f);
            volumeSlider.value = savedVolume;
            AudioListener.volume = Mathf.Clamp01(savedVolume);
        }
    }

    public void SetVolume(float volume)
    {
        float clamped = Mathf.Clamp01(volume);
        AudioListener.volume = clamped;
        PlayerPrefs.SetFloat(VOLUME_PREF_KEY, clamped);
        PlayerPrefs.Save();
    }
}

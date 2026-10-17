using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MasterVolumeSettings : MonoBehaviour
{
    private const string PreferenceKey = "912.MasterVolume";

    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject panel;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TMP_Text percentageLabel;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RestoreVolume()
    {
        AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(PreferenceKey, 1f));
    }

    private void Awake()
    {
        float volume = Mathf.Clamp01(PlayerPrefs.GetFloat(PreferenceKey, 1f));
        AudioListener.volume = volume;
        volumeSlider.SetValueWithoutNotify(volume);
        RefreshLabel(volume);
        panel.SetActive(false);
        openButton.onClick.AddListener(Open);
        closeButton.onClick.AddListener(Close);
        volumeSlider.onValueChanged.AddListener(SetVolume);
    }

    private void OnDestroy()
    {
        openButton.onClick.RemoveListener(Open);
        closeButton.onClick.RemoveListener(Close);
        volumeSlider.onValueChanged.RemoveListener(SetVolume);
    }

    private void Open() => panel.SetActive(true);
    private void Close() => panel.SetActive(false);

    private void SetVolume(float value)
    {
        float volume = Mathf.Clamp01(value);
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat(PreferenceKey, volume);
        PlayerPrefs.Save();
        RefreshLabel(volume);
    }

    private void RefreshLabel(float value)
    {
        percentageLabel.text = $"%{Mathf.RoundToInt(value * 100)}";
    }
}

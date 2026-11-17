
using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Graphics Quality")]
    [SerializeField] private Button lowButton;
    [SerializeField] private Button mediumButton;
    [SerializeField] private Button highButton;

    [Header("Button Colors")]
    [SerializeField] private Color selectedColor = Color.white;
    [SerializeField] private Color normalColor = Color.gray;

    private SettingsManager settingsManager;

    private void Start()
    {
        settingsManager = SettingsManager.Instance;

        if (settingsManager == null)
        {
            Debug.LogError("[SettingsUI] SettingsManager not found.");
            return;
        }

        // 현재 설정값을 UI에 반영
        ApplyCurrentSettings();

        // 오디오 슬라이더 이벤트 등록
        bgmSlider.onValueChanged.AddListener(OnBgmSliderChanged);
        sfxSlider.onValueChanged.AddListener(OnSfxSliderChanged);

        // 그래픽 품질 버튼 이벤트 등록
        lowButton.onClick.AddListener(OnLowClicked);
        mediumButton.onClick.AddListener(OnMediumClicked);
        highButton.onClick.AddListener(OnHighClicked);

        // 설정 변경 이벤트 구독
        settingsManager.OnSettingsChanged += OnSettingsChanged;
    }

    private void ApplyCurrentSettings()
    {
        SettingsData settings = settingsManager.CurrentSettings;

        bgmSlider.SetValueWithoutNotify(
            settings.audio.bgmVolume
        );

        sfxSlider.SetValueWithoutNotify(
            settings.audio.sfxVolume
        );

        UpdateQualityButtonColors(
            settings.graphics.qualityLevel
        );
    }

    // ==============================
    // Audio
    // ==============================

    private void OnBgmSliderChanged(float value)
    {
        settingsManager.SetBgmVolume(value);
    }

    private void OnSfxSliderChanged(float value)
    {
        settingsManager.SetSfxVolume(value);
    }

    // ==============================
    // Graphics Quality
    // ==============================

    private void OnLowClicked()
    {
        settingsManager.SetQualityLevel(0);
    }

    private void OnMediumClicked()
    {
        settingsManager.SetQualityLevel(1);
    }

    private void OnHighClicked()
    {
        settingsManager.SetQualityLevel(2);
    }

    private void UpdateQualityButtonColors(int selectedLevel)
    {
        SetButtonColor(lowButton, selectedLevel == 0);
        SetButtonColor(mediumButton, selectedLevel == 1);
        SetButtonColor(highButton, selectedLevel == 2);
    }

    private void SetButtonColor(Button button, bool isSelected)
    {
        if (button == null || button.image == null)
            return;

        button.image.color = isSelected
            ? selectedColor
            : normalColor;
    }

    // ==============================
    // Settings Changed
    // ==============================

    private void OnSettingsChanged(SettingsData settings)
    {
        bgmSlider.SetValueWithoutNotify(
            settings.audio.bgmVolume
        );

        sfxSlider.SetValueWithoutNotify(
            settings.audio.sfxVolume
        );

        UpdateQualityButtonColors(
            settings.graphics.qualityLevel
        );
    }

    private void OnDestroy()
    {
        if (bgmSlider != null)
            bgmSlider.onValueChanged.RemoveListener(OnBgmSliderChanged);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.RemoveListener(OnSfxSliderChanged);

        if (lowButton != null)
            lowButton.onClick.RemoveListener(OnLowClicked);

        if (mediumButton != null)
            mediumButton.onClick.RemoveListener(OnMediumClicked);

        if (highButton != null)
            highButton.onClick.RemoveListener(OnHighClicked);

        if (settingsManager != null)
            settingsManager.OnSettingsChanged -= OnSettingsChanged;
    }
}

using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [Header("Popup")]
    [SerializeField] private GameObject settingsPopup;   // Settings_Popup 루트
    [SerializeField] private Button settingsButton;      // 우측 상단 톱니 버튼
    [SerializeField] private Button closeButton;         // 팝업 X 버튼

    [Header("Audio Buttons")]
    [SerializeField] private Button sfxButton;           // Settings_Popup/SFX/Button_SFX
    [SerializeField] private Button bgmButton;           // Settings_Popup/BGM/Button_BGM

    [Header("Audio Icons (optional)")]
    [SerializeField] private Image sfxIconImage;         // 아이콘 Image (선택)
    [SerializeField] private Image bgmIconImage;         // 아이콘 Image (선택)
    [SerializeField] private Sprite sfxOnSprite;
    [SerializeField] private Sprite sfxOffSprite;
    [SerializeField] private Sprite bgmOnSprite;
    [SerializeField] private Sprite bgmOffSprite;

    [Header("SoundManager (optional)")]
    [Tooltip("비워두면 SoundManager.Instance를 사용합니다")]
    [SerializeField] private SoundManager soundManager;

    private const string PREF_SFX_MUTED = "PREF_SFX_MUTED";
    private const string PREF_BGM_MUTED = "PREF_BGM_MUTED";

    private bool _sfxMuted;
    private bool _bgmMuted;

    private void Awake()
    {
        // 필수 레퍼런스 체크
        if (settingsPopup == null) Debug.LogError("[SettingsUI] settingsPopup is not assigned.");
        if (settingsButton == null) Debug.LogError("[SettingsUI] settingsButton is not assigned.");
        if (closeButton == null) Debug.LogError("[SettingsUI] closeButton is not assigned.");

        // 시작 시 팝업은 꺼둔다
        if (settingsPopup != null)
            settingsPopup.SetActive(false);

        // 버튼 이벤트 연결
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenPopup);

        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePopup);

        if (sfxButton != null)
            sfxButton.onClick.AddListener(ToggleSfx);

        if (bgmButton != null)
            bgmButton.onClick.AddListener(ToggleBgm);

        // 저장된 설정 로드
        _sfxMuted = PlayerPrefs.GetInt(PREF_SFX_MUTED, 0) == 1;
        _bgmMuted = PlayerPrefs.GetInt(PREF_BGM_MUTED, 0) == 1;

        // UI는 즉시 반영 (오디오 소스는 Start에서 한 번 더 적용)
        SyncAudioUI();
    }

    private void Start()
    {
        // SoundManager 연결 (인스펙터 우선, 없으면 Instance)
        if (soundManager == null)
            soundManager = SoundManager.Instance;

        ApplyAudioState();
        SyncAudioUI();
    }

    public void OpenPopup()
    {
        if (settingsPopup == null) return;
        settingsPopup.SetActive(true);
        // 열 때도 상태 반영 (혹시 다른 곳에서 바뀌었을 수 있음)
        SyncAudioUI();
    }

    public void ClosePopup()
    {
        if (settingsPopup == null) return;
        settingsPopup.SetActive(false);
    }

    // =====================
    // Audio Toggle
    // =====================

    private void ToggleSfx()
    {
        _sfxMuted = !_sfxMuted;
        PlayerPrefs.SetInt(PREF_SFX_MUTED, _sfxMuted ? 1 : 0);
        PlayerPrefs.Save();

        ApplyAudioState();
        SyncAudioUI();
    }

    private void ToggleBgm()
    {
        _bgmMuted = !_bgmMuted;
        PlayerPrefs.SetInt(PREF_BGM_MUTED, _bgmMuted ? 1 : 0);
        PlayerPrefs.Save();

        ApplyAudioState();
        SyncAudioUI();
    }

    private void ApplyAudioState()
    {
        if (soundManager == null)
        {
            // 씬 로딩 순서 때문에 Start 시점에 없을 수 있음
            soundManager = SoundManager.Instance;
            if (soundManager == null)
            {
                Debug.LogWarning("[SettingsUI] SoundManager not found in scene. Audio toggle will only update UI.");
                return;
            }
        }

        // SoundManager.cs 기준: sfxSource / bgmSource 를 mute로 제어
        if (soundManager.sfxSource != null)
            soundManager.sfxSource.mute = _sfxMuted;

        if (soundManager.bgmSource != null)
            soundManager.bgmSource.mute = _bgmMuted;
    }

    private void SyncAudioUI()
    {
        // 아이콘 스프라이트가 연결되어 있을 때만 교체
        if (sfxIconImage != null && sfxOnSprite != null && sfxOffSprite != null)
            sfxIconImage.sprite = _sfxMuted ? sfxOffSprite : sfxOnSprite;

        if (bgmIconImage != null && bgmOnSprite != null && bgmOffSprite != null)
            bgmIconImage.sprite = _bgmMuted ? bgmOffSprite : bgmOnSprite;
    }
}
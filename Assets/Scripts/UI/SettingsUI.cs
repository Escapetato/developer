using System;
using System.Linq;
using System.Reflection;
using UnityEngine.SceneManagement;
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

    [Header("Auth")]
    [SerializeField] private Button logoutButton;        // Settings_Popup/LogoutButton
    [SerializeField] private string authSceneName = "Auth";

    [Header("Quest")]
    [SerializeField] private GameObject questPopup;

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

        if (logoutButton != null)
            logoutButton.onClick.AddListener(LogoutAndGoToAuthScene);

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

        if (settingsButton != null)
        settingsButton.interactable = false;

        SoundManager.Instance?.PlaySFX("open");
        SyncAudioUI();
    }

    public void ClosePopup()
    {
        if (settingsPopup == null) return;
        settingsPopup.SetActive(false);

        if (settingsButton != null)
        settingsButton.interactable = true;

        SoundManager.Instance?.PlaySFX("close");
    }

    private void Update()
    {
        if (questPopup == null || settingsButton == null) return;

        bool isQuestOpen = questPopup.activeSelf;
        settingsButton.gameObject.SetActive(!isQuestOpen);
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

    // =====================
    // Logout + Scene 이동
    // =====================

    private void LogoutAndGoToAuthScene()
    {
        // 팝업은 우선 닫아주기
        ClosePopup();

        // 1) FirebaseAuth SignOut 시도
        TryFirebaseSignOut();

        // 2) Google Sign-In SignOut/Disconnect 시도
        TryGoogleSignOut();

        // 3) Auth 씬으로 이동
        if (string.IsNullOrEmpty(authSceneName))
        {
            Debug.LogError("[SettingsUI] authSceneName is empty. Set it in Inspector.");
            return;
        }

        SceneManager.LoadScene(authSceneName);
    }

    private static void TryFirebaseSignOut()
    {
        try
        {
            // Firebase.Auth.FirebaseAuth.DefaultInstance.SignOut();
            var firebaseAuthType = FindTypeByFullName("Firebase.Auth.FirebaseAuth");
            if (firebaseAuthType == null) return;

            var defaultInstanceProp = firebaseAuthType.GetProperty("DefaultInstance", BindingFlags.Static | BindingFlags.Public);
            if (defaultInstanceProp == null) return;

            var authInstance = defaultInstanceProp.GetValue(null);
            if (authInstance == null) return;

            var signOutMethod = firebaseAuthType.GetMethod("SignOut", BindingFlags.Instance | BindingFlags.Public);
            if (signOutMethod == null) return;

            signOutMethod.Invoke(authInstance, null);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SettingsUI] Firebase SignOut failed: {e.Message}");
        }
    }

    private static void TryGoogleSignOut()
    {
        try
        {
            // 플러그인별로 타입/메서드명이 다를 수 있어서 몇 가지 케이스를 시도

            // Case A) GoogleSignIn.DefaultInstance.SignOut() / Disconnect()
            var googleSignInType = FindTypeByName("GoogleSignIn");
            if (googleSignInType == null) return;

            var defaultInstanceProp = googleSignInType.GetProperty("DefaultInstance", BindingFlags.Static | BindingFlags.Public);
            var instance = defaultInstanceProp?.GetValue(null);
            if (instance == null) return;

            // SignOut 우선
            var signOut = googleSignInType.GetMethod("SignOut", BindingFlags.Instance | BindingFlags.Public);
            if (signOut != null)
            {
                signOut.Invoke(instance, null);
                return;
            }

            // 없으면 Disconnect
            var disconnect = googleSignInType.GetMethod("Disconnect", BindingFlags.Instance | BindingFlags.Public);
            if (disconnect != null)
            {
                disconnect.Invoke(instance, null);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SettingsUI] Google SignOut failed: {e.Message}");
        }
    }

    private static Type FindTypeByFullName(string fullName)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var t = asm.GetType(fullName, throwOnError: false);
                if (t != null) return t;
            }
            catch { }
        }
        return null;
    }

    private static Type FindTypeByName(string typeName)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var t = asm.GetTypes().FirstOrDefault(x => x.Name == typeName);
                if (t != null) return t;
            }
            catch { }
        }
        return null;
    }
}
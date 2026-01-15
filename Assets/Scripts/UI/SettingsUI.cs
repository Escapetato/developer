using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject settingsPopup;   // Settings_Popup 루트
    [SerializeField] private Button settingsButton;      // 우측 상단 톱니 버튼
    [SerializeField] private Button closeButton;         // 팝업 X 버튼

    private void Awake()
    {
        // 안전장치: 누락되면 콘솔에서 바로 알 수 있게
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
    }

    public void OpenPopup()
    {
        if (settingsPopup == null) return;
        settingsPopup.SetActive(true);
    }

    public void ClosePopup()
    {
        if (settingsPopup == null) return;
        settingsPopup.SetActive(false);
    }
}
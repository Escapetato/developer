using UnityEngine;
using UnityEngine.UI;

public class SideMenuUI : MonoBehaviour
{
    // 어디서든 접근할 수 있게 싱글톤 처리
    public static SideMenuUI Instance { get; private set; }

    [Header("Settings")]
    public GameObject menuContainer; // 접었다 폈다 할 대상 (Left_Category)
    public Button toggleButton;      // 화살표 버튼
    public Transform arrowIcon;      // 화살표 아이콘 (회전시키기 위해)

    private bool isExpanded = false; // 현재 펼쳐져 있는지 여부 (기본값: false = 접힘)

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        if (toggleButton != null)
        {
            toggleButton.onClick.RemoveAllListeners();
            toggleButton.onClick.AddListener(OnClickToggle);
        }

        // "처음엔 접힌 상태로 시작"
        CloseMenu();
    }

    // 토글 버튼 눌렀을 때
    public void OnClickToggle()
    {
        isExpanded = !isExpanded;
        UpdateUI();
    }

    // 메뉴 강제로 접기 (다른 창 열릴 때 호출)
    public void CloseMenu()
    {
        isExpanded = false;
        UpdateUI();
    }

    // 화면 갱신
    private void UpdateUI()
    {
        // 1. 목록 껐다 켜기
        if (menuContainer != null)
            menuContainer.SetActive(isExpanded);

        // 2. 화살표 회전 (펼치면 180도, 접으면 0도)
        if (arrowIcon != null)
        {
            // Z축 회전으로 화살표 방향 뒤집기
            float zRotation = isExpanded ? 180f : 0f;
            arrowIcon.localRotation = Quaternion.Euler(0, 0, zRotation);
        }
    }
}
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

    // ▼▼▼ [추가됨] 사이드 메뉴 안에 들어있는 버튼들을 여기에 등록합니다 ▼▼▼
    [Header("Menu Buttons (여기에 버튼을 드래그하세요)")]
    public Button inventoryButton;   // 인벤토리(가방) 버튼
    public Button collectionButton;  // 도감(책) 버튼
    public Button questButton;       // 퀘스트(느낌표) 버튼
    // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲

    private bool isExpanded = false; // 현재 펼쳐져 있는지 여부 (기본값: false = 접힘)

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        // 1. 화살표(토글) 버튼 기능 연결
        if (toggleButton != null)
        {
            toggleButton.onClick.RemoveAllListeners();
            toggleButton.onClick.AddListener(OnClickToggle);
            // 소리도 여기서 연결하면 편합니다
            toggleButton.onClick.AddListener(() => SoundManager.Instance.PlaySFX("button"));
        }

        // ▼▼▼ [추가됨] 씬이 시작될 때마다 "살아있는 UIManager"를 찾아 버튼에 연결! ▼▼▼

        // (1) 인벤토리 버튼 연결
        if (inventoryButton != null)
        {
            inventoryButton.onClick.RemoveAllListeners();
            inventoryButton.onClick.AddListener(() => {
                UIManager.Instance.OpenInventoryPopup(); // 싱글톤으로 매니저 호출
                SoundManager.Instance.PlaySFX("enter");
            });
        }

        // (2) 도감 버튼 연결
        if (collectionButton != null)
        {
            collectionButton.onClick.RemoveAllListeners();
            collectionButton.onClick.AddListener(() => {
                UIManager.Instance.OpenCollectionPopup();
                SoundManager.Instance.PlaySFX("enter");
            });
        }

        // (3) 퀘스트 버튼 연결
        if (questButton != null)
        {
            questButton.onClick.RemoveAllListeners();
            questButton.onClick.AddListener(() => {
                UIManager.Instance.OpenQuestPopup(); // (UIManager에 OpenQuestPopup 함수가 있어야 함)
                SoundManager.Instance.PlaySFX("enter");
            });
        }
        // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲

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
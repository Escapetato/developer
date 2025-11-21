using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // [필수] TextMeshPro 사용을 위해 네임스페이스 추가

// 게임 내 모든 UI 팝업과 알림창을 중앙에서 관리하는 매니저 클래스
public class UIManager : MonoBehaviour
{
    // 싱글톤 패턴: 어디서든 UIManager.Instance로 접근 가능
    public static UIManager Instance { get; private set; }

    [Header("UI Popups (메인 팝업창들)")]
    // Inspector에서 각 팝업창(Panel) 오브젝트를 연결해야 함
    public GameObject inventoryPopup;   // 인벤토리 창
    public GameObject researchLabPopup; // 연구실 창
    public GameObject storePopup;       // 상점 창
    public GameObject collectionPopup; // 도감 창

    [Header("Alert Popup (단순 알림창)")]
    public GameObject alertPopup;       // 알림창 패널
    public TextMeshProUGUI alertMessageText; // [TMP] 알림 메시지 (예: "돈이 부족합니다")
    public Button alertCloseButton;     // 알림창 닫기(확인) 버튼

    [Header("Item Acquired Popup (아이템 획득 팝업)")]
    public GameObject itemAcquiredPopup;       // 획득 팝업 패널
    public Image itemAcquiredIcon;             // 획득한 아이템 아이콘
    public TextMeshProUGUI itemAcquiredNameText; // [TMP] 획득한 아이템 이름
    public Button itemAcquiredConfirmButton;   // 확인 버튼

    [Header("Main UI Elements (재화 UI 이동 관리)")]
    public RectTransform poingBarRect;        // 포잉(돈) 표시줄 UI
    public Transform poingBarOriginalParent;  // 포잉 바의 원래 위치(부모)를 기억하는 변수

    [Header("Farm Popups")]
    public GameObject seedPopup;
    private Field currentField; // [추가] 씨앗을 심을 밭
    private Field lastField = null;   // 마지막으로 클릭한 밭
    private bool isSeedPopupOpen = false; // 시드 팝업 열림 여부

    void Awake()
    {
        // 싱글톤 초기화
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 게임 시작 시 포잉 바의 원래 부모(위치)를 저장해둠
        // (나중에 팝업창 안으로 이동했다가 다시 돌아올 때 사용)
        if (poingBarRect != null)
        {
            poingBarOriginalParent = poingBarRect.parent;
        }
    }

    void Start()
    {
        // 게임 시작 시 모든 팝업을 닫고 시작
        CloseAllPopups();

        // 안전장치: 알림창들이 켜져 있다면 강제로 끔
        if (alertPopup != null) alertPopup.SetActive(false);
        if (itemAcquiredPopup != null) itemAcquiredPopup.SetActive(false);
        if (seedPopup != null) seedPopup.SetActive(false);
    }

    // 화면에 떠 있는 모든 메인 팝업을 닫는 함수
    public void CloseAllPopups()
    {
        if (inventoryPopup != null) inventoryPopup.SetActive(false);
        if (researchLabPopup != null) researchLabPopup.SetActive(false);
        if (seedPopup != null) seedPopup.SetActive(false);
        if (storePopup != null) storePopup.SetActive(false);
        if (collectionPopup != null) collectionPopup.SetActive(false);

        // 팝업이 닫힐 때, 포잉 바(재화 UI)를 원래 위치(메인 화면)로 되돌림
        ResetPoingUIPosition();
    }

    // [기능] 단순 메시지 알림창 띄우기
    public void ShowAlertPopup(string message)
    {
        // 텍스트 내용 변경
        if (alertMessageText != null) alertMessageText.text = message;

        // 기존에 연결된 버튼 이벤트 제거 (중복 실행 방지)
        alertCloseButton.onClick.RemoveAllListeners();

        // '확인' 버튼 누르면 팝업 꺼지도록 설정
        alertCloseButton.onClick.AddListener(() => {
            alertPopup.SetActive(false);
        });

        alertPopup.SetActive(true); // 팝업 켜기
    }

    // [기능] 아이템 획득 팝업 띄우기
    public void ShowItemAcquiredPopup(ItemData item)
    {
        if (itemAcquiredIcon != null)
        {
            itemAcquiredIcon.sprite = item.itemIcon;
            itemAcquiredIcon.color = Color.white;
        }

        if (itemAcquiredNameText != null)
        {
            itemAcquiredNameText.text = item.itemName + " (획득)";
        }

        itemAcquiredConfirmButton.onClick.RemoveAllListeners();
        itemAcquiredConfirmButton.onClick.AddListener(() =>
        {


            // 그냥 창만 닫으면 됨 (이미 ResearchLab이 아이템 줬음)
            itemAcquiredPopup.SetActive(false);
        });

        itemAcquiredPopup.SetActive(true);
    }

    // --- 팝업 열기 함수들 ---

    public void OpenInventoryPopup()
    {
        CloseAllPopups(); // 다른 창 닫고
        if (inventoryPopup != null)
        {
            inventoryPopup.SetActive(true); // 인벤토리 열기
            MovePoingUIToPopup(inventoryPopup.transform); // 포잉 바를 인벤토리 창 안으로 이동
        }
    }

    public void OpenResearchLabPopup()
    {
        CloseAllPopups();
        if (researchLabPopup != null)
        {
            researchLabPopup.SetActive(true);

            MovePoingUIToPopup(researchLabPopup.transform);
        }
    }

    public void OpenStorePopup()
    {
        CloseAllPopups();
        if (storePopup != null)
        {
            storePopup.SetActive(true);
            MovePoingUIToPopup(storePopup.transform); // 포잉 바를 상점 창 안으로 이동
        }
    }

    public void OpenCollectionPopup()
    {
        CloseAllPopups(); // 다른 창 다 닫고
        if (collectionPopup != null)
        {
            collectionPopup.SetActive(true); // 도감 열기
            // (도감은 화면을 꽉 채우니까 포잉 바 이동은 선택사항. 필요하면 아래 줄 주석 해제)
            // MovePoingUIToPopup(collectionPopup.transform); 
        }
    }

    // 밭을 클릭했을 때 씨앗 메뉴 열기
    public void OpenSeedPopup(Field field)
    {
        CloseAllPopups();
        currentField = field;
        seedPopup.SetActive(true);
        seedPopup.GetComponent<SeedPopupUI>().RefreshButtons(field);

        isSeedPopupOpen = true;
    }

    public void ToggleSeedPopup(Field field)
    {
        // 새로운 밭 → 열기
        lastField = field;
        OpenSeedPopup(field);
    }

    // 현재 열려있는 밭 정보를 반환 (다른 스크립트에서 사용)
    public Field GetCurrentField()
    {
        return currentField;
    }

    // [유틸리티] 포잉 UI(재화 바)를 특정 팝업창 안으로 이동시키는 함수
    // 이유: 팝업창 위에 돈이 보여야 하므로, 계층 구조상 부모를 바꿔줌
    private void MovePoingUIToPopup(Transform targetParent)
    {
        if (poingBarRect == null || targetParent == null) return;

        poingBarRect.SetParent(targetParent); // 부모 변경

        // 위치 및 앵커(Anchor) 재설정 (우측 상단 고정 등)
        poingBarRect.anchorMin = new Vector2(1, 1);
        poingBarRect.anchorMax = new Vector2(1, 1);
        poingBarRect.pivot = new Vector2(1, 1);
        poingBarRect.anchoredPosition = new Vector2(-50, -50); // 여백 조정
        poingBarRect.localScale = Vector3.one; // 크기 초기화
    }

    // [유틸리티] 포잉 UI를 원래 위치(메인 화면)로 복구하는 함수
    private void ResetPoingUIPosition()
    {
        if (poingBarRect == null || poingBarOriginalParent == null) return;

        poingBarRect.SetParent(poingBarOriginalParent); // 원래 부모로 복귀

        // 원래 위치 좌표로 재설정 (좌측 상단 등)
        poingBarRect.anchorMin = new Vector2(0, 1);
        poingBarRect.anchorMax = new Vector2(0, 1);
        poingBarRect.pivot = new Vector2(0, 1);
        poingBarRect.anchoredPosition = new Vector2(50, -50);
        poingBarRect.localScale = Vector3.one;
    }
    void Update()
{
    if (isSeedPopupOpen && Input.GetMouseButtonDown(0)) // 좌클릭
    {
        // 클릭한 오브젝트 감지
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            // 클릭한 오브젝트가 Field인지 확인
            Field clickedField = hit.collider.GetComponent<Field>();

            // 밭이 아니면 시드 팝업 닫기
            if (clickedField == null)
            {
                seedPopup.SetActive(false);
            }
        }

    }
}

}
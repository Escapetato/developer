using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Popups")]
    // Inspector에서 팝업창 오브젝트를 연결할 슬롯
    public GameObject inventoryPopup;
    public GameObject researchLabPopup;
    public GameObject storePopup;
    // (나중에 상점, 도감 등 여기에 추가)

    [Header("Alert Popup")]
    public GameObject alertPopup;
    public Text alertMessageText;
    public Button alertCloseButton; // "확인" 버튼

    [Header("Item Acquired Popup")]
    public GameObject itemAcquiredPopup;
    public Image itemAcquiredIcon;
    public Text itemAcquiredNameText;
    public Button itemAcquiredConfirmButton; // "확인" 버튼

    [Header("Farm Popups")]
    public GameObject seedPopup;
    private Field currentField; // [추가] 씨앗을 심을 밭

    [Header("Main UI Elements")]
    public RectTransform poingBarRect; // 메인 Poing UI (Poing_Bar_Background)
    public Transform poingBarOriginalParent; // Poing UI의 원래 부모 (Canvas)
    public Transform storePoingTargetParent; // 상점 팝업 안의 새 위치 (우측 상단)

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // [추가] 게임 시작 시 Poing UI의 원래 부모를 기억
        if (poingBarRect != null)
        {
            poingBarOriginalParent = poingBarRect.parent;
        }
    }

    void Start()
    {
        // 게임 시작 시 모든 팝업창을 끈 상태로 시작
        CloseAllPopups();

        if (alertPopup != null)
            alertPopup.SetActive(false); // 알림창도 꺼둠

        if (itemAcquiredPopup != null)
            itemAcquiredPopup.SetActive(false);

        if (seedPopup != null) seedPopup.SetActive(false); // [추가]
    }

    // 모든 팝업을 닫는 함수
    public void CloseAllPopups()
    {
        // (기존 팝업 닫기)
        if (inventoryPopup != null)
            inventoryPopup.SetActive(false);
        if (researchLabPopup != null)
            researchLabPopup.SetActive(false);
        if (seedPopup != null)
            seedPopup.SetActive(false);

        if (storePopup != null)
            storePopup.SetActive(false);

        // [추가] 팝업 닫을 때 Poing UI를 원래 위치로 복구
        ResetPoingUIPosition();
    }

    public void ShowAlertPopup(string message)
    {
        // 메시지 텍스트를 바꿈
        alertMessageText.text = message;

        // "확인" 버튼이 눌렸을 때의 행동을 설정
        alertCloseButton.onClick.RemoveAllListeners();
        // (단순히 알림창을 닫는 기능만 새로 연결)
        alertCloseButton.onClick.AddListener(() => {
            alertPopup.SetActive(false);
        });

        // 알림 팝업을 켠다
        alertPopup.SetActive(true);
    }
    // --- 버튼과 연결할 함수들 ---

    public void ShowItemAcquiredPopup(ItemData item)
    {
        // 3a. 아이콘과 이름 텍스트를 설정
        itemAcquiredIcon.sprite = item.itemIcon;
        itemAcquiredNameText.text = item.itemName + " (획득)";
        itemAcquiredIcon.color = Color.white; // (투명도 복구)

        // 3b. "확인" 버튼이 눌렸을 때의 행동을 설정
        itemAcquiredConfirmButton.onClick.RemoveAllListeners();
        itemAcquiredConfirmButton.onClick.AddListener(() => {

            // 1. 인벤토리에 아이템 추가
            InventoryManager.Instance.AddItem(item, 1);

            // 2. 팝업 닫기
            itemAcquiredPopup.SetActive(false);
        });

        // 3c. 팝업을 켠다
        itemAcquiredPopup.SetActive(true);
    }

    // 인벤토리 팝업 열기 함수
    public void OpenInventoryPopup()
    {
        CloseAllPopups(); // 다른 걸 먼저 닫고
        inventoryPopup.SetActive(true); // 인벤토리만
    }

    // 연구실 팝업 열기 함수
    public void OpenResearchLabPopup()
    {
        CloseAllPopups(); // 다른 걸 먼저 닫고
        researchLabPopup.SetActive(true); // 연구실만
    }

    // 상점 팝업 열기 함수
    public void OpenStorePopup()
    {
        CloseAllPopups(); // 다른 걸 먼저 닫고
        storePopup.SetActive(true); // 상점만 연다

        // [추가] 상점을 열 때 Poing UI를 상점 안으로 이동
        MovePoingUIToStore();
    }

    public void OpenSeedPopup(Field field)
        {
            CloseAllPopups(); // 다른 팝업 닫기
            currentField = field; // 심을 밭 기억
            seedPopup.SetActive(true);

            // [추가] 팝업을 켤 때마다 버튼 새로고침
            seedPopup.GetComponent<SeedPopupUI>().RefreshButtons(field);
        }

    // [추가] SeedPopupUI가 심을 밭을 물어볼 함수
    public Field GetCurrentField()
    {
        return currentField;
    }

// Poing UI를 상점으로 옮기는 함수
    private void MovePoingUIToStore()
    {
        if (poingBarRect == null || storePoingTargetParent == null) return;

        // 1. Poing UI의 부모를 '상점 팝업 안'으로 변경
        poingBarRect.SetParent(storePoingTargetParent);
        
        // 2. 앵커/위치/크기를 상점 우측 상단에 맞게 강제 설정
        poingBarRect.anchorMin = new Vector2(1, 1); // (우측 상단)
        poingBarRect.anchorMax = new Vector2(1, 1); // (우측 상단)
        poingBarRect.pivot = new Vector2(1, 1);     // (기준점)
        poingBarRect.anchoredPosition = new Vector2(-50, -50); // (우측 상단 여백 예시)
        poingBarRect.localScale = Vector3.one; // 크기 1로
    }

    // Poing UI를 원래 위치로 복구하는 함수
    private void ResetPoingUIPosition()
    {
        if (poingBarRect == null || poingBarOriginalParent == null) return;

        // 1. Poing UI의 부모를 '원래 부모' (Canvas)로 변경
        poingBarRect.SetParent(poingBarOriginalParent);
        
        // 2. 원래 앵커/위치/크기로 복구
        // (주의: 이 값들은 Poing_Bar_Background의 원래 RectTransform 값이어야 함)
        poingBarRect.anchorMin = new Vector2(0, 1); // (좌측 상단 예시)
        poingBarRect.anchorMax = new Vector2(0, 1); // (좌측 상단 예시)
        poingBarRect.pivot = new Vector2(0, 1);     // (기준점)
        poingBarRect.anchoredPosition = new Vector2(50, -50); // (원래 여백 예시)
        poingBarRect.localScale = Vector3.one; // 크기 1로
    }
}
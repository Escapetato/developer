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
    // 도감 등 여기에 추가

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
    public GameObject fertilizerPopup;

    private Field currentField; // [추가] 씨앗을 심을 밭
    private Field lastField = null;   // 마지막으로 클릭한 밭
    private bool isSeedPopupOpen = false; // 시드 팝업 열림 여부

    [Header("Main UI Elements")]
    public RectTransform poingBarRect;
    public Transform poingBarOriginalParent;

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
        if (fertilizerPopup != null)
            fertilizerPopup.SetActive(false);

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
        alertCloseButton.onClick.AddListener(() =>
        {
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
        itemAcquiredConfirmButton.onClick.AddListener(() =>
        {

            // 1. 인벤토리에 아이템 추가
            InventoryManager.Instance.AddItem(item, 1);

            // 2. 팝업 닫기
            itemAcquiredPopup.SetActive(false);
        });

        // 3c. 팝업을 켠다
        itemAcquiredPopup.SetActive(true);
    }

    // --- 버튼과 연결할 함수들 ---

    // 인벤토리 팝업 열기 함수
    public void OpenInventoryPopup()
    {
        CloseAllPopups();
        inventoryPopup.SetActive(true);

        MovePoingUIToPopup(inventoryPopup.transform);
    }

    // 연구실 팝업 열기 함수
    public void OpenResearchLabPopup()
    {
        CloseAllPopups();
        researchLabPopup.SetActive(true);
        // (연구실은 Poing UI를 옮기지 않음)
    }

    // 상점 팝업 열기 함수
    public void OpenStorePopup()
    {
        CloseAllPopups();
        storePopup.SetActive(true);

        MovePoingUIToPopup(storePopup.transform);
    }

    public void OpenSeedPopup(Field field)
    {
        CloseAllPopups();
        currentField = field;
        if (seedPopup != null)
        {
            seedPopup.SetActive(true);
            seedPopup.GetComponent<SeedPopupUI>()?.RefreshButtons(field);
        }
        
        isSeedPopupOpen = true;
    }
    public void OpenFertilizerPopup(Field field)
    {
        CloseAllPopups(); // 다른 모든 팝업 닫기
        currentField = field;
        
        if (fertilizerPopup != null)
        {
            fertilizerPopup.SetActive(true);
            // FertilizerPopupUI 컴포넌트의 Show 함수 호출 (비료 정보 로드)
            // UIManager는 GameObject만 참조하고, 실제 UI 로직은 해당 스크립트가 처리합니다.
            FertilizerPopupUI fertilizerUI = fertilizerPopup.GetComponent<FertilizerPopupUI>();
            if (fertilizerUI != null)
            {
                fertilizerUI.Show(field);
            }
            else
            {
                Debug.LogError("FertilizerPopup에 FertilizerPopupUI 스크립트가 없습니다.");
            }
        }
    }

    public Field GetCurrentField()
    {
        return currentField;
    }

    private void MovePoingUIToPopup(Transform targetParent)
    {
        if (poingBarRect == null || targetParent == null) return;

        poingBarRect.SetParent(targetParent);

        poingBarRect.anchorMin = new Vector2(1, 1);
        poingBarRect.anchorMax = new Vector2(1, 1);
        poingBarRect.pivot = new Vector2(1, 1);
        poingBarRect.anchoredPosition = new Vector2(-50, -50);
        poingBarRect.localScale = Vector3.one;
    }


    // Poing UI를 원래 위치로 복구하는 함수 
    private void ResetPoingUIPosition()
    {
        if (poingBarRect == null || poingBarOriginalParent == null) return;

        poingBarRect.SetParent(poingBarOriginalParent);
        poingBarRect.anchorMin = new Vector2(0, 1);
        poingBarRect.anchorMax = new Vector2(0, 1);
        poingBarRect.pivot = new Vector2(0, 1);
        poingBarRect.anchoredPosition = new Vector2(50, -50);
        poingBarRect.localScale = Vector3.one;
    }
    void Update()
    {
        if (isSeedPopupOpen && Input.GetMouseButtonDown(0)) 
        {
            // 클릭한 오브젝트 감지
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                // 클릭한 오브젝트가 Field인지 확인
                Field clickedField = hit.collider.GetComponent<Field>();

                // UI 외부를 클릭했을 때만 팝업을 닫습니다.
                if (clickedField == null)
                {
                    // 팝업을 닫고 상태 플래그도 초기화
                    CloseAllPopups(); 
                }
            }
        }
    }
}
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
    }

    void Start()
    {
        // 게임 시작 시 모든 팝업창을 끈 상태로 시작
        CloseAllPopups();

        if (alertPopup != null)
            alertPopup.SetActive(false); // 알림창도 꺼둠

        if (itemAcquiredPopup != null)
            itemAcquiredPopup.SetActive(false);
    }

    // 모든 팝업을 닫는 함수
    public void CloseAllPopups()
    {
        // null이 아닌지 확인하고
        if (inventoryPopup != null)
            inventoryPopup.SetActive(false);

        if (researchLabPopup != null)
            researchLabPopup.SetActive(false);
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
}
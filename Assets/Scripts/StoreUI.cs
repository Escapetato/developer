using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StoreUI : MonoBehaviour
{
    public static StoreUI Instance { get; private set; }

    [Header("Store Slots")]
    public Transform slotParent;
    private List<ItemSlot> slots;

    //  상점 DB (Inspector에서 'StoreDB.asset' 연결)
    public StoreDatabase storeDB;

    // 선택된 아이템
    private ItemData selectedItem;
    private ItemSlot selectedSlot;

    [Header("Details Panel")]
    public GameObject detailPanelObject;
    public Image detailImage;
    public Text detailNameText;
    // 가격 텍스트와 구매 버튼
    public Text detailPriceText;
    public Button purchaseButton;

    private string currentCategory = "All";

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        slots = new List<ItemSlot>();
        slotParent.GetComponentsInChildren<ItemSlot>(slots);

        if (detailPanelObject != null)
            detailPanelObject.SetActive(false);
    }

    void OnEnable()
    {
        SetCategory("All"); // 켤 때는 항상 "모두" 탭
    }

    void OnDisable()
    {
    }

    // 카테고리 버튼들이 호출할 함수
    public void SetCategory(string category)
    {
        currentCategory = category;
        ClearSelection();
        RedrawStore();
    }

    // 슬롯 선택 함수
    public void SelectSlot(ItemSlot slot)
    {
        if (selectedSlot != null)
        {
            selectedSlot.SetSelected(false);
        }

        if (slot.item != null)
        {
            selectedItem = slot.item;
            selectedSlot = slot;
            selectedSlot.SetSelected(true);

            UpdateDetailPanel(selectedItem); // 상세 정보 패널 업데이트
        }
    }

    // 선택 해제 함수
    public void ClearSelection()
    {
        if (selectedSlot != null)
        {
            selectedSlot.SetSelected(false);
        }
        selectedItem = null;
        selectedSlot = null;

        if (detailPanelObject != null)
            detailPanelObject.SetActive(false);
    }

    // 상세 정보 패널 업데이트 (가격, 구매 버튼 추가)
    private void UpdateDetailPanel(ItemData item)
    {
        if (item != null)
        {
            detailPanelObject.SetActive(true);
            detailImage.sprite = item.itemIcon;
            detailImage.color = Color.white;
            detailNameText.text = item.itemName;

            // 가격 표시
            detailPriceText.text = "" + item.price.ToString();

            // 구매 버튼에 '구매' 기능 연결
            purchaseButton.gameObject.SetActive(true);
            purchaseButton.onClick.RemoveAllListeners();
            purchaseButton.onClick.AddListener(() => {
                OnPurchaseButtonClick(); // 구매 함수 호출
            });
        }
    }

    // 상점 다시 그리기 (필터링 로직)
    private void RedrawStore()
    {
        // '상점 DB'에서 전체 판매 목록을 가져옴
        List<ItemData> allItemsForSale = storeDB.itemsForSale;
        int i = 0; // UI 슬롯 인덱스

        foreach (ItemData item in allItemsForSale)
        {
            if (currentCategory == "All" || item.itemCategory == currentCategory)
            {
                if (i < slots.Count)
                {
                    // 슬롯에 아이템과 '가격'을 표시
                    slots[i].SetStoreSlot(item); // (다음 단계에서 이 함수 만들 것)

                    if (selectedSlot == slots[i])
                    {
                        selectedSlot.SetSelected(true);
                    }
                    i++;
                }
            }
        }
        // 남은 슬롯들은 모두 비움
        for (int j = i; j < slots.Count; j++)
        {
            slots[j].ClearSlot();
        }
        if (i == 0)
        {
            ClearSelection();
        }
    }

    // 구매 버튼 로직
    public void OnPurchaseButtonClick()
    {
        if (selectedItem == null) return;

        // 포잉(Poing)이 충분한가?
        if (PoingManager.Instance.HasEnoughPoing(selectedItem.price))
        {
            // 포잉 차감
            PoingManager.Instance.DecreasePoing(selectedItem.price);

            // 인벤토리에 아이템 추가
            InventoryManager.Instance.AddItem(selectedItem, 1);

            // 알림
            UIManager.Instance.ShowAlertPopup(selectedItem.itemName + " 구매 완료!");
        }
        else
        {
            // 포잉 부족 알림
            UIManager.Instance.ShowAlertPopup("포잉이 부족합니다.");
        }
    }
}
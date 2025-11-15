using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StoreUI : MonoBehaviour
{
    public static StoreUI Instance { get; private set; }

    [Header("Store Slots")]
    public Transform slotParent;
    private List<ItemSlot> slots;

    [Header("Databases")]
    public StoreDatabase storeDB; // '진짜' 씨앗 목록
    // 1. [추가] 특수 아이템 에셋 연결
    public ItemData lockedSeedItem; // (Inspector에서 'Item_Seed_Locked' 연결)
    public ItemData randomSeedItem; // (Inspector에서 'Item_Seed_Random' 연결)

    private ItemData selectedItem;
    private ItemSlot selectedSlot;

    [Header("Details Panel")]
    public GameObject detailPanelObject;
    public Image detailImage;
    public Text detailNameText;
    public Text detailPriceText;
    public Button purchaseButton;

    [Header("Category Buttons")]
    public List<CategoryButton> categoryButtons; // (Inspector에서 'CategoryButton' 스크립트 연결)
    public CategoryButton defaultCategoryButton; // (Inspector에서 'Seed' 버튼 연결)

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
        if (defaultCategoryButton != null)
        {
            SetCategory(defaultCategoryButton);
        }
        else if (categoryButtons != null && categoryButtons.Count > 0)
        {
            SetCategory(categoryButtons[0]);
        }
    }

    public void SetCategory(CategoryButton clickedButton)
    {
        // 3a. 모든 카테고리 버튼을 '선택 해제' 상태로
        foreach (CategoryButton btn in categoryButtons)
        {
            btn.SetSelected(false);
        }

        // 3b. '클릭된' 버튼만 '선택' 상태로 변경
        clickedButton.SetSelected(true);

        // 3c. 클릭된 버튼의 'categoryName'을 현재 카테고리로 사용
        currentCategory = clickedButton.categoryName;

        // 3d. 상점 다시 그리기
        ClearSelection();
        RedrawStore();
    }

    private void RedrawStore()
    {
        int i = 0; // 슬롯 인덱스

        // 1. 탭을 바꿀 때마다 일단 모든 슬롯을 비움
        for (int j = 0; j < slots.Count; j++)
        {
            slots[j].ClearSlot();
        }

        if (storeDB == null) return;

        // 2. '상점 DB'의 모든 아이템을 검사
        foreach (ItemData item in storeDB.itemsForSale)
        {
            if (i >= slots.Count) break;

            // 3. [필터링] 아이템 카테고리가 현재 탭과 일치하는가?
            if (item.itemCategory == currentCategory)
            {
                // 4. 잠금 확인
                // (GameProgressionManager의 함수 이름 변경 반영)
                if (GameProgressionManager.Instance.IsItemUnlocked(item))
                {
                    // 4a. 해금됐으면: 진짜 아이템을 보여 줌
                    slots[i].SetStoreSlot(item);
                }
                else
                {
                    // 4b. 잠겼으면: '물음표' 씨앗을 보여줌
                    // (주의: lockedSeedItem 에셋이 Tool에도 사용됨
                    //  Tool용 lockedItem을 따로 만들어도 됨)
                    slots[i].SetStoreSlot(lockedSeedItem);
                }
                i++;
            }
        }

        // '씨앗' 탭일 때만 '랜덤 씨앗'을 마지막에 추가
        if (currentCategory == "Seed")
        {
            if (i < slots.Count && randomSeedItem != null)
            {
                slots[i].SetStoreSlot(randomSeedItem);
                i++;
            }
        }

        ClearSelection();
    }

    public void SelectSlot(ItemSlot slot)
    {
        if (selectedSlot != null) selectedSlot.SetSelected(false);

        if (slot.item == null)
        {
            ClearSelection();
            return;
        }

        selectedItem = slot.item;
        selectedSlot = slot;
        selectedSlot.SetSelected(true);

        // '물음표'를 클릭했는가?
        if (selectedItem == lockedSeedItem)
        {
            // 물음표 자체의 가격(300)을 표시하되 구매는 막음
            UpdateDetailPanel(lockedSeedItem, false);
        }
        // 2. '랜덤 씨앗'을 클릭했는가?
        else if (selectedItem == randomSeedItem)
        {
            UpdateDetailPanel(randomSeedItem, true); // 구매 가능
        }
        // 3. '해금된 아이템' (도구, 포션, 씨앗 등)을 클릭했는가?
        else
        {
            UpdateDetailPanel(selectedItem, true); // 구매 가능
        }
    }

    // 6. 상세 정보 패널 업데이트
    private void UpdateDetailPanel(ItemData item, bool isUnlocked)
    {
        if (item != null)
        {
            detailPanelObject.SetActive(true);
            detailImage.sprite = item.itemIcon;
            detailImage.color = Color.white;
            detailNameText.text = item.itemName;
            detailPriceText.text = "P : " + item.price.ToString();

            // 6a. 잠겨 있으면 '구매' 버튼 비활성화 (회색으로)
            purchaseButton.gameObject.SetActive(true);
            purchaseButton.interactable = isUnlocked;
        }
    }

    // 7. 구매 버튼 로직
    public void OnPurchaseButtonClick()
    {
        if (selectedItem == null) return;

        // 7a. 구매하려는 것이 '랜덤 씨앗'인가?
        if (selectedItem == randomSeedItem)
        {
            if (PoingManager.Instance.HasEnoughPoing(randomSeedItem.price))
            {
                PoingManager.Instance.DecreasePoing(randomSeedItem.price);

                // 인벤토리에 '랜덤 씨앗' 아이템을 추가
                InventoryManager.Instance.AddItem(randomSeedItem, 1);

                UIManager.Instance.ShowAlertPopup("랜덤 씨앗 구매 완료!");
            }
            else
            {
                UIManager.Instance.ShowAlertPopup("포잉이 부족합니다.");
            }
        }
        // 7b. 그 외 '일반 씨앗'인가?
        else if (selectedItem != lockedSeedItem)
        {
            if (PoingManager.Instance.HasEnoughPoing(selectedItem.price))
            {
                PoingManager.Instance.DecreasePoing(selectedItem.price);
                InventoryManager.Instance.AddItem(selectedItem, 1);
                UIManager.Instance.ShowAlertPopup(selectedItem.itemName + " 구매 완료!");
            }
            else
            {
                UIManager.Instance.ShowAlertPopup("포잉이 부족합니다.");
            }
        }
    }

    // 8. 선택 해제 함수
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
}
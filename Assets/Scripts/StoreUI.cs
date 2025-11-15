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
        SetCategory("Seed");
    }

    public void SetCategory(string category)
    {
        currentCategory = category;
        ClearSelection();
        RedrawStore();
    }

    private void RedrawStore()
    {
        int i = 0; // 슬롯 인덱스

        // 1. [추가] 탭을 바꿀 때마다 일단 모든 슬롯을 비움
        for (int j = 0; j < slots.Count; j++)
        {
            slots[j].ClearSlot();
        }

        // 2. "Seed" 탭은 '잠금' 기능이 있는 특수 로직 사용
        if (currentCategory == "Seed")
        {
            if (storeDB != null)
            {
                // (기존 'Seed' 탭 로직은 그대로 둡니다)
                foreach (ItemData seed in storeDB.itemsForSale)
                {
                    if (i >= slots.Count) break;
                    if (seed.itemCategory != "Seed") continue;

                    if (GameProgressionManager.Instance.IsSeedUnlocked(seed))
                    {
                        slots[i].SetStoreSlot(seed);
                    }
                    else
                    {
                        slots[i].SetStoreSlot(lockedSeedItem);
                    }
                    i++;
                }
            }
            if (i < slots.Count && randomSeedItem != null)
            {
                slots[i].SetStoreSlot(randomSeedItem);
                i++;
            }
        }
        // 3. 그 외 모든 탭("Tool", "Potion" 등) 로직
        else
        {
            if (storeDB != null)
            {
                // '상점 DB'의 모든 아이템을 검사
                foreach (ItemData item in storeDB.itemsForSale)
                {
                    if (i >= slots.Count) break;

                    // 3a. [필터링] 아이템 카테고리가 현재 탭과 일치하는가?
                    if (item.itemCategory == currentCategory)
                    {
                        // 일치하면 슬롯에 표시
                        slots[i].SetStoreSlot(item);

                        if (selectedSlot == slots[i])
                        {
                            selectedSlot.SetSelected(true);
                        }
                        i++;
                    }
                }
            }
        }

        ClearSelection();
    }

    // 슬롯 선택 함수
    public void SelectSlot(ItemSlot slot)
    {
        if (selectedSlot != null) selectedSlot.SetSelected(false);

        if (slot.item == null)
        {
            ClearSelection();
            return;
        }

        selectedItem = slot.item; // 슬롯이 보여주는 아이템 (물음표, 랜덤, 진짜)
        selectedSlot = slot;
        selectedSlot.SetSelected(true);

        // 5b. '물음표'를 클릭했는가?
        if (selectedItem == lockedSeedItem)
        {
            UpdateDetailPanel(lockedSeedItem, false); // 구매 불가
        }
        // 5c. '랜덤 씨앗'을 클릭했는가?
        else if (selectedItem == randomSeedItem)
        {
            UpdateDetailPanel(randomSeedItem, true); // 구매 가능
        }
        // 5d. '해금된 씨앗'을 클릭했는가?
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
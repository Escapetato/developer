using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoreUI : MonoBehaviour
{
    public static StoreUI Instance { get; private set; }

    [Header("Store Slots")]
    public Transform slotParent;
    private List<ItemSlot> slots;

    [Header("Databases")]
    public StoreDatabase storeDB;
    public ItemData lockedSeedItem;
    public ItemData randomSeedItem;

    private ItemData selectedItem;
    private ItemSlot selectedSlot;

    [Header("Details Panel UI")]
    public GameObject detailPanelObject;
    public Image detailImage;
    public TextMeshProUGUI detailNameText;

    // ▼ [기존] 총 가격 표시 (코인 아이콘 옆)
    public TextMeshProUGUI detailTotalPriceText;

    // ▼▼▼ [추가] 현재 보유 개수 표시 (박스 아이콘 옆) ▼▼▼
    public TextMeshProUGUI detailOwnedText;

    [Header("Quantity Controls")]
    public Button decreaseButton;      // 수량 감소 (<)
    public Button increaseButton;      // 수량 증가 (>)
    public TextMeshProUGUI quantityText; // 가운데 구매할 수량 숫자 (1)

    public Button buyButton;
    public TextMeshProUGUI buyButtonText;

    [Header("Category Buttons")]
    public List<CategoryButton> categoryButtons;
    public CategoryButton defaultCategoryButton;

    private string currentCategory = "All";
    private int currentBuyQuantity = 1;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        slots = new List<ItemSlot>();
        slotParent.GetComponentsInChildren<ItemSlot>(slots);

        if (detailPanelObject != null) detailPanelObject.SetActive(false);

        if (decreaseButton != null)
        {
            decreaseButton.onClick.RemoveAllListeners();
            decreaseButton.onClick.AddListener(OnDecreaseQuantity);
        }
        if (increaseButton != null)
        {
            increaseButton.onClick.RemoveAllListeners();
            increaseButton.onClick.AddListener(OnIncreaseQuantity);
        }
        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(OnRealPurchaseClick);
        }
    }

    void OnEnable()
    {
        if (defaultCategoryButton != null) SetCategory(defaultCategoryButton);
        else if (categoryButtons != null && categoryButtons.Count > 0) SetCategory(categoryButtons[0]);
    }

    public void SetCategory(CategoryButton clickedButton)
    {
        foreach (CategoryButton btn in categoryButtons) btn.SetSelected(false);
        clickedButton.SetSelected(true);
        currentCategory = clickedButton.categoryName;
        ClearSelection();
        RedrawStore();
    }

    private void RedrawStore()
    {
        // (기존과 동일하여 생략 가능하지만 복붙 편의를 위해 유지)
        int i = 0;
        if (storeDB == null) return;

        foreach (ItemData item in storeDB.itemsForSale)
        {
            if (i >= slots.Count) break;

            if (item.itemCategory == currentCategory || currentCategory == "All")
            {
                slots[i].gameObject.SetActive(true);
                if (item.isDefaultUnlocked || GameProgressionManager.Instance.IsItemUnlocked(item))
                    slots[i].SetSlot(item);
                else
                    slots[i].SetSlot(lockedSeedItem);
                i++;
            }
        }

        if (currentCategory == "Seed" || currentCategory == "All")
        {
            if (i < slots.Count && randomSeedItem != null)
            {
                slots[i].gameObject.SetActive(true);
                slots[i].SetSlot(randomSeedItem);
                i++;
            }
        }

        for (int j = i; j < slots.Count; j++)
        {
            slots[j].ClearSlot();
            slots[j].gameObject.SetActive(false);
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

        currentBuyQuantity = 1;

        if (selectedItem == lockedSeedItem) UpdateDetailPanel(lockedSeedItem, false);
        else if (selectedItem == randomSeedItem) UpdateDetailPanel(randomSeedItem, true);
        else UpdateDetailPanel(selectedItem, true);
    }

    private void UpdateDetailPanel(ItemData item, bool isUnlocked)
    {
        if (item != null)
        {
            detailPanelObject.SetActive(true);

            if (detailImage != null) detailImage.sprite = item.itemIcon;
            if (detailNameText != null) detailNameText.text = item.itemName;

            if (buyButton != null) buyButton.gameObject.SetActive(true);

            // ▼▼▼ [수정됨] 현재 보유량(Owned) 표시 로직 ▼▼▼
            int myCount = InventoryManager.Instance.GetItemCount(item);
            if (detailOwnedText != null)
            {
                // 박스 아이콘 옆 텍스트에 내 개수 표시
                detailOwnedText.text = myCount.ToString();
            }
            // ▲▲▲ 추가 완료 ▲▲▲


            // 1. 잠긴 아이템
            if (item.itemName == "???")
            {
                SetQuantityControlsActive(false);
                if (detailTotalPriceText != null) detailTotalPriceText.text = "???";

                if (buyButton != null)
                {
                    buyButton.interactable = false;
                    if (buyButtonText != null) buyButtonText.text = "해금 필요";
                }
            }
            // 2. 도구 (Tool)
            else if (item.itemCategory == "Tool")
            {
                SetQuantityControlsActive(false);

                if (detailTotalPriceText != null) detailTotalPriceText.text = item.price.ToString();

                if (myCount > 0)
                {
                    if (buyButton != null)
                    {
                        buyButton.interactable = false;
                        if (buyButtonText != null) buyButtonText.text = "보유 중";
                    }
                }
                else
                {
                    if (buyButton != null)
                    {
                        buyButton.interactable = isUnlocked;
                        if (buyButtonText != null) buyButtonText.text = "구매하기";
                    }
                }
            }
            // 3. 일반 아이템
            else
            {
                SetQuantityControlsActive(true);
                UpdateQuantityUI(); // 가격 계산 갱신

                if (buyButton != null)
                {
                    buyButton.interactable = isUnlocked;
                }
            }
        }
    }

    private void SetQuantityControlsActive(bool isActive)
    {
        if (decreaseButton != null) decreaseButton.gameObject.SetActive(isActive);
        if (increaseButton != null) increaseButton.gameObject.SetActive(isActive);
        if (quantityText != null) quantityText.gameObject.SetActive(isActive);
    }

    public void OnDecreaseQuantity()
    {
        if (currentBuyQuantity > 1)
        {
            currentBuyQuantity--;
            UpdateQuantityUI();
        }
    }

    public void OnIncreaseQuantity()
    {
        if (selectedItem == null) return;

        int myPoing = PoingManager.Instance.GetPoing();
        int price = selectedItem.price > 0 ? selectedItem.price : 999999;
        int maxAffordable = myPoing / price;

        if (maxAffordable == 0) maxAffordable = 1;

        if (currentBuyQuantity < 99)
        {
            currentBuyQuantity++;
            UpdateQuantityUI();
        }
        else
        {
            UIManager.Instance.ShowAlertPopup("최대 수량입니다.");
        }
    }

    private void UpdateQuantityUI()
    {
        if (selectedItem == null) return;

        // 1. 가운데 구매 수량 (< 1 >)
        if (quantityText != null)
            quantityText.text = currentBuyQuantity.ToString();

        // 2. 총 가격 (코인 아이콘 옆)
        int totalCost = selectedItem.price * currentBuyQuantity;
        if (detailTotalPriceText != null)
            detailTotalPriceText.text = totalCost.ToString();

        // 3. 버튼 텍스트
        if (buyButtonText != null)
            buyButtonText.text = $"{currentBuyQuantity}개 구매";
    }

    public void OnRealPurchaseClick()
    {
        if (selectedItem == null) return;

        int totalCost = selectedItem.price * currentBuyQuantity;

        if (PoingManager.Instance.HasEnoughPoing(totalCost))
        {
            PoingManager.Instance.DecreasePoing(totalCost);
            InventoryManager.Instance.AddItem(selectedItem, currentBuyQuantity);

            NotifyPurchaseToQuest(selectedItem, currentBuyQuantity);

            UIManager.Instance.ShowAlertPopup($"{selectedItem.itemName} {currentBuyQuantity}개 구매 완료!");
            SoundManager.Instance.PlaySFX("Money");

            // ▼▼▼ [중요] 구매 후 보유량이 늘었으니 UI를 갱신해줌! ▼▼▼
            UpdateDetailPanel(selectedItem, true);
        }
        else
        {
            UIManager.Instance.ShowAlertPopup("포잉이 부족합니다!");
        }
    }

    public void ClearSelection()
    {
        if (selectedSlot != null) selectedSlot.SetSelected(false);
        selectedItem = null;
        selectedSlot = null;
        if (detailPanelObject != null) detailPanelObject.SetActive(false);
        if (buyButton != null) buyButton.gameObject.SetActive(false);
    }

    private void NotifyPurchaseToQuest(ItemData item, int quantity)
    {
        if (QuestManager.Instance == null || item == null) return;

        switch (item.itemCategory)
        {
            case "Seed": QuestManager.Instance.NotifyAction(QuestConditionType.BuySeed, item, quantity); break;
            case "Potion": QuestManager.Instance.NotifyAction(QuestConditionType.BuyPotion, item, quantity); break;
            case "Tool": QuestManager.Instance.NotifyAction(QuestConditionType.BuyTool, item, quantity); break;
            case "Theme": QuestManager.Instance.NotifyAction(QuestConditionType.BuyTheme, item, quantity); break;
        }
    }
}
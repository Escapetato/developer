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

    [Header("Details Panel")]
    public GameObject detailPanelObject;
    public Image detailImage;
    public TextMeshProUGUI detailNameText;
    public TextMeshProUGUI detailPriceText;

    public Button openBuyPopupButton;

    [Header("Category Buttons")]
    public List<CategoryButton> categoryButtons;
    public CategoryButton defaultCategoryButton;

    [Header("Buy Popup Settings")]
    public GameObject buyPopupObject;
    public Image popupItemIcon;
    public TextMeshProUGUI popupNameText;
    public Slider popupSlider;
    public TextMeshProUGUI popupCountText;
    public TextMeshProUGUI popupTotalCostText;
    public Button popupConfirmButton;
    public Button popupCancelButton;

    private string currentCategory = "All";
    private int currentBuyQuantity = 1;

    public TextMeshProUGUI buyButtonText;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        slots = new List<ItemSlot>();
        slotParent.GetComponentsInChildren<ItemSlot>(slots);

        if (detailPanelObject != null) detailPanelObject.SetActive(false);
        if (buyPopupObject != null) buyPopupObject.SetActive(false);

        if (openBuyPopupButton != null)
        {
            openBuyPopupButton.onClick.RemoveAllListeners();
            openBuyPopupButton.onClick.AddListener(OnOpenBuyPopupClick);
        }

        if (popupSlider != null)
        {
            popupSlider.onValueChanged.RemoveAllListeners();
            popupSlider.onValueChanged.AddListener(OnSliderValueChanged);
        }

        if (popupConfirmButton != null)
        {
            popupConfirmButton.onClick.RemoveAllListeners();
            popupConfirmButton.onClick.AddListener(OnRealPurchaseClick);
        }

        if (popupCancelButton != null)
        {
            popupCancelButton.onClick.RemoveAllListeners();
            popupCancelButton.onClick.AddListener(CloseBuyPopup);
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

        if (selectedItem == lockedSeedItem) UpdateDetailPanel(lockedSeedItem, false);
        else if (selectedItem == randomSeedItem) UpdateDetailPanel(randomSeedItem, true);
        else UpdateDetailPanel(selectedItem, true);
    }

    private void UpdateDetailPanel(ItemData item, bool isUnlocked)
    {
        if (item != null)
        {
            detailPanelObject.SetActive(true);

            // 아이템 선택 시 구매 버튼 켜주기 (ClearSelection으로 꺼졌던 것 복구)
            if (openBuyPopupButton != null)
                openBuyPopupButton.gameObject.SetActive(true);

            if (detailImage != null) detailImage.sprite = item.itemIcon;
            if (detailNameText != null) detailNameText.text = item.itemName;

            // ★ [중요] 내 보유 개수 확인 (계산만 하고, 텍스트 표시는 안 함)
            int myCount = InventoryManager.Instance.GetItemCount(item);

            // ▼▼▼ [삭제됨] 수량 텍스트 갱신 코드 삭제 ▼▼▼
            // if (detailQuantityText != null) detailQuantityText.text = myCount.ToString();


            // ▼▼▼ [수정] 잠긴 아이템("???")인지 확인 ▼▼▼
            if (item.itemName == "???")
            {
                if (detailPriceText != null)
                {
                    detailPriceText.gameObject.SetActive(true);
                    detailPriceText.text = "???"; // 가격 물음표
                }

                if (openBuyPopupButton != null)
                {
                    openBuyPopupButton.interactable = false;
                    if (buyButtonText != null) buyButtonText.text = "해금 필요";
                }
            }
            // ▼▼▼ 기존 로직 (도구/일반 아이템 구분) ▼▼▼
            else if (item.itemCategory == "Tool")
            {
                bool isOwnedTool = (myCount > 0); // 보유 여부 확인

                if (isOwnedTool)
                {
                    // 보유 중이면 가격 숨기기
                    if (detailPriceText != null) detailPriceText.gameObject.SetActive(false);

                    if (openBuyPopupButton != null)
                    {
                        openBuyPopupButton.interactable = false;
                        if (buyButtonText != null) buyButtonText.text = "보유 중";
                    }
                }
                else
                {
                    if (detailPriceText != null)
                    {
                        detailPriceText.gameObject.SetActive(true);
                        detailPriceText.text = item.price.ToString();
                    }
                    if (openBuyPopupButton != null)
                    {
                        openBuyPopupButton.interactable = isUnlocked;
                        if (buyButtonText != null) buyButtonText.text = "결제";
                    }
                }
            }
            else
            {
                // [일반 아이템]
                if (detailPriceText != null)
                {
                    detailPriceText.gameObject.SetActive(true);
                    detailPriceText.text = item.price.ToString();
                }

                if (openBuyPopupButton != null)
                {
                    openBuyPopupButton.interactable = isUnlocked;
                    if (buyButtonText != null) buyButtonText.text = "결제";
                }
            }
        }
    }

    public void ClearSelection()
    {
        if (selectedSlot != null) selectedSlot.SetSelected(false);
        selectedItem = null;
        selectedSlot = null;
        if (detailPanelObject != null) detailPanelObject.SetActive(false);
        if (openBuyPopupButton != null) openBuyPopupButton.gameObject.SetActive(false);
    }

    public void OnOpenBuyPopupClick()
    {
        if (selectedItem == null) return;

        int myPoing = PoingManager.Instance.GetPoing();
        int price = selectedItem.price;

        if (price <= 0) return;

        int maxCanBuy = myPoing / price;

        if (maxCanBuy <= 0)
        {
            UIManager.Instance.ShowAlertPopup("포잉이 부족합니다!");
            return;
        }

        buyPopupObject.SetActive(true);
        if (popupItemIcon != null) popupItemIcon.sprite = selectedItem.itemIcon;
        if (popupNameText != null) popupNameText.text = selectedItem.itemName;

        if (popupSlider != null)
        {
            popupSlider.minValue = 1;
            popupSlider.maxValue = maxCanBuy;
            popupSlider.value = 1;
            currentBuyQuantity = 1;
        }

        UpdatePopupTexts();
        SoundManager.Instance.PlaySFX("PopupOpen");
    }

    public void OnSliderValueChanged(float value)
    {
        currentBuyQuantity = (int)value;
        UpdatePopupTexts();
    }

    private void UpdatePopupTexts()
    {
        if (popupCountText != null)
            popupCountText.text = currentBuyQuantity.ToString();

        if (selectedItem != null && popupTotalCostText != null)
        {
            int total = selectedItem.price * currentBuyQuantity;
            popupTotalCostText.text = total.ToString() + " 포잉";
        }
    }

    public void OnRealPurchaseClick()
    {
        if (selectedItem == null) return;

        int totalCost = selectedItem.price * currentBuyQuantity;

        if (PoingManager.Instance.HasEnoughPoing(totalCost))
        {
            PoingManager.Instance.DecreasePoing(totalCost);
            InventoryManager.Instance.AddItem(selectedItem, currentBuyQuantity);

            // 퀘스트 진행도 : 구매 (종류별로 분기)
            NotifyPurchaseToQuest(selectedItem, currentBuyQuantity);

            UIManager.Instance.ShowAlertPopup($"{selectedItem.itemName} {currentBuyQuantity}개 구매 완료!");

            CloseBuyPopup();

            // ★ 구매 후 수량 즉시 갱신을 위해 패널 업데이트 호출
            UpdateDetailPanel(selectedItem, true);
        }
        else
        {
            UIManager.Instance.ShowAlertPopup("포잉이 부족합니다!");
        }
    }

    public void CloseBuyPopup()
    {
        if (buyPopupObject != null) buyPopupObject.SetActive(false);
        SoundManager.Instance.PlaySFX("Button");
    }

    // 퀘스트 진행도 : 구매 종류 구별 함수 
    private void NotifyPurchaseToQuest(ItemData item, int quantity)
    {
        if (QuestManager.Instance == null || item == null) return;

        switch (item.itemCategory)
        {
            case "Seed":
                QuestManager.Instance.NotifyAction(QuestConditionType.BuySeed, item, quantity);
                break;

            case "Potion":
                QuestManager.Instance.NotifyAction(QuestConditionType.BuyPotion, item, quantity);
                break;

            case "Tool":
                QuestManager.Instance.NotifyAction(QuestConditionType.BuyTool, item, quantity);
                break;

            case "Theme":
                QuestManager.Instance.NotifyAction(QuestConditionType.BuyTheme, item, quantity);
                break;

            default:
                break;
        }
    }



}
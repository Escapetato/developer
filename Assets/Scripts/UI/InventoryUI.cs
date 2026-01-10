using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }

    [Header("Inventory Slots")]
    public Transform slotParent;
    private List<ItemSlot> slots;

    public ItemData selectedItem { get; private set; }
    public ItemSlot selectedSlot { get; private set; }

    [Header("Details Panel")]
    public GameObject detailPanelObject;
    public Image detailImage;
    public TextMeshProUGUI detailNameText;
    public TextMeshProUGUI detailPriceText;

    // ▼▼▼ [추가 1] 수량 텍스트를 연결할 변수 추가 ▼▼▼
    public TextMeshProUGUI detailQuantityText;

    public Button openSellPopupButton;
    public TextMeshProUGUI sellButtonText;

    [Header("Sell Popup Settings")]
    public GameObject sellPopupObject;
    public Image popupItemIcon;
    public TextMeshProUGUI popupNameText;
    public Slider popupSlider;
    public TextMeshProUGUI popupCountText;
    public TextMeshProUGUI popupPriceText;
    public Button popupConfirmButton;
    public Button popupCancelButton;

    [Header("Category Buttons")]
    public List<CategoryButton> categoryButtons;
    public CategoryButton defaultCategoryButton;

    private string currentCategory = "Seed";
    private int currentSellQuantity = 1;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        slots = new List<ItemSlot>();
        slotParent.GetComponentsInChildren<ItemSlot>(slots);

        if (detailPanelObject != null) detailPanelObject.SetActive(false);
        if (sellPopupObject != null) sellPopupObject.SetActive(false);

        if (openSellPopupButton != null)
        {
            openSellPopupButton.onClick.RemoveAllListeners();
            openSellPopupButton.onClick.AddListener(OnOpenSellPopupClick);
        }

        if (popupSlider != null)
        {
            popupSlider.onValueChanged.RemoveAllListeners();
            popupSlider.onValueChanged.AddListener(OnSliderValueChanged);
        }

        if (popupConfirmButton != null)
        {
            popupConfirmButton.onClick.RemoveAllListeners();
            popupConfirmButton.onClick.AddListener(OnRealSellClick);
        }

        if (popupCancelButton != null)
        {
            popupCancelButton.onClick.RemoveAllListeners();
            popupCancelButton.onClick.AddListener(CloseSellPopup);
        }
    }

    void OnEnable()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged += RedrawInventory;

        if (defaultCategoryButton != null) SetCategory(defaultCategoryButton);
        else
        {
            currentCategory = "Seed";
            RedrawInventory();
        }

        ClearSelection();
    }

    void OnDisable()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged -= RedrawInventory;
    }

    public void SetCategory(CategoryButton clickedButton)
    {
        foreach (CategoryButton btn in categoryButtons) btn.SetSelected(false);
        clickedButton.SetSelected(true);
        currentCategory = clickedButton.categoryName;
        ClearSelection();
        RedrawInventory();
    }

    public void SelectSlot(ItemSlot slot)
    {
        if (selectedSlot != null) selectedSlot.SetSelected(false);

        if (slot.item != null)
        {
            selectedItem = slot.item;
            selectedSlot = slot;
            selectedSlot.SetSelected(true);
            UpdateDetailPanel(slot.item);
        }
        else ClearSelection();
    }

    private void UpdateDetailPanel(ItemData item)
    {
        if (item != null)
        {
            detailPanelObject.SetActive(true);

            if (openSellPopupButton != null)
                openSellPopupButton.gameObject.SetActive(true);

            if (detailImage != null) detailImage.sprite = item.itemIcon;
            if (detailNameText != null) detailNameText.text = item.itemName;

            // ▼▼▼ [추가 2] 현재 보유 개수 가져와서 텍스트 갱신 ▼▼▼
            int myCount = 0;
            if (InventoryManager.Instance.items.ContainsKey(item))
            {
                myCount = InventoryManager.Instance.items[item];
            }

            if (detailQuantityText != null)
            {
                // 예: "5" 또는 "보유 수량: 5" 등 원하는 대로 수정 가능
                detailQuantityText.text = myCount.ToString();
            }
            // ▲▲▲ 추가 완료 ▲▲▲


            // 1. 도구(Tool)인지 확인
            if (item.itemCategory == "Tool")
            {
                if (detailPriceText != null)
                    detailPriceText.gameObject.SetActive(false);

                if (openSellPopupButton != null)
                {
                    openSellPopupButton.interactable = false;
                    if (sellButtonText != null) sellButtonText.text = "보유 중";
                }
            }
            else
            {
                if (detailPriceText != null)
                {
                    detailPriceText.gameObject.SetActive(true);
                    detailPriceText.text = item.price.ToString();
                }

                if (openSellPopupButton != null)
                {
                    openSellPopupButton.interactable = (myCount > 0);
                    if (sellButtonText != null) sellButtonText.text = "판매";
                }
            }
        }
    }

    public void OnOpenSellPopupClick()
    {
        if (selectedItem == null) return;
        if (selectedItem.itemCategory == "Tool") return;

        int ownedCount = 0;
        if (InventoryManager.Instance.items.ContainsKey(selectedItem))
            ownedCount = InventoryManager.Instance.items[selectedItem];

        if (ownedCount <= 0) return;

        sellPopupObject.SetActive(true);

        if (popupItemIcon != null) popupItemIcon.sprite = selectedItem.itemIcon;
        if (popupNameText != null) popupNameText.text = selectedItem.itemName;

        if (popupSlider != null)
        {
            popupSlider.minValue = 1;
            popupSlider.maxValue = ownedCount;
            popupSlider.value = 1;
            currentSellQuantity = 1;
        }

        UpdatePopupTexts();
    }

    public void OnSliderValueChanged(float value)
    {
        currentSellQuantity = (int)value;
        UpdatePopupTexts();
    }

    private void UpdatePopupTexts()
    {
        if (popupCountText != null)
            popupCountText.text = currentSellQuantity.ToString();

        if (selectedItem != null && popupPriceText != null)
        {
            int total = selectedItem.price * currentSellQuantity;
            popupPriceText.text = total.ToString() + " 포잉";
        }
    }

    public void OnRealSellClick()
    {
        if (selectedItem == null) return;

        int totalPrice = selectedItem.price * currentSellQuantity;

        InventoryManager.Instance.RemoveItem(selectedItem, currentSellQuantity);
        PoingManager.Instance.IncreasePoing(totalPrice);

        // 퀘스트 진행도 : 판매 
        if (QuestManager.Instance != null && selectedItem != null)
        {
            // 작물만 카운트
            if (selectedItem.itemCategory == "Crop")
            {
                QuestManager.Instance.NotifyAction(QuestConditionType.SellItem, selectedItem, currentSellQuantity);
            }
        }

        UIManager.Instance.ShowAlertPopup($"판매 완료! (+{totalPrice} 포잉)");
        SoundManager.Instance.PlaySFX("button");

        CloseSellPopup();
        RedrawInventory();

        if (InventoryManager.Instance.items.ContainsKey(selectedItem))
            UpdateDetailPanel(selectedItem);
        else
            ClearSelection();
    }

    public void CloseSellPopup()
    {
        if (sellPopupObject != null) sellPopupObject.SetActive(false);
        SoundManager.Instance.PlaySFX("button");
    }

    private void RedrawInventory()
    {
        Dictionary<ItemData, int> allItems = InventoryManager.Instance.items;
        int i = 0;

        foreach (KeyValuePair<ItemData, int> itemPair in allItems)
        {
            if (itemPair.Key.itemCategory == currentCategory)
            {
                if (i < slots.Count)
                {
                    slots[i].gameObject.SetActive(true);
                    slots[i].SetSlot(itemPair.Key, itemPair.Value);

                    if (selectedSlot == slots[i])
                    {
                        selectedSlot.SetSelected(true);
                    }
                    i++;
                }
            }
        }

        for (int j = i; j < slots.Count; j++)
        {
            slots[j].ClearSlot();
            slots[j].gameObject.SetActive(false);
        }

        if (i == 0) ClearSelection();
    }

    public void ClearSelection()
    {
        if (selectedSlot != null) selectedSlot.SetSelected(false);
        selectedItem = null;
        selectedSlot = null;
        if (detailPanelObject != null) detailPanelObject.SetActive(false);
        if (openSellPopupButton != null) openSellPopupButton.gameObject.SetActive(false);
    }
}
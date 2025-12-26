using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    public Button purchaseButton;

    [Header("Category Buttons")]
    public List<CategoryButton> categoryButtons;
    public CategoryButton defaultCategoryButton;

    [Header("Popups")]
    public GameObject confirmPopupObject; // 팝업창 전체 (패널)
    public TextMeshProUGUI confirmPopupText;

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
        foreach (CategoryButton btn in categoryButtons)
        {
            btn.SetSelected(false);
        }

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

            if (item.itemCategory == currentCategory)
            {
                // 1. 슬롯 오브젝트를 켠다 (보이게 함)
                slots[i].gameObject.SetActive(true);

                if (item.isDefaultUnlocked || GameProgressionManager.Instance.IsItemUnlocked(item))
                {
                    slots[i].SetSlot(item);
                }
                else
                {
                    slots[i].SetSlot(lockedSeedItem);
                }
                i++;
            }
        }

        if (currentCategory == "Seed")
        {
            if (i < slots.Count && randomSeedItem != null)
            {
                // 랜덤 씨앗 슬롯도 켠다
                slots[i].gameObject.SetActive(true);
                slots[i].SetSlot(randomSeedItem);
                i++;
            }
        }

        // 2. 남는 슬롯들은 모두 끈다 (숨김)
        for (int j = i; j < slots.Count; j++)
        {
            slots[j].ClearSlot();
            slots[j].gameObject.SetActive(false); // ★ 핵심: 아예 안 보이게 꺼버림
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

        if (selectedItem == lockedSeedItem)
        {
            UpdateDetailPanel(lockedSeedItem, false);
        }
        else if (selectedItem == randomSeedItem)
        {
            UpdateDetailPanel(randomSeedItem, true);
        }
        else
        {
            UpdateDetailPanel(selectedItem, true);
        }
    }

    private void UpdateDetailPanel(ItemData item, bool isUnlocked)
    {
        if (item != null)
        {
            detailPanelObject.SetActive(true);
            detailImage.sprite = item.itemIcon;
            detailImage.color = Color.white;

            detailNameText.text = item.itemName;
            detailPriceText.text = "" + item.price.ToString();

            purchaseButton.gameObject.SetActive(true);
            purchaseButton.interactable = isUnlocked;
        }
    }

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

    public void OnPurchaseButtonClick()
    {
        if (selectedItem == null) return;

        if (confirmPopupText != null)
        {
            confirmPopupText.text = selectedItem.price.ToString() + " 포잉으로 결제하시겠습니까?";
        }

        // 2. 팝업창 켜기
        if (confirmPopupObject != null)
        {
            confirmPopupObject.SetActive(true);
        }
        else
        {
            OnConfirmPurchase(); // 팝업 없으면 그냥 바로 구매
        }
    }

    // 팝업에서 '네' 클릭
    public void OnConfirmPurchase()
    {
        if (confirmPopupObject != null) confirmPopupObject.SetActive(false);
        if (selectedItem == null) return;

        if (selectedItem == randomSeedItem)
        {
            if (PoingManager.Instance.HasEnoughPoing(randomSeedItem.price))
            {
                PoingManager.Instance.DecreasePoing(randomSeedItem.price);
                InventoryManager.Instance.AddItem(randomSeedItem, 1);
                UIManager.Instance.ShowAlertPopup("랜덤 씨앗 구매 완료!");
            }
            else
            {
                UIManager.Instance.ShowAlertPopup("포잉이 부족합니다!");
            }
        }
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
                UIManager.Instance.ShowAlertPopup("포잉이 부족합니다!");
            }
        }
    }

    // 팝업에서 '아니오' 클릭
    public void OnCancelPurchase()
    {
        if (confirmPopupObject != null) confirmPopupObject.SetActive(false);
    }
}
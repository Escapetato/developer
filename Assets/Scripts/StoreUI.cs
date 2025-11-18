using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // [필수] 네임스페이스 추가

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

    // [변경] 기존 Text -> TextMeshProUGUI 로 변경
    public TextMeshProUGUI detailNameText;
    public TextMeshProUGUI detailPriceText;

    public Button purchaseButton;

    [Header("Category Buttons")]
    public List<CategoryButton> categoryButtons;
    public CategoryButton defaultCategoryButton;

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

        for (int j = 0; j < slots.Count; j++)
        {
            slots[j].ClearSlot();
        }

        if (storeDB == null) return;

        foreach (ItemData item in storeDB.itemsForSale)
        {
            if (i >= slots.Count) break;

            if (item.itemCategory == currentCategory)
            {
                if (GameProgressionManager.Instance.IsItemUnlocked(item))
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
                slots[i].SetSlot(randomSeedItem);
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

            // [로직은 동일] .text 속성 사용법은 TextMeshPro도 같습니다.
            detailNameText.text = item.itemName;
            detailPriceText.text = "P : " + item.price.ToString();

            purchaseButton.gameObject.SetActive(true);
            purchaseButton.interactable = isUnlocked;
        }
    }

    public void OnPurchaseButtonClick()
    {
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
                UIManager.Instance.ShowAlertPopup("포잉이 부족합니다.");
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
                UIManager.Instance.ShowAlertPopup("포잉이 부족합니다.");
            }
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
}